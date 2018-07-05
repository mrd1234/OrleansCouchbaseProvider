namespace TestGrains.ExpiryCalculators
{
    using System;
    using System.Threading.Tasks;
    using CouchBaseDocumentExpiry.DocumentExpiry;
    using Orleans;

    [Serializable]
    public class CouchBaseStorageGrainWithDynamicExpiryCalculator : GenericExpiryCalculatorBase<CouchBaseStorageGrainWithDynamicExpiry, StorageData>
    {
        protected override TimeSpan ExpiryOnError { get; } = TimeSpan.FromDays(365);

        public CouchBaseStorageGrainWithDynamicExpiryCalculator(IGrainFactory grainFactory) : base(grainFactory)
        {
        }
        
        protected override Task PerformCalculationAsync(ExpiryManager.ExpiryCalculationArgs e, StorageData model)
        {
            e.SetExpiry(TimeSpan.FromSeconds(30));
            return TaskDone.Done;
        }
    }
}
