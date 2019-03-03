using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Format;
using Microsoft.Bot.Streaming.Transport;

namespace Microsoft.Bot.Streaming.Protocol.Managers
{
    /// <summary>
    /// On Send: queues up sends and sends them along the transport
    /// On Receive: receives a packet header and some bytes and dispatches it to the subscriber
    /// </summary>
    public class PacketManager : IPacketManager
    {
        private const int MaxChunkLength = 4096;

        private Func<PacketHeader, Stream> _getStream;
        private Action<PacketHeader, Stream, int> _receiveAction;
        private readonly SendQueue<Packet> _sendQueue;
        private readonly EventWaitHandle _connectedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private ITransportReader _incoming;
        private ITransportWriter _outgoing;

        public PacketManager()
        {
            _sendQueue = new SendQueue<Packet>(this.WritePacketAsync);
        }

        public event EventHandler Disconnected;

        public bool IsConnected
        {
            get { return _incoming != null && _outgoing != null; }
        }

        public void Connect(ITransportReader incoming, ITransportWriter outgoing)
        {
            if (_incoming != null || _outgoing != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _incoming = incoming;
            _outgoing = outgoing;

            RunReceive();

            _connectedEvent.Set();
        }

        public void Subscribe(
            Func<PacketHeader, Stream> getStream,
            Action<PacketHeader, Stream, int> receiveAction)
        {
            _getStream = getStream;
            _receiveAction = receiveAction;
        }

        public void SendPacket(Packet packet)
        {
            _sendQueue.Post(packet);
        }

        public void Disconnect(EventArgs e = null)
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
                Disconnected?.Invoke(this, e ?? EventArgs.Empty);
            }
        }

        private byte[] _sendHeaderBuffer = new byte[ProtocolSerializer.PacketHeaderLength];
        private byte[] _sendContentBuffer = new byte[MaxChunkLength];

        private async Task WritePacketAsync(Packet packet)
        {
            _connectedEvent.WaitOne();
            
            try
            {
                int length;

                // Note: there are no zero-byte sends; sending a length of 0 means the pipe was closed

                ProtocolSerializer.Serialize(packet.Header, _sendHeaderBuffer, 0);

                // Send: Packet Header
                length = await _outgoing.WriteAsync(_sendHeaderBuffer, 0, _sendHeaderBuffer.Length).ConfigureAwait(false);
                if (length == 0)
                {
                    throw new TransportDisconnectedException();
                }

                if (packet.Header.TypeHeader != null)
                {
                    // Send: Packet Optional Type Header 
                    length = await _outgoing.WriteAsync(packet.Header.TypeHeader, 0, packet.Header.TypeHeader.Length).ConfigureAwait(false);
                    if (length == 0)
                    {
                        throw new TransportDisconnectedException();
                    }
                }

                int offset = 0;

                // Send content in MaxChunkLength chunks
                do
                {
                    int count = Math.Min(packet.Header.Length - offset, MaxChunkLength);

                    // copy the stream to the buffer
                    count = await packet.Content.ReadAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);

                    // Send: Packet content
                    length = await _outgoing.WriteAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);
                    if (length == 0)
                    {
                        throw new TransportDisconnectedException();
                    }

                    offset += count;
                } while (offset < packet.Header.Length);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void RunReceive()
        {
            Background.Run(ReceivePacketsAsync);
        }

        private async Task ReceivePacketsAsync()
        {
            _connectedEvent.WaitOne();

            var contentBuffer = new byte[MaxChunkLength];
            var headerBuffer = new byte[ProtocolSerializer.PacketHeaderLength];
            bool isClosed = false;
            int length;

            while (_incoming != null && _incoming.IsConnected && !isClosed)
            {
                // receive a single packet
                try
                {
                    // read the packet header
                    length = await _incoming.ReadAsync(headerBuffer, 0, headerBuffer.Length).ConfigureAwait(false);
                    if (length == 0)
                    {
                        throw new TransportDisconnectedException();
                    }

                    // parse the packet header
                    var packetHeader = ProtocolSerializer.Deserialize(headerBuffer, 0);


                    // determine if there is any type header
                    var typeHeaderLength = ProtocolType.GetTypeHeaderLength(packetHeader.Type);

                    if(typeHeaderLength > 0)
                    {
                        packetHeader.TypeHeader = new byte[typeHeaderLength];
                        length = await _incoming.ReadAsync(packetHeader.TypeHeader, 0, typeHeaderLength).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException();
                        }
                    }
                    
                    // read the packet content
                    var contentStream = _getStream(packetHeader);
                    int offset = 0;

                    do
                    {
                        // read in chunks
                        int count = Math.Min(packetHeader.Length - offset, MaxChunkLength);

                        // read the content
                        length = await _incoming.ReadAsync(contentBuffer, 0, count).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException();
                        }

                        await contentStream.WriteAsync(contentBuffer, 0, length).ConfigureAwait(false);

                        offset += length;
                    } while (offset < packetHeader.Length);

                    _receiveAction(packetHeader, contentStream, offset);
                }
                catch (Exception)
                {
                    isClosed = true;
                }
            }

            Disconnect();
        }
    }
}
