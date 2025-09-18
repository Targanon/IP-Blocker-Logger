using Android.Content;
using Android.App;

namespace IP_Blocker_Logger;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
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
