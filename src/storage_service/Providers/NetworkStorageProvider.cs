using Storage.Configuration;

namespace Storage.Providers
{
    /// <summary>
    /// Network file share storage provider implementation
    /// </summary>
    public class NetworkStorageProvider : IStorageProvider
    {
        private readonly NetworkOptions _options;

        public NetworkStorageProvider(NetworkOptions options)
        {
            _options = options ?? throw new System.ArgumentNullException(nameof(options));
        }

        public IStorage CreateStorage()
        {
            return new NetworkStorage(_options);
        }
    }
}