using Amazon.S3;
using Storage;
using Storage.Exceptions;
using System;
using System.Net;
using Xunit;

namespace Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void StorageNotFoundException_Constructor_WithResourceKey_SetsProperties()
        {
            // Arrange
            var resourceKey = "test-resource";

            // Act
            var exception = new StorageNotFoundException(resourceKey);

            // Assert
            Assert.Equal(resourceKey, exception.ResourceKey);
            Assert.Contains(resourceKey, exception.Message);
            Assert.Contains("not found", exception.Message.ToLower());
        }

        [Fact]
        public void StorageNotFoundException_Constructor_WithResourceKeyAndInnerException_SetsProperties()
        {
            // Arrange
            var resourceKey = "test-resource";
            var innerException = new Exception("Inner exception");

            // Act
            var exception = new StorageNotFoundException(resourceKey, innerException);

            // Assert
            Assert.Equal(resourceKey, exception.ResourceKey);
            Assert.Contains(resourceKey, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void StorageAccessDeniedException_Constructor_WithResourceKey_SetsProperties()
        {
            // Arrange
            var resourceKey = "protected-resource";

            // Act
            var exception = new StorageAccessDeniedException(resourceKey);

            // Assert
            Assert.Equal(resourceKey, exception.ResourceKey);
            Assert.Contains(resourceKey, exception.Message);
            Assert.Contains("access denied", exception.Message.ToLower());
            Assert.Contains("permissions", exception.Message.ToLower());
        }

        [Fact]
        public void StorageAccessDeniedException_Constructor_WithResourceKeyAndInnerException_SetsProperties()
        {
            // Arrange
            var resourceKey = "protected-resource";
            var innerException = new Exception("Access denied by AWS");

            // Act
            var exception = new StorageAccessDeniedException(resourceKey, innerException);

            // Assert
            Assert.Equal(resourceKey, exception.ResourceKey);
            Assert.Contains(resourceKey, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void StorageAuthenticationException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Authentication failed";

            // Act
            var exception = new StorageAuthenticationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void StorageAuthenticationException_Constructor_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            var message = "Authentication failed";
            var innerException = new Exception("Invalid credentials");

            // Act
            var exception = new StorageAuthenticationException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void StorageConfigurationException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Configuration error";

            // Act
            var exception = new StorageConfigurationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void StorageUnavailableException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Service temporarily unavailable";

            // Act
            var exception = new StorageUnavailableException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void StorageTimeoutException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Operation timed out";

            // Act
            var exception = new StorageTimeoutException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void StorageInternalException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Internal storage error";

            // Act
            var exception = new StorageInternalException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void BuildStorageException_NotFound_ReturnsStorageNotFoundException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Not found", Amazon.Runtime.ErrorType.Unknown, "404", "request-id", HttpStatusCode.NotFound);
            var resourceKey = "test-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageNotFoundException>(result);
            var notFoundEx = (StorageNotFoundException)result;
            Assert.Equal(resourceKey, notFoundEx.ResourceKey);
            Assert.Contains(resourceKey, notFoundEx.Message);
            Assert.Same(amazonException, notFoundEx.InnerException);
        }

        [Fact]
        public void BuildStorageException_Forbidden_ReturnsStorageAccessDeniedException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Forbidden", Amazon.Runtime.ErrorType.Unknown, "403", "request-id", HttpStatusCode.Forbidden);
            var resourceKey = "protected-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageAccessDeniedException>(result);
            var accessDeniedEx = (StorageAccessDeniedException)result;
            Assert.Equal(resourceKey, accessDeniedEx.ResourceKey);
            Assert.Same(amazonException, accessDeniedEx.InnerException);
        }

        [Fact]
        public void BuildStorageException_Unauthorized_ReturnsStorageAccessDeniedException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Unauthorized", Amazon.Runtime.ErrorType.Unknown, "401", "request-id", HttpStatusCode.Unauthorized);
            var resourceKey = "secure-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageAccessDeniedException>(result);
        }

        [Fact]
        public void BuildStorageException_ServiceUnavailable_ReturnsStorageUnavailableException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Service unavailable", Amazon.Runtime.ErrorType.Unknown, "503", "request-id", HttpStatusCode.ServiceUnavailable);
            var resourceKey = "temp-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageUnavailableException>(result);
            Assert.Contains(resourceKey, result.Message);
            Assert.Contains("temporarily unavailable", result.Message.ToLower());
            Assert.Same(amazonException, result.InnerException);
        }

        [Fact]
        public void BuildStorageException_TooManyRequests_ReturnsStorageUnavailableException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Too many requests", Amazon.Runtime.ErrorType.Unknown, "429", "request-id", HttpStatusCode.TooManyRequests);
            var resourceKey = "rate-limited-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageUnavailableException>(result);
            Assert.Contains(resourceKey, result.Message);
        }

        [Fact]
        public void BuildStorageException_RequestTimeout_ReturnsStorageTimeoutException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Request timeout", Amazon.Runtime.ErrorType.Unknown, "408", "request-id", HttpStatusCode.RequestTimeout);
            var resourceKey = "slow-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageTimeoutException>(result);
            Assert.Contains(resourceKey, result.Message);
            Assert.Contains("timed out", result.Message.ToLower());
            Assert.Same(amazonException, result.InnerException);
        }

        [Fact]
        public void BuildStorageException_InternalServerError_ReturnsStorageInternalException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Internal server error", Amazon.Runtime.ErrorType.Unknown, "500", "request-id", HttpStatusCode.InternalServerError);
            var resourceKey = "error-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageInternalException>(result);
            Assert.Contains(resourceKey, result.Message);
            Assert.Contains("failed", result.Message.ToLower());
            Assert.Same(amazonException, result.InnerException);
        }

        [Fact]
        public void BuildStorageException_BadGateway_ReturnsStorageInternalException()
        {
            // Arrange
            var amazonException = new AmazonS3Exception("Bad gateway", Amazon.Runtime.ErrorType.Unknown, "502", "request-id", HttpStatusCode.BadGateway);
            var resourceKey = "gateway-key";

            // Act
            var result = S3.BuildStorageException(amazonException, resourceKey);

            // Assert
            Assert.IsType<StorageInternalException>(result);
            Assert.Contains(resourceKey, result.Message);
        }

        [Fact]
        public void StorageException_IsAbstractBaseClass()
        {
            // Assert
            Assert.True(typeof(StorageException).IsAbstract);
            Assert.True(typeof(StorageException).IsSubclassOf(typeof(Exception)));
        }
    }
}