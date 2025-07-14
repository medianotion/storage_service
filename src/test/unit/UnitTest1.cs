using Amazon.S3.Model;
using Amazon.S3;
using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Storage;
using Storage.Exceptions;

namespace Tests
{
    
    public class UnitTest1
    {


        [Fact]
        public async Task Exists_False()
        {
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default(CancellationToken))).ThrowsAsync(new AmazonS3Exception("", Amazon.Runtime.ErrorType.Unknown, "404", "id", HttpStatusCode.NotFound));
            var result = await s3.ExistsAsync("key");
            Assert.False(result);
        }

        [Fact]
        public async Task Exists_True()
        {
            var response = new GetObjectMetadataResponse { };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            var result = await s3.ExistsAsync("key");
            Assert.True(result);
        }

        [Fact]
        public async Task GetObjectCount()
        {
            var response = new GetObjectMetadataResponse { };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            var result = await s3.ExistsAsync("key");
            Assert.True(result);
        }


        [Fact]
        public async Task Get_KeyNotFoundException()
        {
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x =>  x.GetObjectAsync(It.IsAny<GetObjectRequest>(), default(CancellationToken))).ThrowsAsync(new AmazonS3Exception("",Amazon.Runtime.ErrorType.Unknown,"404","id", HttpStatusCode.NotFound));
            var exception = await Assert.ThrowsAsync<StorageNotFoundException>(async () => await s3.GetAsync("key"));
            Assert.Equal("key", exception.ResourceKey);
        }

        [Fact]
        public async Task Get_Success()
        {
            var response = new GetObjectResponse() { ResponseStream = new MemoryStream() };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            var result = await s3.GetAsync("key");
            Assert.IsType<MemoryStream>(result);
        }

        [Fact]
        public async Task Copy_Success()
        {
            var response = new CopyObjectResponse() {  };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.CopyObjectAsync(It.IsAny<CopyObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            await s3.CopyAsync("key", "destinationBucket","DestinationKey");
            Mock.VerifyAll();
        }



        [Fact]
        public async Task Put_Success()
        {
            long expected = 1;
            var response = new PutObjectResponse() { ContentLength = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            var result = await s3.PutAsync("key",new MemoryStream(new byte[1]),".pdf");
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Put_GZIP_Success()
        {
            long expected = 1;
            var response = new PutObjectResponse() { ContentLength = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            var result = await s3.PutAsync("key", new MemoryStream(new byte[1]), ".pdf", true);
            Assert.Equal(expected, result);
        }


        [Fact]
        public async Task Put_NullStream()
        {
            long expected = 1;
            var response = new PutObjectResponse() { ContentLength = expected };
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(response));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await s3.PutAsync("key", null,".pdf"));
        }


        [Fact]
        public async Task PutInParts_Success()
        {
            var stream = new MemoryStream(new byte[S3.MIN_PART_SIZE + 1]);
            long expected = stream.Length;
            var initResponse = new InitiateMultipartUploadResponse() { UploadId = "uploadid" };
            var uploadResponse = new UploadPartResponse() { ContentLength = expected };
            var completeResponse = new CompleteMultipartUploadResponse() { ContentLength = expected  };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(initResponse));
            s3Mock.Setup(x => x.UploadPartAsync(It.IsAny<UploadPartRequest>(), default(CancellationToken))).Returns(Task.FromResult(uploadResponse));
            s3Mock.Setup(x => x.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(completeResponse)).Verifiable();

            var result = await s3.PutAsync("key", stream, ".pdf");
            Assert.Equal(expected, result);
        }
        [Fact]
        public async Task InitiateMultipartUploadAsync_Success()
        {
            string expected = "uploadid";
            var initResponse = new InitiateMultipartUploadResponse() { UploadId = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(initResponse));

            var result = await s3.InitiateMultipartUploadAsync("key", "text/xml");
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task InitiateMultipartUploadAsync_GZIP_Success()
        {
            string expected = "uploadid";
            var initResponse = new InitiateMultipartUploadResponse() { UploadId = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(initResponse));

            var result = await s3.InitiateMultipartUploadAsync("key", "text/xml", true);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task UploadPartAsync_Success()
        {
            int expected = 1;
            var uploadResponse = new UploadPartResponse() { ContentLength = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.UploadPartAsync(It.IsAny<UploadPartRequest>(), default(CancellationToken))).Returns(Task.FromResult(uploadResponse));
            var result = await s3.PutPartAsync("key", new MemoryStream(), "uploadid",1);

            Assert.IsType<UploadPartResponse>(result);
        }


        [Fact]
        public async Task CompleteMultipartUpload_Success()
        {
            long expected = 1;
            string uploadid = "uploadid";
            var completeResponse = new CompleteMultipartUploadResponse() { ContentLength = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(completeResponse)).Verifiable();
            await s3.CompleteMultipartUploadAsync("key", uploadid, new List<UploadPartResponse>());
            Mock.VerifyAll();
        }

        [Fact]
        public async Task AbortMultipartUpload_Success()
        {

            string uploadid = "uploadid";
            var abortResponse = new AbortMultipartUploadResponse() { ContentLength = 1 };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(abortResponse)).Verifiable();
            await s3.AbortMultipartUploadAsync("key", uploadid);
            Mock.VerifyAll();
        }


        [Fact]
        public async Task PutInParts_Abort()
        {
            var stream = new MemoryStream(new byte[S3.MIN_PART_SIZE + 1]);
            long expected = stream.Length;
            var initResponse = new InitiateMultipartUploadResponse() { UploadId = "uploadid" };
            var uploadResponse = new UploadPartResponse() { ContentLength = expected };
            var abortResponse = new AbortMultipartUploadResponse() { ContentLength = expected };

            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(initResponse));
            s3Mock.Setup(x => x.UploadPartAsync(It.IsAny<UploadPartRequest>(), default(CancellationToken))).ThrowsAsync(new AmazonS3Exception("", Amazon.Runtime.ErrorType.Unknown, "500", "id", HttpStatusCode.InternalServerError));
            s3Mock.Setup(x => x.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), default(CancellationToken))).Returns(Task.FromResult(abortResponse)).Verifiable();
            var exception = await Assert.ThrowsAsync<StorageInternalException>(async () => await s3.PutAsync("key", stream, ".pdf"));
            Assert.Contains("key", exception.Message);
        }

        [Fact]
        public async Task Delete_Success()
        {
            var deleteResponse = new DeleteObjectResponse() {  };
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default(CancellationToken))).Returns(Task.FromResult(deleteResponse)).Verifiable();
            await s3.DeleteAsync("key");
            Mock.VerifyAll();
        }

        [Fact]
        public async Task List_Success()
        {
            var listResponse = new ListObjectsV2Response() { };
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default(CancellationToken))).Returns(Task.FromResult(listResponse));
            Assert.IsType<List<StorageObject>>(await s3.ListAsync("prefix"));
            
        }

        [Fact]
        public async Task ListBucket_Success()
        {
            var listResponse = new ListObjectsV2Response() { };
            string bucket = "bucket";
            var s3Mock = new Mock<IAmazonS3>();
            var s3 = new S3(s3Mock.Object, bucket);
            s3Mock.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), default(CancellationToken))).Returns(Task.FromResult(listResponse));
            Assert.IsType<List<S3Object>>(await s3.ListBucketAsync("prefix"));

        }

        [Fact]
        public async Task GetStreamChunkAsync()
        {
            var stream = new MemoryStream(new byte[S3.MIN_PART_SIZE + 1]);
            var output = new MemoryStream();
            var bytesRead = await S3.GetStreamChunkAsync(stream, output , 1);
            Assert.Equal(1, bytesRead);
            Assert.Equal(output.Length,(long) 1);
        }

        [Fact]
        public void ConvertFileExtensionToContentType_WithoutDot()
        {
            var result = S3.ConvertFileExtensionToContentType("pdf");
            Assert.Equal("application/pdf", result);
        }
        [Fact]
        public void ConvertFileExtensionToContentType_WithDot()
        {
            var result = S3.ConvertFileExtensionToContentType(".pdf");
            Assert.Equal("application/pdf", result);
        }


        [Fact]
        public void BuildStorageException()
        {
           Assert.IsType<StorageInternalException>(S3.BuildStorageException(new AmazonS3Exception("", Amazon.Runtime.ErrorType.Unknown, "500", "id", HttpStatusCode.InternalServerError), "test-key"));
        }

        [Fact]
        public void CheckStreamNull()
        {
            Assert.Throws<ArgumentNullException>(() => S3.CheckStreamNullOrEmpty("p", null));
        }

        [Fact]
        public void CheckStreamEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => S3.CheckStreamNullOrEmpty("p", new MemoryStream()));
        }

        [Fact]
        public void CheckStringNull()
        {
            Assert.Throws<ArgumentNullException>(() => S3.CheckStringNullOrEmpty("p", null));
        }
        [Fact]
        public void CheckStringEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => S3.CheckStringNullOrEmpty("p", ""));
        }
        [Fact]
        public void CheckNumberLessThanZero()
        {
            Assert.Throws<ArgumentException>(() => S3.CheckNumberLTEZero("p", -1));
        }
        [Fact]
        public void CheckNumberZero()
        {
            Assert.Throws<ArgumentException>(() => S3.CheckNumberLTEZero("p", 0));
        }

    }
}