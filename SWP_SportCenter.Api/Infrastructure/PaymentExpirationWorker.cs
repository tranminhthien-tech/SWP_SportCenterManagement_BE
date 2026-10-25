using SWP_SportCenter.Service.Payment;
namespace SWP_SportCenter.Infrastructure;

// FIX B39: scope mới mỗi lượt, retry lần sau nếu DB xung đột; không chạy khi server ngủ.
public sealed class PaymentExpirationWorker(IServiceScopeFactory scopes, ILogger<PaymentExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Yield();
        while (!ct.IsCancellationRequested) {
            try {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<PaymentExpirationService>().ExpireAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Pending payment expiration failed."); }
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }
}
