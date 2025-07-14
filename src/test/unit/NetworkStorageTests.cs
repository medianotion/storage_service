using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Storage;
using Storage.Configuration;
using Storage.Exceptions;
using Xunit;

namespace Tests
{
    public class NetworkStorageTests : IDisposable
    {
        private readonly string _testBasePath;
        private readonly NetworkOptions _options;
        private readonly NetworkStorage _storage;

        public NetworkStorageTests()
        {
            _testBasePath = Path.Combine(Path.GetTempPath(), "NetworkStorageTests", Guid.NewGuid().ToString());
            _options = new NetworkOptions
            {
                BasePath = _testBasePath,
                CreateDirectoriesIfNotExist = true,
                UseTransactionalCopy = true,
                OverwriteExisting = true,
                BufferSize = 4096 // Smaller buffer for testing
            };
            _storage = new NetworkStorage(_options);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }

        [Fact]
        public async Task PutAsync_CreatesFileWithContent()
        {
            // Arrange
            var key = "test/file.txt";
            var content = "Hello, World!";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            // Act
            var result = await _storage.PutAsync(key, stream, ".txt");

            // Assert
            Assert.Equal(content.Length, result);
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");
            Assert.True(File.Exists(filePath));
            var fileContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal(content, fileContent);
        }

        [Fact]
        public async Task GetAsync_ReturnsFileContent()
        {
            // Arrange
            var key = "test/file.txt";
            var content = "Hello, World!";
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, content);

            // Act
            using var stream = await _storage.GetAsync(key);
            using var reader = new StreamReader(stream);
            var result = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(content, result);
        }

        [Fact]
        public async Task GetAsync_FileNotFound_ThrowsStorageNotFoundException()
        {
            // Arrange
            var key = "nonexistent/file.txt";

            // Act & Assert
            await Assert.ThrowsAsync<StorageNotFoundException>(() => _storage.GetAsync(key));
        }

        [Fact]
        public async Task ExistsAsync_FileExists_ReturnsTrue()
        {
            // Arrange
            var key = "test/file.txt";
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, "content");

            // Act
            var result = await _storage.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_FileNotExists_ReturnsFalse()
        {
            // Arrange
            var key = "nonexistent/file.txt";

            // Act
            var result = await _storage.ExistsAsync(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_RemovesFile()
        {
            // Arrange
            var key = "test/file.txt";
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, "content");

            // Act
            await _storage.DeleteAsync(key);

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task DeleteAsync_FileNotExists_DoesNotThrow()
        {
            // Arrange
            var key = "nonexistent/file.txt";

            // Act & Assert (should not throw)
            await _storage.DeleteAsync(key);
        }

        [Fact]
        public async Task ListAsync_ReturnsFiles()
        {
            // Arrange
            var files = new[]
            {
                ("test/file1.txt", "content1"),
                ("test/file2.txt", "content2"),
                ("test/subfolder/file3.txt", "content3")
            };

            foreach (var (key, content) in files)
            {
                var filePath = Path.Combine(_testBasePath, key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await File.WriteAllTextAsync(filePath, content);
            }

            // Act
            var result = await _storage.ListAsync("test/");

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Key == "test/file1.txt");
            Assert.Contains(result, x => x.Key == "test/file2.txt");
            Assert.Contains(result, x => x.Key == "test/subfolder/file3.txt");
            
            // Check file properties
            var file1 = result.First(x => x.Key == "test/file1.txt");
            Assert.Equal(8, file1.SizeInBytes); // "content1".Length
        }

        [Fact]
        public async Task ListAsync_EmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            var prefix = "empty/";

            // Act
            var result = await _storage.ListAsync(prefix);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CopyAsync_CopiesFile()
        {
            // Arrange
            var sourceKey = "source/file.txt";
            var destKey = "dest/file.txt";
            var content = "Hello, World!";
            var sourceFilePath = Path.Combine(_testBasePath, "source", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFilePath));
            await File.WriteAllTextAsync(sourceFilePath, content);

            // Act
            await _storage.CopyAsync(sourceKey, "ignored", destKey);

            // Assert
            var destFilePath = Path.Combine(_testBasePath, "dest", "file.txt");
            Assert.True(File.Exists(destFilePath));
            var copiedContent = await File.ReadAllTextAsync(destFilePath);
            Assert.Equal(content, copiedContent);
        }

        [Fact]
        public async Task CopyAsync_SourceNotFound_ThrowsStorageNotFoundException()
        {
            // Arrange
            var sourceKey = "nonexistent/file.txt";
            var destKey = "dest/file.txt";

            // Act & Assert
            await Assert.ThrowsAsync<StorageNotFoundException>(() => _storage.CopyAsync(sourceKey, "ignored", destKey));
        }

        [Fact]
        public async Task PutAsync_WithTransactionalCopy_UsesTemporaryFile()
        {
            // Arrange
            var key = "test/file.txt";
            var content = "Hello, World!";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");

            // Act
            var result = await _storage.PutAsync(key, stream, ".txt");

            // Assert
            Assert.Equal(content.Length, result);
            Assert.True(File.Exists(filePath));
            
            // Verify no temp files left behind
            var directory = Path.GetDirectoryName(filePath);
            var tempFiles = Directory.GetFiles(directory, "*.tmp");
            Assert.Empty(tempFiles);
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NetworkStorage(null));
        }

        [Fact]
        public void Constructor_EmptyBasePath_ThrowsStorageConfigurationException()
        {
            // Arrange
            var options = new NetworkOptions { BasePath = "" };

            // Act & Assert
            Assert.Throws<StorageConfigurationException>(() => new NetworkStorage(options));
        }

        [Fact]
        public async Task PutAsync_InvalidKey_ThrowsArgumentException()
        {
            // Arrange - use null character which is definitely invalid
            var invalidKey = "file\0with\0null\0chars";
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.PutAsync(invalidKey, stream, ".txt"));
        }

        [Fact]
        public async Task PutAsync_NullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var key = "test/file.txt";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.PutAsync(key, null, ".txt"));
        }

        [Fact]
        public async Task GetAsync_InvalidKey_ThrowsArgumentException()
        {
            // Arrange - use null character which is definitely invalid
            var invalidKey = "file\0with\0null\0chars";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.GetAsync(invalidKey));
        }

        [Fact]
        public async Task PutAsync_LargeFile_HandlesCorrectly()
        {
            // Arrange
            var key = "test/largefile.bin";
            var size = 1024 * 1024; // 1MB
            var data = new byte[size];
            new Random().NextBytes(data);
            using var stream = new MemoryStream(data);

            // Act
            var result = await _storage.PutAsync(key, stream, ".bin");

            // Assert
            Assert.Equal(size, result);
            var filePath = Path.Combine(_testBasePath, "test", "largefile.bin");
            Assert.True(File.Exists(filePath));
            var fileInfo = new FileInfo(filePath);
            Assert.Equal(size, fileInfo.Length);
        }

        [Fact]
        public async Task CopyAsync_WithTransactionalCopy_UsesTemporaryFile()
        {
            // Arrange
            var sourceKey = "source/file.txt";
            var destKey = "dest/file.txt";
            var content = "Hello, World!";
            var sourceFilePath = Path.Combine(_testBasePath, "source", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFilePath));
            await File.WriteAllTextAsync(sourceFilePath, content);

            // Act
            await _storage.CopyAsync(sourceKey, "ignored", destKey);

            // Assert
            var destFilePath = Path.Combine(_testBasePath, "dest", "file.txt");
            Assert.True(File.Exists(destFilePath));
            
            // Verify no temp files left behind
            var destDirectory = Path.GetDirectoryName(destFilePath);
            var tempFiles = Directory.GetFiles(destDirectory, "*.tmp");
            Assert.Empty(tempFiles);
        }

        [Fact]
        public async Task GetAsync_ReturnsFileStreamForMemoryEfficiency()
        {
            // Arrange
            var key = "test/file.txt";
            var content = "Hello, World!";
            var filePath = Path.Combine(_testBasePath, "test", "file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, content);

            // Act
            using var stream = await _storage.GetAsync(key);

            // Assert - Verify we get a FileStream for memory efficiency, not MemoryStream
            Assert.IsType<FileStream>(stream);
        }
    }
}