using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol;
using Microsoft.Bot.Streaming.Protocol.Managers;
using Microsoft.Bot.Streaming.Transport;

namespace Microsoft.Bot.Streaming
{
    public class NamedPipeServer
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly RequestManager _requestManager;
        private readonly PacketManager _connection;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;

        public NamedPipeServer(string baseName, RequestHandler requestHandler, bool autoReconnect = true)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _connection = new PacketManager();
            _requestManager = new RequestManager();
            _protocolAdapter = new ProtocolAdapter(_requestHandler, _connection, _requestManager);
            _connection.Disconnected += OnConnectionDisconnected;
        }

        public async Task StartAsync()
        {
            var incomingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var incomingServer = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incomingServer.WaitForConnectionAsync().ConfigureAwait(false);

            var outgoingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var outgoingServer = new NamedPipeServerStream(outgoingPipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoingServer.WaitForConnectionAsync().ConfigureAwait(false);
            
            _connection.Connect(
                new NamedPipeTransport(incomingServer), 
                new NamedPipeTransport(outgoingServer));
        }

        public Task<ReceiveResponse> SendAsync(string method, string path, IDictionary<string, string> headers, HttpContent body = null)
        {
            var request = new Request() { Method = method, Path = path, Headers = headers, ContentFeeds = new List<HttpContent>() { } };
            if(body != null)
            {
                request.ContentFeeds.Add(body);
            }
            return SendAsync(request);
        }

        public Task<ReceiveResponse> SendAsync(Request request)
        {
            return _protocolAdapter.SendRequestAsync(request);
        }

        public void Disconnect()
        {
            _connection.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            if (_autoReconnect)
            {
                // Try to rerun the server connection 
                Background.Run(StartAsync);
            }
        }
    }
}
