namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Orleans;
    
    public abstract class ExpiringGrainBase<TGrainState> : Grain<TGrainState>, IExpiringGrainBase where TGrainState : new()
    {
        public bool IsDeactivating { get; private set; }

        private IDisposable ExpiryTimer { get; set; }

        public override Task OnActivateAsync()
        {
            ExpiryManagerEventNotifier.Instance.ExpiryCalculated += OnExpiryCalculated;
            return TaskDone.Done;
        }

        public override Task OnDeactivateAsync()
        {
            ExpiryManagerEventNotifier.Instance.ExpiryCalculated -= OnExpiryCalculated;
            return base.OnDeactivateAsync();
        }

        private void OnExpiryCalculated(object sender, ExpiryManager.ExpiryCalculationArgs e)
        {
            var keyMatches = GrainKeyHelper.KeyMatches(this, e.GrainPrimaryKeyAsString);

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

            //Prevent dueTime being over the max allowed value (49.7 days)
            //This is way more than orleans default deactivation time so it doesn't matter that it doesn't match the couchbase document expiry value
            var timerMaxValue = TimeSpan.FromDays(49.7);
            if (newExpiry > timerMaxValue)
            {
                newExpiry = timerMaxValue;
            }

            ExpiryTimer = RegisterTimer(ExpiryTimerFiredAsync, null, newExpiry, Timeout.InfiniteTimeSpan);
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
