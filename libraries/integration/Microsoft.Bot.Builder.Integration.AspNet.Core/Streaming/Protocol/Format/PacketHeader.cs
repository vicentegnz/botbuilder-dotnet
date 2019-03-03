using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public class PacketHeader
    {
        public byte Type { get; set; }

        public Guid RequestId { get; set; }

        public int Length { get; set; }

        public bool IsEnd { get; set; }

        public byte[] TypeHeader { get; set; }      // optional extra information that is type specific
    }
}
