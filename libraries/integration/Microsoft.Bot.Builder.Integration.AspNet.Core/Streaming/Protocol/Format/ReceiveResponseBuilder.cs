using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Headers;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public class ReceiveResponseBuilder
    {
        private ReceiveResponse _response = null;

        public ReceiveResponse Get()
        {
            return _response;
        }
        
        public bool Add(Payload payload)
        {
            if(_response == null)
            {
                // first payload
                var responseHeader = ProtocolSerializer.Deserialize<ResponseHeader>(payload);

                _response = new ReceiveResponse()
                {
                    StatusCode = responseHeader.StatusCode,
                    Headers = responseHeader.Headers
                };
                _response.InitializeContentFeeds(responseHeader.ContentFeedCount);

                return responseHeader.ContentFeedCount == 0;
            }
            else if(payload.Type == ProtocolType.ContentHeader)
            {
                var contentHeader = ProtocolSerializer.Deserialize<ContentHeader>(payload);
                _response.SetContentFeed(contentHeader.Id, contentHeader.Headers);
                return false;
            }
            else if (payload.Type == ProtocolType.Content)
            {
                _response.ContentFeeds[payload.TypeHeader[0]].Content = payload.Content;
                return _response.ContentFeeds.All(x => x != null);
            }
            else if (payload.Type == ProtocolType.ContentStream)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
