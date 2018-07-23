namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Orleans;

    public abstract class GenericExpiryCalculatorBase<TGrain, TState> : ExpiryCalculatorBase where TGrain : IGrain where TState : new()
    {
        protected GenericExpiryCalculatorBase(IGrainFactory grainFactory) : base(grainFactory)
        {
        }

        public override string GrainType { get; } = typeof(TGrain).Name;

        protected abstract TimeSpan ExpiryOnError { get; }

        public override async Task CalculateAsync(ExpiryManager.ExpiryCalculationArgs e)
        {
            try
            {
                var model = JsonConvert.DeserializeObject<TState>(e.Data);
                await this.PerformCalculationAsync(e, model);
            }
            catch (Exception ex)
            {
                e.SetException(this.ExpiryOnError, ex);
            }
        }

        protected abstract Task PerformCalculationAsync(ExpiryManager.ExpiryCalculationArgs e, TState model);
    }
}
