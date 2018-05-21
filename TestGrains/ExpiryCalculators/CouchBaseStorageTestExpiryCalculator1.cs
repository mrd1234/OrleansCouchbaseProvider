namespace TestGrains.ExpiryCalculators
{
    using System;
    using System.Threading.Tasks;
    using CouchBaseDocumentExpiry.DocumentExpiry;
    using Orleans;

    [Serializable]
    public class CouchBaseStorageTestExpiryCalculator1 : ExpiryCalculatorBase
    {
        public override string GrainType { get; } = typeof(CouchBaseStorageGrainFactoryTest1).Name;

        public CouchBaseStorageTestExpiryCalculator1(IGrainFactory grainFactory) : base(grainFactory)
        {
        }
        
        public override Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e)
        {
            //This line should throw
            var grain = GrainClient.GrainFactory.GetGrain<ICouchBaseStorageGrain>(Guid.NewGuid());

            e.SetExpiry(TimeSpan.FromSeconds(30));
            return TaskDone.Done;
        }
    }
}