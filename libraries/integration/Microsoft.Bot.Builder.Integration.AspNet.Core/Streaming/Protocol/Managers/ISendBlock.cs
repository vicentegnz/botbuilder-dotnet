using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming.Protocol.Managers
{
    public interface ISendBlock<T>
    {
        Task<T> GetAsync();
    }
}
