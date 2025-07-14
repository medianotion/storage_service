using System;

namespace Storage.Exceptions
{
    /// <summary>
    /// Base exception for all storage operations
    /// </summary>
    public abstract class StorageException : Exception
    {
        protected StorageException(string message) : base(message) { }
        protected StorageException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a resource is not found
    /// </summary>
    public class StorageNotFoundException : StorageException
    {
        public string ResourceKey { get; }

        public StorageNotFoundException(string resourceKey) 
            : base($"Storage resource '{resourceKey}' was not found")
        {
            ResourceKey = resourceKey;
        }

        public StorageNotFoundException(string resourceKey, Exception innerException) 
            : base($"Storage resource '{resourceKey}' was not found", innerException)
        {
            ResourceKey = resourceKey;
        }
    }

    /// <summary>
    /// Exception thrown when access is denied due to insufficient permissions
    /// </summary>
    public class StorageAccessDeniedException : StorageException
    {
        public string ResourceKey { get; }

        public StorageAccessDeniedException(string resourceKey) 
            : base($"Access denied to storage resource '{resourceKey}'. Check permissions.")
        {
            ResourceKey = resourceKey;
        }

        public StorageAccessDeniedException(string resourceKey, Exception innerException) 
            : base($"Access denied to storage resource '{resourceKey}'. Check permissions.", innerException)
        {
            ResourceKey = resourceKey;
        }
    }

    /// <summary>
    /// Exception thrown when there are authentication issues
    /// </summary>
    public class StorageAuthenticationException : StorageException
    {
        public StorageAuthenticationException(string message) : base(message) { }
        public StorageAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when storage configuration is invalid
    /// </summary>
    public class StorageConfigurationException : StorageException
    {
        public StorageConfigurationException(string message) : base(message) { }
        public StorageConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when storage service is temporarily unavailable or rate limited
    /// </summary>
    public class StorageUnavailableException : StorageException
    {
        public StorageUnavailableException(string message) : base(message) { }
        public StorageUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a storage operation times out
    /// </summary>
    public class StorageTimeoutException : StorageException
    {
        public StorageTimeoutException(string message) : base(message) { }
        public StorageTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown for unexpected storage errors
    /// </summary>
    public class StorageInternalException : StorageException
    {
        public StorageInternalException(string message) : base(message) { }
        public StorageInternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}