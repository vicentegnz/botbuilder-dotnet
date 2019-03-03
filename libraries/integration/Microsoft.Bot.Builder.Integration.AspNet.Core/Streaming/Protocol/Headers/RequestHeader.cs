using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Headers
{
    public class RequestHeader
    {
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

        /// <summary>
        /// Number of content feeds that accompany this request
        /// </summary>
        public int ContentFeedCount { get; set; }
    }
}
