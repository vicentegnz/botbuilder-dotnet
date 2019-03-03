using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Streaming.Protocol;
using Newtonsoft.Json;

namespace Microsoft.Bot.Streaming
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

            if (string.IsNullOrEmpty(request.Method) || request.Method.ToUpperInvariant() != "POST")
            {
                response.StatusCode = 405;
                return response;
            }

            var body = request.ReadBodyAsString();

            if (string.IsNullOrEmpty(body) || request.ContentFeeds == null || request.ContentFeeds.Count == 0)
            {
                // no body
                response.StatusCode = 400;
                return response;
            }

            var contentHeaders = request.ContentFeeds[0].Headers;

            string contentType;
            MediaTypeHeaderValue mediaTypeHeaderValue;
            if (!contentHeaders.TryGetValue("Content-Type", out contentType) || !MediaTypeHeaderValue.TryParse(contentType, out mediaTypeHeaderValue) || mediaTypeHeaderValue.MediaType != "application/json")
            {
                response.StatusCode = 406;
                return response;
            }

            var activity = JsonConvert.DeserializeObject<Activity>(body, SerializationSettings.DefaultDeserializationSettings);
            try
            {
                var token = (string)null;
                if (request.Headers != null)
                {
                    request.Headers.TryGetValue("Authorization", out token);
                }

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
