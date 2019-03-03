using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming.Protocol.Headers;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public class ReceiveRequestBuilder
    {
        private ReceiveRequest _request = null;
        
        public ReceiveRequest Get()
        {
            return _request;
        }

        public bool Add(Payload payload)
        {
            if(_request == null)
            {
                // first payload
                var requestHeader = ProtocolSerializer.Deserialize<RequestHeader>(payload);

                _request = new ReceiveRequest()
                {
                    Method = requestHeader.Method,
                    Path = requestHeader.Path,
                    Headers = requestHeader.Headers
                };
                _request.InitializeContentFeeds(requestHeader.ContentFeedCount);

                return requestHeader.ContentFeedCount == 0;
            }
            else if(payload.Type == ProtocolType.ContentHeader)
            {
                var contentHeader = ProtocolSerializer.Deserialize<ContentHeader>(payload);
                _request.SetContentFeed(contentHeader.Id, contentHeader.Headers);
                return false;
            }
            else if (payload.Type == ProtocolType.Content)
            {
                _request.ContentFeeds[payload.TypeHeader[0]].Content = payload.Content;
                return _request.ContentFeeds.All(x => x != null);
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
