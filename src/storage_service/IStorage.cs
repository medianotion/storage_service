using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("unittest")]
namespace Storage
{
    
	public interface IStorage
	{
        Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default);
        Task<long> PutAsync(string key, Stream stream, string fileExtension, bool gZipped = false, CancellationToken cancellationToken = default);
		Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<List<StorageObject>> ListAsync(string prefix, CancellationToken cancellationToken = default);
		Task CopyAsync(string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = default);
        Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    }
}