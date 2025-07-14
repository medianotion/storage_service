using Xunit;
using Storage.Configuration;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void StorageOptions_DefaultValues_AreSet()
        {
            // Arrange & Act
            var options = new StorageOptions();

            // Assert
            Assert.Equal("S3", options.DefaultProvider);
            Assert.NotNull(options.Providers);
            Assert.Empty(options.Providers);
            Assert.NotNull(options.S3);
        }

        [Fact]
        public void S3Options_DefaultValues_AreSet()
        {
            // Arrange & Act
            var options = new S3Options();

            // Assert
            Assert.Equal("us-east-1", options.Region);
            Assert.Equal(string.Empty, options.Bucket);
            Assert.Equal(3, options.MaxRetries);
            Assert.NotNull(options.Credentials);
        }

        [Fact]
        public void CredentialsOptions_DefaultValues_AreSet()
        {
            // Arrange & Act
            var credentials = new CredentialsOptions();

            // Assert
            Assert.Equal("None", credentials.AuthenticationType);
            Assert.Equal(string.Empty, credentials.AccessKey);
            Assert.Equal(string.Empty, credentials.SecretKey);
            Assert.Equal(string.Empty, credentials.SessionToken);
            Assert.NotNull(credentials.Properties);
            Assert.Empty(credentials.Properties);
        }

        [Fact]
        public void S3Options_BackwardCompatibility_DelegatesToCredentials()
        {
            // Arrange
            var options = new S3Options();

            // Act
            options.AccessKey = "test-access-key";
            options.SecretKey = "test-secret-key";
            options.SessionToken = "test-session-token";

            // Assert
            Assert.Equal("test-access-key", options.Credentials.AccessKey);
            Assert.Equal("test-secret-key", options.Credentials.SecretKey);
            Assert.Equal("test-session-token", options.Credentials.SessionToken);
        }

        [Fact]
        public void CredentialsOptions_SupportsDifferentAuthTypes()
        {
            // Test AccessKey authentication
            var accessKeyAuth = new CredentialsOptions
            {
                AuthenticationType = "AccessKey",
                AccessKey = "AKIA123",
                SecretKey = "secret123"
            };
            Assert.Equal("AccessKey", accessKeyAuth.AuthenticationType);
            Assert.Equal("AKIA123", accessKeyAuth.AccessKey);

            // Test STS authentication
            var stsAuth = new CredentialsOptions
            {
                AuthenticationType = "STS",
                AccessKey = "AKIA123",
                SecretKey = "secret123",
                SessionToken = "session123"
            };
            Assert.Equal("STS", stsAuth.AuthenticationType);
            Assert.Equal("session123", stsAuth.SessionToken);

            // Test Custom authentication
            var customAuth = new CredentialsOptions
            {
                AuthenticationType = "Custom",
                Properties = new Dictionary<string, string>
                {
                    { "endpoint", "https://vault.company.com" },
                    { "token", "hvs.123" }
                }
            };
            Assert.Equal("Custom", customAuth.AuthenticationType);
            Assert.Equal(2, customAuth.Properties.Count);
            Assert.Equal("https://vault.company.com", customAuth.Properties["endpoint"]);
        }

        [Fact]
        public void StorageOptions_SectionName_IsCorrect()
        {
            // Act & Assert
            Assert.Equal("StorageService", StorageOptions.SectionName);
        }

        [Fact]
        public void S3Options_CanSetBucketAndRegion()
        {
            // Arrange & Act
            var options = new S3Options
            {
                Bucket = "my-test-bucket",
                Region = "eu-west-1",
                MaxRetries = 5,
            };

            // Assert
            Assert.Equal("my-test-bucket", options.Bucket);
            Assert.Equal("eu-west-1", options.Region);
            Assert.Equal(5, options.MaxRetries);
        }

        [Fact]
        public void ProviderOptions_DefaultValues_AreSet()
        {
            // Arrange & Act
            var options = new ProviderOptions();

            // Assert
            Assert.Equal(string.Empty, options.Type);
            Assert.True(options.Enabled);
            Assert.NotNull(options.Settings);
            Assert.Empty(options.Settings);
        }

        [Fact]
        public void ProviderOptions_CanConfigureTypeAndSettings()
        {
            // Arrange & Act
            var options = new ProviderOptions
            {
                Type = "S3",
                Enabled = false,
                Settings = new Dictionary<string, string>
                {
                    { "region", "us-west-2" },
                    { "encryption", "enabled" }
                }
            };

            // Assert
            Assert.Equal("S3", options.Type);
            Assert.False(options.Enabled);
            Assert.Equal(2, options.Settings.Count);
            Assert.Equal("us-west-2", options.Settings["region"]);
            Assert.Equal("enabled", options.Settings["encryption"]);
        }

        [Fact]
        public void StorageOptions_CanConfigureMultipleProviders()
        {
            // Arrange & Act
            var options = new StorageOptions
            {
                DefaultProvider = "AzureBlob",
                Providers = new Dictionary<string, ProviderOptions>
                {
                    { "S3", new ProviderOptions { Type = "S3", Enabled = true } },
                    { "AzureBlob", new ProviderOptions { Type = "AzureBlob", Enabled = true } }
                }
            };

            // Assert
            Assert.Equal("AzureBlob", options.DefaultProvider);
            Assert.Equal(2, options.Providers.Count);
            Assert.True(options.Providers.ContainsKey("S3"));
            Assert.True(options.Providers.ContainsKey("AzureBlob"));
            Assert.Equal("S3", options.Providers["S3"].Type);
        }

        [Fact]
        public void CredentialsOptions_CustomProperties_CanBeSet()
        {
            // Arrange & Act
            var credentials = new CredentialsOptions
            {
                AuthenticationType = "Custom",
                Properties = new Dictionary<string, string>
                {
                    { "vault_url", "https://vault.example.com" },
                    { "role_id", "app-role-123" },
                    { "secret_id", "secret-456" }
                }
            };

            // Assert
            Assert.Equal("Custom", credentials.AuthenticationType);
            Assert.Equal(3, credentials.Properties.Count);
            Assert.Equal("https://vault.example.com", credentials.Properties["vault_url"]);
            Assert.Equal("app-role-123", credentials.Properties["role_id"]);
            Assert.Equal("secret-456", credentials.Properties["secret_id"]);
        }

        [Fact]
        public void S3Options_CredentialsReference_IsSameObject()
        {
            // Arrange
            var options = new S3Options();

            // Act
            var credentialsRef1 = options.Credentials;
            var credentialsRef2 = options.Credentials;

            // Assert - Should be the same object reference
            Assert.Same(credentialsRef1, credentialsRef2);
        }
    }
}