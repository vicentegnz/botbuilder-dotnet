using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{

    internal class PipeConnection
    {
        public const string ServerIncomingPath = ".incoming";
        public const string ServerOutgoingPath = ".outgoing";

        public static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter>
            {
                new Iso8601TimeSpanConverter(),
            },
        };

        public static readonly JsonSerializerSettings DeserializationSettings = new JsonSerializerSettings
        {
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter(),
                },
        };

        private PipeStream _incoming;
        private PipeStream _outgoing;

        private readonly StreamRequestHandler _requestHandler;

        public PipeConnection(StreamRequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
        }

        public event EventHandler Disconnected;

        public bool IsConnected
        {
            get { return _incoming != null && _outgoing != null; }
        }

        private ConcurrentDictionary<string, TaskCompletionSource<StreamMessage>> Responses { get; set; }

        public void Connect(PipeStream incoming, PipeStream outgoing)
        {
            if (_incoming != null || _outgoing != null)
            {
                throw new InvalidOperationException("Pipes are already connected.");
            }

            _incoming = incoming;
            _outgoing = outgoing;

            Responses = new ConcurrentDictionary<string, TaskCompletionSource<StreamMessage>>();
            RunReceiveStream();
        }


        public async Task<StreamMessage> SendAsync(string method, string path, IDictionary<string, string> headers, string body)
        {
            // create the request
            var request = new StreamMessage()
            {
                RequestId = CreateRequestId(),
                Method = method,
                Path = path,
                Body = body,
                Headers = headers,
                StatusCode = 0,
            };

            var response = await SendAsync(request, true).ConfigureAwait(false);

            return response;
        }

        public async Task<StreamMessage> SendAsync(StreamMessage message, bool waitForResponse)
        {
            if (message.RequestId == null)
            {
                message.RequestId = CreateRequestId();
            }

            if (!_outgoing.IsConnected)
            {
                return new StreamMessage()
                {
                    RequestId = message.RequestId,
                    StatusCode = 500,
                };
            }

            var payload = JsonConvert.SerializeObject(message, SerializationSettings);

            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            int offset = 0;

            for (int retries = 0; retries < 3; retries++)
            {
                try
                {
                    int frames = 0;

                    // do/while here to allow zero-byte sends
                    do
                    {
                        int count = Math.Min(bytes.Length - offset, 1024);
                        bool end = (offset + count) == bytes.Length;

                        byte[] countBytes = BitConverter.GetBytes(count);
                        byte[] endBytes = BitConverter.GetBytes(end);

                        // Send: Length
                        await _outgoing.WriteAsync(countBytes, 0, countBytes.Length).ConfigureAwait(false);

                        // Send: Content
                        await _outgoing.WriteAsync(bytes, offset, count).ConfigureAwait(false);

                        // Send: End
                        await _outgoing.WriteAsync(endBytes, 0, endBytes.Length).ConfigureAwait(false);

                        offset += count;
                        frames++;
                    }
                    while (offset < bytes.Length);
                    break;
                }
                catch (Exception e)
                {
                    //return new StreamMessage()
                    //{
                    //    StatusCode = 500,
                    //    Body = e.Message
                    //};
                }
            }

            if (waitForResponse)
            {
                var response = await GetResponseAsync(message.RequestId).ConfigureAwait(false);

                return response;
            }

            return null;
        }

        private void OnDisconnected(EventArgs e = null)
        {
            try
            {
                _incoming.Close();
                _incoming.Dispose();
            }
            catch (Exception)
            {
            }
            _incoming = null;

            try
            {
                _outgoing.Close();
                _outgoing.Dispose();
            }
            catch (Exception)
            {
            }
            _outgoing = null;

            Disconnected?.Invoke(this, e ?? EventArgs.Empty);
        }

        private void RunReceiveStream()
        {
            Task.Run(ReceiveMessagesAsync);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            bool end = false;
            byte[] endBytes = new byte[16];
            bool isClosed = false;

            while (_incoming.IsConnected && !isClosed)
            {
                StreamMessage message = null;
                end = false;

                // receive a single message
                using (var memory = new MemoryStream())
                {
                    do
                    {
                        // read the length
                        var len = await _incoming.ReadAsync(buffer, 0, 4).ConfigureAwait(false);
                        if (len == 0)
                        {
                            end = true;
                            isClosed = true;
                            break;
                        }
                        var count = BitConverter.ToInt32(buffer, 0);

                        // read the content
                        len = await _incoming.ReadAsync(buffer, 0, count).ConfigureAwait(false);
                        if (len == 0)
                        {
                            end = true;
                            isClosed = true;
                            break;
                        }
                        memory.Write(buffer, 0, len); // TODO: what if len != count ??

                        len = await _incoming.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                        if (len == 0)
                        {
                            end = true;
                            isClosed = true;
                            break;
                        }
                        end = BitConverter.ToBoolean(buffer, 0);

                        // TODO: check for close, etc.
                    }
                    while (!end);

                    if (!isClosed && memory.Length > 0)
                    {
                        var memoryBuffer = memory.GetBuffer();

                        var text = Encoding.UTF8.GetString(memoryBuffer, 0, (int)memory.Length);

                        if (!string.IsNullOrEmpty(text))
                        {
                            message = JsonConvert.DeserializeObject<StreamMessage>(text, DeserializationSettings);
                        }
                    }
                }

                // Dispatch the message
                if (message != null)
                {
                    if (message.StatusCode != 0)
                    {
                        // It's a response
                        if (Responses.TryGetValue(message.RequestId, out TaskCompletionSource<StreamMessage> signal))
                        {
                            signal.SetResult(message);
                        }
                    }
                    else
                    {
                        // It's a request
                        Console.WriteLine($"Incoming request: {message.Method} {message.Path}");
                        if (_requestHandler != null)
                        {
                            Task.Run(async () => { await HandleRequestAsync(_requestHandler, message); });
                        }
                    }
                }
            }

            OnDisconnected();
        }

        private async Task<StreamMessage> GetResponseAsync(string requestId)
        {
            TaskCompletionSource<StreamMessage> responseTask = new TaskCompletionSource<StreamMessage>();

            Responses.TryAdd(requestId, responseTask);

            var response = await responseTask.Task.ConfigureAwait(false);

            Responses.TryRemove(requestId, out responseTask);

            return response;
        }

        private async Task HandleRequestAsync(StreamRequestHandler serverRequestHandler, StreamMessage request)
        {
            if (serverRequestHandler != null)
            {
                var response = await serverRequestHandler.ProcessRequestAsync(request).ConfigureAwait(false);
                if (response != null)
                {
                    await SendAsync(response, false).ConfigureAwait(false);
                }
            }
        }

        private string CreateRequestId()
        {
            return Guid.NewGuid().ToString("D");
        }
    }
}
