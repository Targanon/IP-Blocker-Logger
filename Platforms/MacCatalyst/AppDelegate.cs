#if MACCATALYST
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace IP_Blocker_Logger.Platforms.MacCatalyst;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
#endif
