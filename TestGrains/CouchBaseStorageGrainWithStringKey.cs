namespace TestGrains
{
    using System.Threading.Tasks;
    using Orleans;

    public interface ICouchBaseStorageGrainWithStringKey : IGrainWithStringKey
    {
    }

    public class CouchBaseStorageGrainWithStringKey : Grain<StorageData>, ICouchBaseStorageGrainWithStringKey
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
                State = new StorageData();
            return base.OnActivateAsync();
        }
    }
}
