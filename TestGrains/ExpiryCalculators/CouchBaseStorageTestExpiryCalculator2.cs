namespace TestGrains.ExpiryCalculators
{
    using System;
    using System.Threading.Tasks;
    using CouchBaseDocumentExpiry.DocumentExpiry;
    using Orleans;

    [Serializable]
    public class CouchBaseStorageTestExpiryCalculator2 : ExpiryCalculatorBase
    {
        public override string GrainType { get; } = typeof(CouchBaseStorageGrainFactoryTest2).Name;

        public CouchBaseStorageTestExpiryCalculator2(IGrainFactory grainFactory) : base(grainFactory)
        {
        }

        public override Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e)
        {
            //This line should NOT throw
            var grain = GrainFactory.GetGrain<ICouchBaseStorageGrain>(Guid.NewGuid());

            e.SetExpiry(TimeSpan.FromSeconds(30));
            return TaskDone.Done;
        }
    }
}