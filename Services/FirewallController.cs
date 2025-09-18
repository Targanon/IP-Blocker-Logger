using System.Threading.Tasks;
using System;
#if ANDROID
using Android.Content;
using Microsoft.Maui.ApplicationModel;
using Android.App;
#endif

namespace IP_Blocker_Logger.Services;

public class FirewallController : IFirewallController
{
    public async Task<bool> CheckVpnPermissionAsync()
    {
#if ANDROID
        try
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("FirewallController: No context available");
                return false;
            }
            
            var prepareIntent = Android.Net.VpnService.Prepare(context);
            bool hasPermission = prepareIntent == null; // null means permission is already granted
            
            System.Diagnostics.Debug.WriteLine($"FirewallController: VPN permission check: {hasPermission}");
            return hasPermission;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallController: Failed to check VPN permission: {ex.Message}");
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> StartAsync()
    {
#if ANDROID
        try
        {
            System.Diagnostics.Debug.WriteLine("FirewallController: StartAsync called");
            
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("FirewallController: No context available for starting service");
                return false;
            }
            
            // First check if VPN permission is needed
            var prepareIntent = Android.Net.VpnService.Prepare(context);
            
            if (prepareIntent != null)
            {
                System.Diagnostics.Debug.WriteLine("FirewallController: VPN permission required, launching permission request");
                
                // VPN permission is required - launch the permission request
                if (context is Activity activity)
                {
                    activity.StartActivityForResult(prepareIntent, 1001);
                    System.Diagnostics.Debug.WriteLine("FirewallController: Started VPN permission request from activity");
                }
                else
                {
                    // If we don't have an activity context, try with application context
                    prepareIntent.SetFlags(ActivityFlags.NewTask);
                    context.StartActivity(prepareIntent);
                    System.Diagnostics.Debug.WriteLine("FirewallController: Started VPN permission request from application context");
                }
                return false; // Will return false until permission is granted
            }
            
            // Permission is already granted, start the service
            System.Diagnostics.Debug.WriteLine("FirewallController: VPN permission already granted, starting service");
            
            var intent = new Intent(context, typeof(IPBlockerLogger.FirewallVpnService));
            var result = context.StartForegroundService(intent); // Use StartForegroundService for Android 8+
            
            System.Diagnostics.Debug.WriteLine($"FirewallController: StartForegroundService result: {result != null}");
            
            // Give the service a moment to start
            await Task.Delay(1000);
            
            bool isRunning = IPBlockerLogger.FirewallVpnService.IsRunning;
            System.Diagnostics.Debug.WriteLine($"FirewallController: Service running status: {isRunning}");
            
            return isRunning;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallController: Failed to start VPN service: {ex}");
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task StopAsync()
    {
#if ANDROID
        try
        {
            System.Diagnostics.Debug.WriteLine("FirewallController: StopAsync called");
            
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("FirewallController: No context available for stopping service");
                return;
            }
            
            var intent = new Intent(context, typeof(IPBlockerLogger.FirewallVpnService));
            bool stopped = context.StopService(intent);
            
            System.Diagnostics.Debug.WriteLine($"FirewallController: StopService result: {stopped}");
            
            // Give the service a moment to stop
            await Task.Delay(500);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallController: Failed to stop VPN service: {ex}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    public async Task<bool> IsRunningAsync()
    {
#if ANDROID
        bool running = IPBlockerLogger.FirewallVpnService.IsRunning;
        System.Diagnostics.Debug.WriteLine($"FirewallController: IsRunningAsync: {running}");
        return await Task.FromResult(running);
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task RefreshRulesAsync()
    {
#if ANDROID
        try
        {
            System.Diagnostics.Debug.WriteLine("FirewallController: RefreshRulesAsync called");
            IPBlockerLogger.FirewallVpnService.SignalRulesChanged();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallController: Failed to refresh rules: {ex}");
        }
#endif
        await Task.CompletedTask;
    }
}