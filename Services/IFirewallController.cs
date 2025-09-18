using System.Threading.Tasks;

namespace IP_Blocker_Logger.Services;

public interface IFirewallController
{
    Task<bool> StartAsync();
    Task StopAsync();
    Task<bool> IsRunningAsync();
    Task RefreshRulesAsync();
    Task<bool> CheckVpnPermissionAsync();
}