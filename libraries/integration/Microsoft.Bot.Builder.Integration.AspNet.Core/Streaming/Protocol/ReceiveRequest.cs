using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class ReceiveRequest : ReceiveBase
    {
        public ReceiveRequest(int contentFeedCount = 0)
        {
            InitializeContentFeeds(contentFeedCount);
        }

        /// <summary>
        /// Request verb, null on responses
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Request path; null on responses
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// header pairs
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }
    }
}
