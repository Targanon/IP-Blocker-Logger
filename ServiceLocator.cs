using Microsoft.Extensions.DependencyInjection;

namespace IPBlockerLogger
{
    public static class ServiceLocator
    {
        public static IServiceProvider? Services { get; private set; }

        public static void Initialize(IServiceProvider services)
        {
            Services = services;
        }

        // Helper methods for easy access
        public static T GetRequiredService<T>() where T : notnull
        {
            if (Services == null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");

            return Services.GetRequiredService<T>();
        }

        public static T? GetService<T>() where T : class
        {
            return Services?.GetService<T>();
        }
    }
}