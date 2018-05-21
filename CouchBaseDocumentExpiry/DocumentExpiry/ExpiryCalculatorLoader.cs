namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Orleans;
    using Orleans.Runtime;
    using CouchBaseDocumentExpiry.Exceptions;

    public class ExpiryCalculatorLoader
    {
        private IGrainFactory GrainFactory { get; }

        private Logger Logger { get; }

        public ExpiryCalculatorLoader(IGrainFactory grainFactory, Logger logger)
        {
            GrainFactory = grainFactory;
            Logger = logger;
        }

        public Dictionary<string, IExpiryCalculator> LoadExpiryCalculators()
        {
            return LoadExpiryCalculators(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        private Dictionary<string, IExpiryCalculator> LoadExpiryCalculators(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException($"{nameof(folder)} cannot be empty", nameof(folder));

            var expiryCalculators = new Dictionary<string, IExpiryCalculator>();

            var files = Directory.GetFiles(folder, "*.dll").ToList();

            files.ForEach(filename =>
            {
                //Get all expiry calculators
                GetExpiryCalculators(filename, expiryCalculators);
            });

            return expiryCalculators;
        }

        private void GetExpiryCalculators(string filename, IDictionary<string, IExpiryCalculator> expiryCalculators)
        {
            void HandleDuplicates(IExpiryCalculator calculator)
            {
                if (!expiryCalculators.ContainsKey(calculator.GrainType)) return;

                var message = $"Unable to load expiry calculator {calculator.GetType().FullName} as {expiryCalculators[calculator.GrainType].GetType().FullName} already handles expiry calculations for grain type {calculator.GrainType}";
                throw new MultipleExpiryCalculatorsForGrainTypeException(message);
            }

            GetCalculatorsFromAssembly(filename, out var assembliesInheritingFromBaseClass, out var assembliesImplementingInterface);

            assembliesInheritingFromBaseClass.ForEach(f =>
            {
                var calculator = (ExpiryCalculatorBase) Activator.CreateInstance(f, GrainFactory);
                HandleDuplicates(calculator);
                expiryCalculators.Add(calculator.GrainType, calculator);
            });

            if (assembliesInheritingFromBaseClass.Any())
            {
                Logger.Info($"{Path.GetFileName(filename)} contains {assembliesInheritingFromBaseClass.Count} expiry calculators that inherit ExpiryCalculatorBase base class");
            }

            assembliesImplementingInterface.ForEach(f =>
            {
                var calculator = (IExpiryCalculator) Activator.CreateInstance(f);
                HandleDuplicates(calculator);
                expiryCalculators.Add(calculator.GrainType, calculator);
            });

            if (assembliesImplementingInterface.Any())
            {
                Logger.Info($"{Path.GetFileName(filename)} contains {assembliesImplementingInterface.Count} expiry calculators that implement IExpiryCalculator interface");
            }
        }

        /// <summary>
        /// Returns all types that inherit from ExpiryCalculatorBase or that only implement IExpiryCalculator
        /// </summary>
        /// <param name="file"></param>
        /// <param name="assembliesInheritingFromBaseClass"></param>
        /// <param name="assembliesImplementingInterface"></param>
        /// <returns></returns>
        private static void GetCalculatorsFromAssembly(string file, out List<Type> assembliesInheritingFromBaseClass, out List<Type> assembliesImplementingInterface)
        {
            try
            {
                assembliesInheritingFromBaseClass = Assembly.LoadFile(file).GetExportedTypes().Where(w => w.IsSubclassOf(typeof(ExpiryCalculatorBase))).ToList();
            }
            catch (Exception)
            {
                assembliesInheritingFromBaseClass = new List<Type>();
            }

            try
            {
                assembliesImplementingInterface = Assembly.LoadFile(file).GetExportedTypes().Where(w => w != typeof(ExpiryCalculatorBase) && typeof(IExpiryCalculator).IsAssignableFrom(w) && !w.IsInterface && !w.IsSubclassOf(typeof(ExpiryCalculatorBase))).ToList();
            }
            catch (Exception)
            {
                assembliesImplementingInterface = new List<Type>();
            }
        }
    }
}