namespace SWP_SportCenter.Infrastructure;
public sealed class ClassLifecycleWorker(IServiceScopeFactory scopes, ILogger<ClassLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try { while (await timer.WaitForNextTickAsync(stoppingToken)) {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<SWP_SportCenter.Service.Classes.ClassLifecycleService>().RefreshAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Không thể cập nhật trạng thái lớp theo lịch."); }
        }} catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
