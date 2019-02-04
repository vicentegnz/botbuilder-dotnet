using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    internal class BotRequestHandler : StreamRequestHandler
    {
        public BotRequestHandler(BotFrameworkV4Adapter adapter, IBot bot)
        {
            this.Adapter = adapter;
            this.Bot = bot;
        }

        private BotFrameworkV4Adapter Adapter { get; set; }

        private IBot Bot { get; set; }

        public override async Task<StreamMessage> ProcessRequestAsync(StreamMessage request)
        {
            StreamMessage response = new StreamMessage()
            {
                RequestId = request.RequestId,
            };

            if (string.IsNullOrEmpty(request.Method) || request.Method.ToUpperInvariant() != "POST")
            {
                response.StatusCode = 405;
                return response;
            }

            if (string.IsNullOrEmpty(request.Body))
            {
                response.StatusCode = 400;
                return response;
            }

            string contentType;
            MediaTypeHeaderValue mediaTypeHeaderValue;
            if (request.Headers == null || !request.Headers.TryGetValue("Content-Type", out contentType) || !MediaTypeHeaderValue.TryParse(contentType, out mediaTypeHeaderValue) || mediaTypeHeaderValue.MediaType != "application/json")
            {
                response.StatusCode = 406;
                return response;
            }

            Activity activity = (Activity)JsonConvert.DeserializeObject<Activity>(request.Body, PipeConnection.DeserializationSettings);
            try
            {
                string token = (string)null;
                if (request.Headers != null)
                {
                    request.Headers.TryGetValue("Authorization", out token);
                }

                InvokeResponse invokeResponse = await this.Adapter.ProcessActivityAsync(token, activity, new BotCallbackHandler(this.Bot.OnTurnAsync), CancellationToken.None).ConfigureAwait(false);
                if (invokeResponse == null)
                {
                    response.StatusCode = 200;
                }
                else
                {
                    response.StatusCode = invokeResponse.Status;
                    if (invokeResponse.Body != null)
                    {
                        response.Headers = (IDictionary<string, string>)new Dictionary<string, string>()
                        {
                              { "Content-Type", "application/json" },
                        };
                        response.Body = JsonConvert.SerializeObject(invokeResponse.Body, PipeConnection.SerializationSettings);
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
