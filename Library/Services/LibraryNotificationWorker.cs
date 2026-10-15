namespace S379658_HIT339_Assignment2_Library.Services;
// Periodically checks due dates and expired holds. Uses a new scoped DbContext per run.
public sealed class LibraryNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LibraryNotificationWorker> _logger;
    public LibraryNotificationWorker(IServiceScopeFactory scopes, ILogger<LibraryNotificationWorker> logger)
    { _scopes = scopes; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await RunSafelyAsync(ct);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(ct)) await RunSafelyAsync(ct);
    }
    private async Task RunSafelyAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<NotificationSweepService>().RunAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError(ex, "Notification simulation scan failed"); }
    }
}
