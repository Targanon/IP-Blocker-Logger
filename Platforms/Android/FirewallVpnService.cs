#if ANDROID
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Java.IO;
using Android.Runtime;
using IP_Blocker_Logger.Services;

namespace IPBlockerLogger;

[Service(Name = "com.example.ipblocker.FirewallVpnService", 
         Exported = true, 
         Permission = "android.permission.BIND_VPN_SERVICE",
         ForegroundServiceType = Android.Content.PM.ForegroundService.TypeConnectedDevice)]
[Preserve(AllMembers = true)]
public class FirewallVpnService : VpnService
{
    const string NotificationChannelId = "firewall_channel";
    const int NotificationId = 1001;

    static volatile bool _running;
    static volatile bool _rulesChanged;
    static HashSet<string> _blocked = new(StringComparer.OrdinalIgnoreCase);

    Thread? _worker;
    ParcelFileDescriptor? _tunFd;

    public static bool IsRunning => _running;
    public static void SignalRulesChanged() => _rulesChanged = true;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
        System.Diagnostics.Debug.WriteLine("FirewallVpnService: OnCreate called");
    }

    public override Android.App.StartCommandResult OnStartCommand(
        Android.Content.Intent? intent,
        Android.App.StartCommandFlags flags,
        int startId)
    {
        System.Diagnostics.Debug.WriteLine("FirewallVpnService: OnStartCommand called");
        
        if (!_running)
        {
            try
            {
                StartForeground(NotificationId, BuildStatusNotification("Starting..."));
                System.Diagnostics.Debug.WriteLine("FirewallVpnService: Started foreground");
                
                LoadRules();
                StartVpn();
                System.Diagnostics.Debug.WriteLine("FirewallVpnService: VPN started successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error in OnStartCommand: {ex}");
                StopSelf();
                return Android.App.StartCommandResult.NotSticky;
            }
        }
        return Android.App.StartCommandResult.Sticky;
    }

    static void LoadRules()
    {
        try
        {
            _blocked = SharedDataService.GetBlockedIps();
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Loaded {_blocked.Count} blocked IPs");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error loading rules: {ex}");
            _blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    void StartVpn()
    {
        try
        {
            var builder = new VpnService.Builder(this);
            builder.SetSession("IP Blocker VPN");
            builder.AddAddress("10.123.0.1", 32);
            builder.AddDnsServer("1.1.1.1");
            builder.AddRoute("0.0.0.0", 0);
            
            try
            {
                builder.AddRoute("::", 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FirewallVpnService: IPv6 route failed: {ex.Message}");
            }

            _tunFd = builder.Establish();
            if (_tunFd == null)
            {
                System.Diagnostics.Debug.WriteLine("FirewallVpnService: Failed to establish VPN interface");
                StopSelf();
                return;
            }

            _running = true;
            _worker = new Thread(new ParameterizedThreadStart(PacketLoop)) { IsBackground = true, Name = "FirewallVpnWorker" };
            _worker.Start(null);
            
            UpdateNotification();
            System.Diagnostics.Debug.WriteLine("FirewallVpnService: VPN interface established successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error starting VPN: {ex}");
            StopSelf();
        }
    }

    void PacketLoop(object? obj)
    {
        System.Diagnostics.Debug.WriteLine("FirewallVpnService: Packet loop started");
        
        try
        {
            var buffer = new byte[4096];

            if (_tunFd?.FileDescriptor == null) return;

            using var input = new FileInputStream(_tunFd.FileDescriptor);
            using var output = new FileOutputStream(_tunFd.FileDescriptor);

            while (_running && _tunFd != null)
            {
                try
                {
                    if (_rulesChanged)
                    {
                        LoadRules();
                        _rulesChanged = false;
                        UpdateNotification();
                    }

                    int len = input.Read(buffer);
                    if (len <= 0) 
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    if (len >= 20)
                    {
                        byte version = (byte)(buffer[0] >> 4);
                        if (version == 4)
                        {
                            string srcIp = new IPAddress(new ReadOnlySpan<byte>(buffer, 12, 4)).ToString();
                            string dstIp = new IPAddress(new ReadOnlySpan<byte>(buffer, 16, 4)).ToString();
                            bool blocked = _blocked.Contains(dstIp) || _blocked.Contains(srcIp);
                            
                            var logEntry = new FirewallLogEntry(DateTime.UtcNow, "?", srcIp, dstIp, blocked, "IPv4", null, null, null);
                            SharedDataService.LogFirewallEntry(logEntry);
                            
                            if (blocked)
                            {
                                continue; // Drop the packet
                            }
                        }
                    }

                    output.Write(buffer, 0, len);
                }
                catch (System.IO.IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FirewallVpnService: IO Exception: {ex.Message}");
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Packet loop error: {ex}");
                    Thread.Sleep(100);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Fatal packet loop error: {ex}");
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("FirewallVpnService: Packet loop ended");
        }
    }

    void UpdateNotification()
    {
        try
        {
            var n = BuildStatusNotification($"Running. Blocked: {_blocked.Count}");
            var mgr = (NotificationManager?)GetSystemService(Context.NotificationService);
            mgr?.Notify(NotificationId, n);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error updating notification: {ex}");
        }
    }

    Notification BuildStatusNotification(string text)
    {
        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            pendingIntentFlags |= PendingIntentFlags.Immutable;

        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            new Intent(this, typeof(IPBlockerLogger.Platforms.Android.MainActivity)),
            pendingIntentFlags);

        var builder = new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("IP Blocker Active")
            .SetContentText(text)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true);
            
        if (pendingIntent != null)
            builder.SetContentIntent(pendingIntent);
            
        return builder.Build();
    }

    void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        try
        {
            var mgr = (NotificationManager?)GetSystemService(Context.NotificationService);
            if (mgr != null)
            {
                var ch = new NotificationChannel(NotificationChannelId, "Firewall", NotificationImportance.Low)
                {
                    Description = "Firewall status and alerts"
                };
                mgr.CreateNotificationChannel(ch);
                System.Diagnostics.Debug.WriteLine("FirewallVpnService: Notification channel created");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error creating notification channel: {ex}");
        }
    }

    public override void OnDestroy()
    {
        System.Diagnostics.Debug.WriteLine("FirewallVpnService: OnDestroy called");
        
        base.OnDestroy();
        _running = false;
        
        try 
        { 
            _worker?.Join(1000);
            _tunFd?.Close(); 
        } 
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FirewallVpnService: Error in OnDestroy: {ex}");
        }
        
        _tunFd = null;
        System.Diagnostics.Debug.WriteLine("FirewallVpnService: Service destroyed");
    }
}
#else
namespace IPBlockerLogger
{
    public class FirewallVpnService
    {
        public static bool IsRunning => false;
        public static void SignalRulesChanged() { }
    }
}
#endif