using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class ReceiveResponse : ReceiveBase
    {
        public ReceiveResponse(int contentFeedCount = 0)
        {
            InitializeContentFeeds(contentFeedCount);
        }

        /// <summary>
        /// Status - The Response Status
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// header pairs
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }
    }
}
