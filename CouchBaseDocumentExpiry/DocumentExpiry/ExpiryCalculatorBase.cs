namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using Orleans;
    using System.Threading.Tasks;

    public abstract class ExpiryCalculatorBase : IExpiryCalculator
    {
        public abstract string GrainType { get; }

        protected IGrainFactory GrainFactory { get; }

        protected ExpiryCalculatorBase(IGrainFactory grainFactory)
        {
            GrainFactory = grainFactory;
        }

        public abstract Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e);
    }
}
