using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Headers
{
    public class ResponseHeader
    {
        /// <summary>
        /// header pairs
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Status - The Response Status
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Number of content feeds that accompany this response
        /// </summary>
        public int ContentFeedCount { get; set; }
    }
}
