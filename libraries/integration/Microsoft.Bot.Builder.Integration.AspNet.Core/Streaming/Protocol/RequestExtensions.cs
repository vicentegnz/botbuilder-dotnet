using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Streaming.Protocol
{
    public static class RequestExtensions
    {
        public static void AddHeader(this Request message, string key, string value)
        {
            if(key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (message.Headers == null)
            {
                message.Headers = new Dictionary<string, string>();
            }
            message.Headers.Add(key, value);
        }
        
        public static void AddAuthorizationHeader(this Request message, string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            message.AddHeader("Authorization", $"Bearer {token}");
        }

        public static string GetAuthorizationHeader(this ReceiveRequest message)
        {
            if (message.Headers != null)
            {
                if(message.Headers.TryGetValue("Authorization", out string header))
                {
                    return header;
                }
            }
            return null;
        }

        public static string GetBearerToken(this ReceiveRequest message)
        {
            var header = message.GetAuthorizationHeader();
            if(!string.IsNullOrEmpty(header) && header.StartsWith("Bearer "))
            {
                return header.Substring(7);
            }
            return null;
        }

        public static void SetBody(this Request request, string body)
        {
            request.AddContentFeed(new StringContent(body, Encoding.UTF8));
        }

        public static void SetBody(this Request request, object body)
        {
            var json = JsonConvert.SerializeObject(body, SerializationSettings.BotSchemaSerializationSettings);
            request.AddContentFeed(new StringContent(json, Encoding.UTF8, "application/json"));
        }
    }
}
