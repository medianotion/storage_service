using System;
using System.IO;
using Storage;
using Storage.Configuration;
using Storage.Providers;
using Xunit;

namespace Tests
{
    public class NetworkProviderTests
    {
        [Fact]
        public void NetworkStorageProvider_CreateStorage_ReturnsNetworkStorage()
        {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), "NetworkProviderTest", Guid.NewGuid().ToString());
            var options = new NetworkOptions
            {
                BasePath = tempPath,
                CreateDirectoriesIfNotExist = true
            };
            var provider = new NetworkStorageProvider(options);

            try
            {
                // Act
                var storage = provider.CreateStorage();

                // Assert
                Assert.NotNull(storage);
                Assert.IsAssignableFrom<IStorage>(storage);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Fact]
        public void NetworkStorageProvider_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NetworkStorageProvider(null));
        }

        [Fact]
        public void NetworkOptions_DefaultValues_AreSet()
        {
            // Arrange & Act
            var options = new NetworkOptions();

            // Assert
            Assert.Equal(string.Empty, options.BasePath);
            Assert.Equal(81920, options.BufferSize);
            Assert.True(options.CreateDirectoriesIfNotExist);
            Assert.True(options.UseTransactionalCopy);
            Assert.True(options.OverwriteExisting);
            Assert.Equal(".tmp", options.TempFileExtension);
            Assert.Equal(string.Empty, options.Username);
            Assert.Equal(string.Empty, options.Password);
            Assert.Equal(string.Empty, options.Domain);
            Assert.NotNull(options.Credentials);
        }

        [Fact]
        public void NetworkOptions_BackwardCompatibilityProperties_DelegateToCredentials()
        {
            // Arrange
            var options = new NetworkOptions();

            // Act
            options.AccessKey = "test-access-key";
            options.SecretKey = "test-secret-key";

            // Assert
            Assert.Equal("test-access-key", options.Credentials.AccessKey);
            Assert.Equal("test-secret-key", options.Credentials.SecretKey);
            Assert.Equal("test-access-key", options.AccessKey);
            Assert.Equal("test-secret-key", options.SecretKey);
        }

        [Fact]
        public void NetworkOptions_CanBeConfigured()
        {
            // Arrange & Act
            var options = new NetworkOptions
            {
                BasePath = @"\\server\share\storage",
                BufferSize = 16384,
                CreateDirectoriesIfNotExist = false,
                UseTransactionalCopy = false,
                OverwriteExisting = false,
                TempFileExtension = ".temp",
                Username = "testuser",
                Password = "testpass",
                Domain = "testdomain"
            };

            // Assert
            Assert.Equal(@"\\server\share\storage", options.BasePath);
            Assert.Equal(16384, options.BufferSize);
            Assert.False(options.CreateDirectoriesIfNotExist);
            Assert.False(options.UseTransactionalCopy);
            Assert.False(options.OverwriteExisting);
            Assert.Equal(".temp", options.TempFileExtension);
            Assert.Equal("testuser", options.Username);
            Assert.Equal("testpass", options.Password);
            Assert.Equal("testdomain", options.Domain);
        }

        [Fact]
        public void NetworkOptions_WithCredentialsConfiguration_Works()
        {
            // Arrange & Act
            var options = new NetworkOptions();
            options.Credentials.AuthenticationType = "Custom";
            options.Credentials.AccessKey = "network-access-key";
            options.Credentials.SecretKey = "network-secret-key";

            // Assert
            Assert.Equal("Custom", options.Credentials.AuthenticationType);
            Assert.Equal("network-access-key", options.Credentials.AccessKey);
            Assert.Equal("network-secret-key", options.Credentials.SecretKey);
            Assert.Equal("network-access-key", options.AccessKey);
            Assert.Equal("network-secret-key", options.SecretKey);
        }

        [Fact]
        public void NetworkOptions_CredentialsDelegation_WorksBothWays()
        {
            // Arrange
            var options = new NetworkOptions();

            // Act - Set via properties
            options.AccessKey = "prop-access";
            options.SecretKey = "prop-secret";

            // Assert
            Assert.Equal("prop-access", options.Credentials.AccessKey);
            Assert.Equal("prop-secret", options.Credentials.SecretKey);

            // Act - Set via credentials object
            options.Credentials.AccessKey = "cred-access";
            options.Credentials.SecretKey = "cred-secret";

            // Assert
            Assert.Equal("cred-access", options.AccessKey);
            Assert.Equal("cred-secret", options.SecretKey);
        }
    }
}