using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public class Payload
    {
        public byte Type { get; set; }

        public byte[] TypeHeader { get; set; }

        public Guid RequestId { get; set; }

        public Stream Content { get; set; }

        public int ContentLength { get; set; }
    }
}
