using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;
using Android.Content;
using Android.Runtime;

namespace IPBlockerLogger.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[Preserve(AllMembers = true)]
public class MainActivity : MauiAppCompatActivity
{
    const int VpnRequestCode = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        System.Diagnostics.Debug.WriteLine("MainActivity: OnCreate called");
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("MainActivity: OnResume called");
        RequestVpnPermissionIfNeeded();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        
        System.Diagnostics.Debug.WriteLine($"MainActivity: OnActivityResult - RequestCode: {requestCode}, ResultCode: {resultCode}");
        
        if (requestCode == VpnRequestCode)
        {
            if (resultCode == Result.Ok)
            {
                System.Diagnostics.Debug.WriteLine("MainActivity: VPN permission granted");
                // Permission granted, the user can now start the VPN service from the UI
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainActivity: VPN permission denied");
                // Permission denied, show a message or handle accordingly
            }
        }
    }

    void RequestVpnPermissionIfNeeded()
    {
        var intent = global::Android.Net.VpnService.Prepare(this);
        if (intent != null)
        {
            StartActivityForResult(intent, VpnRequestCode);
        }
    }
}
