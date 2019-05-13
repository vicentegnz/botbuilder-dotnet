using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using SurfaceRealizationV2;

namespace Microsoft.Bot.Builder.LanguageGeneration.GrmCheckers
{
    public class GrammarCheckerMiddleware : IMiddleware
    {
        private static readonly SurfaceRealizerENUS surfaceRealizer = new SurfaceRealizerENUS();

        /// <summary>
        /// Use grammar checker to check output activity.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        /// <param name="nextTurn">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <seealso cref="ITurnContext"/>
        /// <seealso cref="Bot.Schema.IActivity"/>
        public async Task OnTurnAsync(ITurnContext context, NextDelegate nextTurn, CancellationToken cancellationToken)
        {
            BotAssert.ContextNotNull(context);
            // hook up onSend pipeline
            context.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                foreach (var activity in activities)
                {
                    TransformActivity(context, activity);
                }

                // run pipeline
                return await nextSend().ConfigureAwait(false);
            });

            // hook up update activity pipeline
            context.OnUpdateActivity(async (ctx, activity, nextUpdate) =>
            {
                TransformActivity(context, activity);

                // run full pipeline
                return await nextUpdate().ConfigureAwait(false);
            });

            if (nextTurn != null)
            {
                await nextTurn(cancellationToken).ConfigureAwait(false);
            }
        }

        private void TransformActivity(ITurnContext turnContext, Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                var newActivity = new Activity(type: ActivityTypes.Message, text: surfaceRealizer.CheckSentence(activity.AsMessageActivity().Text));

                foreach (var property in typeof(Activity).GetProperties())
                {
                    switch (property.Name)
                    {
                        // keep envelope information
                        case nameof(IActivity.ChannelId):
                        case nameof(IActivity.From):
                        case nameof(IActivity.Recipient):
                        case nameof(IActivity.Id):
                        case nameof(IActivity.LocalTimestamp):
                        case nameof(IActivity.Timestamp):
                        case nameof(IActivity.ReplyToId):
                        case nameof(IActivity.ServiceUrl):
                        case nameof(IActivity.Conversation):
                            break;
                        default:
                            // shallow copy all other values
                            property.SetValue(activity, property.GetValue(newActivity));
                            break;
                    }
                }
            }
           
        }

    }
}
