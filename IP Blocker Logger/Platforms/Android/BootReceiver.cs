#if ANDROID
using Android.Content;
using Android.App;
using Android.Runtime;

namespace IPBlockerLogger;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
[Preserve(AllMembers = true)]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == Intent.ActionBootCompleted && context is Context ctx)
        {
            // Optional: add logging for verification
            // Android.Util.Log.Info("BootReceiver", "BOOT_COMPLETED received, starting FirewallVpnService.");

            ctx.StartService(new Intent(ctx, typeof(FirewallVpnService)));
        }
    }
}
#endif
