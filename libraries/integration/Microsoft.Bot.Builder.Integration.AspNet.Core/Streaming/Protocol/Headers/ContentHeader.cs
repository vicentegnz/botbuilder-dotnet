using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Headers
{
    public class ContentHeader
    {
        public byte Id { get; set; }

        /// <summary>
        /// header pairs
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        public byte Direction { get; set; }

        public const byte RequestDirection = 0x00;
        public const byte ResponseDirection = 0x01;
    }
}
