// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Bot.Connector.Authentication
{
    /// <summary>
    /// MicrosoftAppCredentials auth implementation and cache
    /// </summary>
    public partial class MicrosoftAppCredentials : ServiceClientCredentials
    {
        /// <summary>
        /// An empty set of credentials.
        /// </summary>
        public static readonly MicrosoftAppCredentials Empty = new MicrosoftAppCredentials(null, null);

        /// <summary>
        /// The configuration property for the Microsoft app ID.
        /// </summary>
        public const string MicrosoftAppIdKey = "MicrosoftAppId";

        /// <summary>
        /// The configuration property for the Microsoft app Password.
        /// </summary>
        public const string MicrosoftAppPasswordKey = "MicrosoftAppPassword";

        private static readonly HttpClient DefaultHttpClient = new HttpClient();

        /// <summary>
        /// A cache of the outstanding uncompleted or completed tasks for a given token, for ensuring that we never have more then 1 token request in flight
        /// per token at a time.
        /// </summary>
        private static readonly ConcurrentDictionary<string, (Task<OAuthResponse> OAuthResponseTask, DateTime RefreshTime)> TokenCache = new ConcurrentDictionary<string, (Task<OAuthResponse>, DateTime)>();

        private static readonly ConcurrentDictionary<string, DateTime> TrustedHostNames;

        private readonly string _tokenCacheKey;

        static MicrosoftAppCredentials()
        {
            TrustedHostNames = new ConcurrentDictionary<string, DateTime>();

            //TrustedHostNames.TryAdd("state.botframework.com", DateTime.MaxValue); // deprecated state api
            TrustedHostNames.TryAdd("api.botframework.com", DateTime.MaxValue);       // bot connector API
            TrustedHostNames.TryAdd("token.botframework.com", DateTime.MaxValue);    // oauth token endpoint
            TrustedHostNames.TryAdd("api.botframework.us", DateTime.MaxValue);        // bot connector API in US Government DataCenters
            TrustedHostNames.TryAdd("token.botframework.us", DateTime.MaxValue);      // oauth token endpoint in US Government DataCenters
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MicrosoftAppCredentials"/> class.
        /// </summary>
        /// <param name="appId">The Microsoft app ID.</param>
        /// <param name="password">The Microsoft app password.</param>
        public MicrosoftAppCredentials(string appId, string password)
        {
            MicrosoftAppId = appId;
            MicrosoftAppPassword = password;
            _tokenCacheKey = appId;
        }

        /// <summary>
        /// Gets or sets the Microsoft app ID for this credential.
        /// </summary>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        /// Gets or sets the Microsoft app password for this credential.
        /// </summary>
        public string MicrosoftAppPassword { get; set; }


        /// <summary>
        /// Gets the OAuth endpoint to use.
        /// </summary>
        public virtual string OAuthEndpoint { get { return AuthenticationConstants.ToChannelFromBotLoginUrl; } }

        /// <summary>
        /// Gets the OAuth scope to use.
        /// </summary>
        public virtual string OAuthScope { get { return AuthenticationConstants.ToChannelFromBotOAuthScope; } }


        /// <summary>
        /// The time window within which the token will be automatically updated.
        /// </summary>
        public static TimeSpan AutoTokenRefreshTimeSpan { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Adds the host of service url to <see cref="MicrosoftAppCredentials"/> trusted hosts.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <remarks>If expiration time is not provided, the expiration time will DateTime.UtcNow.AddDays(1).</remarks>
        public static void TrustServiceUrl(string serviceUrl) =>
            TrustServiceUrl(serviceUrl, DateTime.UtcNow.Add(TimeSpan.FromDays(1)));

        /// <summary>
        /// Adds the host of service url to <see cref="MicrosoftAppCredentials"/> trusted hosts.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="expirationTime">The expiration time after which this service url is not trusted anymore.</param>
        public static void TrustServiceUrl(string serviceUrl, DateTime expirationTime) =>
            TrustedHostNames.AddOrUpdate(
                new Uri(serviceUrl).Host, 
                expirationTime,
                (host, currentExpiration) => expirationTime > currentExpiration ? expirationTime : currentExpiration);

        /// <summary>
        /// Checks if the service url is for a trusted host or not.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>True if the host of the service url is trusted; False otherwise.</returns>
        public static bool IsTrustedServiceUrl(string serviceUrl)
        {
            if (Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            {
                return IsTrustedUrl(uri);
            }

            return false;
        }

        /// <summary>
        /// Apply the credentials to the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param><param name="cancellationToken">Cancellation token.</param>
        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldSetToken())
            {
                var token = await this.GetTokenAsync().ConfigureAwait(false);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            await base.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

            bool ShouldSetToken()
            {
                if (IsTrustedUrl(request.RequestUri))
                {
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"Service url {request.RequestUri.Authority} is not trusted and JwtToken cannot be sent to it.");

                return false;
            }
        }

        /// <summary>
        /// Gets an OAuth access token.
        /// </summary>
        /// <param name="forceRefresh">True to force a refresh of the token; or false to get
        /// a cached token if it exists.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result contains the access token string.</remarks>
        public async Task<string> GetTokenAsync(bool forceRefresh = false)
        {
            Task<OAuthResponse> oAuthTokenResponseTask = null;

            // if we are being forced or don't have a token in our cache at all
            if (forceRefresh || !TokenCache.TryGetValue(_tokenCacheKey, out var cachedTokenDetails))
            {
                // we will await this task, because we don't have a token and we need it
                oAuthTokenResponseTask = InitiateRetrieveTokenTask(forceRefresh: forceRefresh);

                return (await oAuthTokenResponseTask.ConfigureAwait(false)).access_token;
            }

            var oAuthTokenResponse = await cachedTokenDetails.OAuthResponseTask.ConfigureAwait(false);

            // we have an oAuthToken
            // check to see if our token is expired 
            if (!IsTokenExpired(oAuthTokenResponse))
            {
                AutoRefreshTokenIfApplicable();

                return oAuthTokenResponse.access_token;
            }

            // The token is expired, we initiate getting a new token and await it (NOTE: someone could have already asked for a new token)
            oAuthTokenResponseTask = InitiateRetrieveTokenTask(forceRefresh: false);

            return (await oAuthTokenResponseTask.ConfigureAwait(false)).access_token;

            void AutoRefreshTokenIfApplicable()
            {
                // if we are past the refresh time, refresh the token now in the background so it doesn't run out
                if (DateTime.UtcNow > cachedTokenDetails.RefreshTime)
                {
                    Task.Run(() => RetrieveTokenAsync()
                        .ContinueWith(task =>
                        {
                            if (task.Status == TaskStatus.RanToCompletion)
                            {
                                TokenCache.TryUpdate(
                                    _tokenCacheKey,
                                    (task, DateTime.UtcNow + AutoTokenRefreshTimeSpan),
                                    cachedTokenDetails);
                            }
                            else
                            {
                                var nextRefresh = DateTime.UtcNow + TimeSpan.FromSeconds(30);

                                if (nextRefresh < oAuthTokenResponse.expiration_time)
                                {
                                    TokenCache.TryUpdate(
                                        _tokenCacheKey,
                                        (cachedTokenDetails.OAuthResponseTask, nextRefresh),
                                        cachedTokenDetails);
                                }
                            }
                        }));
                }
            }
        }

        private Task<OAuthResponse> InitiateRetrieveTokenTask(bool forceRefresh)
        {
            Task<OAuthResponse> oAuthResponseTask;

            // if there is not a task or we are forcing it
            if (forceRefresh || !TokenCache.TryGetValue(_tokenCacheKey, out var cachedTokenDetails))
            {
                // create it
                oAuthResponseTask = RetrieveTokenAsync();

                cachedTokenDetails = (oAuthResponseTask, DateTime.UtcNow + AutoTokenRefreshTimeSpan);

                TokenCache.AddOrUpdate(
                    _tokenCacheKey,
                    cachedTokenDetails,
                    (key, existing) => cachedTokenDetails);
            }
            else
            {
                // if task is in faulted or canceled state then replace it with another attempt
                if (cachedTokenDetails.OAuthResponseTask.IsFaulted || cachedTokenDetails.OAuthResponseTask.IsCanceled)
                {
                    oAuthResponseTask = RetrieveTokenAsync();

                    TokenCache.TryUpdate(
                        _tokenCacheKey,
                        (oAuthResponseTask, DateTime.UtcNow + AutoTokenRefreshTimeSpan),
                        cachedTokenDetails);
                }
                else
                {
                    oAuthResponseTask = cachedTokenDetails.OAuthResponseTask;
                }
            }

            return oAuthResponseTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTrustedUrl(Uri uri) =>
            // Check if the host entry present and, if it is, check that it isn't expiring/expired
            TrustedHostNames.TryGetValue(uri.Host, out var trustedServiceUrlExpiration)
                &&
            trustedServiceUrlExpiration > DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5));

        private async Task<OAuthResponse> RetrieveTokenAsync()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", MicrosoftAppId },
                    { "client_secret", MicrosoftAppPassword },
                    { "scope", OAuthScope }
                });

            using (var response = await DefaultHttpClient.PostAsync(OAuthEndpoint, content).ConfigureAwait(false))
            {
                string body = null;
                try
                {
                    response.EnsureSuccessStatusCode();
                    body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var oauthResponse = JsonConvert.DeserializeObject<OAuthResponse>(body);
                    oauthResponse.expiration_time = DateTime.UtcNow.AddSeconds(oauthResponse.expires_in).Subtract(TimeSpan.FromSeconds(60));
                    return oauthResponse;
                }
                catch (Exception exception)
                {
                    throw new OAuthException(body ?? response.ReasonPhrase, exception);
                }
            }
        }

        /// <summary>
        /// Has the token expired?  If so, then we await on every attempt to get a new token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTokenExpired(OAuthResponse token) =>
            DateTime.UtcNow > token.expiration_time;

        /// <summary>
        /// has token reached half/life ? If so, we get more agressive about trying to refresh it in the background
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTokenOld(OAuthResponse token)
        {
            var halfwayExpiration = (token.expiration_time - TimeSpan.FromSeconds(token.expires_in / 2));
            return DateTime.UtcNow > halfwayExpiration;
        }

#pragma warning disable IDE1006
        /// <summary>
        /// Describes the structure of an OAuth access token response.
        /// </summary>
        /// <remarks>
        /// Member variables to this class follow the RFC Naming conventions, rather than C# naming conventions. 
        /// </remarks>
        protected class OAuthResponse
        {
            /// <summary>
            /// Gets or sets the type of token.
            /// </summary>
            public string token_type { get; set; }

            /// <summary>
            /// Gets or sets the time in seconds until the token expires.
            /// </summary>
            public int expires_in { get; set; }

            /// <summary>
            /// Gets or sets the access token string.
            /// </summary>
            public string access_token { get; set; }

            /// <summary>
            /// Gets or sets the time at which the token expires.
            /// </summary>
            public DateTime expiration_time { get; set; }
        }
#pragma warning restore IDE1006
    }
}
