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
    public interface IPacketManager
    {
        bool IsConnected { get; }

        void Connect(ITransportReader incoming, ITransportWriter outgoing);

        void Subscribe(Func<PacketHeader, Stream> getStream, Action<PacketHeader, Stream, int> receiveAction);

        void SendPacket(Packet packet);

        void Disconnect(EventArgs e = null);
    }
}
