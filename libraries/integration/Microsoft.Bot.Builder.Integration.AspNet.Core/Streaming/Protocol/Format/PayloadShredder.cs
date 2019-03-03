using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public static class PayloadShredder
    {
        // this determines the degree of interleaving
        public const int MaxPacketSize = 4096;

        public static IEnumerable<Packet> ToPackets(Payload payload)
        {
            int offset = 0;
            do
            {
                int count = Math.Min(payload.ContentLength - offset, MaxPacketSize);
                offset += count;

                var packet = new Packet()
                {
                    Header = new PacketHeader()
                    {
                        Type = payload.Type,
                        RequestId = payload.RequestId,
                        Length = count,
                        IsEnd = offset >= payload.ContentLength,
                        TypeHeader = payload.TypeHeader
                    },
                    Content = payload.Content       // the same stream is used because as they are read the stream advances
                };

                yield return packet;

            } while (offset < payload.ContentLength);
        }
    }
}
