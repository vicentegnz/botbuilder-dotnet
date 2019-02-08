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

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal class NamedPipeClient
    {
        private readonly string _baseName;
        private readonly StreamRequestHandler<StreamMessage> _requestHandler;
        private readonly NamedPipeConnection _connection;
        private readonly bool _autoReconnect;

        public NamedPipeClient(string baseName, StreamRequestHandler<StreamMessage> requestHandler = null, bool autoReconnect = true, NamedPipeConnectionInterrupt interrupt = null)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _connection = new NamedPipeConnection(_requestHandler, interrupt);

            _connection.Disconnected += OnConnectionDisconnected;
        }
        
        public async Task ConnectAsync()
        {
            var outgoingPipeName = _baseName + NamedPipeConnection.ServerIncomingPath;
            var outgoing = new NamedPipeClientStream(".", outgoingPipeName, PipeDirection.Out, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoing.ConnectAsync().ConfigureAwait(false);

            var incomingPipeName = _baseName + NamedPipeConnection.ServerOutgoingPath;
            var incoming = new NamedPipeClientStream(".", incomingPipeName, PipeDirection.In, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incoming.ConnectAsync().ConfigureAwait(false);

            _connection.Connect(incoming, outgoing);
        }

        public Task<StreamMessage> SendAsync(StreamMessage message)
        {
            return _connection.SendAsync(message, true);
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
