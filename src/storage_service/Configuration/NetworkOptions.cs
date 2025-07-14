namespace Storage.Configuration
{
    /// <summary>
    /// Network file share storage provider configuration options
    /// </summary>
    public class NetworkOptions
    {
        public string BasePath { get; set; } = string.Empty;
        public int BufferSize { get; set; } = 81920; // 80KB for file I/O operations
        public bool CreateDirectoriesIfNotExist { get; set; } = true;
        public CredentialsOptions Credentials { get; set; } = new CredentialsOptions();
        
        // Network share authentication
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        
        // File operation settings
        public bool UseTransactionalCopy { get; set; } = true; // Atomic copy operations
        public bool OverwriteExisting { get; set; } = true;
        public string TempFileExtension { get; set; } = ".tmp";
        
        // Backward compatibility properties (delegate to Credentials)
        public string AccessKey 
        { 
            get => Credentials.AccessKey; 
            set => Credentials.AccessKey = value; 
        }
        public string SecretKey 
        { 
            get => Credentials.SecretKey; 
            set => Credentials.SecretKey = value; 
        }
    }
}