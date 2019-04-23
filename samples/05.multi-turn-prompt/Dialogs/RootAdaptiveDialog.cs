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
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;

namespace Microsoft.BotBuilderSamples
{
    public class RootAdaptiveDialog : ComponentDialog
    {
        public RootAdaptiveDialog()
            : base(nameof(RootAdaptiveDialog))
        {

            // Create instance of adaptive dialog. 
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // These steps are executed when this Adaptive Dialog begins
                Steps = OnBeginDialogSteps(),
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
        private static List<IDialog> OnBeginDialogSteps()
        {
            return new List<IDialog>()
            {
                new TextInput()
                {
                    Prompt = new ActivityTemplate("Please enter your mode of transport."),
                    Property = "user.userProfile.Transport"
                },
                new TextInput()
                {
                    Prompt = new ActivityTemplate("Please enter your name."),
                    Property = "user.userProfile.Name"
                },
                new SendActivity("Thanks, {user.userProfile.Name}"),
                new ConfirmInput()
                {
                    Prompt = new ActivityTemplate("Would you like to give your age?"),
                    Property = "turn.ageConfirmation",
                    AlwaysPrompt = true
                },
                new IfCondition()
                {
                    Condition = new ExpressionEngine().Parse("turn.ageConfirmation == true"),
                    Steps = new List<IDialog>()
                    {
                         new NumberInput<int>()
                         {
                             Prompt = new ActivityTemplate("Please enter your age."),
                             MinValue = 1,
                             MaxValue = 150,
                             RetryPrompt = new ActivityTemplate("The value entered must be greater than 0 and less than 150."),
                             Property = "user.userProfile.Age"
                         },
                         new SendActivity("I have your age as {user.userProfile.Age}.")
                    },
                    ElseSteps = new List<IDialog>()
                    {
                        new SendActivity("No age given.")
                    }
                },
                new ConfirmInput()
                {
                    Prompt = new ActivityTemplate("Is this ok?"),
                    Property = "user.finalConfirmation",
                    AlwaysPrompt = true
                },
                // Use LG template to come back with the final read out.
                // This LG template is a great example of what logic can be wrapped up in LG sub-system.
                new SendActivity("[FinalUserProfileReadOut]"), // examines turn.finalConfirmation
                new EndDialog()
            };
        }
    }
}
