using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Format;
using Microsoft.Bot.Streaming.Protocol.Managers;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class ActiveReceive
    {
        public Payload ActivePayload { get; set; }

        public ConcurrentWaitQueue<Payload> Payloads { get; private set; } = new ConcurrentWaitQueue<Payload>(CancellationToken.None);
    }

    public class ProtocolAdapter
    {
        private readonly RequestHandler _requestHandler;
        private readonly IPacketManager _packetManager;
        private readonly IRequestManager _requestManager;
        private readonly Dictionary<Guid, ActiveReceive> _activeReceives;

        public ProtocolAdapter(RequestHandler requestHandler, IPacketManager packetManager, IRequestManager requestManager)
        {
            _requestHandler = requestHandler;
            _packetManager = packetManager;
            _requestManager = requestManager;
            _activeReceives = new Dictionary<Guid, ActiveReceive>();

            _packetManager.Subscribe(GetPayloadStream, ReceivePacketAsync);
        }

        public async Task<ReceiveResponse> SendRequestAsync(Request request)
        {
            var requestId = Guid.NewGuid();

            var payloads = await ProtocolSerializer.Serialize(requestId, request).ConfigureAwait(false);

            foreach (var payload in payloads)
            {
                var packets = PayloadShredder.ToPackets(payload);
                foreach (var packet in packets)
                {
                    _packetManager.SendPacket(packet);
                }
            }

            // wait for the response
            var response = await _requestManager.GetResponseAsync(requestId).ConfigureAwait(false);

            return response;
        }

        private Stream GetPayloadStream(PacketHeader packetHeader)
        {
            if (!_activeReceives.TryGetValue(packetHeader.RequestId, out ActiveReceive activeReceive))
            {
                // a new requestId has come in, start a new task to process it as it is received
                activeReceive = new ActiveReceive();
                _activeReceives.Add(packetHeader.RequestId, activeReceive);

                Background.Run(() => ProcessReceive(packetHeader.RequestId, activeReceive));
            }

            if(activeReceive.ActivePayload == null)
            {
                activeReceive.ActivePayload = new Payload()
                {
                    Type = packetHeader.Type,
                    TypeHeader = packetHeader.TypeHeader,
                    RequestId = packetHeader.RequestId,
                    Content = new MemoryStream(),
                    ContentLength = packetHeader.Length
                };
            }
            else
            {
                activeReceive.ActivePayload.ContentLength += packetHeader.Length;
            }
            
            return activeReceive.ActivePayload.Content;
        }

        private void ReceivePacketAsync(PacketHeader packetHeader, Stream contentStream, int length)
        {
            if (!_activeReceives.TryGetValue(packetHeader.RequestId, out ActiveReceive activeReceive))
            {
                throw new InvalidOperationException("active receive should exist");
            }

            if (packetHeader.IsEnd)
            {
                activeReceive.ActivePayload.Content.Position = 0;
                activeReceive.Payloads.Enqueue(activeReceive.ActivePayload);
                activeReceive.ActivePayload = null;
            }
        }

        private async Task ProcessReceive(Guid requestId, ActiveReceive activeReceive)
        {
            var payload = await activeReceive.Payloads.Dequeue().ConfigureAwait(false);

            // as payloads come in, parse them and dispatch them
            switch (payload.Type)
            {
                case ProtocolType.Request:
                    {
                        var builder = new ReceiveRequestBuilder();
                        while(!builder.Add(payload))
                        {
                            payload = await activeReceive.Payloads.Dequeue().ConfigureAwait(false);
                        }

                        var request = builder.Get();

                        // request is done, we can handle it
                        if (_requestHandler != null)
                        {
                            var response = await _requestHandler.ProcessRequestAsync(request).ConfigureAwait(false);
                            if (response != null)
                            {
                                await SendResponseAsync(requestId, response).ConfigureAwait(false);
                            }
                        }
                    }
                    break;

                case ProtocolType.Response:
                    {
                        var builder = new ReceiveResponseBuilder();
                        while (!builder.Add(payload))
                        {
                            payload = await activeReceive.Payloads.Dequeue().ConfigureAwait(false);
                        }

                        var response = builder.Get();

                        // we received the response to something, signal it
                        await _requestManager.SignalResponse(requestId, response).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private async Task SendResponseAsync(Guid requestId, Response response)
        {
            var payloads = await ProtocolSerializer.Serialize(requestId, response).ConfigureAwait(false);

            foreach (var payload in payloads)
            {
                var packets = PayloadShredder.ToPackets(payload);
                foreach (var packet in packets)
                {
                    _packetManager.SendPacket(packet);
                }
            }
        }
    }
}
