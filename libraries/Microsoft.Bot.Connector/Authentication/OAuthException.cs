// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Connector.Authentication
{
    public partial class MicrosoftAppCredentials
    {
        /// <summary>
        /// Represents an OAuth exception.
        /// </summary>
        public sealed class OAuthException : Exception
        {
            /// <summary>
            /// Creates a new instance of the <see cref="OAuthException"/> class.
            /// </summary>
            /// <param name="body">The OAuth response body or reason.</param>
            /// <param name="inner">The exception thown during the OAuth request.</param>
            public OAuthException(string body, Exception inner)
                : base(body, inner)
            {
            }
        }
#pragma warning restore IDE1006
    }
}
