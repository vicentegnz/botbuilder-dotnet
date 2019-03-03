using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol
{
    public class ReceiveBase
    {
        public List<ContentFeed> ContentFeeds { get; set; }
        
        public void SetContentFeed(byte id, IDictionary<string, string> headers)
        {
            if (ContentFeeds == null)
            {
                InitializeContentFeeds(id + 1);
            }
            else if(ContentFeeds.Capacity <= id + 1)
            {
                // there is not room for this feed in the list so make it bigger
                var feeds = ContentFeeds;
                InitializeContentFeeds(id + 1);
                for (int i = 0; i < feeds.Count; i++)
                {
                    ContentFeeds[i] = feeds[i];
                }
            }

            ContentFeeds[id] = new ContentFeed() {Id = id, Headers = headers};
        }

        public void SetContentFeed(byte id, Stream content)
        {
            if (ContentFeeds == null)
            {
                InitializeContentFeeds(id + 1);
            }
            else if (ContentFeeds.Capacity <= id + 1)
            {
                // there is not room for this feed in the list so make it bigger
                var feeds = ContentFeeds;
                InitializeContentFeeds(id + 1);
                for (int i = 0; i < feeds.Count; i++)
                {
                    ContentFeeds[i] = feeds[i];
                }
            }

            ContentFeeds[id] = new ContentFeed() { Id = id, Content = content};
        }

        public void InitializeContentFeeds(int count)
        {
            ContentFeeds = new List<ContentFeed>(count);
            for(int i=0; i< count; i++)
            {
                ContentFeeds.Add(null);
            }
        }
    }
}
