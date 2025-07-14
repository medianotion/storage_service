using System.Collections.Generic;

namespace Storage.Configuration
{
    public class StorageOptions
    {
        public const string SectionName = "StorageService";
        
        public string DefaultProvider { get; set; } = "S3";
        public Dictionary<string, ProviderOptions> Providers { get; set; } = new Dictionary<string, ProviderOptions>();
        public S3Options S3 { get; set; } = new S3Options();
        public NetworkOptions Network { get; set; } = new NetworkOptions();
    }

    public class ProviderOptions
    {
        public string Type { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
    }
}