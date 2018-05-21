using System;
using System.Threading.Tasks;
using Orleans.Providers;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using CouchBaseDocumentExpiry.DocumentExpiry;
using CouchBaseProviders.Configuration;
using Orleans.Runtime;

namespace Orleans.Storage
{
    /// <summary>
    /// Orleans storage provider implementation for CouchBase http://www.couchbase.com 
    /// </summary>
    /// <remarks>
    /// The storage provider should be registered via programatic config or in a config file before you can use it.
    /// 
    /// This providers uses optimistic concurrency and leverages the CAS of CouchBase when touching
    /// the database. If we don't use this feature always the last write wins and it might be desired
    /// in specific scenarios and can be added later on as a feature. CAS is a ulong value stored as string in the
    /// ETag of the state object.
    /// </remarks>
    public class OrleansCouchBaseStorage : BaseJSONStorageProvider
    {
        /// <summary>
        /// This is used internally only to avoid reinitializing the client connection
        /// when multiple providers of this type are defined to store values in multiple
        /// buckets.
        /// </summary>
        internal static bool IsInitialized;

        internal static IProviderRuntime ProviderRuntime;

        /// <summary>
        /// Initializes the provider during silo startup.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;

            ProviderRuntime = providerRuntime;
            
            var clientConfiguration = config.Properties.ReadCouchbaseConfiguration(out var storageBucketName);

            DataManager = new CouchBaseDataManager(storageBucketName, clientConfiguration, providerRuntime);
            return base.Init(name, providerRuntime, config);
        }
    }

    /// <summary>
    /// Interfaces with CouchBase on behalf of the provider.
    /// </summary>
    public class CouchBaseDataManager : IJSONStateDataManager
    {
        private int count = 0;

        /// <summary>
        /// Name of the bucket that it works with.
        /// </summary>
        protected string bucketName;

        /// <summary>
        /// The cached bucket reference
        /// </summary>
        protected IBucket bucket;

        private ExpiryManager ExpiryManager { get; }
        public Logger Logger { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        public CouchBaseDataManager(string bucketName, ClientConfiguration clientConfig)
        {
            Initialise(bucketName, clientConfig);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        /// <param name="providerRuntime">Runtime reference used to gain access to GrainFactory etc</param>
        public CouchBaseDataManager(string bucketName, ClientConfiguration clientConfig, IProviderRuntime providerRuntime)
        {
            Initialise(bucketName, clientConfig);
            ExpiryManager = new ExpiryManager(providerRuntime);

            Logger = providerRuntime.GetLogger(this.GetType().FullName);
            Logger.Info("{0} - constructor called", this.GetType().FullName);
        }

        /// <summary>
        /// Validates and applies storage provider configuration
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        private void Initialise(string bucketName, ClientConfiguration clientConfig)
        {
            //Bucket name should not be empty
            //Keep in mind that you should create the buckets before being able to use them either
            //using the commandline tool or the web console
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("bucketName can not be null or empty");

            //config should not be null either
            if (clientConfig == null)
                throw new ArgumentException("You should supply a configuration to connect to CouchBase");

            this.bucketName = bucketName;
            if (!OrleansCouchBaseStorage.IsInitialized)
            {
                ClusterHelper.Initialize(clientConfig);
                OrleansCouchBaseStorage.IsInitialized = true;
            }
            else
            {
                foreach (var conf in clientConfig.BucketConfigs)
                {
                    if (ClusterHelper.Get().Configuration.BucketConfigs.ContainsKey(conf.Key))
                    {
                        ClusterHelper.Get().Configuration.BucketConfigs.Remove(conf.Key);
                    }

                    ClusterHelper.Get().Configuration.BucketConfigs.Add(conf.Key, conf.Value);
                }
            }

            //cache the bucket.
            bucket = ClusterHelper.GetBucket(this.bucketName);
        }

        /// <summary>
        /// Deletes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Delete(string collectionName, string key, string eTag)
        {
            var docID = GetDocumentId(collectionName, key);
            var result = await bucket.RemoveAsync(docID, ulong.Parse(eTag));
            if (!result.Success)
                throw new Orleans.Storage.InconsistentStateException(result.Message, eTag, result.Cas.ToString());
        }

        /// <summary>
        /// Reads a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<Tuple<string, string>> Read(string collectionName, string key)
        {
            var docID = GetDocumentId(collectionName, key);

            //If there is a value we read it and consider the CAS as ETag as well and return
            //both as a tuple
            var result = await bucket.GetAsync<string>(docID);
            if (result.Success)
                return Tuple.Create<string, string>(result.Value, result.Cas.ToString());
            if (!result.Success && result.Status == Couchbase.IO.ResponseStatus.KeyNotFound) //not found
                return Tuple.Create<string, string>(null, "");

            throw result.Exception;
        }

        /// <summary>
        /// Writes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored.</param>
        /// <param name="eTag"></param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Write(string collectionName, string key, string entityData, string eTag, string primaryKey)
        {
            var documentId = GetDocumentId(collectionName, key);

            var expiry = await ExpiryManager.GetExpiryAsync(collectionName, entityData, primaryKey);

            var result = string.Empty;

            if (ulong.TryParse(eTag, out var realETag))
            {
                var r = await bucket.UpsertAsync<string>(documentId, entityData, realETag, expiry.Expiry.Expiry);
                if (!r.Success)
                {
                    throw new InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }

                result = r.Cas.ToString();
            }
            else
            {
                var r = await bucket.InsertAsync<string>(documentId, entityData, expiry.Expiry.Expiry);

                //check if key exist and we don't have the CAS
                if (!r.Success && r.Status == Couchbase.IO.ResponseStatus.KeyExists)
                {
                    throw new InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }

                if (!r.Success)
                    throw new Exception(r.Status.ToString());

                result = r.Cas.ToString();
            }

            if (expiry.Expiry.Expiry == TimeSpan.Zero) return result;

            //Notify the grain that there is an expiry value for it so it can ensure it deactivates within the expiry time
            ExpiryManagerEventNotifier.Instance.NotifyGrainOfExpiry(expiry);
            
            return result;
        }

        public void Dispose()
        {
            bucket.Dispose();
            bucket = null;
            //Closes the DB connection
            ClusterHelper.Close();
            OrleansCouchBaseStorage.IsInitialized = false;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates a document ID based on the type name and key of the grain
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks>Each ID should be at most 250 bytes and it should not cause an issue unless you have
        /// an appetite for very long class names.
        /// The id will be of form TypeName_Key where TypeName doesn't include any namespace
        /// or version info.
        /// </remarks>
        private static string GetDocumentId(string collectionName, string key)
        {
            return collectionName + "_" + key;
        }
    }
}