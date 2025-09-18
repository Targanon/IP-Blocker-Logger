using Microsoft.Extensions.Logging;
using IP_Blocker_Logger.Services;
using IP_Blocker_Logger.Pages;

namespace IP_Blocker_Logger
{
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

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
