// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Tests
{
    public class TestUtilities
    {
        public static TurnContext CreateEmptyContext()
        {
            var b = new TestAdapter();
            var a = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = "EmptyContext",
                Conversation = new ConversationAccount
                {
                    Id = "test",
                },
                From = new ChannelAccount
                {
                    Id = "empty@empty.context.org",
                },
            };
            var bc = new TurnContext(b, a);

            return bc;
        }

        /*
        public static T CreateEmptyContext<T>() where T:ITurnContext
        {
            TestAdapter b = new TestAdapter();
            Activity a = new Activity();
            if (typeof(T).IsAssignableFrom(typeof(ITurnContext)))
            {
                ITurnContext bc = new TurnContext(b, a);
                return (T)bc;
            }
            else
                throw new ArgumentException($"Unknown Type {typeof(T).Name}");
        }
        */
    }
}
