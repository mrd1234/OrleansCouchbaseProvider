namespace TestGrains
{
    using System.Threading.Tasks;
    using Orleans;
    using CouchBaseDocumentExpiry.DocumentExpiry;

    public interface ICouchBaseStorageGrainWithAppConfigExpiry : IGrainWithGuidKey
    {
        Task<int> GetValue();
        Task<bool> IsInitialised();
        Task Write(int value);
    }

    public class CouchBaseStorageGrainWithAppConfigExpiry : ExpiringGrainBase<StorageData>, ICouchBaseStorageGrainWithAppConfigExpiry
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
                State = new StorageData();
            return base.OnActivateAsync();
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(State.Value);
        }

        public async Task<bool> IsInitialised()
        {
            return await Task.FromResult(State.Initialised);
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
