using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Streaming
{
    internal static class Background
    {
        /// <summary>
        /// Register background task with ASP.Net hosting environment and trace exceptions
        /// Falls back to Thread pool if not running under ASP.Net
        /// </summary>
        /// <param name="task">background task to execute</param>
        /// <param name="properties">name value pairs to trace if an exception is thrown</param>
        public static void Run(Func<Task> task, IDictionary<string, object> properties = null)
        {
            Run((ct) => task(), properties);
        }

        /// <summary>
        /// Register background task with ASP.Net hosting environment and trace exceptions
        /// Falls back to Thread pool if not running under ASP.Net
        /// </summary>
        /// <param name="task">background task to execute</param>
        /// <param name="properties">name value pairs to trace if an exception is thrown</param>
        public static void Run(Func<CancellationToken, Task> task, IDictionary<string, object> properties = null)
        {
            Task.Run(() => TrackAsRequestAsync(() => task(CancellationToken.None), properties));
        }

        private static async Task TrackAsRequestAsync(Func<Task> task, IDictionary<string, object> properties)
        {
            try
            {
                await task().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
    }
}
