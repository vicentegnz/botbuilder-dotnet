// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Lets you modify an array in memory
    /// </summary>
    public class RemoteCall : DialogCommand
    {
        public enum OAuthProvider
        {
            /// <summary>
            /// Azure Activity Directory authentication provider.
            /// </summary>
            AzureAD,

            /// <summary>
            /// Google authentication provider.
            /// </summary>
            Google,

            /// <summary>
            /// Todoist authentication provider.
            /// </summary>
            Todoist,
        }

        public class ProviderTokenResponse
        {
            public OAuthProvider AuthenticationProvider { get; set; }

            public TokenResponse TokenResponse { get; set; }
        }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("tokenProperty")]
        public string TokenProperty { get; set; }


        private static readonly HttpClient client = new HttpClient();

        [JsonConstructor]
        public RemoteCall([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            var postBody = JsonConvert.SerializeObject(activity);

            var response = await client.PostAsync(this.Url, new StringContent(postBody, Encoding.UTF8, "application/json"));

            var result = await response.Content.ReadAsStringAsync();

            var responseActivities = JsonConvert.DeserializeObject<List<Activity>>(result);

            foreach (var act in responseActivities)
            {
                if (act.Name == "tokens/request")
                {
                    var tokenResponse = dc.Context.Activity.CreateReply();
                    tokenResponse.Type = ActivityTypes.Event;
                    tokenResponse.Name = "tokens/response";
                    var tokenTemp = await new TextTemplate(TokenProperty).BindToData(dc.Context, dc.State);
                    tokenResponse.Value = new ProviderTokenResponse()
                    {
                        AuthenticationProvider = OAuthProvider.AzureAD,
                        TokenResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenTemp)
                    };

                    var tokenResponseBody = JsonConvert.SerializeObject(tokenResponse);

                    var tokenRes = await client.PostAsync(this.Url, new StringContent(tokenResponseBody, Encoding.UTF8, "application/json"));
                    var tokenResult = await tokenRes.Content.ReadAsStringAsync();
                    var tokenResponseActivities = JsonConvert.DeserializeObject<List<Activity>>(tokenResult);
                    foreach (var tokenAct in tokenResponseActivities)
                    {
                        await dc.Context.SendActivityAsync(tokenAct);
                    }

                }
                else
                {
                    await dc.Context.SendActivityAsync(act);
                }
            }

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
