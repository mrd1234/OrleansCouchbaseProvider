namespace TestGrains
{
    using System.Threading.Tasks;
    using Orleans;

    public interface ICouchBaseStorageGrainFactoryTest2 : IGrainWithGuidKey
    {
        Task Write(int value);
    }
    
    public class CouchBaseStorageGrainFactoryTest2 : Grain<StorageData>, ICouchBaseStorageGrainFactoryTest2
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
                State = new StorageData();
            return base.OnActivateAsync();
        }

        public Task Write(int value)
        {
            State.Value = value;
            State.Initialised = true;
            var result = WriteStateAsync();
            return result;
        }
    }
}
