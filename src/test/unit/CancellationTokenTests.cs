using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using Storage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class CancellationTokenTests
    {
        [Fact]
        public async Task GetAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new GetObjectResponse
            {
                ResponseStream = new MemoryStream()
            };

            mockS3Client.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            // Act
            var result = await s3.GetAsync("test-key", cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), cancellationToken), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PutAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new PutObjectResponse { ContentLength = 100 };
            mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            using var stream = new MemoryStream(new byte[100]);

            // Act
            var result = await s3.PutAsync("test-key", stream, ".txt", false, cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), cancellationToken), Times.Once);
            Assert.Equal(100, result);
        }

        [Fact]
        public async Task ExistsAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new GetObjectMetadataResponse();
            mockS3Client.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            // Act
            var result = await s3.ExistsAsync("test-key", cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), cancellationToken), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task ListAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new ListObjectsV2Response();
            mockS3Client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            // Act
            var result = await s3.ListAsync("test-prefix", cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), cancellationToken), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CopyAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new CopyObjectResponse();
            mockS3Client.Setup(x => x.CopyObjectAsync(It.IsAny<CopyObjectRequest>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            // Act
            await s3.CopyAsync("source-key", "dest-bucket", "dest-key", cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.CopyObjectAsync(It.IsAny<CopyObjectRequest>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_PassesCancellationTokenToAWS()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationToken = new CancellationToken();

            var response = new DeleteObjectResponse();
            mockS3Client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), cancellationToken))
                       .Returns(Task.FromResult(response));

            // Act
            await s3.DeleteAsync("test-key", cancellationToken);

            // Assert
            mockS3Client.Verify(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task PutAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var mockS3Client = new Mock<IAmazonS3>();
            var s3 = new S3(mockS3Client.Object, "test-bucket");
            var cancellationTokenSource = new CancellationTokenSource();

            mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new TaskCanceledException());

            using var stream = new MemoryStream(new byte[100]);
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => s3.PutAsync("test-key", stream, ".txt", false, cancellationTokenSource.Token));
        }
    }
}