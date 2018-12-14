// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.TestBot
{
    public sealed class MyReallyAwesomeMakeBelieveService : IMakeBelieveService
    {
        private readonly ILogger<MyReallyAwesomeMakeBelieveService> _logger;

        public MyReallyAwesomeMakeBelieveService(ILogger<MyReallyAwesomeMakeBelieveService> logger)
        {
            _logger = logger;
        }

        public async Task MakeBelieveAsync()
        {
            _logger.LogInformation("I'm a believer!");

            await Task.Delay(500);
        }
    }


}
