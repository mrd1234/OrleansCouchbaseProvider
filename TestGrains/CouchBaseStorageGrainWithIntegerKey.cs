namespace TestGrains
{
    using System.Threading.Tasks;
    using Orleans;

    public interface ICouchBaseStorageGrainWithIntegerKey : IGrainWithIntegerKey
    {
    }

    public class CouchBaseStorageGrainWithIntegerKey : Grain<StorageData>, ICouchBaseStorageGrainWithIntegerKey
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
                State = new StorageData();
            return base.OnActivateAsync();
        }
    }
}
