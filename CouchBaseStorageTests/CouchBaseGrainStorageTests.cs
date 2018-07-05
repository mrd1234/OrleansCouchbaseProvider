namespace CouchBaseStorageTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Orleans.TestingHost;
    using Polly;
    using TestGrains;

    public class CouchBaseGrainStorageTests : IClassFixture<CouchBaseGrainStorageFixture>
    {
        private TestCluster host;

        public CouchBaseGrainStorageTests(CouchBaseGrainStorageFixture fixture)
        {
            host = fixture.HostedCluster;
        }

        [Fact]
        public async Task StartSiloWithCouchBaseStorage()
        {
            var id = Guid.NewGuid();
            var grain = host.GrainFactory.GetGrain<ICouchBaseStorageGrain>(id);
            var first = await grain.GetValue();
            Assert.Equal(0, first);
            await grain.Write(3);
            Assert.Equal(3, await grain.GetValue());
            await grain.Delete();
            Assert.Equal(0, await grain.GetValue());
        }

        [Fact]
        public async Task GrainWithAppConfigExpiryTest()
        {
            var grainId = Guid.NewGuid();
            var grain = this.host.GrainFactory.GetGrain<ICouchBaseStorageGrainWithAppConfigExpiry>(grainId);

            var startTime = DateTime.Now;

            await grain.Write(100);
            Assert.True(await grain.IsInitialised());

            //Wait for the grain to expire
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(600, x => TimeSpan.FromMilliseconds(100), (e, t) => Console.WriteLine("Grain has not yet expired - retrying..."));
            retryPolicy.Execute(() =>
            {
                var isInitialised = grain.IsInitialised().Result;
                var value = grain.GetValue().Result;

                Assert.False(isInitialised, "State is still initialised");
                Assert.Equal(value, 0);

                var timeTaken = DateTime.Now.Subtract(startTime);

                //Note that Orleans decides exactly when to deactivate a grain so the time will never exactly match the configured expiry value

                //Check deactivation happened after the 10 second timeout in the config file
                Assert.True(timeTaken >= TimeSpan.FromSeconds(10), $"Expected expiry of around 10 seconds but it took {timeTaken.TotalSeconds} seconds");
            });
        }

        [Fact]
        public async Task GrainWithDynamicExpiryGrainTest()
        {
            var grainId = Guid.NewGuid();
            var grain = this.host.GrainFactory.GetGrain<ICouchBaseStorageGrainWithDynamicExpiry>(grainId);

            var startTime = DateTime.Now;

            await grain.Write(100);
            Assert.True(await grain.IsInitialised());

            //Wait for the grain to expire
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(600, x => TimeSpan.FromMilliseconds(100), (e, t) => Console.WriteLine("Grain has not yet expired - retrying..."));
            retryPolicy.Execute(() =>
            {
                var isInitialised = grain.IsInitialised().Result;
                var value = grain.GetValue().Result;

                Assert.False(isInitialised, "State is still initialised");
                Assert.Equal(value, 0);

                var timeTaken = DateTime.Now.Subtract(startTime);

                //Note that Orleans decides exactly when to deactivate a grain so the time will never exactly match the configured expiry value

                //Check deactivation happened after the 10 second timeout in the config file (since expiry calculator will override that value)
                Assert.True(timeTaken > TimeSpan.FromSeconds(10));

                //Check the deactivation didn't occur before the 30 seconds set by expiry calculator
                Assert.True(timeTaken >= TimeSpan.FromSeconds(30), $"Expected expiry of around 30 seconds but it took {timeTaken.TotalSeconds} seconds");
            });
        }

        [Fact]
        public async Task StoresGrainStateWithReferencedGrainTest()
        {
            var grainId = Guid.NewGuid();
            var grain = this.host.GrainFactory.GetGrain<ICouchBaseWithGrainReferenceStorageGrain>(grainId);

            // Request grain to reference another grain
            var referenceTag = $"Referenced by grain {grainId}";
            await grain.ReferenceOtherGrain(referenceTag);

            // Verify referenced grain values
            var retrievedReferenceTag = await grain.GetReferencedTag();
            Assert.Equal(referenceTag, retrievedReferenceTag);
            var retrievedReferencedAt = await grain.GetReferencedAt();
            Assert.NotEqual(default(DateTime), retrievedReferencedAt);

            // Write state
            await grain.Write();

            // Restart all test silos
            var silos = new[] { this.host.Primary }.Union(this.host.SecondarySilos).ToList();
            foreach (var siloHandle in silos)
            {
                this.host.RestartSilo(siloHandle);
            }

            // Re-initialize client
            host.KillClient();
            host.InitializeClient();

            // Revive persisted grain
            var grainPostRestart = this.host.GrainFactory.GetGrain<ICouchBaseWithGrainReferenceStorageGrain>(grainId);

            // Force read persisted state
            await grainPostRestart.Read();

            // Verify persisted state post restart
            var retrievedReferenceTagPostWrite = await grainPostRestart.GetReferencedTag();
            Assert.Equal(referenceTag, retrievedReferenceTagPostWrite);
            var retrievedReferencedAtPostWrite = await grainPostRestart.GetReferencedAt();
            Assert.Equal(retrievedReferencedAt, retrievedReferencedAtPostWrite);
        }
    }
}
