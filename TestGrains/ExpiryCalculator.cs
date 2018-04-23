using System;
using CouchBaseDocumentExpiry.DocumentExpiry;

namespace TestGrains
{
    [Serializable]
    public class ExpiryCalculator : IExpiryCalculator
    {
        public string GrainType { get; } = typeof(CouchBaseStorageGrain).Name;

        public void Calculate(ExpiryManager.ExpiryCalculationArgs e)
        {
            e.SetExpiry(TimeSpan.FromSeconds(10));
        }
    }
}
