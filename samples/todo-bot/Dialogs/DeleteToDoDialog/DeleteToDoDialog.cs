using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples
{
    public class DeleteToDoDialog : ComponentDialog
    {
        public DeleteToDoDialog()
            : base(nameof(DeleteToDoDialog))
        {
            // Create instance of adaptive dialog. 
            var DeleteToDoDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // Note: that 'Help' and 'Cancel' are not locally handled here.
                // You will automatically see global 'Help' and 'Cancel' from the RootDialog.
                Steps = new List<IDialog>()
                {
                    // Handle case where there are no items in todo list
                    new IfCondition()
                    {
                        Condition = new ExpressionEngine().Parse("user.todos == null || count(user.todos) <= 0"),
                        Steps = new List<IDialog>()
                        {
                            new SendActivity("[Delete-Empty-List]"),
                            new SendActivity("[Welcome-Actions]"),
                            new EndDialog()
                        }
                    },
                    // User could have already specified the todo to delete via 
                    // todoTitle as simple machine learned LUIS entity or
                    // todoTitle_patternAny as pattern.any LUIS entity .or.
                    // prebuilt number entity that denotes the position of the todo item in the list .or.
                    // todoIdx machine learned entity that can detect things like first or last etc. 
                    

                    // Use a code step to determine the index of the todo to delete
                    new CodeStep(GetToDoTitleToDelete)
                    {
                        OutputBinding = "turn.todoTitle"
                    },
                    new IfCondition()
                    {
                        Condition = new ExpressionEngine().Parse("turn.todoTitle == null"),
                        Steps = new List<IDialog>()
                        {
                            // First show the current list of Todos
                            new BeginDialog(nameof(ViewToDoDialog)),
                            new ChoiceInput()
                            {
                                Property = "turn.todoTitle",
                                Prompt = new ActivityTemplate("[Get-ToDo-Title-To-Delete]"),
                                Style = ListStyle.Auto,
                                ChoicesProperty = "user.todos"
                            }
                        }
                    },
                    new EditArray()
                    {
                        Property = "user.todos",
                        ItemProperty = "turn.todoTitle",
                        ChangeType = EditArray.ArrayChangeType.Remove
                    },
                    new SendActivity("[Delete-readBack]")
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(DeleteToDoDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }

        private async Task<DialogTurnResult> GetToDoTitleToDelete(DialogContext dc, System.Object options)
        {
            var todoList = dc.State.GetValue<string[]>("user.todos");
            string numberEntity, todoTitle, todoTitle_patternAny, todoIdx;
            dc.State.TryGetValue("turn.entities.number", out numberEntity);
            dc.State.TryGetValue("turn.entities.todoTitle", out todoTitle);
            dc.State.TryGetValue("turn.entities.todoTitle_patternAny", out todoTitle_patternAny);
            dc.State.TryGetValue("turn.entities.todoIdx", out todoIdx);
            if (todoList == null && (numberEntity != null || todoTitle != null || todoTitle_patternAny != null || todoIdx != null)) {

            }
            
            await dc.Context.SendActivityAsync(MessageFactory.Text("In custom code step"));
            await dc.Context.SendActivityAsync(MessageFactory.Text(options.ToString()));
            // This code step decided to just return the input payload as the result.
            return new DialogTurnResult(DialogTurnStatus.Complete, options);
        }
    }
}
