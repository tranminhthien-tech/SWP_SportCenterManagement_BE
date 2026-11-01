using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
namespace SWP_SportCenter.Infrastructure;

// FIX B48: lỗi quota local có JSON ổn định giống lỗi service, không trả body rỗng.
public static class AiRateLimitResponse
{
    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken ct)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = 429;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var problem = new ProblemDetails { Status = 429, Title = "Bạn gọi AI quá nhiều lần. Vui lòng thử lại sau.", Detail = "Giới hạn 10 yêu cầu mỗi phút cho mỗi tài khoản." };
        problem.Extensions["message"] = problem.Title;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        await response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: ct);
    }
}
