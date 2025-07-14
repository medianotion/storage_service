using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Storage.Configuration;
using Storage.Exceptions;

namespace Storage
{
    internal class NetworkStorage : IStorage
    {
        private readonly string _basePath;
        private readonly NetworkOptions _options;
        private readonly int _bufferSize;

        public NetworkStorage(NetworkOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            
            if (string.IsNullOrEmpty(options.BasePath))
                throw new StorageConfigurationException("BasePath is required for network storage.");
                
            _basePath = options.BasePath;
            _bufferSize = options.BufferSize;
            
            // Validate base path exists or can be created
            EnsureBasePathExists();
        }

        public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckKey(key);
            
            var filePath = GetFilePath(key);
            
            try
            {
                if (!File.Exists(filePath))
                    throw new StorageNotFoundException(key);
                    
                // Return FileStream directly for memory-efficient streaming of large files
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, true);
                return await Task.FromResult(fileStream);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageAccessDeniedException(key, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new StorageNotFoundException(key, ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new StorageNotFoundException(key, ex);
            }
            catch (IOException ex)
            {
                throw new StorageInternalException($"Network storage operation failed for resource '{key}': {ex.Message}", ex);
            }
        }

        public async Task<long> PutAsync(string key, Stream stream, string fileExtension, bool gZipped = false, CancellationToken cancellationToken = default)
        {
            CheckKey(key);
            CheckStream(stream);
            
            var filePath = GetFilePath(key);
            var directory = Path.GetDirectoryName(filePath);
            
            try
            {
                // Ensure directory exists
                if (_options.CreateDirectoriesIfNotExist && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                if (_options.UseTransactionalCopy)
                {
                    return await PutWithTransactionAsync(filePath, stream, cancellationToken);
                }
                else
                {
                    return await PutDirectAsync(filePath, stream, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageAccessDeniedException(key, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new StorageNotFoundException(key, ex);
            }
            catch (IOException ex)
            {
                throw new StorageInternalException($"Network storage operation failed for resource '{key}': {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckKey(key);
            
            try
            {
                var filePath = GetFilePath(key);
                return await Task.FromResult(File.Exists(filePath));
            }
            catch (UnauthorizedAccessException)
            {
                return false; // Treat access denied as not exists for compatibility
            }
            catch (IOException)
            {
                return false; // Network issues treated as not exists
            }
        }

        public async Task<List<StorageObject>> ListAsync(string prefix, CancellationToken cancellationToken = default)
        {
            CheckKey(prefix);
            
            try
            {
                var searchPath = GetFilePath(prefix);
                var directory = Path.GetDirectoryName(searchPath);
                var searchPattern = Path.GetFileName(searchPath) + "*";
                
                if (!Directory.Exists(directory))
                    return new List<StorageObject>();
                
                var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
                var result = new List<StorageObject>();
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var relativePath = GetRelativeKey(file);
                    result.Add(new StorageObject(relativePath, fileInfo.Length, fileInfo.LastWriteTimeUtc));
                }
                
                return await Task.FromResult(result.OrderBy(x => x.Key).ToList());
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageAccessDeniedException(prefix, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new StorageNotFoundException(prefix, ex);
            }
            catch (IOException ex)
            {
                throw new StorageInternalException($"Network storage operation failed for resource '{prefix}': {ex.Message}", ex);
            }
        }

        public async Task CopyAsync(string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = default)
        {
            CheckKey(sourceKey);
            CheckKey(destinationKey);
            
            var sourceFilePath = GetFilePath(sourceKey);
            var destFilePath = GetFilePath(destinationKey);
            
            try
            {
                if (!File.Exists(sourceFilePath))
                    throw new StorageNotFoundException(sourceKey);
                
                var destDirectory = Path.GetDirectoryName(destFilePath);
                if (_options.CreateDirectoriesIfNotExist && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                
                if (_options.UseTransactionalCopy)
                {
                    await CopyWithTransactionAsync(sourceFilePath, destFilePath, cancellationToken);
                }
                else
                {
                    File.Copy(sourceFilePath, destFilePath, _options.OverwriteExisting);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageAccessDeniedException(sourceKey, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new StorageNotFoundException(sourceKey, ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new StorageNotFoundException(sourceKey, ex);
            }
            catch (IOException ex)
            {
                throw new StorageInternalException($"Network storage copy operation failed for resource '{sourceKey}': {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            CheckKey(key);
            
            var filePath = GetFilePath(key);
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                // Note: Not throwing if file doesn't exist for consistency with cloud providers
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageAccessDeniedException(key, ex);
            }
            catch (IOException ex)
            {
                throw new StorageInternalException($"Network storage delete operation failed for resource '{key}': {ex.Message}", ex);
            }
            
            await Task.CompletedTask;
        }

        private async Task<long> PutWithTransactionAsync(string filePath, Stream stream, CancellationToken cancellationToken)
        {
            var tempPath = filePath + _options.TempFileExtension;
            
            try
            {
                // Write to temp file first
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, true))
                {
                    await stream.CopyToAsync(fileStream, _bufferSize, cancellationToken);
                }
                
                // Atomic move
                if (File.Exists(filePath) && !_options.OverwriteExisting)
                {
                    File.Delete(tempPath);
                    throw new StorageInternalException($"File already exists and overwrite is disabled: {filePath}");
                }
                
                File.Move(tempPath, filePath);
                
                return new FileInfo(filePath).Length;
            }
            finally
            {
                // Clean up temp file if it still exists
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* Ignore cleanup failures */ }
                }
            }
        }

        private async Task<long> PutDirectAsync(string filePath, Stream stream, CancellationToken cancellationToken)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, true))
            {
                await stream.CopyToAsync(fileStream, _bufferSize, cancellationToken);
            }
            
            return new FileInfo(filePath).Length;
        }

        private async Task CopyWithTransactionAsync(string sourceFilePath, string destFilePath, CancellationToken cancellationToken)
        {
            var tempPath = destFilePath + _options.TempFileExtension;
            
            try
            {
                File.Copy(sourceFilePath, tempPath, true);
                
                if (File.Exists(destFilePath) && !_options.OverwriteExisting)
                {
                    File.Delete(tempPath);
                    throw new StorageInternalException($"Destination file already exists and overwrite is disabled: {destFilePath}");
                }
                
                File.Move(tempPath, destFilePath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* Ignore cleanup failures */ }
                }
            }
            
            await Task.CompletedTask;
        }

        private string GetFilePath(string key)
        {
            // Normalize the key to use platform-appropriate path separators
            var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            
            // Remove leading separators
            normalizedKey = normalizedKey.TrimStart(Path.DirectorySeparatorChar);
            
            return Path.Combine(_basePath, normalizedKey);
        }

        private string GetRelativeKey(string filePath)
        {
            var relativePath = Path.GetRelativePath(_basePath, filePath);
            // Convert back to forward slashes for consistency with cloud storage keys
            return relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }

        private void EnsureBasePathExists()
        {
            try
            {
                if (!Directory.Exists(_basePath))
                {
                    if (_options.CreateDirectoriesIfNotExist)
                    {
                        Directory.CreateDirectory(_basePath);
                    }
                    else
                    {
                        throw new StorageConfigurationException($"Base path does not exist: {_basePath}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new StorageConfigurationException($"Access denied to base path: {_basePath}", ex);
            }
            catch (IOException ex)
            {
                throw new StorageConfigurationException($"Cannot access base path: {_basePath}: {ex.Message}", ex);
            }
        }

        private static void CheckKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            
            // Validate key doesn't contain invalid path characters, but allow forward/back slashes for hierarchy
            var invalidChars = Path.GetInvalidPathChars().Where(c => c != '/' && c != '\\').ToArray();
            if (key.IndexOfAny(invalidChars) >= 0)
                throw new ArgumentException($"Key contains invalid characters: {key}", nameof(key));
        }

        private static void CheckStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");
        }
    }
}