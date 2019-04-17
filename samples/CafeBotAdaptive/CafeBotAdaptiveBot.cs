// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.AI.Luis;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using System;

namespace CafeBotAdaptive
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class CafeBotAdaptiveBot : IBot
    {
        private readonly CafeBotAdaptiveAccessors _accessors;
        private readonly ILogger _logger;
        private readonly DialogSet _dialogs;
        public const string DialogStateProperty = "dialogStateProperty";
        private readonly IStatePropertyAccessor<DialogState> _dialogAccessor;
        private const string rootDialogName = "cafeBotRootDialog";
        private const string whoAreYouDialogName = "whoAreYouDialog";
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public CafeBotAdaptiveBot(ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CafeBotAdaptiveBot>();
            _logger.LogTrace("Turn start.");

            _dialogAccessor = conversationState.CreateProperty<DialogState>(DialogStateProperty);
            _dialogs = new DialogSet(_dialogAccessor);

            var rootDialog = new AdaptiveDialog(rootDialogName)
            {
                AutoEndDialog = false,
                Recognizer = new LuisRecognizer(new LuisApplication("1195bcf1-4610-4285-982e-3a97cce409a2", "a95d07785b374f0a9d7d40700e28a285", "https://westus.api.cognitive.microsoft.com")),
                Rules = new List<IRule>()
                {
                    new IntentRule("Book_Flight",
                    steps: new List<IDialog>()
                    {
                        new SendActivity("I can help you book a flight..")
                    }),
                    new IntentRule("Cancel",
                    steps: new List<IDialog>()
                    {
                        new SendActivity("Sure, I've cancelled that."),
                        new CancelAllDialogs(),
                        new SendActivity("Thank you."),
                        new EndDialog()
                    }),
                    new UnknownIntentRule(
                    steps: new List<IDialog>()
                    {
                        new SendActivity("Sorry, I do not understand that. Try saying 'who are you' or 'what can you do")
                    }),
                    new EventRule(new List<String>() { AdaptiveEvents.ActivityReceived }, new List<IDialog>()
                    {
                        new SendActivity("Value: {turn.activity.text}")
                    }, constraint:"turn.DialogEvent.Value.Intents.Welcome.Score > 0")
                }
            };

            var whoAreYouDialog = new AdaptiveDialog(whoAreYouDialogName)
            {
                Recognizer = new LuisRecognizer(new LuisApplication("dabb5f49-d641-4260-a5ef-81249c359339", "a95d07785b374f0a9d7d40700e28a285", "https://westus.api.cognitive.microsoft.com")),
                Steps = new List<IDialog>()
                {
                    new IfCondition()
                    {
                        Condition = new ExpressionEngine().Parse("@userName != null"),
                        Steps = new List<IDialog>()
                        {
                            new SaveEntity("user.name", "@userName")
                        },
                        ElseSteps = new List<IDialog>()
                        {
                            new IfCondition()
                            {
                                Condition = new ExpressionEngine().Parse("@userName_patternAny != null"), 
                                Steps = new List<IDialog>()
                                {
                                    new SaveEntity("user.name", "@userName_patternAny")
                                }
                            }
                        }
                    },
                    new IfCondition()
                    {
                        Condition = new ExpressionEngine().Parse("user.name == null"),
                        Steps = new List<IDialog> ()
                        {
                            new TextInput()
                            {
                                Prompt = new ActivityTemplate("Hello, I'm the cafe bot! What is your name?"),
                                Property = "user.name"
                            }
                        },
                        ElseSteps = new List<IDialog> ()
                        {
                            new SendActivity("Hello {user.name}, nice to see you again! How can I be of help today?")
                        }
                    }
                },
                Rules = new List<IRule>()
                {
                    new IntentRule("No_Name", 
                        steps: new List<IDialog>()
                        {
                            new SetProperty()
                            {
                                Property = "user.name",
                                Value = new ExpressionEngine().Parse("'Human'")
                            },
                            new EndDialog()
                        }
                    ),
                    new IntentRule("Why_do_you_ask",
                        steps: new List<IDialog> ()
                        {
                            new SendActivity("I need your name to be able to address you correctly!")
                        }
                    ),
                    new IntentRule()
                    {
                        Intent = "Get_user_name",
                        Constraint = "(@userName != null) || (@userNamePatternAny != null)",
                        Steps = new List<IDialog>()
                        {
                            new SaveEntity("user.name", "@userName"),
                            new SaveEntity("user.name", "@userName_patternAny"),
                            new SendActivity("Hello {user.name}, nice to meet you! How can I be of help today?"),
                            new EndDialog()
                        }
                    },
                    new UnknownIntentRule(
                        steps: new List<IDialog>()
                        {
                            new SendActivity("None intent in who are you dialog!")
                        }
                    )
                }
            };
            rootDialog.AddDialog(new List<IDialog>() { whoAreYouDialog });
            _dialogs.Add(rootDialog);
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            //if (turnContext.Activity.Type == ActivityTypes.Message)
            //{
                // Create dialog context.
                var dc = await _dialogs.CreateContextAsync(turnContext);

                // Continue outstanding dialogs.
                await dc.ContinueDialogAsync();

                // Begin main dialog if no outstanding dialogs/ no one responded.
                if (!dc.Context.Responded)
                {
                    await dc.BeginDialogAsync(rootDialogName);
                }
            //}
            //else
            //{
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            //}
        }
    }
}
