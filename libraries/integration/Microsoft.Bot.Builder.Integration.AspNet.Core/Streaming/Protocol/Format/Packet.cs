using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Format;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public class Packet
    {
        public PacketHeader Header { get; set; }

        public Stream Content { get; set; }
    }
}
