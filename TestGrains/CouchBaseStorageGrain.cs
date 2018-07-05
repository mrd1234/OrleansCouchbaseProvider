using System.Threading.Tasks;
using Orleans;

namespace TestGrains
{
    public interface ICouchBaseStorageGrain : IGrainWithGuidKey
    {
        Task Read();
        Task<int> GetValue();
        Task Write(int value);
        Task SetValue(int val);
        Task Delete();
    }

    public class StorageData
    {
        public int Value;
        public bool Initialised;
    }

    public class CouchBaseStorageGrain : Grain<StorageData>, ICouchBaseStorageGrain
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

        public Task Read()
        {
            return ReadStateAsync();
        }

        public Task Write(int value)
        {
            State.Value = value;
            State.Initialised = true;
            var result = WriteStateAsync();
            return result;
        }

        public Task SetValue(int val)
        {
            State.Value = val;
            return TaskDone.Done;
        }

        public async Task Delete()
        {
            await ClearStateAsync();
            State.Value = 0;
        }
    }
}
