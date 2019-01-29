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
    internal class PipeServer
    {
        private readonly string _baseName;
        private readonly StreamRequestHandler _requestHandler;
        private PipeConnection _connection;

        public PipeServer(string baseName, StreamRequestHandler requestHandler)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;

            _connection = new PipeConnection(_requestHandler);
            _connection.Disconnected += OnConnectionDisconnected;
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            // Try to rerun the server connection 
            Task.Run(() => StartAsync());
        }

        public async Task StartAsync()
        {
            var incomingPipeName = _baseName + PipeConnection.ServerIncomingPath;
            var incomingServer = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incomingServer.WaitForConnectionAsync().ConfigureAwait(false);

            var outgoingPipeName = _baseName + PipeConnection.ServerOutgoingPath;
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
    }
}
