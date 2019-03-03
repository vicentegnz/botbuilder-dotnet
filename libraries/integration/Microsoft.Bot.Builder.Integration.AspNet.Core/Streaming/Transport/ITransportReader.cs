using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Transport
{
    public interface ITransportReader : ITransport
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
    }
}
