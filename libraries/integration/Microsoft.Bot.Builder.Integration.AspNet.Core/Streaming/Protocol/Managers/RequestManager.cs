using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Managers
{
    public class RequestManager : IRequestManager
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>> _responseTasks;

        public RequestManager()
        {
            _responseTasks = new ConcurrentDictionary<Guid, TaskCompletionSource<ReceiveResponse>>();
        }

        public async Task<bool> SignalResponse(Guid requestId, ReceiveResponse response)
        {
            if (_responseTasks.TryGetValue(requestId, out TaskCompletionSource<ReceiveResponse> signal))
            {
                await Task.Run(() => { signal.SetResult(response); }).ConfigureAwait(false);
                return true;
            }
            return false;
        }
        

        public async Task<ReceiveResponse> GetResponseAsync(Guid requestId)
        {
            TaskCompletionSource<ReceiveResponse> responseTask = new TaskCompletionSource<ReceiveResponse>();

            if (!_responseTasks.TryAdd(requestId, responseTask))
            {
                return null;
            }

            try
            {
                var response = await responseTask.Task.ConfigureAwait(false);
                return response;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                _responseTasks.TryRemove(requestId, out responseTask);
            }
        }
    }
}
