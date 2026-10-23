using SWP_SportCenter.Service.Notification;

namespace SWP_SportCenter.Infrastructure;

// FIX B32: chạy lúc khởi động và mỗi ngày; scope riêng, lỗi không làm chết API.
// Render ngủ/tắt thì job không chạy; cần dịch vụ always-on hoặc scheduler ngoài nếu cần giờ cố định.
public sealed class MembershipExpiryWorker(IServiceScopeFactory scopes, ILogger<MembershipExpiryWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Không giữ startup chờ truy vấn DB; startup migration chạy trước khi host khởi động worker.
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<MembershipExpiryNotifier>().RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Membership expiry notification failed; retry on next cycle.");
                // Cả unique-conflict khi nhiều instance đều rollback; scope mới retry sau 5 phút.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
