namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;

    public partial class ExpiryManager
    {
        public class ExpiryCalculationArgs : EventArgs
        {
            public string GrainType { get; }

            public string GrainPrimaryKeyAsString { get; }

            public string Data { get; }

            public ExpirySourceAndValue Expiry { get; }

            public Exception CalculationException { get; private set; }

            public ExpiryCalculationArgs(string grainType, string data, ExpirySourceAndValue expiry, string grainPrimaryKeyAsString)
            {
                GrainType = grainType;
                Data = data;
                Expiry = expiry;
                GrainPrimaryKeyAsString = grainPrimaryKeyAsString;
            }

            public class ExpirySourceAndValue
            {
                public ExpirySources Source { get; set; }
                public TimeSpan Expiry { get; set; }
            }

            public void SetExpiry(TimeSpan expiry)
            {
                if (expiry == TimeSpan.Zero)
                {
                    NoExpiry();
                    return;
                }

                Expiry.Expiry = expiry;
                Expiry.Source = ExpirySources.Dynamic;
            }

            public void SetException(TimeSpan expiry, Exception exception)
            {
                CalculationException = exception;
                Expiry.Expiry = expiry;
                Expiry.Source = ExpirySources.ErrorValue;
            }

            public void NoExpiry()
            {
                Expiry.Expiry = TimeSpan.Zero;
                Expiry.Source = ExpirySources.NoExpiry;
            }

            public enum ExpirySources
            {
                NoExpiry,
                ConfigFile,
                Dynamic,
                ErrorValue
            }
        }
    }
}
