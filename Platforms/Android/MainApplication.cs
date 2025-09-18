using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace IPBlockerLogger.Platforms.Android;

[Application]
[Preserve(AllMembers = true)]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => IP_Blocker_Logger.MauiProgram.CreateMauiApp();
}
