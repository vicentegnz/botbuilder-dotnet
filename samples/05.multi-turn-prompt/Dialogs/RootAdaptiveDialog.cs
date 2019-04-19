using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
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
                        Prompt = new ActivityTemplate("Give me your favorite color. You can always say cancel to stop this."),
                        Property = "turn.favColor",
                    },
                    new EditArray()
                    {
                        ArrayProperty = "user.favColors",
                        ItemProperty = "turn.favColor",
                        ChangeType = EditArray.ArrayChangeType.Push
                    },
                    // This is required because TextInput will skip prompt if the property exists - which it will from the previous turn.
                    new DeleteProperty() {
                        Property = "turn.favColor"
                    },
                    // Repeat dialog step will restart this dialog.
                    new RepeatDialog()
                },
                Recognizer = new RegexRecognizer()
                {
                    Intents = new Dictionary<string, string>()
                    {
                        { "HelpIntent", "(?i)help" },
                        { "CancelIntent", "(?i)cancel|never mind" }
                    }
                },
                Rules = new List<IRule>()
                {
                    new IntentRule("CancelIntent")
                    {
                        Steps = new List<IDialog>()
                        {
                            new SendActivity("You have {count(user.favColors)} favorite colors - {join(user.favColors, ',', 'and')}"),
                            new EndDialog()
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);
            
            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
