#if ANDROID
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using IP_Blocker_Logger.Services;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Java.IO;
using Microsoft.Maui;

namespace IP_Blocker_Logger;

[Service(Name = "com.example.ipblocker.FirewallVpnService", Exported = true, Permission = "android.permission.BIND_VPN_SERVICE")]
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
    }

    // Fully-qualified types to ensure exact signature match with base Service.OnStartCommand
    public override Android.App.StartCommandResult OnStartCommand(
        Android.Content.Intent? intent,
        Android.App.StartCommandFlags flags,
        int startId)
    {
        if (!_running)
        {
            StartForeground(NotificationId, BuildStatusNotification("Starting..."));
            LoadRules();
            StartVpn();
        }
        return Android.App.StartCommandResult.Sticky;
    }

    void LoadRules()
    {
        var services = IPlatformApplication.Current?.Services;
        var repo = services?.GetService<BlockedIpRepository>();
        if (repo != null)
            _blocked = repo.EnabledSet().ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    void StartVpn()
    {
        var builder = new VpnService.Builder(this);
        builder.SetSession("IP Blocker VPN");
        builder.AddAddress("10.123.0.1", 32);
        builder.AddDnsServer("1.1.1.1");
        builder.AddRoute("0.0.0.0", 0);
        builder.AddRoute("::", 0);

        _tunFd = builder.Establish();
        if (_tunFd == null)
        {
            StopSelf();
            return;
        }

        _running = true;
        _worker = new Thread(PacketLoop) { IsBackground = true, Name = "FirewallVpnWorker" };
        _worker.Start();
    }

    void PacketLoop()
    {
        var services = IPlatformApplication.Current?.Services;
        var logRepo = services?.GetService<FirewallLogRepository>();
        var buffer = new byte[4096];

        using var input = new FileInputStream(_tunFd!.FileDescriptor);
        using var output = new FileOutputStream(_tunFd!.FileDescriptor);

        while (_running)
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
                if (len <= 0) continue;

                if (len >= 20)
                {
                    byte version = (byte)(buffer[0] >> 4);
                    if (version == 4)
                    {
                        string srcIp = new IPAddress(new ReadOnlySpan<byte>(buffer, 12, 4)).ToString();
                        string dstIp = new IPAddress(new ReadOnlySpan<byte>(buffer, 16, 4)).ToString();
                        bool blocked = _blocked.Contains(dstIp) || _blocked.Contains(srcIp);
                        if (blocked)
                        {
                            logRepo?.Add(new FirewallLogEntry(DateTime.UtcNow, "?", srcIp, dstIp, true, "IPv4", null, null, null));
                            SendAttemptNotification(dstIp);
                            continue;
                        }
                        else
                        {
                            logRepo?.Add(new FirewallLogEntry(DateTime.UtcNow, "?", srcIp, dstIp, false, "IPv4", null, null, null));
                        }
                    }
                }

                output.Write(buffer, 0, len);
            }
            catch (System.IO.IOException)
            {
                Thread.Sleep(50);
            }
            catch
            {
            }
        }
    }

    void UpdateNotification()
    {
        var n = BuildStatusNotification($"Running. Blocked: {_blocked.Count}");
        var mgr = (NotificationManager?)GetSystemService(Context.NotificationService);
        mgr?.Notify(NotificationId, n);
    }

    Notification BuildStatusNotification(string text)
    {
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        return new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("IP Blocker Active")
            .SetContentText(text)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .Build();
    }

    void SendAttemptNotification(string ip)
    {
        var mgr = (NotificationManager?)GetSystemService(Context.NotificationService);
        var n = new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("Blocked IP")
            .SetContentText(ip)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
            .SetAutoCancel(true)
            .Build();
        mgr?.Notify((int)SystemClock.UptimeMillis(), n);
    }

    void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        var mgr = (NotificationManager?)GetSystemService(Context.NotificationService);
        var ch = new NotificationChannel(NotificationChannelId, "Firewall", NotificationImportance.Low)
        {
            Description = "Firewall status and alerts"
        };
        mgr?.CreateNotificationChannel(ch);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _running = false;
        try { _tunFd?.Close(); } catch { }
        _tunFd = null;
    }
}
#else
namespace IP_Blocker_Logger
{
    public class FirewallVpnService
    {
        public static bool IsRunning => false;
        public static void SignalRulesChanged() { }
    }
}
#endif
