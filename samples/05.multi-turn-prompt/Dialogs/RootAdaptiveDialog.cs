using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;

namespace Microsoft.BotBuilderSamples
{
    public class RootAdaptiveDialog : ComponentDialog
    {
        public RootAdaptiveDialog()
            : base(nameof(RootAdaptiveDialog))
        {

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Steps = new List<IDialog>()
                {
                    new TextInput()
                    {
                        Prompt = new ActivityTemplate("What is your name?"),
                        Property = "user.name"
                    },
                    new SendActivity("Hello {user.name}, nice to meet you!")
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);
            
            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
