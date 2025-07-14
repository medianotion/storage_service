# Storage Service for .NET Standard

A modern storage service library for .NET Standard applications with multiple storage providers, including AWS S3 and network file shares. This library supports dependency injection, multiple authentication methods, and provides a comprehensive set of features for file storage operations.

## Features

- **Provider Pattern**: Supports dependency injection and modern .NET configuration patterns
- **Multiple Storage Providers**: AWS S3 and Network file share support
- **Multiple Authentication Types**: IAM roles, access keys, STS tokens, and custom authentication
- **Transactional Operations**: Atomic file operations with rollback support (Network)
- **Multipart Uploads**: Automatic multipart handling for files larger than 5MB (S3)
- **Content-Type Detection**: Automatic content-type detection based on file extensions
- **Comprehensive Exception Handling**: Custom exception hierarchy that abstracts provider dependencies
- **CancellationToken Support**: Full async/await support with proper cancellation
- **Comprehensive Testing**: 115+ unit tests covering all functionality

## Installation

Add the storage service to your project and configure it using dependency injection.

## Configuration

### Using appsettings.json

```json
{
  "StorageService": {
    "DefaultProvider": "S3",
    "S3": {
      "Bucket": "my-storage-bucket",
      "Region": "us-east-1",
      "MaxRetries": 3,
      "Credentials": {
        "AuthenticationType": "None"
      }
    }
  }
}
```

### Authentication Types

#### IAM Role (Recommended)
```json
{
  "StorageService": {
    "S3": {
      "Credentials": {
        "AuthenticationType": "None"
      }
    }
  }
}
```

#### Access Key Authentication
```json
{
  "StorageService": {
    "S3": {
      "Credentials": {
        "AuthenticationType": "AccessKey",
        "AccessKey": "AKIA...",
        "SecretKey": "your-secret-key"
      }
    }
  }
}
```

#### STS Token Authentication
```json
{
  "StorageService": {
    "S3": {
      "Credentials": {
        "AuthenticationType": "STS",
        "AccessKey": "ASIA...",
        "SecretKey": "your-secret-key",
        "SessionToken": "your-session-token"
      }
    }
  }
}
```

## Network Storage Provider

The library also supports network file shares using System.IO for local and network storage scenarios.

### Using appsettings.json for Network Storage

```json
{
  "StorageService": {
    "DefaultProvider": "Network",
    "Network": {
      "BasePath": "\\\\server\\share\\storage",
      "Username": "serviceaccount",
      "Password": "password123",
      "Domain": "corporate",
      "CreateDirectoriesIfNotExist": true,
      "UseTransactionalCopy": true,
      "BufferSize": 81920
    }
  }
}
```

### Network Storage Examples

#### Local Directory
```json
{
  "StorageService": {
    "DefaultProvider": "Network",
    "Network": {
      "BasePath": "/var/storage",
      "CreateDirectoriesIfNotExist": true
    }
  }
}
```

#### UNC Share with Authentication
```json
{
  "StorageService": {
    "DefaultProvider": "Network",
    "Network": {
      "BasePath": "\\\\fileserver\\documents",
      "Username": "domain\\serviceuser",
      "Password": "securepassword",
      "Domain": "corporate"
    }
  }
}
```

#### Mapped Network Drive
```json
{
  "StorageService": {
    "DefaultProvider": "Network",
    "Network": {
      "BasePath": "Z:\\shared\\storage",
      "UseTransactionalCopy": true,
      "OverwriteExisting": true
    }
  }
}
```

## Dependency Injection Setup

### Method 1: Using IConfiguration
```csharp
services.AddStorageService(configuration);
```

### Method 2: Using Action Configuration

#### S3 Provider
```csharp
services.AddStorageService(options =>
{
    options.DefaultProvider = "S3";
    options.S3.Bucket = "my-bucket";
    options.S3.Region = "us-west-2";
    options.S3.Credentials.AuthenticationType = "AccessKey";
    options.S3.Credentials.AccessKey = "AKIA...";
    options.S3.Credentials.SecretKey = "secret";
});
```

#### Network Provider
```csharp
services.AddStorageService(options =>
{
    options.DefaultProvider = "Network";
    options.Network.BasePath = @"\\fileserver\storage";
    options.Network.Username = "serviceuser";
    options.Network.Password = "password";
    options.Network.Domain = "corporate";
    options.Network.CreateDirectoriesIfNotExist = true;
});
```

### Method 3: Using StorageOptions Object
```csharp
var storageOptions = new StorageOptions
{
    S3 = new S3Options
    {
        Bucket = "my-bucket",
        Region = "eu-west-1"
    }
};
services.AddStorageService(storageOptions);
```

## Usage

### ASP.NET Core Applications (With DI)

#### Startup Configuration
```csharp
// Program.cs or Startup.cs
using Storage.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add storage service using configuration
builder.Services.AddStorageService(builder.Configuration);

// Or configure programmatically
builder.Services.AddStorageService(options =>
{
    options.DefaultProvider = "S3";
    options.S3.Bucket = "my-web-app-storage";
    options.S3.Region = "us-west-2";
    options.S3.Credentials.AuthenticationType = "None"; // Use IAM roles
});

var app = builder.Build();
```

#### File Upload Controller
```csharp
using Microsoft.AspNetCore.Mvc;
using Storage;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IStorage _storage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IStorage storage, ILogger<FilesController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var key = $"uploads/{Guid.NewGuid()}{fileExtension}";
            
            using var stream = file.OpenReadStream();
            var size = await _storage.PutAsync(key, stream, fileExtension);
            
            _logger.LogInformation("File uploaded successfully: {Key}, Size: {Size}", key, size);
            
            return Ok(new { Key = key, Size = size, FileName = file.FileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            return StatusCode(500, "Error uploading file");
        }
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> DownloadFile(string key)
    {
        try
        {
            if (!await _storage.ExistsAsync(key))
                return NotFound();

            var stream = await _storage.GetAsync(key);
            var fileName = Path.GetFileName(key);
            
            return File(stream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {Key}", key);
            return StatusCode(500, "Error downloading file");
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteFile(string key)
    {
        try
        {
            await _storage.DeleteAsync(key);
            _logger.LogInformation("File deleted: {Key}", key);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Key}", key);
            return StatusCode(500, "Error deleting file");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListFiles([FromQuery] string prefix = "")
    {
        try
        {
            var files = await _storage.ListAsync(prefix);
            return Ok(files.Select(f => new 
            { 
                f.Key, 
                f.SizeInBytes, 
                f.LastModifiedUtc 
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files with prefix: {Prefix}", prefix);
            return StatusCode(500, "Error listing files");
        }
    }
}
```

#### Document Service Example
```csharp
using Storage;
using Storage.Exceptions;

public interface IDocumentService
{
    Task<string> SaveDocumentAsync(Stream document, string fileName);
    Task<Stream> GetDocumentAsync(string documentId);
    Task<bool> DeleteDocumentAsync(string documentId);
    Task<IEnumerable<DocumentInfo>> GetUserDocumentsAsync(string userId);
}

public class DocumentService : IDocumentService
{
    private readonly IStorage _storage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IStorage storage, ILogger<DocumentService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string> SaveDocumentAsync(Stream document, string fileName)
    {
        var documentId = Guid.NewGuid().ToString();
        var fileExtension = Path.GetExtension(fileName);
        var key = $"documents/{documentId}/{fileName}";

        try
        {
            var size = await _storage.PutAsync(key, document, fileExtension);
            _logger.LogInformation("Document saved: {DocumentId}, Size: {Size}", documentId, size);
            return documentId;
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error saving document: {DocumentId}", documentId);
            throw new InvalidOperationException("Failed to save document", ex);
        }
    }

    public async Task<Stream> GetDocumentAsync(string documentId)
    {
        try
        {
            var files = await _storage.ListAsync($"documents/{documentId}/");
            if (!files.Any())
                throw new FileNotFoundException($"Document not found: {documentId}");

            var documentKey = files.First().Key;
            return await _storage.GetAsync(documentKey);
        }
        catch (StorageNotFoundException)
        {
            throw new FileNotFoundException($"Document not found: {documentId}");
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error retrieving document: {DocumentId}", documentId);
            throw new InvalidOperationException("Failed to retrieve document", ex);
        }
    }

    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        try
        {
            var files = await _storage.ListAsync($"documents/{documentId}/");
            foreach (var file in files)
            {
                await _storage.DeleteAsync(file.Key);
            }
            
            _logger.LogInformation("Document deleted: {DocumentId}", documentId);
            return true;
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error deleting document: {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<IEnumerable<DocumentInfo>> GetUserDocumentsAsync(string userId)
    {
        try
        {
            var files = await _storage.ListAsync($"documents/");
            return files.Select(f => new DocumentInfo
            {
                DocumentId = ExtractDocumentId(f.Key),
                FileName = Path.GetFileName(f.Key),
                Size = f.SizeInBytes,
                LastModified = f.LastModifiedUtc
            });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error listing user documents: {UserId}", userId);
            return Enumerable.Empty<DocumentInfo>();
        }
    }

    private string ExtractDocumentId(string key)
    {
        // Extract document ID from key like "documents/{documentId}/filename.pdf"
        var parts = key.Split('/');
        return parts.Length >= 2 ? parts[1] : string.Empty;
    }
}

public class DocumentInfo
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}

// Register the service in Program.cs
builder.Services.AddScoped<IDocumentService, DocumentService>();
```

#### Background Service for File Processing
```csharp
using Storage;

public class FileProcessingService : BackgroundService
{
    private readonly IStorage _storage;
    private readonly ILogger<FileProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public FileProcessingService(
        IStorage storage, 
        ILogger<FileProcessingService> logger,
        IServiceProvider serviceProvider)
    {
        _storage = storage;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingFiles(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file processing service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessPendingFiles(CancellationToken cancellationToken)
    {
        var pendingFiles = await _storage.ListAsync("pending/");
        
        foreach (var file in pendingFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ProcessFile(file.Key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {Key}", file.Key);
            }
        }
    }

    private async Task ProcessFile(string key, CancellationToken cancellationToken)
    {
        using var stream = await _storage.GetAsync(key, cancellationToken);
        
        // Process the file (resize image, convert document, etc.)
        using var processedStream = await ProcessFileContent(stream);
        
        // Save processed file
        var processedKey = key.Replace("pending/", "processed/");
        await _storage.PutAsync(processedKey, processedStream, Path.GetExtension(key), false, cancellationToken);
        
        // Delete original pending file
        await _storage.DeleteAsync(key, cancellationToken);
        
        _logger.LogInformation("File processed: {Key} -> {ProcessedKey}", key, processedKey);
    }

    private async Task<Stream> ProcessFileContent(Stream input)
    {
        // Implement your file processing logic here
        var output = new MemoryStream();
        await input.CopyToAsync(output);
        output.Position = 0;
        return output;
    }
}

// Register the background service in Program.cs
builder.Services.AddHostedService<FileProcessingService>();
```

### Using with Generic Services
```csharp
public class MyService
{
    private readonly IStorage _storage;
    
    public MyService(IStorage storage)
    {
        _storage = storage;
    }

    public async Task<bool> BackupUserDataAsync(string userId, object userData)
    {
        try
        {
            var json = JsonSerializer.Serialize(userData);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            
            var key = $"backups/{userId}/{DateTime.UtcNow:yyyy-MM-dd}.json";
            await _storage.PutAsync(key, stream, ".json");
            
            return true;
        }
        catch (StorageException)
        {
            return false;
        }
    }
}
```

### Console Application Examples (Without DI)

#### S3 Storage Console Example
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Storage;
using Storage.Configuration;
using Storage.Providers;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure S3 options
        var s3Options = new S3Options
        {
            Bucket = "my-storage-bucket",
            Region = "us-east-1",
            Credentials = new CredentialsOptions
            {
                AuthenticationType = "AccessKey",
                AccessKey = "AKIA...",
                SecretKey = "your-secret-key"
            }
        };

        // Create storage provider directly
        var provider = new S3StorageProvider();
        var storage = provider.CreateStorage(s3Options);

        // Use the storage service
        var content = "Hello from console app!";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        
        // Put a file
        var size = await storage.PutAsync("test/console-file.txt", stream, ".txt");
        Console.WriteLine($"Uploaded file, size: {size} bytes");

        // Get the file back
        using var retrievedStream = await storage.GetAsync("test/console-file.txt");
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        Console.WriteLine($"Retrieved content: {retrievedContent}");

        // Check if file exists
        var exists = await storage.ExistsAsync("test/console-file.txt");
        Console.WriteLine($"File exists: {exists}");

        // List files
        var files = await storage.ListAsync("test/");
        Console.WriteLine($"Found {files.Count} files in test/ folder");

        // Delete the file
        await storage.DeleteAsync("test/console-file.txt");
        Console.WriteLine("File deleted");
    }
}
```

#### Network Storage Console Example
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Storage;
using Storage.Configuration;
using Storage.Providers;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Network storage options
        var networkOptions = new NetworkOptions
        {
            BasePath = @"C:\temp\storage", // or "/tmp/storage" on Linux/Mac
            CreateDirectoriesIfNotExist = true,
            UseTransactionalCopy = true,
            OverwriteExisting = true
        };

        // Create storage provider directly
        var provider = new NetworkStorageProvider();
        var storage = provider.CreateStorage(networkOptions);

        // Use the storage service
        var content = "Hello from network storage console app!";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        
        // Put a file
        var size = await storage.PutAsync("documents/readme.txt", stream, ".txt");
        Console.WriteLine($"Saved file, size: {size} bytes");

        // Get the file back
        using var retrievedStream = await storage.GetAsync("documents/readme.txt");
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        Console.WriteLine($"Retrieved content: {retrievedContent}");

        // Copy the file
        await storage.CopyAsync("documents/readme.txt", "", "documents/readme-copy.txt");
        Console.WriteLine("File copied successfully");

        // List all files
        var files = await storage.ListAsync("documents/");
        Console.WriteLine($"Found {files.Count} files:");
        foreach (var file in files)
        {
            Console.WriteLine($"  - {file.Key} ({file.SizeInBytes} bytes, modified: {file.LastModifiedUtc})");
        }

        // Clean up
        await storage.DeleteAsync("documents/readme.txt");
        await storage.DeleteAsync("documents/readme-copy.txt");
        Console.WriteLine("Files deleted");
    }
}
```

#### Network Storage with UNC Share Example
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Storage;
using Storage.Configuration;
using Storage.Providers;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Network storage for UNC share
        var networkOptions = new NetworkOptions
        {
            BasePath = @"\\fileserver\shared\storage",
            Username = "domain\\serviceuser",
            Password = "securepassword",
            Domain = "corporate",
            CreateDirectoriesIfNotExist = true,
            UseTransactionalCopy = true
        };

        try
        {
            // Create storage provider directly
            var provider = new NetworkStorageProvider();
            var storage = provider.CreateStorage(networkOptions);

            Console.WriteLine("Connected to network share successfully");

            // Upload a file
            var content = "Data from network share example";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var size = await storage.PutAsync("reports/data.txt", stream, ".txt");
            Console.WriteLine($"Uploaded to network share, size: {size} bytes");

            // Verify upload
            var exists = await storage.ExistsAsync("reports/data.txt");
            Console.WriteLine($"File exists on network share: {exists}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing network share: {ex.Message}");
        }
    }
}
```

### Basic Operations (DI Examples)

#### Get a file as a stream
```csharp
Stream fileStream = await _storage.GetAsync("your/path/to/filekey");
```

#### Put a file
Content-Type is auto-discovered by file extension:
```csharp
long sizeStored = await _storage.PutAsync("your/destination/path/to/filekey", fileStream, ".pdf");
```

#### Put a file with gzip encoding
FileStream must be gzipped by consumer:
```csharp
long sizeStored = await _storage.PutAsync("your/destination/path/to/filekey", fileStream, ".jpg", true);
```

#### Check if file exists
```csharp
bool exists = await _storage.ExistsAsync("your/path/to/filekey");
```

#### List all files in a bucket prefix/folder/directory
S3 returns a max of 1000 results per call. This method will continue calling S3 beyond 1000 results until all files in the prefix are returned. The StorageObject returns a slimmed down list of objects containing file name, size in bytes, and last updated UTC:
```csharp
List<StorageObject> allFiles = await _storage.ListAsync("your/path/to/filekey");
```

#### Copy file
```csharp
await _storage.CopyAsync("your/path/to/filekey", "destinationBucket", "your/destination/filekey");
```

#### Delete file
```csharp
await _storage.DeleteAsync("your/path/to/filekey");
```

#### CancellationToken Support
All async methods support CancellationToken for proper cancellation:
```csharp
var cts = new CancellationTokenSource();
Stream fileStream = await _storage.GetAsync("your/path/to/filekey", cts.Token);
```

## Exception Handling

The library provides a comprehensive custom exception hierarchy that abstracts AWS SDK dependencies:

- `StorageNotFoundException`: Resource not found (404)
- `StorageAccessDeniedException`: Access denied or unauthorized (401, 403)
- `StorageAuthenticationException`: Authentication failures
- `StorageConfigurationException`: Configuration errors
- `StorageUnavailableException`: Service temporarily unavailable (503, 429)
- `StorageTimeoutException`: Request timeout (408)
- `StorageInternalException`: Internal server errors (500, 502)

## Security

- **HTTPS Only**: All communications use HTTPS (HTTP is disabled)
- **Credential Security**: Supports multiple authentication patterns including IAM roles for enhanced security

## Performance

- **Multipart Uploads**: Files larger than 5MB are automatically uploaded using multipart uploads for better performance and reliability (S3)
- **Connection Pooling**: Uses AWS SDK connection pooling for optimal performance (S3)
- **Retry Logic**: Built-in retry logic with configurable retry counts (S3)
- **Transactional Copy**: Atomic file operations using temporary files for data integrity (Network)
- **Buffered I/O**: Configurable buffer sizes for optimal network file share performance (Network)

## Network Storage Provider Features

### Supported Storage Types
- **Local Directories**: `/var/storage`, `C:\Storage`
- **UNC Shares**: `\\server\share\folder`
- **Mapped Network Drives**: `Z:\storage`
- **Any System.IO accessible path**

### Network Provider Benefits
- **Atomic Operations**: Uses temporary files for safe writes
- **Directory Auto-Creation**: Automatically creates directory structure
- **Flexible Authentication**: Supports domain credentials
- **Path Normalization**: Handles both forward and backslash separators
- **Error Mapping**: Maps I/O exceptions to storage exceptions
- **Memory Efficient**: Configurable buffer sizes for optimal performance

### Network Provider Configuration Options
- `BasePath`: Base directory path (UNC, local, or mapped drive)
- `Username/Password/Domain`: Network share authentication
- `CreateDirectoriesIfNotExist`: Auto-create directory structure
- `UseTransactionalCopy`: Enable atomic operations with temp files
- `OverwriteExisting`: Allow overwriting existing files
- `BufferSize`: I/O buffer size for performance tuning
- `TempFileExtension`: Extension for temporary files during transactions