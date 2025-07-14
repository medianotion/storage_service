# Storage Service for .NET Standard

A modern storage service library for .NET Standard applications.

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

### Injecting the Storage Service
```csharp
public class MyService
{
    private readonly IStorage _storage;
    
    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}
```

### Get a file as a stream
```csharp
Stream fileStream = await _storage.GetAsync("your/path/to/filekey");
```

### Put a file
Content-Type is auto-discovered by file extension:
```csharp
long sizeStored = await _storage.PutAsync("your/destination/path/to/filekey", fileStream, ".pdf");
```

### Put a file with gzip encoding
FileStream must be gzipped by consumer:
```csharp
long sizeStored = await _storage.PutAsync("your/destination/path/to/filekey", fileStream, ".jpg", true);
```

### Check if file exists
```csharp
bool exists = await _storage.ExistsAsync("your/path/to/filekey");
```

### List all files in a bucket prefix/folder/directory
S3 returns a max of 1000 results per call. This method will continue calling S3 beyond 1000 results until all files in the prefix are returned. The StorageObject returns a slimmed down list of objects containing file name, size in bytes, and last updated UTC:
```csharp
List<StorageObject> allFiles = await _storage.ListAsync("your/path/to/filekey");
```

### Copy file
```csharp
await _storage.CopyAsync("your/path/to/filekey", "destinationBucket", "your/destination/filekey");
```

### Delete file
```csharp
await _storage.DeleteAsync("your/path/to/filekey");
```

### CancellationToken Support
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