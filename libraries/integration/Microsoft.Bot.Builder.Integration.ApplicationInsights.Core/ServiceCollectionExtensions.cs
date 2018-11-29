// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Builder.Integration.ApplicationInsights.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures services for Application Insights to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> which specifies the contract for a collection of service descriptors.</param>
        /// <param name="botConfiguration">Bot configuration that contains the Application Insights configuration information.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBotApplicationInsights(this IServiceCollection services, BotConfiguration botConfiguration) =>
            services.AddBotApplicationInsights(botConfiguration, null);

        /// <summary>
        /// Adds and configures services for Application Insights to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> which specifies the contract for a collection of service descriptors.</param>
        /// <param name="botConfiguration">Bot configuration that contains the Application Insights configuration information.</param>
        /// <param name="appInsightsInstanceName">The name of the Application Insights instance to resolve from the <paramref name="botConfiguration">config</paramref>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBotApplicationInsights(this IServiceCollection services, BotConfiguration botConfiguration, string appInsightsInstanceName)
        {
            if (botConfiguration == null)
            {
                throw new ArgumentNullException(nameof(botConfiguration));
            }

            var appInsightsConfigs = botConfiguration.Services.Where(s => s.Type == "appInsights");

            if (appInsightsInstanceName != null)
            {
                appInsightsConfigs = appInsightsConfigs.Where(s => s.Name == appInsightsInstanceName);
            }

            var appInsightsConfig = appInsightsConfigs.FirstOrDefault();

            if (appInsightsConfig == null)
            {
                var exceptionMessage = appInsightsInstanceName == null ? 
                                            "The .bot file is missing an Application Insights (appInsights) service." 
                                                : 
                                            $"The .bot file is an Application Insights (appInsights) service with the name \"{appInsightsInstanceName}\".";

                throw new InvalidOperationException(exceptionMessage);
            }

            // Enables Bot Telemetry to save user/session id's as the bot user id and session
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            //services.AddSingleton<ITelemetryModule, DiagnosticSourceTelemetryModule>();
            return services;
        }
    }
}
