// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.TestBot
{
    public static class DialogFactoryBuilderExtensions
    {
        public static DialogFactoryBuilder AddMyReusableDialog(this DialogFactoryBuilder dialogFactoryBuilder, string dialogId)
        {
            return dialogFactoryBuilder
                .AddDialogScope(dialogId, scopeBuilder =>
                {
                    scopeBuilder
                        .AddDialog<MyReusableDialog>(Dialog.RootDialogId)
                        .AddPrompt<ConfirmPrompt, bool>("myOwnConfirmPrompt");
                });
        }
    }

    public sealed class MyReusableDialog : Dialog
    {
        private readonly IMakeBelieveService _makeBelieveService;

        public MyReusableDialog(IMakeBelieveService makeBelieveService)
        {
            _makeBelieveService = makeBelieveService;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await dc.Context.SendActivityAsync("This dialog offers some reusable functionality that I've packaged up and can share across bots.").ConfigureAwait(false);

            dc.ActiveDialog.State["currentStatus"] = "pendingConfirmationPrompt";

            return await dc.PromptAsync(
                "myOwnConfirmPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want to invoke the waterfall prompt from the outer scope?"),
                },
                cancellationToken);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.ContinueDialogAsync(dc, cancellationToken);
        }

        public override Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (reason == DialogReason.EndCalled)
            {
                await _makeBelieveService.MakeBelieveAsync();


                switch (dc.ActiveDialog.State["currentStatus"] as string)
                {
                    case "pendingConfirmationPrompt":
                        if ((bool)result == false)
                        {
                            await dc.Context.SendActivityAsync("Aw man. 😭").ConfigureAwait(false);
                            await dc.Context.SendActivityAsync(new Activity { Type = ActivityTypes.EndOfConversation });

                            return await dc.EndDialogAsync();
                        }

                        dc.ActiveDialog.State["currentStatus"] = "showingWaterfall";

                        // This will invoke a dialog named waterfall from the parent scope
                        return await dc.BeginDialogAsync("waterfall", cancellationToken: cancellationToken).ConfigureAwait(false);

                    default:
                        await dc.Context.SendActivityAsync("Remember to tell everyone you used this reusable dialog!");
                        await dc.Context.SendActivityAsync(new Activity { Type = ActivityTypes.EndOfConversation });

                        return await dc.EndDialogAsync();
                }
            }

            return await base.ResumeDialogAsync(dc, reason, result, cancellationToken).ConfigureAwait(false);
        }
    }


}
