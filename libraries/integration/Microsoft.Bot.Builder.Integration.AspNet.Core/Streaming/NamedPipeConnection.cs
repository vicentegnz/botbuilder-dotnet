using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal class NamedPipeConnection
    {
        public const string ServerIncomingPath = ".incoming";
        public const string ServerOutgoingPath = ".outgoing";

        public static readonly JsonSerializerSettings BotSchemaSerializationSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.None,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            //Converters = new List<JsonConverter>
            //{
            //    new Iso8601TimeSpanConverter(),
            //},
        };

        public static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.None,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            //ContractResolver = new ReadOnlyJsonContractResolver(),
            //Converters = new List<JsonConverter>
            //{
            //    new Iso8601TimeSpanConverter(),
            //},
        };

        public static readonly JsonSerializerSettings DeserializationSettings = new JsonSerializerSettings
        {
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            //ContractResolver = new ReadOnlyJsonContractResolver(),
            //Converters = new List<JsonConverter>
            //    {
            //        new Iso8601TimeSpanConverter(),
            //    },
        };

        private readonly StreamRequestHandler<StreamMessage> _requestHandler;
        private readonly NamedPipeConnectionInterrupt _interrupt;
        private readonly SendQueue<StreamMessage> _sendQueue;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<StreamMessage>> _responses;
        private readonly EventWaitHandle _connectedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private PipeStream _incoming;
        private PipeStream _outgoing;
        
        public NamedPipeConnection(StreamRequestHandler<StreamMessage> requestHandler)
            : this(requestHandler, null)
        {
        }

        public NamedPipeConnection(StreamRequestHandler<StreamMessage> requestHandler, NamedPipeConnectionInterrupt interrupt)
        {
            _requestHandler = requestHandler;
            _interrupt = interrupt ?? new NamedPipeConnectionInterrupt();
            _sendQueue = new SendQueue<StreamMessage>(this.SendMessageAsync);
            _responses = new ConcurrentDictionary<string, TaskCompletionSource<StreamMessage>>();
        }

        public event EventHandler Disconnected;

        public bool IsConnected
        {
            get { return _incoming != null && _outgoing != null; }
        }
        
        public void Connect(PipeStream incoming, PipeStream outgoing)
        {
            if(_incoming != null || _outgoing != null)
            {
                throw new InvalidOperationException("Pipes are already connected.");
            }

            _incoming = incoming;
            _outgoing = outgoing;

            RunReceiveStream();

            _connectedEvent.Set();
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

            _sendQueue.Post(message);

            if (waitForResponse)
            {
                var response = await GetResponseAsync(message.RequestId).ConfigureAwait(false);

                return response;
            }

            return null;
        }

        public async Task Disconnect(EventArgs e = null)
        {
            bool didDisconnect = false;

            lock (_connectedEvent)
            {
                try
                {
                    if (_incoming != null)
                    {
                        _incoming.Close();
                        _incoming.Dispose();
                        didDisconnect = true;
                    }
                }
                catch (Exception)
                {
                }
                _incoming = null;

                try
                {
                    if (_outgoing != null)
                    {
                        _outgoing.Close();
                        _outgoing.Dispose();
                        didDisconnect = true;
                    }
                }
                catch (Exception)
                {
                }
                _outgoing = null;
            }

            if (didDisconnect)
            {
                _connectedEvent.Reset();

                // empty the Response queue
                while (!_responses.IsEmpty)
                {
                    var doomedRequestId = _responses.Keys.First();
                    if (_responses.TryRemove(doomedRequestId, out TaskCompletionSource<StreamMessage> doomedTask))
                    {
                        await Task.Run(() => { doomedTask.SetCanceled(); }).ConfigureAwait(false);
                    }
                }

                Disconnected?.Invoke(this, e ?? EventArgs.Empty);
            }
        }

        private async Task SendMessageAsync(StreamMessage message)
        {
            _connectedEvent.WaitOne();

            var payload = JsonConvert.SerializeObject(message, SerializationSettings);

            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            int offset = 0;
            bool isClosed = false;

            try
            {
                int frames = 0;
                int length;

                // Note: there are no zero-byte sends; sending a length of 0 means the pipe was closed
                do
                {
                    int count = Math.Min(bytes.Length - offset, 1024);
                    bool end = (offset + count) == bytes.Length;

                    byte[] countBytes = BitConverter.GetBytes(count);
                    byte[] endBytes = BitConverter.GetBytes(end);

                    // Send: Length
                    _interrupt.BeforeWriteLength();
                    length = await SendToStreamAsync(countBytes, 0, countBytes.Length).ConfigureAwait(false);
                    if(length == 0)
                    {
                        isClosed = true;
                        break;
                    }

                    // Send: Content
                    _interrupt.BeforeWriteContent();
                    await SendToStreamAsync(bytes, offset, count).ConfigureAwait(false);
                    if (length == 0)
                    {
                        isClosed = true;
                        break;
                    }

                    // Send: End
                    _interrupt.BeforeWriteEnd();
                    await SendToStreamAsync(endBytes, 0, endBytes.Length).ConfigureAwait(false);
                    if(length == 0)
                    {
                        isClosed = true;
                        break;
                    }

                    offset += count;
                    frames++;
                }
                while (offset < bytes.Length);

                if (isClosed)
                {
                    await Disconnect().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                await Disconnect().ConfigureAwait(false);
            }
        }

        private async Task<int> SendToStreamAsync(byte[] bytes, int startIndex, int count)
        {
            try
            {
                if (_outgoing != null)
                {
                    await _outgoing.WriteAsync(bytes, startIndex, count).ConfigureAwait(false);
                    return count;
                }
            }
            catch(ObjectDisposedException)
            {
                // _outgoing was disposed by a Disconnect call
            }
            catch (IOException)
            {
                // _outgoing was disposed by a disconnect of a broken pipe
            }

            return 0;
        }

        private void RunReceiveStream()
        {
            Background.Run(ReceiveMessagesAsync);
        }
        
        private async Task ReceiveMessagesAsync()
        {
            _connectedEvent.WaitOne();

            var buffer = new byte[1024 * 4];
            bool end = false;
            byte[] endBytes = new byte[16];
            bool isClosed = false;
            int length;

            while (_incoming != null && _incoming.IsConnected && !isClosed)
            {
                StreamMessage message = null;
                end = false;

                // receive a single message
                using (var memory = new MemoryStream())
                {
                    try
                    {
                        do
                        {
                            // read the length
                            _interrupt.BeforeReadLength();
                            length = await ReadFromStreamAsync(buffer, 0, 4).ConfigureAwait(false);
                            if (length == 0)
                            {
                                isClosed = true;
                                break;
                            }

                            var count = BitConverter.ToInt32(buffer, 0);

                            // read the content
                            _interrupt.BeforeReadContent();
                            length = await ReadFromStreamAsync(buffer, 0, count).ConfigureAwait(false);
                            if (length == 0)
                            {
                                isClosed = true;
                                break;
                            }

                            memory.Write(buffer, 0, length);

                            // read the end bit
                            _interrupt.BeforeReadEnd();
                            length = await ReadFromStreamAsync(buffer, 0, 1).ConfigureAwait(false);
                            if (length == 0)
                            {
                                isClosed = true;
                                break;
                            }

                            end = BitConverter.ToBoolean(buffer, 0);
                        }
                        while (!end && !isClosed);
                    }
                    catch(Exception)
                    {
                        isClosed = true;
                    }

                    if (end && !isClosed && memory.Length > 0)
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
                await DispatchMessage(message).ConfigureAwait(false);
            }

            await Disconnect().ConfigureAwait(false);
        }

        private async Task<int> ReadFromStreamAsync(byte[] buffer, int startIndex, int count)
        {
            try
            {
                if (_incoming != null)
                {
                    var length = await _incoming.ReadAsync(buffer, startIndex, count).ConfigureAwait(false);
                    return length;
                }
            }
            catch(ObjectDisposedException)
            {
                // _incoming was disposed by a disconnect
            }

            return 0;
        }

        private async Task DispatchMessage(StreamMessage message)
        {
            if (message != null)
            {
                if (message.StatusCode != 0)
                {
                    // It's a response
                    if (_responses.TryGetValue(message.RequestId, out TaskCompletionSource<StreamMessage> signal))
                    {
                        await Task.Run(() => { signal.SetResult(message); }).ConfigureAwait(false);
                    }
                }
                else
                {
                    // It's a request
                    if (_requestHandler != null)
                    {
                        Background.Run(async () => { await HandleRequestAsync(_requestHandler, message).ConfigureAwait(false); });
                    }
                }
            }
        }

        private async Task<StreamMessage> GetResponseAsync(string requestId)
        {
            TaskCompletionSource<StreamMessage> responseTask = new TaskCompletionSource<StreamMessage>();

            if(!_responses.TryAdd(requestId, responseTask))
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
                _responses.TryRemove(requestId, out responseTask);
            }
        }

        private async Task HandleRequestAsync(StreamRequestHandler<StreamMessage> serverRequestHandler, StreamMessage request)
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
