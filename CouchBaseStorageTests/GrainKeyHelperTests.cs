namespace CouchBaseStorageTests
{
    using System;
    using CouchBaseDocumentExpiry.DocumentExpiry;
    using Xunit;
    using Orleans.TestingHost;
    using TestGrains;

    public class GrainKeyHelperTests : IClassFixture<CouchBaseGrainStorageFixture>
    {
        private readonly TestCluster host;

        public GrainKeyHelperTests(CouchBaseGrainStorageFixture fixture)
        {
            host = fixture.HostedCluster;
        }

        [Fact]
        public void TestGuidKey()
        {
            var id = Guid.NewGuid();
            var grain = host.GrainFactory.GetGrain<ICouchBaseStorageGrain>(id);

            var result = GrainKeyHelper.KeyMatches(grain, id.ToString());
            Assert.True(result);
        }

        [Fact]
        public void TestStringKey()
        {
            const string id = "TheKey";
            var grain = host.GrainFactory.GetGrain<ICouchBaseStorageGrainWithStringKey>(id);

            var result = GrainKeyHelper.KeyMatches(grain, id);
            Assert.True(result);
        }

        [Fact]
        public void TestIntegerKey()
        {
            const int id = 100;
            var grain = host.GrainFactory.GetGrain<ICouchBaseStorageGrainWithIntegerKey>(id);

            var result = GrainKeyHelper.KeyMatches(grain, id.ToString());
            Assert.True(result);
        }
    }
}
