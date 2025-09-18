using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace IPBlockerLogger.Platforms.Android;

[Application]
[Preserve(AllMembers = true)]
[Register("crc64b594d46450fc2e24.MainApplication")]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => IP_Blocker_Logger.MauiProgram.CreateMauiApp();
}
