namespace CouchBaseStorageTests
{
    using System;
    using System.Threading.Tasks;
    using Orleans.TestingHost;
    using TestGrains;
    using Xunit;

    /// <summary>
    /// Expiry calculators are called by the storage provider via ExpiryManager, but in the context of the grain having it's state written.
    /// This means that if the expiry calculator needs to call another grain to perform the calculation, it cannot use GrainClient.GrainFactory.
    /// Instead it must use the ProviderRuntime.GrainFactory which is provided to the ExpiryCalculatorBase class by ExpiryManager.
    /// </summary>
    public class CouchBaseExpiryCalculatorGrainFactoryTests : IClassFixture<CouchBaseGrainStorageFixture>
    {
        private readonly TestCluster _host;

        public CouchBaseExpiryCalculatorGrainFactoryTests(CouchBaseGrainStorageFixture fixture)
        {
            _host = fixture.HostedCluster;
        }
        
        [Fact]
        public async Task ExpiryCalculatorUsingGrainClientGrainFactoryTest_ShouldThrow()
        {
            var grain = _host.GrainFactory.GetGrain<ICouchBaseStorageGrainFactoryTest1>(Guid.NewGuid());

            var exceptionCalled = false;

            try
            {
                //This should throw because the grain expiry calculator is trying to use grainclient.grainfactory
                await grain.Write(123);
            }
            catch (Exception e)
            {
                exceptionCalled = true;
                Assert.True(e.Message.Contains("You are running inside a grain. GrainClient.GrainFactory should only be used on the client side. Inside a grain use GrainFactory property of the Grain base class (use this.GrainFactory)."));
            }
            finally
            {
                Assert.True(exceptionCalled, "An exception was expected but not thrown");
            }
        }

        [Fact]
        public async Task ExpiryCalculatorUsingProviderRuntimeGrainFactoryTest_ShouldNotThrow()
        {
            var grain = _host.GrainFactory.GetGrain<ICouchBaseStorageGrainFactoryTest2>(Guid.NewGuid());

            //This should NOT throw because the grain expiry calculator is trying to use providerruntime.grainfactory - which is passed into constructor by storage provider
            await grain.Write(123);
        }
    }
}
