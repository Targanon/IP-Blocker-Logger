#if IOS
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace IP_Blocker_Logger.Platforms.iOS;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
#endif
