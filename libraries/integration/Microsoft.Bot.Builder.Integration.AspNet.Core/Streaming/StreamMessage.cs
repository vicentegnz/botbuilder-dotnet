using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal class StreamMessage
    {
        /// <summary>
        /// RequestId -- a new value for new requests, or the corresponding value for responses
        /// </summary>
        public string RequestId { get; set; }

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
        /// content body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Status - The Response Status
        /// an HttpStatus, or 0 if it's a new request
        /// </summary>
        public int StatusCode { get; set; }
    }
}
