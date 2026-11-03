using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SWP_SportCenter.Service.Ai;

// FIX B48 02/11: generateContent cho Gemini 3.8, không đổi provider/model hoặc đưa key vào URL.
public sealed class GeminiClient(HttpClient client, IOptionsSnapshot<AiOptions> options,
    ILogger<GeminiClient> logger, IHttpContextAccessor http) : IAiClient
{
    // Giới hạn trong một process; không thay thế quota theo project/giữa nhiều replica.
    private static readonly SemaphoreSlim InFlight = new(2, 2);
    public static Dictionary<string, object> GenerationConfig(AiOptions c)
    {
        var config = new Dictionary<string, object> { ["maxOutputTokens"] = c.MaxOutputTokens };
        // Không gửi thinkingLevel cho Gemini 2.x; Gemini 3.8 hỗ trợ LOW, không hỗ trợ MINIMAL.
        if (Regex.IsMatch(c.Model, "^gemini-3[.-]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            config["thinkingConfig"] = new { thinkingLevel = c.ThinkingLevel.ToUpperInvariant(), includeThoughts = false };
        // Không temperature/topP/topK: các tham số sampling cũ không phù hợp Gemini 3.8.
        return config;
    }

    public async Task<string> GenerateAsync(string system, IReadOnlyList<ChatMessage> messages, CancellationToken ct)
    {
        var c = options.Value; c.RequireConfigured();
        var timer = Stopwatch.StartNew();
        var traceId = http.HttpContext?.TraceIdentifier ?? Activity.Current?.Id ?? "background";
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        // CODE CŨ: budget.CancelAfter(TimeSpan.FromSeconds(c.TotalTimeoutSeconds));
        budget.CancelAfter(TimeSpan.FromSeconds(c.EffectiveTotalTimeoutSeconds)); // FIX: env cũ 45/90 không vượt deadline FE.
        bool entered = false;
        try {
            await InFlight.WaitAsync(budget.Token); entered = true;
            logger.LogInformation("Gemini queue-ready traceId {TraceId} queueMs {QueueMs}", traceId, timer.ElapsedMilliseconds); // FIX
            for (var attempt = 1; attempt <= c.MaxRetries + 1; attempt++) {
                var attemptTimer = Stopwatch.StartNew();
                var phase = "headers"; // FIX: phân biệt chờ response với chờ body.
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(budget.Token);
                attemptCts.CancelAfter(TimeSpan.FromSeconds(c.AttemptTimeoutSeconds));
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://generativelanguage.googleapis.com/v1beta/models/" + Uri.EscapeDataString(c.Model) + ":generateContent");
                req.Headers.Add("x-goog-api-key", c.ApiKey);
                req.Content = JsonContent.Create(new {
                    systemInstruction = new { parts = new[] { new { text = system } } },
                    contents = messages.Select(m => new { role = m.Role == "assistant" ? "model" : "user", parts = new[] { new { text = m.Content } } }),
                    generationConfig = GenerationConfig(c)
                });
                try {
                    using var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token);
                    phase = "body";
                    logger.LogInformation("Gemini response traceId {TraceId} model {Model} attempt {Attempt} httpStatus {HttpStatus} elapsedMs {ElapsedMs}",
                        traceId, c.Model, attempt, (int)response.StatusCode, timer.ElapsedMilliseconds);
                    if (!response.IsSuccessStatusCode) {
                        var body = await ReadBoundedAsync(response.Content, 32768, attemptCts.Token);
                        var failure = GeminiFailure.Read((int)response.StatusCode, body);
                        // Chỉ log dữ liệu allowlist; tuyệt đối không body/message Google, prompt, key hay exception thô.
                        logger.LogWarning("Gemini failure traceId {TraceId} model {Model} attempt {Attempt} httpStatus {HttpStatus} providerStatus {ProviderStatus} category {Category} elapsedMs {ElapsedMs}",
                            traceId, c.Model, attempt, failure.HttpStatus, failure.ProviderStatus, failure.Category, timer.ElapsedMilliseconds);
                        var headerDelay = response.Headers.RetryAfter?.Delta ??
                            (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : (TimeSpan?)null);
                        var retryAfter = MaxDelay(headerDelay, failure.RetryAfter);
                        var retryable = failure.Transient || (failure.HttpStatus == 429 && failure.Category != "DailyQuota" && headerDelay.HasValue);
                        var canRetry = attempt <= c.MaxRetries && retryable && !budget.IsCancellationRequested &&
                            (retryAfter == null || retryAfter <= TimeSpan.FromSeconds(2));
                        var backoff = TimeSpan.FromMilliseconds(600 * Math.Pow(2, attempt - 1) + Random.Shared.Next(50, 200));
                        var delay = retryAfter > backoff ? retryAfter.Value : backoff;
                        // CODE CŨ: so với TotalTimeoutSeconds có thể là 45/90 từ env.
                        if (canRetry && timer.Elapsed + delay + TimeSpan.FromSeconds(3) < TimeSpan.FromSeconds(c.EffectiveTotalTimeoutSeconds)) {
                            logger.LogInformation("Gemini retry traceId {TraceId} nextAttempt {Attempt} delayMs {DelayMs}", traceId, attempt + 1, delay.TotalMilliseconds);
                            // Sequential, exponential backoff + jitter, không gọi chồng. Không bỏ qua Retry-After dài.
                            await Task.Delay(delay, budget.Token); continue;
                        }
                        throw failure.ToException();
                    }
                    var successBody = await ReadBoundedAsync(response.Content, 262144, attemptCts.Token);
                    var reply = ExtractReply(successBody);
                    budget.Token.ThrowIfCancellationRequested();
                    logger.LogInformation("Gemini success traceId {TraceId} model {Model} attempt {Attempt} httpStatus {HttpStatus} elapsedMs {ElapsedMs}",
                        traceId, c.Model, attempt, (int)response.StatusCode, timer.ElapsedMilliseconds);
                    return reply;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
                    // Không retry timeout mù: provider có thể vẫn đang tính và tính phí lần đầu.
                    // CODE CŨ: chỉ elapsedMs tổng, không biết lần gọi nào đang chờ headers/body.
                    logger.LogWarning("Gemini timeout traceId {TraceId} model {Model} attempt {Attempt} phase {Phase} attemptMs {AttemptMs} totalBudgetExpired {Expired} elapsedMs {ElapsedMs}",
                        traceId, c.Model, attempt, phase, attemptTimer.ElapsedMilliseconds, budget.IsCancellationRequested, timer.ElapsedMilliseconds);
                    // CODE CŨ: chỉ có message, cùng 503 như quota ngày.
                    throw new AiUnavailableException("AI phản hồi quá lâu. Vui lòng thử lại sau, không gửi nhiều yêu cầu cùng lúc.", "AI_TIMEOUT"); // FIX
                }
                catch (JsonException) {
                    logger.LogWarning("Gemini malformed-response traceId {TraceId} model {Model} attempt {Attempt}", traceId, c.Model, attempt);
                    throw new AiProviderException("Dịch vụ AI trả về dữ liệu không hợp lệ.");
                }
                catch (HttpRequestException) {
                    // Không log exception gốc có thể chứa dữ liệu request; không replay kết nối không rõ kết quả.
                    logger.LogWarning("Gemini transport-error traceId {TraceId} model {Model} attempt {Attempt} elapsedMs {ElapsedMs}",
                        traceId, c.Model, attempt, timer.ElapsedMilliseconds);
                    throw new AiProviderException("Không kết nối được dịch vụ AI. Vui lòng thử lại sau.");
                }
            }
            throw new AiUnavailableException("Gemini chưa sẵn sàng. Vui lòng thử lại sau.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            logger.LogInformation("Gemini caller-cancelled traceId {TraceId} model {Model} elapsedMs {ElapsedMs}", traceId, c.Model, timer.ElapsedMilliseconds);
            throw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
            logger.LogWarning("Gemini budget-timeout traceId {TraceId} model {Model} elapsedMs {ElapsedMs}", traceId, c.Model, timer.ElapsedMilliseconds);
            // CODE CŨ: không có mã riêng cho timeout queue/budget.
            throw new AiUnavailableException("AI đang bận hoặc phản hồi quá lâu. Vui lòng thử lại sau.", "AI_TIMEOUT"); // FIX
        }
        finally { if (entered) InFlight.Release(); }
    }

    private static TimeSpan? MaxDelay(TimeSpan? a, TimeSpan? b) => a == null ? b : b == null ? a : a > b ? a : b;
    private static async Task<string> ReadBoundedAsync(HttpContent content, int maxBytes, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream(); var chunk = new byte[4096];
        while (true) {
            var read = await stream.ReadAsync(chunk, ct); if (read == 0) break;
            if (buffer.Length + read > maxBytes) throw new AiProviderException("Dịch vụ AI trả về dữ liệu vượt giới hạn.");
            buffer.Write(chunk, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
    private static string ExtractReply(string body)
    {
        using var json = JsonDocument.Parse(body); var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException();
        if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.ValueKind == JsonValueKind.Object && feedback.TryGetProperty("blockReason", out _))
            return "Không thể đưa ra gợi ý an toàn cho yêu cầu này. Hãy hỏi huấn luyện viên hoặc chuyên gia phù hợp.";
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            throw new JsonException();
        var candidate = candidates[0]; if (candidate.ValueKind != JsonValueKind.Object) throw new JsonException();
        var finish = candidate.TryGetProperty("finishReason", out var reason) && reason.ValueKind == JsonValueKind.String ? reason.GetString() : null;
        if (finish is "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT")
            return "Không thể đưa ra gợi ý an toàn cho yêu cầu này. Hãy hỏi huấn luyện viên hoặc chuyên gia phù hợp.";
        if (finish == "MAX_TOKENS") throw new AiUnavailableException("Gemini chưa hoàn tất câu trả lời trong giới hạn output. Hãy rút ngắn yêu cầu hoặc liên hệ quản trị viên.");
        if (!candidate.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Object ||
            !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array) throw new JsonException();
        var text = string.Join("\n", parts.EnumerateArray().Where(p => p.ValueKind == JsonValueKind.Object &&
            (!p.TryGetProperty("thought", out var thought) || thought.ValueKind != JsonValueKind.True) &&
            p.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String).Select(p => p.GetProperty("text").GetString()));
        // Không trả 200 câu mẫu để giả inference thành công khi output rỗng/chỉ có thought.
        if (string.IsNullOrWhiteSpace(text)) throw new AiProviderException("Gemini chưa trả về nội dung trả lời. Vui lòng thử lại sau.");
        return text[..Math.Min(text.Length, 8000)];
    }
}

// FIX: phân loại tối thiểu từ JSON lỗi, không giữ/lộ message thô hoặc quota identifier/project.
public sealed record GeminiFailure(int HttpStatus, string ProviderStatus, string Category, bool Transient, TimeSpan? RetryAfter)
{
    public static GeminiFailure Read(int httpStatus, string body)
    {
        string status = "UNKNOWN"; bool daily = false; TimeSpan? retry = null;
        try {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object) {
                if (error.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String &&
                    new[] { "RESOURCE_EXHAUSTED", "UNAVAILABLE", "DEADLINE_EXCEEDED", "INVALID_ARGUMENT", "PERMISSION_DENIED", "NOT_FOUND", "UNAUTHENTICATED", "INTERNAL", "FAILED_PRECONDITION", "UNKNOWN" }.Contains(s.GetString())) status = s.GetString()!;
                if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                    foreach (var detail in details.EnumerateArray()) {
                        if (detail.ValueKind != JsonValueKind.Object) continue;
                        if (detail.TryGetProperty("retryDelay", out var d) && d.ValueKind == JsonValueKind.String && d.GetString() is { } raw && raw.EndsWith('s') &&
                            decimal.TryParse(raw[..^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0 && seconds <= 86400)
                            retry = TimeSpan.FromSeconds((double)seconds);
                        if (detail.TryGetProperty("violations", out var violations) && violations.ValueKind == JsonValueKind.Array)
                            foreach (var v in violations.EnumerateArray())
                                if (v.ValueKind == JsonValueKind.Object && v.TryGetProperty("quotaId", out var id) && id.ValueKind == JsonValueKind.String &&
                                    id.GetString() is { } quota && (quota.Contains("PerDay", StringComparison.OrdinalIgnoreCase) || quota.Contains("per_day", StringComparison.OrdinalIgnoreCase))) daily = true;
                    }
            }
        } catch (JsonException) { /* HTML/non-JSON lỗi chỉ phân loại bằng HTTP status, không log body. */ }
        var category = daily ? "DailyQuota" : httpStatus == 429 ? "RateLimit" : httpStatus is 400 or 401 or 402 or 403 or 404 ? "ConfigurationOrPermission" :
            httpStatus is 408 or 500 or 502 or 503 or 504 ? "ProviderUnavailable" : "UnexpectedHttp";
        // 429 thiếu RetryInfo không đủ biết quota ngày hay phút: không retry mù.
        bool transient = !daily && (httpStatus is 408 or 500 or 502 or 503 or 504 || (httpStatus == 429 && retry.HasValue));
        return new(httpStatus, status, category, transient, retry);
    }
    /* CODE CŨ: lỗi quota/provider chỉ giữ thông báo, mất HTTP status và loại lỗi để FE phân biệt.
    public Exception ToException() => HttpStatus == 429
        ? new AiUnavailableException(Category == "DailyQuota" ? "Gemini đã chạm hạn mức ngày của project. Quản trị viên cần kiểm tra quota." : "Gemini đang giới hạn tần suất hoặc hạn mức. Vui lòng thử lại sau.")
        : Category == "ConfigurationOrPermission" ? new AiUnavailableException("Gemini chưa sẵn sàng. Quản trị viên cần kiểm tra key, model, billing và quyền sử dụng ở backend.")
        : Category == "ProviderUnavailable" ? new AiUnavailableException("Dịch vụ Gemini tạm thời chưa sẵn sàng. Vui lòng thử lại sau.")
        : new AiProviderException("Dịch vụ AI trả về lỗi không hợp lệ. Vui lòng thử lại sau.");
    */
    // FIX: giữ 503 contract hiện tại; thêm mã ổn định và metadata allowlist, không lộ raw body/key.
    public Exception ToException() => HttpStatus == 429
        ? new AiUnavailableException(Category == "DailyQuota" ? "Gemini đã chạm hạn mức ngày của project. Quản trị viên cần kiểm tra quota." : "Gemini đang giới hạn tần suất hoặc hạn mức. Vui lòng thử lại sau.",
            Category == "DailyQuota" ? "AI_DAILY_QUOTA" : "AI_RATE_LIMIT", HttpStatus, ProviderStatus)
        : Category == "ConfigurationOrPermission"
            ? new AiUnavailableException("Gemini chưa sẵn sàng. Quản trị viên cần kiểm tra key, model, billing và quyền sử dụng ở backend.", "AI_PROVIDER_CONFIGURATION", HttpStatus, ProviderStatus)
        : Category == "ProviderUnavailable"
            ? new AiUnavailableException("Dịch vụ Gemini tạm thời chưa sẵn sàng. Vui lòng thử lại sau.", "AI_PROVIDER_UNAVAILABLE", HttpStatus, ProviderStatus)
        : new AiProviderException("Dịch vụ AI trả về lỗi không hợp lệ. Vui lòng thử lại sau.");
}
