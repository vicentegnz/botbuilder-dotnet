using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";

        public static StreamMessage CreateGet(string path = null, string body = null)
        {
            return CreateRequest(GET, path, body);
        }

        public static StreamMessage CreatePost(string path = null, string body = null)
        {
            return CreateRequest(POST, path, body);
        }

        public static StreamMessage CreatePut(string path = null, string body = null)
        {
            return CreateRequest(PUT, path, body);
        }

        public static StreamMessage CreateDelete(string path = null, string body = null)
        {
            return CreateRequest(DELETE, path, body);
        }

        public static StreamMessage CreateRequest(string method, string path = null, string body = null)
        {
            return new StreamMessage()
            {
                Method = method,
                Path = path,
                Body = body,
            };
        }

        public static StreamMessage NotFound(StreamMessage request, string body = null)
        {
            return CreateResponse(request, HttpStatusCode.NotFound, body);
        }

        public static StreamMessage Forbidden(StreamMessage request, string body = null)
        {
            return CreateResponse(request, HttpStatusCode.Forbidden, body);
        }

        public static StreamMessage OK(StreamMessage request, string body = null)
        {
            return CreateResponse(request, HttpStatusCode.OK, body);
        }

        public static StreamMessage InternalServerError(StreamMessage request, string body = null)
        {
            return CreateResponse(request, HttpStatusCode.InternalServerError, body);
        }

        public static StreamMessage CreateResponse(StreamMessage request, HttpStatusCode statusCode, string body = null)
        {
            return new StreamMessage()
            {
                RequestId = request.RequestId,
                StatusCode = (int)statusCode,
                Body = body
            };
        }
    }
}
