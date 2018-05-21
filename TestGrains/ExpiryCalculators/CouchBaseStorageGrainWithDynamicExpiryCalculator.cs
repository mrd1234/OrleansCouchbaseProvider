namespace TestGrains.ExpiryCalculators
{
    using System;
    using System.Threading.Tasks;
    using CouchBaseDocumentExpiry.DocumentExpiry;
    using Orleans;

    [Serializable]
    public class CouchBaseStorageGrainWithDynamicExpiryCalculator : ExpiryCalculatorBase
    {
        public override string GrainType { get; } = typeof(CouchBaseStorageGrainWithDynamicExpiry).Name;

        public CouchBaseStorageGrainWithDynamicExpiryCalculator(IGrainFactory grainFactory) : base(grainFactory)
        {
        }

        public override Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e)
        {
            e.SetExpiry(TimeSpan.FromSeconds(30));
            return TaskDone.Done;
        }
    }
}
