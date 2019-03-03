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
    public static class ResponseExtensions
    {
        public static void AddHeader(this Response message, string key, string value)
        {
            if (key == null)
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
        
        public static void SetBody(this Response response, string body)
        {
            response.AddContentFeed(new StringContent(body, Encoding.UTF8));
        }
        
        public static void SetBody(this Response response, object body)
        {
            var json = JsonConvert.SerializeObject(body, SerializationSettings.BotSchemaSerializationSettings);
            response.AddContentFeed(new StringContent(json, Encoding.UTF8, "application/json"));
        }

        public static T ReadBodyAsJson<T>(this ReceiveBase receiveBase)
        {
            ContentFeed contentFeed = receiveBase.ContentFeeds?.FirstOrDefault();
            if (contentFeed != null && contentFeed.Content != null)
            {
                using (var reader = new StreamReader(contentFeed.Content, Encoding.UTF8))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        var serializer = JsonSerializer.Create(SerializationSettings.DefaultDeserializationSettings);
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }
            }
            return default(T);
        }

        public static string ReadBodyAsString(this ReceiveBase receiveBase)
        {
            ContentFeed contentFeed = receiveBase.ContentFeeds?.FirstOrDefault();
            if (contentFeed != null && contentFeed.Content != null)
            {
                using (var reader = new StreamReader(contentFeed.Content, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            return null;
        }
    }
}
