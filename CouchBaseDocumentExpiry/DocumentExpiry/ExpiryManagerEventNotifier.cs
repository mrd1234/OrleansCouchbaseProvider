namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using WeakEvent;

    public class ExpiryManagerEventNotifier
    {
        private static readonly object Locker = new object();
        private static ExpiryManagerEventNotifier _instance;

        private readonly WeakEventSource<ExpiryManager.ExpiryCalculationArgs> expiryCalculationEventSource = new WeakEventSource<ExpiryManager.ExpiryCalculationArgs>();
        public event EventHandler<ExpiryManager.ExpiryCalculationArgs> ExpiryCalculated
        {
            add => expiryCalculationEventSource.Subscribe(value);
            remove => expiryCalculationEventSource.Unsubscribe(value);
        }

        public static ExpiryManagerEventNotifier Instance
        {
            get
            {
                lock (Locker)
                {
                    if (_instance != null) return _instance;

                    _instance = new ExpiryManagerEventNotifier();
                    return _instance;
                }
            }
        }

        public void NotifyGrainOfExpiry(ExpiryManager.ExpiryCalculationArgs eventArgs)
        {
            lock (Locker)
            {
                expiryCalculationEventSource.Raise(this, eventArgs);
            }
        }
    }
}