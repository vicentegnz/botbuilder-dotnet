// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Bot.Builder.Diagnostics
{
    internal static class BotAdapterDiagnosticSourceExtensions
    {
        private const string RunPipelineAsyncEventName = "Microsoft.BotBuilder.Core.BotAdapter.RunPipelineAsync";

        public static Activity StartRunPipelineAsyncActivity(this DiagnosticSource diagnosticSource, ITurnContext turnContext)
        {
            if (!diagnosticSource.IsEnabled(RunPipelineAsyncEventName, turnContext))
            {
                return null;
            }

            return StartActivity();

            Activity StartActivity()
            {
                var activity = turnContext.Activity;

                var runPipelineAsyncDiagnosticActivity = new Activity(RunPipelineAsyncEventName)
                    .AddBaggage("BotBuilder.Activity.Id", activity.Id)
                    .AddBaggage("BotBuilder.Activity.Conversation.Id", activity.Conversation.Id)
                    .AddTag("BotBuilder.Activity.Type", activity.Type)
                    .AddTag("BotBuilder.Activity.ChannelId", activity.ChannelId);

                diagnosticSource.StartActivity(runPipelineAsyncDiagnosticActivity, new { TurnContext = turnContext });

                return runPipelineAsyncDiagnosticActivity;
            }
        }

        public static void StopBotAdapterRunPipelineAsyncActivity(this DiagnosticSource diagnosticSource, Activity runPipelineAsyncDiagnosticActivity, ITurnContext turnContext, Exception exception = null)
        {
            if (runPipelineAsyncDiagnosticActivity != null)
            {
                var turnState = turnContext.TurnState;

                runPipelineAsyncDiagnosticActivity.AddTag("BotBuilder.TurnContext.Responded", turnContext.Responded.ToString());

                diagnosticSource.StopActivity(runPipelineAsyncDiagnosticActivity, new { TurnContext = turnContext });
            }
        }
    }
}
