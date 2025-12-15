using SimpleCache.Abstractions;
using SimpleCache.Models;
using SimpleCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SimpleCache.Extensions
{
    /// <summary>
    /// Extension methods for registering SimpleCache services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSimpleCache(this IServiceCollection services, Action<CacheOptions>? configureOptions = null)
        {
            var options = new CacheOptions();
            configureOptions?.Invoke(options);

            services.AddMemoryCache();
            services.AddSingleton(options);
            services.AddSingleton<ICacheProvider>(sp =>
                new MemoryCacheProvider(
                    sp.GetRequiredService<IMemoryCache>(),
                    options.EnableSerialization));
            services.AddSingleton<ISimpleCache>(sp =>
                new SimpleCacheService(
                    sp.GetRequiredService<ICacheProvider>(),
                    options,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<SimpleCacheService>>()));

            return services;
        }

        public static IServiceCollection AddSimpleCache(this IServiceCollection services, CacheOptions options)
        {
            services.AddMemoryCache();
            services.AddSingleton(options);
            services.AddSingleton<ICacheProvider>(sp =>
                new MemoryCacheProvider(
                    sp.GetRequiredService<IMemoryCache>(),
                    options.EnableSerialization));
            services.AddSingleton<ISimpleCache>(sp =>
                new SimpleCacheService(
                    sp.GetRequiredService<ICacheProvider>(),
                    options,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<SimpleCacheService>>()));

            return services;
        }

        private static T GetRequiredService<T>(this IServiceProvider provider)
        {
            return (T)(provider.GetService(typeof(T)) ?? throw new InvalidOperationException($"Service of type {typeof(T)} is not registered"));
        }

        private static T? GetService<T>(this IServiceProvider provider) where T : class
        {
            return provider.GetService(typeof(T)) as T;
        }
    }
}
