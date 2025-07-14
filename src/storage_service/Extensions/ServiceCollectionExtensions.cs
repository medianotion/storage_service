using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Storage.Configuration;
using Storage.Providers;

namespace Storage.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Storage Service using configuration from appsettings.json
        /// </summary>
        public static IServiceCollection AddStorageService(this IServiceCollection services, IConfiguration configuration, string sectionName = StorageOptions.SectionName)
        {
            services.Configure<StorageOptions>(configuration.GetSection(sectionName));
            services.AddSingleton<IStorageProvider>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
                return CreateProvider(options);
            });
            services.AddSingleton<IStorage>(provider =>
            {
                var storageProvider = provider.GetRequiredService<IStorageProvider>();
                return storageProvider.CreateStorage();
            });

            return services;
        }

        /// <summary>
        /// Add Storage Service using manual configuration with action
        /// </summary>
        public static IServiceCollection AddStorageService(this IServiceCollection services, Action<StorageOptions> configureOptions)
        {
            var options = new StorageOptions();
            configureOptions(options);
            return services.AddStorageService(options);
        }

        /// <summary>
        /// Add Storage Service using manual configuration with options object
        /// </summary>
        public static IServiceCollection AddStorageService(this IServiceCollection services, StorageOptions options)
        {
            services.AddSingleton<IStorageProvider>(_ => CreateProvider(options));
            services.AddSingleton<IStorage>(provider =>
            {
                var storageProvider = provider.GetRequiredService<IStorageProvider>();
                return storageProvider.CreateStorage();
            });

            return services;
        }

        private static IStorageProvider CreateProvider(StorageOptions options)
        {
            switch (options.DefaultProvider)
            {
                case "S3":
                    return new S3StorageProvider(options.S3);
                case "Network":
                    return new NetworkStorageProvider(options.Network);
                default:
                    throw new ArgumentException($"Unsupported storage provider: {options.DefaultProvider}");
            }
        }
    }
}