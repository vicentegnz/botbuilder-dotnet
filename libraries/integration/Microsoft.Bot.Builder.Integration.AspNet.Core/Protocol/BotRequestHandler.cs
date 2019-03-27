using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Protocol
{
    internal class BotRequestHandler : RequestHandler
    {
        public BotRequestHandler(NamedPipeBotAdapter adapter, IBot bot)
        {
            this.Adapter = adapter;
            this.Bot = bot;
        }

        private NamedPipeBotAdapter Adapter { get; set; }

        private IBot Bot { get; set; }

        public override async Task<Response> ProcessRequestAsync(ReceiveRequest request)
        {
            var response = new Response();

            if (string.IsNullOrEmpty(request.Verb) || request.Verb.ToUpperInvariant() != "POST")
            {
                response.StatusCode = 405;
                return response;
            }

            var body = await request.ReadBodyAsString().ConfigureAwait(false);

            if (string.IsNullOrEmpty(body) || request.Streams == null || request.Streams.Count == 0)
            {
                // no body
                response.StatusCode = 400;
                return response;
            }

            if (request.Streams[0].Type != "application/json; charset=utf-8")
            {
                response.StatusCode = 406;
                return response;
            }

            var activity = JsonConvert.DeserializeObject<Activity>(body, SerializationSettings.DefaultDeserializationSettings);
            try
            {
                string token = null;

                var invokeResponse = await this.Adapter.ProcessActivityAsync(token, activity, new BotCallbackHandler(this.Bot.OnTurnAsync), CancellationToken.None).ConfigureAwait(false);
                if (invokeResponse == null)
                {
                    response.StatusCode = 200;
                }
                else
                {
                    response.StatusCode = invokeResponse.Status;
                    if (invokeResponse.Body != null)
                    {
                        response.SetBody(invokeResponse.Body);
                    }
                }

                token = (string)null;
                invokeResponse = (InvokeResponse)null;
            }
            catch (UnauthorizedAccessException)
            {
                response.StatusCode = 403;
            }

            return response;
        }
    }
}
