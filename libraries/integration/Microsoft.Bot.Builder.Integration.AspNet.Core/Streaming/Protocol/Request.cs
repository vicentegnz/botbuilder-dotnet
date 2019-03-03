using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class Request
    {
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";

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
        
        public static Request CreateGet(string path = null, HttpContent body = null)
        {
            return CreateRequest(GET, path, body);
        }

        public static Request CreatePost(string path = null, HttpContent body = null)
        {
            return CreateRequest(POST, path, body);
        }

        public static Request CreatePut(string path = null, HttpContent body = null)
        {
            return CreateRequest(PUT, path, body);
        }

        public static Request CreateDelete(string path = null, HttpContent body = null)
        {
            return CreateRequest(DELETE, path, body);
        }

        public static Request CreateRequest(string method, string path = null, HttpContent body = null)
        {
            var request = new Request()
            {
                Method = method,
                Path = path,
                Headers = new Dictionary<string, string>()
            };

            if (body != null)
            {
                request.AddContentFeed(body);
            }

            return request;
        }
    }
}
