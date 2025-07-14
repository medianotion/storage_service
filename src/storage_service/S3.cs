using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Storage.Configuration;
using Storage.Exceptions;


namespace Storage
{
    internal class S3 : IStorage
    {

        private readonly string _bucket;
        private readonly IAmazonS3 _amazonS3Client;

        internal const int MIN_PART_SIZE = 5242880; // 5 MB
        internal const int MAX_PART_SIZE = 104857600; // 100 MB
        internal const int MAX_PARTS = 1000; // AWS allows 10,000 but we keep it lower for performance
        internal const int BUFFER_SIZE = 81920; // 80KB buffer for stream operations
        internal const int MAX_KEYS = 1000;
        internal const string DEFAULT_REGION = "us-east-1";
        internal const int DEFAULT_RETRY = 3;

        // New constructor for provider pattern
        public S3(S3Options options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            CheckStringNullOrEmpty(nameof(options.Bucket), options.Bucket);
            _bucket = options.Bucket;
            _amazonS3Client = CreateS3Client(options);
        }

        // used for unit testing
        [ExcludeFromCodeCoverage]
        internal S3(IAmazonS3 client, string bucket)
        {
            _bucket = bucket;
            _amazonS3Client = client;
        }


        internal static AmazonS3Config CreateConfig(string region = DEFAULT_REGION, int retries = DEFAULT_RETRY)
        {

            CheckStringNullOrEmpty(nameof(region), region);
            CheckNumberLTEZero(nameof(retries), retries);

            return new AmazonS3Config
            {
                MaxErrorRetry = retries,
                UseHttp = false,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };


        }

        private static IAmazonS3 CreateS3Client(S3Options options)
        {
            var config = new AmazonS3Config
            {
                MaxErrorRetry = options.MaxRetries,
                UseHttp = false,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            // Handle different authentication types
            switch (options.Credentials.AuthenticationType)
            {
                case "STS":
                    if (!string.IsNullOrEmpty(options.Credentials.SessionToken))
                        return new AmazonS3Client(options.Credentials.AccessKey, options.Credentials.SecretKey, options.Credentials.SessionToken, config);
                    break;
                case "AccessKey":
                    if (!string.IsNullOrEmpty(options.Credentials.AccessKey))
                        return new AmazonS3Client(options.Credentials.AccessKey, options.Credentials.SecretKey, config);
                    break;
                case "None":
                default:
                    // Use default AWS credential chain (IAM roles, environment variables, etc.)
                    return new AmazonS3Client(config);
            }

            // Fallback to default credential chain
            return new AmazonS3Client(config);
        }


        public async Task CopyAsync(string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = default)
        {

            CheckStringNullOrEmpty(nameof(sourceKey), sourceKey);
            CheckStringNullOrEmpty(nameof(destinationBucket), destinationBucket);
            CheckStringNullOrEmpty(nameof(destinationKey), destinationKey);

            try
            {
                var request = new CopyObjectRequest
                {
                    SourceBucket = _bucket,
                    SourceKey = sourceKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey
                };

                request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;

                await _amazonS3Client.CopyObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, sourceKey);
            }
        }


        public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckStringNullOrEmpty(nameof(key), key);
            try
            {
                var request = new GetObjectRequest()
                {
                    BucketName = _bucket,
                    Key = key
                };

                var response = await _amazonS3Client.GetObjectAsync(request, cancellationToken);
                var ms = new MemoryStream();
                await response.ResponseStream.CopyToAsync(ms, BUFFER_SIZE, cancellationToken);
                ms.Position = 0;
                response.Dispose();
                return ms;
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckStringNullOrEmpty(nameof(key), key);

            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucket,
                Key = key
            };

            try
            {
                await _amazonS3Client.GetObjectMetadataAsync(request, cancellationToken);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                throw BuildStorageException(ex, key);
            }

        }


        internal async Task<List<S3Object>> ListBucketAsync(string prefix, CancellationToken cancellationToken = default)
        {

            CheckStringNullOrEmpty(nameof(prefix), prefix);

            var request = new ListObjectsV2Request()
            {
                BucketName = _bucket,
                Prefix = prefix,
                MaxKeys = MAX_KEYS
            };
            var results = new List<S3Object>();

            try
            {

                do
                {
                    var response = await _amazonS3Client.ListObjectsV2Async(request, cancellationToken);
                    results.AddRange(response.S3Objects);

                    if (response.IsTruncated)
                        request.ContinuationToken = response.NextContinuationToken;
                    else
                        request = null;
                } while (request != null);

                return results;

            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, prefix);
            }
        }

        /// <summary>
        /// Lists the contents key, size in bytes and lastmodified.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        public async Task<List<StorageObject>> ListAsync(string prefix, CancellationToken cancellationToken = default)
        {
            CheckStringNullOrEmpty(nameof(prefix), prefix);

            var result = new List<StorageObject>();
            
            foreach (var s3Object in await ListBucketAsync(prefix, cancellationToken))
                result.Add(new StorageObject(s3Object.Key, s3Object.Size, s3Object.LastModified));

            return result;
        }


        public async Task<long> PutAsync(string key, Stream stream, string fileExtension, bool gZipped = false, CancellationToken cancellationToken = default)
        {
            CheckStringNullOrEmpty(nameof(key), key);
            CheckStreamNullOrEmpty(nameof(stream), stream);
            CheckStringNullOrEmpty(nameof(fileExtension), fileExtension);

            string contentType = ConvertFileExtensionToContentType(fileExtension);

            try
            {

                if (stream.Length <= MIN_PART_SIZE)
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucket,
                        Key = key,
                        ContentType = contentType,
                        InputStream = stream
                    };
                    if (gZipped)
                        putRequest.Headers.ContentEncoding = "gzip";
                    putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;

                    var response = await _amazonS3Client.PutObjectAsync(putRequest, cancellationToken);

                    return response.ContentLength;
                }
                else
                {
                    int optimalPartSize = CalculateOptimalPartSize(stream.Length);
                    return await PutInPartsAsync(key, stream, contentType, optimalPartSize, gZipped, cancellationToken);
                }

            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }
        }


        internal async Task<long> PutInPartsAsync(string key, Stream fileStream, string contentType, int partSize,
            bool gZipped = false, CancellationToken cancellationToken = default)
        {
            fileStream.Position = 0;

            var uploadResponses = new List<object>();
            var contentLength = fileStream.Length;
            long offset = 0;
            string uploadId = null;
            
            // Calculate total parts for validation
            long totalParts = (long)Math.Ceiling((double)contentLength / partSize);
            ValidatePartSize(partSize, false, totalParts);

            try
            {
                uploadId = await InitiateMultipartUploadAsync(key, contentType, gZipped, cancellationToken);

                for (var partNumber = 1; offset < contentLength; partNumber++)
                {
                    // Use disposable MemoryStream for each part to minimize memory footprint
                    using (var uploadStream = new MemoryStream())
                    {
                        long remainingBytes = contentLength - offset;
                        int currentPartSize = (int)Math.Min(partSize, remainingBytes);
                        bool isLastPart = partNumber == totalParts;
                        
                        // Validate this part size
                        ValidatePartSize(currentPartSize, isLastPart, totalParts);
                        
                        int actualBytesRead = await GetStreamChunkAsync(fileStream, uploadStream, currentPartSize, cancellationToken);
                        
                        if (actualBytesRead == 0)
                            break; // End of stream reached
                            
                        uploadStream.Position = 0;
                        
                        try
                        {
                            uploadResponses.Add(await PutPartAsync(key, uploadStream, uploadId, partNumber, cancellationToken));
                        }
                        catch (AmazonS3Exception ex)
                        {
                            throw new StorageInternalException($"Failed to upload part {partNumber} for resource '{key}': {ex.Message}. Part size: {actualBytesRead} bytes.", ex);
                        }
                        
                        offset += actualBytesRead;
                    }
                }
                await CompleteMultipartUploadAsync(key, uploadId, uploadResponses, cancellationToken);
                return contentLength;
            }
            catch (Exception ex)
            {
                // Always attempt to abort the multipart upload on failure
                if (!string.IsNullOrEmpty(uploadId))
                {
                    try
                    {
                        await AbortMultipartUploadAsync(key, uploadId, cancellationToken);
                    }
                    catch (Exception abortEx)
                    {
                        // Log the abort failure but don't let it mask the original exception
                        // In a real application, you'd use ILogger here
                    }
                }
                
                // Re-throw with context about multipart upload failure
                if (ex is AmazonS3Exception s3Ex)
                {
                    throw BuildStorageException(s3Ex, key);
                }
                
                throw new StorageInternalException($"Multipart upload failed for resource '{key}': {ex.Message}", ex);
            }
        }

        internal async Task AbortMultipartUploadAsync(string key, string uploadId, CancellationToken cancellationToken = default)
        {
            var abortRequest = new AbortMultipartUploadRequest
            {
                BucketName = _bucket,
                Key = key,
                UploadId = uploadId
            };
            try
            {
                await _amazonS3Client.AbortMultipartUploadAsync(abortRequest, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }

        }



        internal async Task CompleteMultipartUploadAsync(string key, string uploadId, IEnumerable<object> uploadResponses, CancellationToken cancellationToken = default)
        {
            var responses = new List<UploadPartResponse>();
            foreach (var response in uploadResponses)
                responses.Add((UploadPartResponse)response);

            var compRequest = new CompleteMultipartUploadRequest();
            compRequest.AddPartETags(responses);
            compRequest.BucketName = _bucket;
            compRequest.Key = key;
            compRequest.UploadId = uploadId;
            try
            {
                await _amazonS3Client.CompleteMultipartUploadAsync(compRequest, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }
        }


        internal async Task<string> InitiateMultipartUploadAsync(string key, string contentType, bool gZipped = false, CancellationToken cancellationToken = default)
        {
            var initRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucket,
                ContentType = contentType,
                Key = key
            };
            if (gZipped)
                initRequest.Headers.ContentEncoding = "gzip";
            initRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;

            try
            {
                var response = await _amazonS3Client.InitiateMultipartUploadAsync(initRequest, cancellationToken);
                return response.UploadId;
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }
            
        }


        internal async Task<object> PutPartAsync(string key, Stream stream, string uploadId, int partNumber, CancellationToken cancellationToken = default)
        {
            stream.Position = 0;
            var uploadRequest = new UploadPartRequest
            {
                BucketName = _bucket,
                Key = key,
                UploadId = uploadId,
                PartNumber = partNumber,
                InputStream = stream,
                PartSize = stream.Length
            };
            uploadRequest.InputStream = stream;

            try
            {
                return await _amazonS3Client.UploadPartAsync(uploadRequest, cancellationToken);
            }
            catch(AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckStringNullOrEmpty(nameof(key), key);
            DeleteObjectRequest request = new DeleteObjectRequest();
            request.BucketName = _bucket;
            request.Key = key;

            try
            {
                await _amazonS3Client.DeleteObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                throw BuildStorageException(ex, key);
            }

        }

        internal static async Task<int> GetStreamChunkAsync(Stream source, Stream destination, int chunkSize, CancellationToken cancellationToken = default)
        {
            // Use smaller buffer for reading to reduce memory pressure
            var bufferSize = Math.Min(BUFFER_SIZE, chunkSize);
            var buffer = new byte[bufferSize];
            var totalBytesRead = 0;
            
            // Continue reading until we have the full chunk or reach end of stream
            while (totalBytesRead < chunkSize)
            {
                int bytesToRead = Math.Min(bufferSize, chunkSize - totalBytesRead);
                int bytesRead = await source.ReadAsync(buffer, 0, bytesToRead, cancellationToken);
                
                if (bytesRead == 0)
                    break; // End of stream reached
                    
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;
            }
            
            return totalBytesRead;
        }

        internal static void CheckStreamNullOrEmpty(string parameter, Stream stream)
        {
            if (stream == null || stream.Length == 0) 
                throw new ArgumentNullException(parameter, $"{parameter} stream is null or empty.");
        }

        internal static void CheckStringNullOrEmpty(string parameter, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentNullException(parameter, $"{parameter} is null or empty.");
        }
        internal static void CheckNumberLTEZero(string parameter, int value)
        {
            if (value <= 0)
                throw new ArgumentException($"{parameter} is less than or equal to 0.", parameter);
        }

        internal static Storage.Exceptions.StorageException BuildStorageException(AmazonS3Exception exception, string resourceKey)
        {
            switch (exception.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new StorageNotFoundException(resourceKey, exception);
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                    return new StorageAccessDeniedException(resourceKey, exception);
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.TooManyRequests:
                    return new StorageUnavailableException($"Storage service temporarily unavailable for resource '{resourceKey}': {exception.Message}", exception);
                case HttpStatusCode.RequestTimeout:
                    return new StorageTimeoutException($"Storage operation timed out for resource '{resourceKey}': {exception.Message}", exception);
                default:
                    return new StorageInternalException($"Storage operation failed for resource '{resourceKey}': {exception.Message}", exception);
            }
        }

        internal static int CalculateOptimalPartSize(long fileSize)
        {
            if (fileSize <= MIN_PART_SIZE)
                return MIN_PART_SIZE;

            // Calculate part size to keep total parts under MAX_PARTS
            int calculatedPartSize = (int)Math.Ceiling((double)fileSize / MAX_PARTS);
            
            // Ensure part size is at least minimum and doesn't exceed maximum
            calculatedPartSize = Math.Max(MIN_PART_SIZE, calculatedPartSize);
            calculatedPartSize = Math.Min(MAX_PART_SIZE, calculatedPartSize);
            
            // Round up to nearest MB for cleaner part sizes
            const int oneMB = 1024 * 1024;
            calculatedPartSize = ((calculatedPartSize + oneMB - 1) / oneMB) * oneMB;
            
            return calculatedPartSize;
        }

        internal static void ValidatePartSize(long partSize, bool isLastPart, long totalParts)
        {
            if (totalParts > 10000)
                throw new StorageConfigurationException($"File requires {totalParts} parts, which exceeds AWS S3 limit of 10,000 parts per upload.");
            
            if (!isLastPart && partSize < MIN_PART_SIZE)
                throw new StorageConfigurationException($"Part size {partSize} bytes is below minimum required size of {MIN_PART_SIZE} bytes (5MB).");
            
            if (partSize > MAX_PART_SIZE)
                throw new StorageConfigurationException($"Part size {partSize} bytes exceeds maximum allowed size of {MAX_PART_SIZE} bytes (100MB).");
        }

        internal static string ConvertFileExtensionToContentType(string extension)
        {
            CheckStringNullOrEmpty(nameof(extension), extension);

            if (!extension.StartsWith("."))
                extension = "." + extension;

            string value;
            if (ContentTypes.match.TryGetValue(extension, out value))
                return value;
            else
                return "binary/octet-stream";
        }

    }
}