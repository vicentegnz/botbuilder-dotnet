using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class ContentFeed
    {
        public byte Id { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public Stream Content { get; set; }
    }
}
