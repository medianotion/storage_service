using System.Collections.Generic;

namespace Storage.Configuration
{
    /// <summary>
    /// Generic credential system for multi-provider support
    /// </summary>
    public class CredentialsOptions
    {
        public string AuthenticationType { get; set; } = "None";     // "None", "AccessKey", "STS", "Custom"
        public string AccessKey { get; set; } = string.Empty;        // For AWS/cloud providers
        public string SecretKey { get; set; } = string.Empty;        // For AWS/cloud providers  
        public string SessionToken { get; set; } = string.Empty;     // For STS temporary credentials
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>(); // For custom auth
    }
}