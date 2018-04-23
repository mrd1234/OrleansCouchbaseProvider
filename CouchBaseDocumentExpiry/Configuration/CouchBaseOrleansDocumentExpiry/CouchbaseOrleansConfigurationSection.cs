using System.Configuration;

namespace CouchBaseDocumentExpiry.Configuration.CouchBaseOrleansDocumentExpiry
{
    public class CouchbaseOrleansConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty(CouchbaseOrleansGrainExpiryConstants.GrainExpiryCollectionName, IsDefaultCollection = false)]
        public GrainExpiryCollection GrainExpiries => (GrainExpiryCollection) base[CouchbaseOrleansGrainExpiryConstants.GrainExpiryCollectionName];
    }
}
