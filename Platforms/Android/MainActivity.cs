using Android.App;
using Android.Content.PM;
using Android.OS;

namespace IP_Blocker_Logger
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int VpnRequestCode = 9876;

        protected override void OnResume()
        {
            base.OnResume();
            RequestVpnPermissionIfNeeded();
        }

        void RequestVpnPermissionIfNeeded()
        {
            var intent = Android.Net.VpnService.Prepare(this);
            if (intent != null)
            {
                StartActivityForResult(intent, VpnRequestCode);
            }
        }
    }
}
