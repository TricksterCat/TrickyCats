using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GameRules.Scripts.Thread
{
    public class AsyncEventWaitHandle : ManualResetEventSlim
    {
        private static readonly ConcurrentBag<AsyncEventWaitHandle> _pool = new ConcurrentBag<AsyncEventWaitHandle>();

        public bool IsCanFree { get; }

        public AsyncEventWaitHandle(bool canFree = true) : base(false)
        {
            IsCanFree = canFree;
        }

        public static AsyncEventWaitHandle GetNext()
        {
            if (_pool.TryTake(out var result))
                result.Reset();
            else 
                result = new AsyncEventWaitHandle();
            return result;
        }

        public Task WaitAsync()
        {
            return Task.Factory.StartNew(InternalWait);
        }
        
        public async Task WaitAsyncAndFree()
        {
            await WaitAsync();
            Free();
        }

        private void InternalWait()
        {
            Wait();
        }

        public void Free()
        {
            if (IsCanFree)
            {
                Set();
                _pool.Add(this);
            }
        }
    }
}