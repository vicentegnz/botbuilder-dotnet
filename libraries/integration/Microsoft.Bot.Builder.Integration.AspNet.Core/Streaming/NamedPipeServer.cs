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
    internal class NamedPipeServer
    {
        private readonly string _baseName;
        private readonly StreamRequestHandler<StreamMessage> _requestHandler;
        private readonly NamedPipeConnection _connection;
        private readonly bool _autoReconnect;

        public NamedPipeServer(string baseName, StreamRequestHandler<StreamMessage> requestHandler, bool autoReconnect = true, NamedPipeConnectionInterrupt interrupt = null)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _connection = new NamedPipeConnection(_requestHandler, interrupt);
            _connection.Disconnected += OnConnectionDisconnected;
        }

        public async Task StartAsync()
        {
            var incomingPipeName = _baseName + NamedPipeConnection.ServerIncomingPath;
            var incomingServer = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incomingServer.WaitForConnectionAsync().ConfigureAwait(false);

            var outgoingPipeName = _baseName + NamedPipeConnection.ServerOutgoingPath;
            var outgoingServer = new NamedPipeServerStream(outgoingPipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoingServer.WaitForConnectionAsync().ConfigureAwait(false);

            _connection.Connect(incomingServer, outgoingServer);
        }

        public Task<StreamMessage> SendAsync(StreamMessage message)
        {
            return _connection.SendAsync(message, true);
        }

        public Task<StreamMessage> SendAsync(string method, string path, IDictionary<string, string> headers, string body = null)
        {
            return _connection.SendAsync(method, path, headers, body);
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
