﻿namespace CouchBaseStorageTests
{
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Orleans.Storage;
    using Couchbase;
    using Orleans;
    using Orleans.Providers;
    using Orleans.Runtime;
    using Orleans.TestingHost.Utils;

    public class ProviderRuntimeFixture : IClassFixture<IProviderRuntime>, IProviderRuntime
    {
        public Logger GetLogger(string loggerName)
        {
            return new NoOpTestLogger();
        }

        public void SetInvokeInterceptor(InvokeInterceptor interceptor)
        {
            throw new NotImplementedException();
        }

        public InvokeInterceptor GetInvokeInterceptor()
        {
            throw new NotImplementedException();
        }

        public Guid ServiceId { get; }
        public string SiloIdentity { get; }
        public IGrainFactory GrainFactory { get; }
        public IServiceProvider ServiceProvider { get; }
    }

    public class CouchBaseDataManagerTests : IClassFixture<CouchBaseDataManagerTests.CouchBaseFixture>
    {
        public class CouchBaseFixture : IDisposable
        {
            public CouchBaseDataManager manager;
            
            public CouchBaseFixture()
            {
                var clientConfig = new Couchbase.Configuration.Client.ClientConfiguration();
                clientConfig.Servers.Clear();
                clientConfig.Servers.Add(new Uri("http://localhost:8091"));
                clientConfig.BucketConfigs.Clear();
                clientConfig.BucketConfigs.Add("default", new Couchbase.Configuration.Client.BucketConfiguration
                {
                    BucketName = "default",
                    Username = "",
                    Password = ""
                });
                manager = new CouchBaseDataManager("default", clientConfig, new ProviderRuntimeFixture());
            }

            public void Dispose()
            {
                CleanupBucket();
                manager.Dispose();
                manager = null;
            }
            
            internal static void CleanupBucket()
            {
                var b = ClusterHelper.GetBucket("default");
                b.Remove("test_1");
                b.Remove("test_2");
                b.Remove("test_3");
                b.Remove("test_5");
            }
        }

        private readonly CouchBaseDataManager manager;

        public CouchBaseDataManagerTests(CouchBaseFixture fixture)
        {
            manager = fixture.manager;
            CouchBaseFixture.CleanupBucket();
        }

        [Fact]
        public async Task WriteTest()
        {
            var etag = await manager.Write("test", "1", "data", "", "");
            Assert.False(string.IsNullOrWhiteSpace(etag), "eTag should not be null or whitespace");
            ulong val;
            Assert.True(ulong.TryParse(etag, out val), "ETag should be parsable to uLong (couchbase CAS values");
        }

        [Fact]
        public async Task ReadTest()
        {
            var etag = await manager.Write("test", "2", "data", "", "");
            Assert.False(string.IsNullOrWhiteSpace(etag), "eTag should not be null or whitespace");
            ulong val;
            Assert.True(ulong.TryParse(etag, out val), "ETag should be parsable to uLong (couchbase CAS values");
            var t = await manager.Read("test", "2");
            Assert.Equal(t.Item1, "data");
        }

        [Fact]
        public async Task ReadWriteTest()
        {
            var etag = await manager.Write("test", "3", "data", "", "");

            var t = await manager.Read("test", "3");
            Assert.Equal(t.Item1, "data");
            var etag2 = await manager.Write("test", "3", "data2", t.Item2, "");
            Assert.False(string.IsNullOrWhiteSpace(etag2));
            var nextRead = await manager.Read("test", "3");
            Assert.Equal(nextRead.Item1, "data2");
        }

        [Fact]
        public async Task WriteDeleteTest()
        {
            var etag = await manager.Write("test", "4", "data", "", "");
            await manager.Delete("test", "4",etag);
            var r = await manager.Read("test", "4");
            Assert.Equal(r.Item1, null);

        }
    }
}
