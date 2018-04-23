namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    public interface IExpiryCalculator
    {
        string GrainType { get; }
        void Calculate(ExpiryManager.ExpiryCalculationArgs e);
    }
}