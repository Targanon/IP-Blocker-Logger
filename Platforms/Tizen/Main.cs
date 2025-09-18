#if TIZEN
using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace IP_Blocker_Logger.Platforms.Tizen;

internal class TizenProgram : MauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args)
    {
        var app = new TizenProgram();
        app.Run(args);
    }
}
#endif
