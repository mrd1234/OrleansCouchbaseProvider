namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Threading.Tasks;
    using Orleans;
    
    public abstract class ExpiringGrainBase<TGrainState> : Grain<TGrainState>, IExpiringGrainBase where TGrainState : new()
    {
        public bool IsDeactivating { get; private set; }

        private IDisposable ExpiryTimer { get; set; }

        public override Task OnActivateAsync()
        {
            ExpiryManager.Instance.ExpiryCalculated += OnExpiryCalculated;
            return TaskDone.Done;
        }

        public override Task OnDeactivateAsync()
        {
            ExpiryManager.Instance.ExpiryCalculated -= OnExpiryCalculated;
            return base.OnDeactivateAsync();
        }

        private void OnExpiryCalculated(object sender, ExpiryManager.ExpiryCalculationArgs e)
        {
            var keyMatches = GrainKeyMatcher.KeyMatches(this, e.GrainKey, this.IdentityString);

            if (!keyMatches)
            {
                return;
            }

            SetTimer(e.Expiry.Expiry);
        }

        private void SetTimer(TimeSpan newExpiry)
        {
            ExpiryTimer?.Dispose();

            if (newExpiry == TimeSpan.Zero) return;

            ExpiryTimer = RegisterTimer(ExpiryTimerFiredAsync, null, newExpiry, TimeSpan.FromSeconds(10));
        }

        private Task ExpiryTimerFiredAsync(object o)
        {
            this.ExpiryTimer?.Dispose();
            this.DeactivateOnIdle();
            this.IsDeactivating = true;

            return TaskDone.Done;
        }
    }
}
