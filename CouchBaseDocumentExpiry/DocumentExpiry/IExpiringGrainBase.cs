namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    public interface IExpiringGrainBase
    {
        bool IsDeactivating { get; }
    }
}