// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.TestBot
{
    public sealed class MyComponentDialogsDefaultMakeBelieveService : IMakeBelieveService
    {
        private readonly ILogger<MyComponentDialogsDefaultMakeBelieveService> _logger;

        public MyComponentDialogsDefaultMakeBelieveService(ILogger<MyComponentDialogsDefaultMakeBelieveService> logger)
        {
            _logger = logger;
        }

        public async Task MakeBelieveAsync()
        {
            _logger.LogInformation("I'm not so sure...");

            await Task.Delay(5000);
        }
    }


}
