using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class Response
    {
        /// <summary>
        /// header pairs
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Status - The Response Status
        /// </summary>
        public int StatusCode { get; set; }
        
        public List<HttpContent> ContentFeeds { get; set; }

        public void AddContentFeed(HttpContent contentFeed)
        {
            if (contentFeed == null)
            {
                throw new ArgumentNullException(nameof(contentFeed));
            }

            if (ContentFeeds == null)
            {
                ContentFeeds = new List<HttpContent>();
            }

            ContentFeeds.Add(contentFeed);
        }

        public static Response NotFound(HttpContent body = null)
        {
            return CreateResponse(HttpStatusCode.NotFound, body);
        }

        public static Response Forbidden(HttpContent body = null)
        {
            return CreateResponse(HttpStatusCode.Forbidden, body);
        }

        public static Response OK(HttpContent body = null)
        {
            return CreateResponse(HttpStatusCode.OK, body);
        }

        public static Response InternalServerError(HttpContent body = null)
        {
            return CreateResponse(HttpStatusCode.InternalServerError, body);
        }

        public static Response CreateResponse(HttpStatusCode statusCode, HttpContent body = null)
        {
            var response = new Response()
            {
                StatusCode = (int)statusCode,
                Headers = new Dictionary<string, string>()
            };

            if (body != null)
            {
                response.AddContentFeed(body);
            }

            return response;
        }
    }
}
