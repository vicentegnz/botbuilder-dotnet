using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal interface ISendBlock<T>
    {
        Task<T> GetAsync();
    }
}
