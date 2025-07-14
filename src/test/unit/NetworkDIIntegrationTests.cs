using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage;
using Storage.Configuration;
using Storage.Extensions;
using Storage.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Tests
{
    public class NetworkDIIntegrationTests
    {
        private readonly string _testBasePath;

        public NetworkDIIntegrationTests()
        {
            _testBasePath = Path.Combine(Path.GetTempPath(), "NetworkDITests", Guid.NewGuid().ToString());
        }

        [Fact]
        public void AddStorageService_WithNetworkProvider_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new StorageOptions
            {
                DefaultProvider = "Network",
                Network = new NetworkOptions
                {
                    BasePath = _testBasePath,
                    CreateDirectoriesIfNotExist = true
                }
            };

            try
            {
                // Act
                services.AddStorageService(options);
                var serviceProvider = services.BuildServiceProvider();

                // Assert
                var storage = serviceProvider.GetService<IStorage>();
                var provider = serviceProvider.GetService<IStorageProvider>();

                Assert.NotNull(storage);
                Assert.NotNull(provider);
                Assert.IsType<NetworkStorageProvider>(provider);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }

        [Fact]
        public void AddStorageService_WithNetworkProviderFromConfiguration_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "StorageService:DefaultProvider", "Network" },
                { "StorageService:Network:BasePath", _testBasePath },
                { "StorageService:Network:CreateDirectoriesIfNotExist", "true" },
                { "StorageService:Network:BufferSize", "16384" },
                { "StorageService:Network:UseTransactionalCopy", "false" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            try
            {
                // Act
                services.AddStorageService(configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Assert
                var storage = serviceProvider.GetService<IStorage>();
                var provider = serviceProvider.GetService<IStorageProvider>();

                Assert.NotNull(storage);
                Assert.NotNull(provider);
                Assert.IsType<NetworkStorageProvider>(provider);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }

        [Fact]
        public void AddStorageService_WithNetworkProviderAction_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();

            try
            {
                // Act
                services.AddStorageService(options =>
                {
                    options.DefaultProvider = "Network";
                    options.Network.BasePath = _testBasePath;
                    options.Network.CreateDirectoriesIfNotExist = true;
                    options.Network.BufferSize = 8192;
                    options.Network.Username = "testuser";
                    options.Network.Domain = "testdomain";
                });

                var serviceProvider = services.BuildServiceProvider();

                // Assert
                var storage = serviceProvider.GetService<IStorage>();
                var provider = serviceProvider.GetService<IStorageProvider>();

                Assert.NotNull(storage);
                Assert.NotNull(provider);
                Assert.IsType<NetworkStorageProvider>(provider);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }

        [Fact]
        public void AddStorageService_WithNetworkCredentialsFromConfiguration_Works()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "StorageService:DefaultProvider", "Network" },
                { "StorageService:Network:BasePath", _testBasePath },
                { "StorageService:Network:Username", "networkuser" },
                { "StorageService:Network:Password", "networkpass" },
                { "StorageService:Network:Domain", "networkdomain" },
                { "StorageService:Network:Credentials:AuthenticationType", "Custom" },
                { "StorageService:Network:Credentials:AccessKey", "net-access-key" },
                { "StorageService:Network:Credentials:SecretKey", "net-secret-key" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            try
            {
                // Act
                services.AddStorageService(configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Assert
                var storage = serviceProvider.GetService<IStorage>();
                Assert.NotNull(storage);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }

        [Fact]
        public void AddStorageService_ServicesSingleton_ReturnsSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new StorageOptions
            {
                DefaultProvider = "Network",
                Network = new NetworkOptions 
                { 
                    BasePath = _testBasePath,
                    CreateDirectoriesIfNotExist = true
                }
            };

            try
            {
                // Act
                services.AddStorageService(options);
                var serviceProvider = services.BuildServiceProvider();

                var storage1 = serviceProvider.GetService<IStorage>();
                var storage2 = serviceProvider.GetService<IStorage>();
                var provider1 = serviceProvider.GetService<IStorageProvider>();
                var provider2 = serviceProvider.GetService<IStorageProvider>();

                // Assert
                Assert.Same(storage1, storage2); // Should be singleton
                Assert.Same(provider1, provider2); // Should be singleton
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }

        [Fact]
        public void AddStorageService_UnsupportedProvider_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new StorageOptions
            {
                DefaultProvider = "UnsupportedProvider"
            };

            // Act & Assert
            services.AddStorageService(options);
            var serviceProvider = services.BuildServiceProvider();
            
            Assert.Throws<ArgumentException>(() => serviceProvider.GetService<IStorageProvider>());
        }

        [Fact]
        public void AddStorageService_WithCustomSectionName_RegistersNetworkServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "CustomSection:DefaultProvider", "Network" },
                { "CustomSection:Network:BasePath", _testBasePath },
                { "CustomSection:Network:CreateDirectoriesIfNotExist", "true" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            try
            {
                // Act
                services.AddStorageService(configuration, "CustomSection");
                var serviceProvider = services.BuildServiceProvider();

                // Assert
                var storage = serviceProvider.GetService<IStorage>();
                Assert.NotNull(storage);
                Assert.IsType<NetworkStorageProvider>(serviceProvider.GetService<IStorageProvider>());
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(_testBasePath))
                {
                    Directory.Delete(_testBasePath, true);
                }
            }
        }
    }
}