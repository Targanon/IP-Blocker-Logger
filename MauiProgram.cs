using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using IP_Blocker_Logger.Services;
using IP_Blocker_Logger.Pages;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace IP_Blocker_Logger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<BlockedIpRepository>();
        builder.Services.AddSingleton<FirewallLogRepository>();
        builder.Services.AddSingleton<IFirewallController, FirewallController>();

        // Pages
        builder.Services.AddSingleton<FirewallPage>();

        return builder.Build();
    }
}
