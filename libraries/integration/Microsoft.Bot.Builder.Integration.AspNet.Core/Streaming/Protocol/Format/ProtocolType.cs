using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Format
{
    public static class ProtocolType
    {
        public const byte Request = 0x00;
        public const byte Response = 0x01;
        public const byte ContentHeader = 0x02;
        public const byte Content = 0x03;
        public const byte ContentStream = 0x04;
        public const byte Cancel = 0x05;


        public static int GetTypeHeaderLength(byte type)
        {
            switch(type)
            {
                case Content:
                case ContentStream:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
