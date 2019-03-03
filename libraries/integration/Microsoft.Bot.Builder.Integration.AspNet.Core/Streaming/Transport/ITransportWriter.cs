using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Transport
{
    public interface ITransportWriter : ITransport
    {
        Task<int> WriteAsync(byte[] buffer, int offset, int count);
    }
}
