namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;

    public partial class ExpiryManager
    {
        public class ExpiryCalculationArgs : EventArgs
        {
            public string GrainType { get; }
            public string GrainKey { get; }
            public string Data { get; }
            public ExpirySourceAndValue Expiry { get; }

            public ExpiryCalculationArgs(string grainType, string grainKey, string data, ExpirySourceAndValue expiry)
            {
                GrainType = grainType;
                GrainKey = grainKey;
                Data = data;
                Expiry = expiry;
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

            public void NoExpiry()
            {
                Expiry.Expiry = TimeSpan.Zero;
                Expiry.Source = ExpirySources.NoExpiry;
            }

            public enum ExpirySources
            {
                NoExpiry,
                ConfigFile,
                Dynamic
            }
        }
    }
}
