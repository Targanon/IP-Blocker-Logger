namespace IP_Blocker_Logger.Services;

public interface IFirewallController
{
    Task<bool> StartAsync();
    Task StopAsync();
    Task<bool> IsRunningAsync();
    Task RefreshRulesAsync();
}

public class FirewallController : IFirewallController
{
    public Task<bool> StartAsync()
    {
#if ANDROID
        Platform.CurrentActivity?.StartService(new Android.Content.Intent(Platform.CurrentActivity, typeof(FirewallVpnService)));
        return Task.FromResult(true);
#else
        return Task.FromResult(false);
#endif
    }

    public Task StopAsync()
    {
#if ANDROID
        Platform.CurrentActivity?.StopService(new Android.Content.Intent(Platform.CurrentActivity, typeof(FirewallVpnService)));
#endif
        return Task.CompletedTask;
    }

    public Task<bool> IsRunningAsync()
    {
#if ANDROID
        return Task.FromResult(FirewallVpnService.IsRunning);
#else
        return Task.FromResult(false);
#endif
    }

    public Task RefreshRulesAsync()
    {
#if ANDROID
        FirewallVpnService.SignalRulesChanged();
#endif
        return Task.CompletedTask;
    }
}
