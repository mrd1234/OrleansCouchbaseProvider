namespace CouchBaseStorageTests
{
    using System;
    using System.Collections.Generic;
    using Orleans;
    using Orleans.Runtime.Configuration;
    using Orleans.TestingHost;

    public class CouchBaseGrainStorageFixture : IDisposable
    {
        public TestCluster HostedCluster;

        private static void AdjustConfig(ClusterConfiguration c)
        {
            c.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("Default",
                new Dictionary<string, string>
                {
                    { "Servers","http://localhost:8091" },
                    { "UserName","" },
                    { "Password","" },
                    { "BucketName","default" }
                });
        }

        public CouchBaseGrainStorageFixture()
        {
            GrainClient.Uninitialize();
            var o = new TestClusterOptions(2);
            AdjustConfig(o.ClusterConfiguration);

            HostedCluster = new TestCluster(o);

            if (HostedCluster.Primary == null)
                HostedCluster.Deploy();
        }

        public void Dispose()
        {
            HostedCluster.StopAllSilos();
        }
    }
}
