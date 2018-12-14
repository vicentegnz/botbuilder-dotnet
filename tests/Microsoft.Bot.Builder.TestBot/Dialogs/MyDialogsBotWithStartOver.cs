// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.TestBot
{
    public sealed class MyDialogsBotWithStartOver : DialogsBot
    {
        private readonly ILogger<MyDialogsBotWithStartOver> _logger;

        public MyDialogsBotWithStartOver(IDialogFactory dialogFactory, IStatePropertyAccessor<DialogState> dialogStatePropertyAccessor, ILogger<MyDialogsBotWithStartOver> logger)
            : base(dialogFactory, dialogStatePropertyAccessor)
        {
            _logger = logger;
        }

        protected override async Task<bool> OnBeforeExecuteNextDialogTurnAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Before executing next dialog...");

            var activity = dialogContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message
                    &&
                activity.Text.Equals("start over", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("User requested to start over!");

                await dialogContext.CancelAllDialogsAsync(cancellationToken);
            }

            return true;
        }

        protected override Task OnAfterExecuteDialogTurnAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("After executing dialog...");

            return Task.CompletedTask;
        }
    }


}
