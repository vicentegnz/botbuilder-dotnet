using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Protocol.Payloads;
using Microsoft.Bot.Protocol.PayloadTransport;
using Microsoft.Bot.Protocol.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Bot.Protocol
{
    public class NamedPipeClient
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly RequestManager _requestManager;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;
        private bool _isDisconnecting = false;

        public NamedPipeClient(string baseName, RequestHandler requestHandler = null, bool autoReconnect = true)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _requestManager = new RequestManager();

            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;

            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        public async Task ConnectAsync()
        {
            var outgoingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var outgoing = new NamedPipeClientStream(".", outgoingPipeName, PipeDirection.Out, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoing.ConnectAsync().ConfigureAwait(false);

            var incomingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var incoming = new NamedPipeClientStream(".", incomingPipeName, PipeDirection.In, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incoming.ConnectAsync().ConfigureAwait(false);

            _sender.Connect(new NamedPipeTransport(outgoing));
            _receiver.Connect(new NamedPipeTransport(incoming));
        }

        public Task<ReceiveResponse> SendAsync(Request message)
        {
            return _protocolAdapter.SendRequestAsync(message);
        }

        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            if (!_isDisconnecting)
            {
                _isDisconnecting = true;

                try
                {
                    if (_sender.IsConnected)
                    {
                        _sender.Disconnect();
                    }

                    if (_receiver.IsConnected)
                    {
                        _receiver.Disconnect();
                    }

                    if (_autoReconnect)
                    {
                        // Try to rerun the client connection 
                        Background.Run(ConnectAsync);
                    }
                }
                finally
                {
                    _isDisconnecting = false;
                }
            }
        }
    }
}
