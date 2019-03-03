using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Managers
{
    public class ConcurrentWaitQueue<T>
    {
        private ConcurrentQueue<T> _bufferQueue;
        private ConcurrentQueue<TaskCompletionSource<T>> _promisesQueue;
        private object _syncRoot = new object();

        public ConcurrentWaitQueue(CancellationToken cancellationToken)
        {
            _bufferQueue = new ConcurrentQueue<T>();
            _promisesQueue = new ConcurrentQueue<TaskCompletionSource<T>>();
        }

        public void Enqueue(T item)
        {
            TaskCompletionSource<T> promise;
            do
            {
                if (_promisesQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
                    return;
                }
            }
            while (promise != null);

            lock (_syncRoot)
            {
                if (_promisesQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
                    return;
                }

                _bufferQueue.Enqueue(item);
            }
        }
        
        public Task<T> Dequeue(CancellationToken cancellationToken = default(CancellationToken))
        {
            T item;

            if (!_bufferQueue.TryDequeue(out item))
            {
                lock (_syncRoot)
                {
                    if (!_bufferQueue.TryDequeue(out item))
                    {
                        var promise = new TaskCompletionSource<T>();
                        if (cancellationToken != null)
                        {
                            cancellationToken.Register(() => promise.TrySetCanceled());
                        }

                        _promisesQueue.Enqueue(promise);

                        return promise.Task;
                    }
                }
            }

            return Task.FromResult(item);
        }
    }
}
