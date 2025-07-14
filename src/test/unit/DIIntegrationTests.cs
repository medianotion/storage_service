using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage;
using Storage.Configuration;
using Storage.Extensions;
using Storage.Providers;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class DIIntegrationTests
    {
        [Fact]
        public void AddStorageService_WithManualStorageOptions_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new StorageOptions
            {
                DefaultProvider = "S3",
                S3 = new S3Options
                {
                    Bucket = "test-bucket",
                    Region = "us-east-1"
                }
            };

            // Act
            services.AddStorageService(options);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            var provider = serviceProvider.GetService<IStorageProvider>();

            Assert.NotNull(storage);
            Assert.NotNull(provider);
            Assert.IsType<S3>(storage);
            Assert.IsType<S3StorageProvider>(provider);
        }

        [Fact]
        public void AddStorageService_WithActionConfiguration_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddStorageService(options =>
            {
                options.DefaultProvider = "S3";
                options.S3.Bucket = "action-bucket";
                options.S3.Region = "eu-west-1";
                options.S3.MaxRetries = 5;
            });

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            var provider = serviceProvider.GetService<IStorageProvider>();

            Assert.NotNull(storage);
            Assert.NotNull(provider);
            Assert.IsType<S3>(storage);
            Assert.IsType<S3StorageProvider>(provider);
        }

        [Fact]
        public void AddStorageService_WithIConfiguration_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "StorageService:DefaultProvider", "S3" },
                { "StorageService:S3:Bucket", "config-bucket" },
                { "StorageService:S3:Region", "ap-southeast-2" },
                { "StorageService:S3:MaxRetries", "7" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act
            services.AddStorageService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            var provider = serviceProvider.GetService<IStorageProvider>();

            Assert.NotNull(storage);
            Assert.NotNull(provider);
            Assert.IsType<S3>(storage);
            Assert.IsType<S3StorageProvider>(provider);
        }

        [Fact]
        public void AddStorageService_WithCustomSectionName_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "CustomStorage:DefaultProvider", "S3" },
                { "CustomStorage:S3:Bucket", "custom-bucket" },
                { "CustomStorage:S3:Region", "us-west-2" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act
            services.AddStorageService(configuration, "CustomStorage");
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void AddStorageService_ServicesSingleton_ReturnsSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new StorageOptions
            {
                S3 = new S3Options { Bucket = "singleton-bucket", Region = "us-east-1" }
            };

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

        [Fact]
        public void AddStorageService_WithCredentialsFromConfiguration_Works()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "StorageService:S3:Bucket", "secure-bucket" },
                { "StorageService:S3:Region", "us-east-1" },
                { "StorageService:S3:Credentials:AuthenticationType", "AccessKey" },
                { "StorageService:S3:Credentials:AccessKey", "AKIA123456" },
                { "StorageService:S3:Credentials:SecretKey", "secret123456" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act
            services.AddStorageService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void AddStorageService_WithSTSCredentialsFromConfiguration_Works()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                { "StorageService:S3:Bucket", "sts-bucket" },
                { "StorageService:S3:Region", "eu-central-1" },
                { "StorageService:S3:Credentials:AuthenticationType", "STS" },
                { "StorageService:S3:Credentials:AccessKey", "ASIATEMP123" },
                { "StorageService:S3:Credentials:SecretKey", "tempsecret123" },
                { "StorageService:S3:Credentials:SessionToken", "session123token" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            // Act
            services.AddStorageService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var storage = serviceProvider.GetService<IStorage>();
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }
    }
}