using System;

namespace Storage
{
	public class StorageObject
	{
		public readonly string Key;
		public readonly long SizeInBytes;
		public readonly DateTime LastUpdatedUTC;
		public StorageObject(string key, long sizeInBytes, DateTime lastUpdatedUTC)
		{
			Key = key;
			SizeInBytes = sizeInBytes;
			LastUpdatedUTC = lastUpdatedUTC;
		}
	}
}