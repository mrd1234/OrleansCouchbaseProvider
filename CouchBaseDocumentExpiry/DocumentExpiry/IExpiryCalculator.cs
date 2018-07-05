namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System.Threading.Tasks;

    public interface IExpiryCalculator
    {
        string GrainType { get; }

        Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e);
    }
}