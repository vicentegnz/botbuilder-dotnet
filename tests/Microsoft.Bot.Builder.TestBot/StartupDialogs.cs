// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Bot.Builder.TestBot
{
    public class StartupDialogs
    {
        public StartupDialogs(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Create storage and conversation state
            var storage = new MemoryStorage();
            var myConversationState = new ConversationState(storage);

            // Add a service that one of our dialogs will depend on
            services.AddTransient<IMakeBelieveService, MyReallyAwesomeMakeBelieveService>();

            // Add a DialogsBot and use the dialogs builder API to configure it
            services.AddDialogsBot(dialogsBuilder =>
            {
                dialogsBuilder
                    /*
                     * Given any BotState instance it will create a IStatePropertyAccessor<DialogState> from it automatically
                     * and register it in the services collection so that it can be injected into anything that needs it later 
                     * which, in this case, will be the DialogsBot
                     */
                    .UseBotState(myConversationState)

                    /*
                     * This extension is here for backwards compatibility with the DialogSet API as well which really only supports singleton Dialog 
                     * instances, but it can help people transition if they've already invested in that API and then move to using dialog factory later.
                     */
                    //.UseDialogSet(new MyDialogSet())

                    /*
                     * Configures the IDialogFactory that will be used by the DialogsBot to create instance of the Dialogs 
                     * when they are needed.
                     * 
                     * NOTE: this whole method is inline here, but you can imagine it being factored out into a separate 
                     * method/class kind of like the Startup.cs pattern itself.
                     */
                    .UseDefaultDialogFactory(fb =>
                    {
                        /* 
                         * This style API allows registering a dialog by *Type* so that it can be created only when needed
                         * and injected with any dependencies that it needs automatically for the scope of the turn.
                         */
                        fb.AddRootDialog<MyRootDialog>()
                          // NOTE: this is the replacement concept for ComponentDialog
                          .AddMyReusableDialog("reusableDialog")
                          // This style API allow registering singleton Dialog instances such as the WaterfallDialog. Mainly this
                          // is for backwards compatibility as, IMNSHO, this is the not the best way to model this API in .NET.
                          .AddDialog(BuildWaterfallDialogInline())
                          //.AddDialog<MyComponentDialog>("myComponentDialog")
                          // This is a helper extension method that enables cleaner registering prompts because they can be 
                          // more type constrained and also have a common pattern of taking a validator function as a parameter.
                          .AddPrompt<ConfirmPrompt, bool>("confirmPrompt")
                          // And here's an overload that supports singleton prompts
                          .AddPrompt(new TextPrompt("namePrompt", async (pc, ct) => !string.IsNullOrWhiteSpace(pc.Recognized.Value)));
                    });
            },
            // Supports the same ability to configure the BotFrameworkOptions inline
            bfo =>
            {
                /* 
                 * NOTE: this is temporary to force the dialog's state to always be saved, but I intend to refactor
                 * the configuration a bit to include a DialogsBotOptions which will have an EnableAutoSave property
                 * which DialogsBot will check in its OnTurn implementation to know if it should be saving the state
                 */
                bfo.Middleware.Add(new AutoSaveStateMiddleware(myConversationState));
            });

            /*
             * This is another overload that could be used to register a custom DialogsBot that subclass that overrides 
             * hooks into the dialog execution lifecycle of the base DialogsBot. This approach would be used if, for example,
             * you needed to intercept every activity coming into the bot to support something like global commands no matter
             * what dialog is currently active.
             */
            //services.AddDialogsBot<MyDialogsBotWithStartOver>(
            //    dialogsBotBuilder =>
            //    {
            //        ...
            //    });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Standard configuration here

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }

        private Dialog BuildWaterfallDialogInline() =>
            new WaterfallDialog(
                "waterfall",
                async (sc, ct) =>
                {
                    await sc.Context.SendActivityAsync("first step").ConfigureAwait(false);

                    return await sc.PromptAsync(
                        "confirmPrompt",
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("shall I keep going?"),
                        },
                        ct);
                },
                async (sc, ct) =>
                {
                    if (sc.Result is bool b && b == true)
                    {
                        return await sc.PromptAsync(
                            "confirmPrompt",
                            new PromptOptions
                            {
                                Prompt = MessageFactory.Text("ok, want to see a dialog scope in action?")
                            },
                            ct).ConfigureAwait(false);
                    }

                    return await sc.EndDialogAsync(cancellationToken: ct);
                },
                async (sc, ct) =>
                {
                    if (sc.Result is bool b && b == true)
                    {
                        return await sc.BeginDialogAsync("reusableDialog", cancellationToken: ct);
                    }

                    return await sc.EndDialogAsync(cancellationToken: ct);
                });

    }
}
