using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Headers;
using Newtonsoft.Json;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public static class ProtocolSerializer
    {
        private static JsonSerializer Serializer = JsonSerializer.Create(SerializationSettings.DefaultSerializationSettings);

        public const int PacketHeaderLength = 22;

        public static void Serialize(PacketHeader packetHeader, byte[] buffer, int offset)
        {
            buffer[offset + 0] = packetHeader.Type;

            var guidBytes = packetHeader.RequestId.ToByteArray();
            Array.Copy(guidBytes, 0, buffer, offset + 1, 16);

            var lengthBytes = BitConverter.GetBytes(packetHeader.Length);
            Array.Copy(lengthBytes, 0, buffer, offset + 17, 4);

            var endBytes = BitConverter.GetBytes(packetHeader.IsEnd);
            Array.Copy(lengthBytes, 0, buffer, offset + 21, 1);
        }

        public static PacketHeader Deserialize(byte[] buffer, int offset)
        {
            var guidBytes = new byte[16];
            Array.Copy(buffer, 1, guidBytes, 0, 16);
            return new PacketHeader()
            {
                Type = buffer[offset],
                RequestId = new Guid(guidBytes),
                Length = BitConverter.ToInt32(buffer, offset + 17),
                IsEnd = BitConverter.ToBoolean(buffer, offset + 21),
            };
        }
        
        public static Payload Serialize<T>(byte type, Guid requestId, T item)
        {
            var memoryStream = new MemoryStream();
            using (var textWriter = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true))
            {
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    Serializer.Serialize(jsonWriter, item);
                    jsonWriter.Flush();
                }
            }
            memoryStream.Position = 0;

            return new Payload()
            {
                Type = type,
                RequestId = requestId,
                Content = memoryStream,
                ContentLength = (int)memoryStream.Length
            };
        }

        public static T Deserialize<T>(Payload payload)
        {
            using (var textReader = new StreamReader(payload.Content))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var item = Serializer.Deserialize<T>(jsonReader);
                    return item;
                }
            }
        }

        public static async Task<Payload[]> Serialize(HttpContent content, Guid requestId, byte contentId, byte direction)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in content.Headers)
            {
                headers.Add(header.Key, header.Value.FirstOrDefault());
            }

            var contentHeader = new ContentHeader()
            {
                Id = contentId,
                Headers = headers,
                Direction = direction
            };

            var contentPayload = Serialize(ProtocolType.ContentHeader, requestId, contentHeader);

            // create the content payload
            var contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            return new Payload[]
            {
                contentPayload,
                new Payload()
                {
                    Type = ProtocolType.Content,
                    TypeHeader = new byte[] { contentId },
                    RequestId = requestId,
                    Content = contentStream,
                    ContentLength = (int)contentStream.Length
                }
            };
        }

        public static async Task<List<Payload>> Serialize(Guid requestId, Request request)
        {
            List<Payload> payloads = new List<Payload>();

            var requestHeader = new RequestHeader()
            {
                Method = request.Method,
                Path = request.Path,
                Headers = request.Headers,
                ContentFeedCount = request.ContentFeeds != null ? request.ContentFeeds.Count : 0
            };

            payloads.Add(Serialize(ProtocolType.Request, requestId, requestHeader));

            if (request.ContentFeeds != null)
            {
                var contentPayloads = await Serialize(requestId, request.ContentFeeds, ContentHeader.RequestDirection).ConfigureAwait(false);
                payloads.AddRange(contentPayloads);
            }

            return payloads;
        }

        public static async Task<List<Payload>> Serialize(Guid requestId, Response response)
        {
            List<Payload> payloads = new List<Payload>();

            var responseHeader = new ResponseHeader()
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                ContentFeedCount = response.ContentFeeds != null ? response.ContentFeeds.Count : 0
            };

            payloads.Add(Serialize(ProtocolType.Response, requestId, responseHeader));

            if (response.ContentFeeds != null)
            {
                var contentPayloads = await Serialize(requestId, response.ContentFeeds, ContentHeader.ResponseDirection).ConfigureAwait(false);
                payloads.AddRange(contentPayloads);
            }

            return payloads;
        }

        private static async Task<List<Payload>> Serialize(Guid requestId, List<HttpContent> contentFeeds, byte direction)
        {
            List<Payload> payloads = new List<Payload>();

            byte contentId = 0x00;
            foreach (var contentFeed in contentFeeds)
            {
                var contentPayloads = await Serialize(contentFeed, requestId, contentId, direction).ConfigureAwait(false);
                payloads.AddRange(contentPayloads);
            }

            return payloads;
        }
    }
}
