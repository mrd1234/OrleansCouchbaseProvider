namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.ComponentModel;
    using System.Collections.Generic;
    using Orleans;
    using Orleans.Providers;
    using Configuration;
    
    using Orleans.Runtime;

    public partial class ExpiryManager
    {
        /// <summary>
        /// Document expiries by grain type
        /// </summary>
        private Dictionary<string, TimeSpan> DocumentExpiries { get; }

        /// <summary>
        /// Calculator classes used to determine document expiry based on model (grain state) data
        /// </summary>
        private Dictionary<string, IExpiryCalculator> ExpiryCalculators { get; set; }

        private IProviderRuntime ProviderRuntime { get; }

        private Logger Logger { get; }

        public ExpiryManager(IProviderRuntime providerRuntime)
        {
            ProviderRuntime = providerRuntime;
            Logger = providerRuntime.GetLogger(this.GetType().FullName);

            DocumentExpiries = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries();
            GetCalculators(ProviderRuntime.GrainFactory);
        }

        private void GetCalculators(IGrainFactory grainFactory)
        {
            var expiryCalculatorLoader = new ExpiryCalculatorLoader(grainFactory, Logger);
            ExpiryCalculators = expiryCalculatorLoader.LoadExpiryCalculators();
            
            ExpiryCalculators.Keys.ToList().ForEach(f =>
            {
                Logger.Info($"CouchbaseStorageProvider has loaded {ExpiryCalculators[f].GetType().FullName} to calculate expiry values for {f} grain type");
            });
        }

        public async Task<ExpiryCalculationArgs> GetExpiryAsync(string grainType, string entityData, string primaryKey)
        {
            var args = BuildExpiryCalculationArgs(grainType, entityData, primaryKey);

            if (ExpiryCalculators.ContainsKey(grainType)) await ExpiryCalculators[grainType].CalculateAsync(args);

            LogExpiryDetails(args, grainType, primaryKey);

            return args;
        }

        private ExpiryCalculationArgs BuildExpiryCalculationArgs(string grainType, string entityData, string primaryKey)
        {
            var expiry = TimeSpan.Zero;
            var expirySource = ExpiryCalculationArgs.ExpirySources.NoExpiry;

            if (DocumentExpiries.ContainsKey(grainType))
            {
                expiry = DocumentExpiries[grainType];
                expirySource = ExpiryCalculationArgs.ExpirySources.ConfigFile;
            }

            var args = new ExpiryCalculationArgs(grainType, entityData, new ExpiryCalculationArgs.ExpirySourceAndValue { Source = expirySource, Expiry = expiry }, primaryKey);
            return args;
        }

        private void LogExpiryDetails(ExpiryCalculationArgs args, string grainType, string primaryKey)
        {
            string message;

            var grainName = grainType.Trim().EndsWith("Grain", StringComparison.InvariantCultureIgnoreCase) ? grainType : $"{grainType} grain";

            switch (args.Expiry.Source)
            {
                case ExpiryCalculationArgs.ExpirySources.NoExpiry:
                    message = $"{grainName} with key {primaryKey} has no expiry value set";
                    break;
                case ExpiryCalculationArgs.ExpirySources.ConfigFile:
                    message = $"{grainName} with key {primaryKey} has an expiry value of {args.Expiry.Expiry.ToLongString()} provided by config file.";
                    break;
                case ExpiryCalculationArgs.ExpirySources.Dynamic:
                    message = $"{grainName} with key {primaryKey} has an expiry value of {args.Expiry.Expiry.ToLongString()} provided by dynamic expiry calculator";
                    break;
                case ExpiryCalculationArgs.ExpirySources.ErrorValue:
                    var detailedMessage = new StringBuilder();
                    detailedMessage.AppendLine($"{grainName} with key {primaryKey} has an expiry value of {args.Expiry.Expiry.ToLongString()} which is error fallback value.");
                    detailedMessage.AppendLine($"Grain state: {args.Data}");
                    detailedMessage.AppendLine($"Exception: {args.CalculationException}");
                    message = detailedMessage.ToString();
                    break;
                default:
                    throw new InvalidEnumArgumentException($"{args.Expiry.Source} is not a valid value for expiry source");
            }

            Logger.Info(message);
        }
    }
}
