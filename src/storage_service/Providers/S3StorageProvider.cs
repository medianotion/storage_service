using System;
using Storage.Configuration;

namespace Storage.Providers
{
    public class S3StorageProvider : IStorageProvider
    {
        private readonly S3Options _options;

        public S3StorageProvider(S3Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IStorage CreateStorage()
        {
            return new S3(_options);
        }
    }
}