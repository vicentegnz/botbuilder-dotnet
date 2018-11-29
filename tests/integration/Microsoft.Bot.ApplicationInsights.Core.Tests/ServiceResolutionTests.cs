// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.TestHost;
using System.IO;
using Microsoft.ApplicationInsights;

namespace Microsoft.Bot.Builder.Integration.ApplicationInsights.Core.Tests
{
    [TestClass]
    [TestCategory("ApplicationInsights")]
    public class ServiceResolutionTests
    {
        public ServiceResolutionTests()
        {
            // Arrange
            //_server = new TestServer(new WebHostBuilder()
                                     //.UseStartup<Startup>());
            //_client = _server.CreateClient();
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void BotFile_NoBotFile()
        {
            ArrangeBotFile(null); // No bot file
            ArrangeAppSettings(); // Default app settings
            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
        }

        [TestMethod]
        public void ServiceResolution_GoodLoad()
        {
            ArrangeBotFile(); // Default bot file
            ArrangeAppSettings(); // Default app settings
            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            Assert.IsTrue(true);
        }

        /// <summary>
        /// Prepare appsettings.json for test
        /// </summary>
        /// <remarks>Ensures appsettings.json file is set up (copy based on different sample files,
        /// post-pended with a version.)  ie, appsettings.json.no_app_insights </remarks>
        /// <param name="version">Post-pended onto the file name to copy (ie, "no_app_insights"). If null, put no file.</param>
        public void ArrangeAppSettings(string version = "default")
        {
            try { File.Delete("appsettings.json"); }
            catch { }
            
            if (!string.IsNullOrWhiteSpace(version))
            {
                File.Copy($"appsettings.json.{version}", "appsettings.json");
            }
        }
        /// <summary>
        /// Prepare testbot.bot for test
        /// </summary>
        /// <remarks>Ensures testbot.bot file is set up (copy based on different sample files,
        /// post-pended with a version.)  ie, testbot.bot.no_app_insights </remarks>
        /// <param name="version">Post-pended onto the file name to copy (ie, "no_app_insights"). If null, put no file.</param>

        public void ArrangeBotFile(string version = "default")
        {
            try { File.Delete("testbot.bot"); }
            catch { }
            
            if (!string.IsNullOrWhiteSpace(version))
            {
                File.Copy($"testbot.bot.{version}", "testbot.bot");
            }
        }

    }
}
