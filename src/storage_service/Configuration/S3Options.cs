namespace Storage.Configuration
{
    /// <summary>
    /// S3-specific configuration options
    /// </summary>
    public class S3Options
    {
        public string Bucket { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public int MaxRetries { get; set; } = 3;
        public CredentialsOptions Credentials { get; set; } = new CredentialsOptions();
        
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
        public string SessionToken 
        { 
            get => Credentials.SessionToken; 
            set => Credentials.SessionToken = value; 
        }
    }
}