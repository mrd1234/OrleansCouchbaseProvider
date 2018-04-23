namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using WeakEvent;
    using CouchBaseDocumentExpiry.Configuration;

    public partial class ExpiryManager
    {
        private static readonly object Locker = new object();
        private static ExpiryManager _instance;

        /// <summary>
        /// Document expiries by grain type
        /// </summary>
        private static Dictionary<string, TimeSpan> DocumentExpiries { get; set; }

        /// <summary>
        /// Calculator classes used to determine document expiry base don model (grain state) data
        /// </summary>
        private static Dictionary<string, IExpiryCalculator> ExpiryCalculators { get; set; }

        private readonly WeakEventSource<ExpiryCalculationArgs> expiryCalculationEventSource = new WeakEventSource<ExpiryCalculationArgs>();
        public event EventHandler<ExpiryCalculationArgs> ExpiryCalculated
        {
            add => expiryCalculationEventSource.Subscribe(value);
            remove => expiryCalculationEventSource.Unsubscribe(value);
        }

        public static ExpiryManager Instance
        {
            get
            {
                lock (Locker)
                {
                    if (_instance != null) return _instance;

                    _instance = new ExpiryManager();

                    DocumentExpiries = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries();
                    ExpiryCalculators = Instance.LoadExpiryCalculators();

                    return _instance;
                }
            }
        }

        private Dictionary<string, IExpiryCalculator> LoadExpiryCalculators()
        {
            return LoadExpiryCalculators(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        private Dictionary<string, IExpiryCalculator> LoadExpiryCalculators(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException($"{nameof(folder)} cannot be empty", nameof(folder));

            var expiryCalculators = new Dictionary<string, IExpiryCalculator>();

            var files = Directory.GetFiles(folder, "*.dll").ToList();

            files.ForEach(file =>
            {
                //Get all expiry calculators
                var calculators = GetCalculatorsFromAssembly(file);

                calculators.ForEach(f =>
                {
                    var calculator = (IExpiryCalculator)Activator.CreateInstance(f);
                    expiryCalculators.Add(calculator.GrainType, calculator);
                });
            });

            return expiryCalculators;
        }

        private Dictionary<string, IExpiryCalculator> LoadExpiryCalculators(string folder, string filename)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException($"{nameof(folder)} cannot be empty", nameof(folder));
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException($"{nameof(filename)} cannot be empty", nameof(filename));
            if (!File.Exists(Path.Combine(folder, filename))) throw new FileNotFoundException($"{filename} could not be found in {folder}");

            var expiryCalculators = new Dictionary<string, IExpiryCalculator>();

            //Get all expiry calculators
            var calculators = GetCalculatorsFromAssembly(filename);

            calculators.ForEach(f =>
            {
                var calculator = (IExpiryCalculator)Activator.CreateInstance(f);
                expiryCalculators.Add(calculator.GrainType, calculator);
            });

            return expiryCalculators;
        }

        private static List<Type> GetCalculatorsFromAssembly(string file)
        {
            try
            {
                return Assembly.LoadFile(file).GetExportedTypes().Where(w => typeof(IExpiryCalculator).IsAssignableFrom(w) && !w.IsInterface).ToList();
            }
            catch (Exception)
            {
                return new List<Type>();
            }
        }

        public async Task<ExpiryCalculationArgs> GetExpiryAsync(string grainType, string grainKey, string entityData)
        {
            var args = BuildExpiryCalculationArgs(grainType, grainKey, entityData);

            if (ExpiryCalculators.ContainsKey(grainType)) ExpiryCalculators[grainType].Calculate(args);

            return args;
        }

        private static ExpiryCalculationArgs BuildExpiryCalculationArgs(string grainType, string grainKey, string entityData)
        {
            var expiry = TimeSpan.Zero;
            var expirySource = ExpiryCalculationArgs.ExpirySources.NoExpiry;

            if (DocumentExpiries.ContainsKey(grainType))
            {
                expiry = DocumentExpiries[grainType];
                expirySource = ExpiryCalculationArgs.ExpirySources.ConfigFile;
            }

            var args = new ExpiryCalculationArgs(grainType, grainKey, entityData, new ExpiryCalculationArgs.ExpirySourceAndValue { Source = expirySource, Expiry = expiry });
            return args;
        }

        public void NotifyGrainOfExpiry(ExpiryCalculationArgs eventArgs)
        {
            lock (Locker)
            {
                expiryCalculationEventSource.Raise(this, eventArgs);
            }
        }
    }
}
