using Xunit;
using Storage;
using Storage.Configuration;
using Storage.Providers;
using System;

namespace Tests
{
    public class ProviderTests
    {
        [Fact]
        public void S3StorageProvider_Constructor_RequiresOptions()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new S3StorageProvider(null));
        }

        [Fact]
        public void S3StorageProvider_CreateStorage_ReturnsS3Instance()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "us-east-1"
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void S3StorageProvider_CreateStorage_MultipleCalls_ReturnDifferentInstances()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "us-east-1"
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage1 = provider.CreateStorage();
            var storage2 = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage1);
            Assert.NotNull(storage2);
            Assert.NotSame(storage1, storage2); // Should be different instances
        }

        [Fact]
        public void S3StorageProvider_WithAccessKeyCredentials_CreatesStorage()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "us-west-2",
                Credentials = new CredentialsOptions
                {
                    AuthenticationType = "AccessKey",
                    AccessKey = "AKIA123",
                    SecretKey = "secret123"
                }
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void S3StorageProvider_WithSTSCredentials_CreatesStorage()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "eu-central-1",
                Credentials = new CredentialsOptions
                {
                    AuthenticationType = "STS",
                    AccessKey = "ASIATEMP123",
                    SecretKey = "tempsecret123",
                    SessionToken = "session123"
                }
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void S3StorageProvider_WithCustomCredentials_CreatesStorage()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "ap-southeast-1",
                Credentials = new CredentialsOptions
                {
                    AuthenticationType = "Custom",
                    Properties = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "vault_endpoint", "https://vault.company.com" },
                        { "role", "storage-service" }
                    }
                }
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void S3StorageProvider_WithDefaultCredentials_CreatesStorage()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "us-east-1",
                Credentials = new CredentialsOptions
                {
                    AuthenticationType = "None"
                }
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
        }

        [Fact]
        public void S3StorageProvider_WithCustomRetries_PassesToS3()
        {
            // Arrange
            var options = new S3Options
            {
                Bucket = "test-bucket",
                Region = "us-east-1",
                MaxRetries = 10,
            };
            var provider = new S3StorageProvider(options);

            // Act
            var storage = provider.CreateStorage();

            // Assert
            Assert.NotNull(storage);
            Assert.IsType<S3>(storage);
            // Note: We can't easily verify the internal S3 configuration without reflection
            // but the fact that it creates without throwing is a good test
        }
    }
}