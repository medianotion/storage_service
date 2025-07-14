namespace Storage.Providers
{
    public interface IStorageProvider
    {
        IStorage CreateStorage();
    }
}