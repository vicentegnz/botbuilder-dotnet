// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.TestBot
{
    public sealed class MyRootDialog : Dialog
    {
        private readonly IMakeBelieveService _makeBelieveService;

        public MyRootDialog(IMakeBelieveService makeBelieveService)
        {
            _makeBelieveService = makeBelieveService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Type == ActivityTypes.ConversationUpdate
                    &&
                dc.Context.Activity.MembersAdded[0].Id != dc.Context.Activity.Recipient.Id)
            {
                await dc.Context.SendActivityAsync("Welcome to the future of Dialogs!").ConfigureAwait(false);
            }

            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Type != ActivityTypes.Message)
            {
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }

            await _makeBelieveService.MakeBelieveAsync();

            dc.ActiveDialog.State["currentStatus"] = "pendingConfirmationPrompt";

            return await dc.PromptAsync(
                "confirmPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Are you ready?"),
                    RetryPrompt = MessageFactory.Text("I asked if you were ready...?"),
                },
                cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (reason == DialogReason.EndCalled)
            {
                switch (dc.ActiveDialog.State["currentStatus"] as string)
                {
                    case "pendingConfirmationPrompt":
                        if ((bool)result == false)
                        {
                            await dc.Context.SendActivityAsync("Too bad, you don't know what you're missing.").ConfigureAwait(false);
                            await dc.Context.SendActivityAsync(new Activity { Type = ActivityTypes.EndOfConversation });

                            return await dc.EndDialogAsync();
                        }

                        dc.ActiveDialog.State["currentStatus"] = "showingWaterfall";

                        return await dc.BeginDialogAsync("waterfall", cancellationToken: cancellationToken).ConfigureAwait(false);

                    default:
                        await dc.Context.SendActivityAsync($"Hope you enjoyed it! See ya next time! [{ScopeId}]");
                        await dc.Context.SendActivityAsync(new Activity { Type = ActivityTypes.EndOfConversation });

                        return await dc.EndDialogAsync();
                }
            }

            return await base.ResumeDialogAsync(dc, reason, result, cancellationToken).ConfigureAwait(false);
        }
    }


}
