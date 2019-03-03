using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol;
using Microsoft.Bot.Streaming.Protocol.Managers;
using Microsoft.Bot.Streaming.Transport;

namespace Microsoft.Bot.Streaming
{
    public class NamedPipeClient
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly PacketManager _connection;
        private readonly RequestManager _requestManager;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;

        public NamedPipeClient(string baseName, RequestHandler requestHandler = null, bool autoReconnect = true)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _connection = new PacketManager();
            _requestManager = new RequestManager();
            _protocolAdapter = new ProtocolAdapter(_requestHandler, _connection, _requestManager);

            _connection.Disconnected += OnConnectionDisconnected;
        }
        
        public async Task ConnectAsync()
        {
            var outgoingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var outgoing = new NamedPipeClientStream(".", outgoingPipeName, PipeDirection.Out, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoing.ConnectAsync().ConfigureAwait(false);

            var incomingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var incoming = new NamedPipeClientStream(".", incomingPipeName, PipeDirection.In, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incoming.ConnectAsync().ConfigureAwait(false);

            _connection.Connect(
                new NamedPipeTransport(incoming),
                new NamedPipeTransport(outgoing));
        }

        public Task<ReceiveResponse> SendAsync(Request message)
        {
            return _protocolAdapter.SendRequestAsync(message);
        }

        public void Disconnect()
        {
            _connection.Disconnect();
        }
        
        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            if (_autoReconnect)
            {
                // Try to rerun the client connection 
                Background.Run(ConnectAsync);
            }
        }
    }
}
