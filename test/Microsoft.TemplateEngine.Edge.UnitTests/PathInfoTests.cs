// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class PathInfoTests
    {
        [Theory]
        [InlineData (false, false, false)]
        [InlineData (false, false, true)]
        [InlineData (false, true, false)]
        [InlineData (false, true, true)]
        [InlineData (true, false, false)]
        [InlineData (true, false, true)]
        [InlineData (true, true, false)]
        [InlineData (true, true, true)]
        public void UserProfileEnvironment(bool useHome, bool useDotnetCliHome, bool useUserProfilePath)
        {
            var environment = A.Fake<IEnvironment>();
            string testDotnetCliHomePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user1" : "/home/path1";
            string testHomePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user2" : "/home/path2";
            string testUserProfilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user3" : "/home/path3";

            A.CallTo(() => environment.GetEnvironmentVariable("DOTNET_CLI_HOME")).Returns(useDotnetCliHome ? testDotnetCliHomePath : null);
            A.CallTo(() => environment.GetEnvironmentVariable("HOME")).Returns(useHome ? testHomePath : null);
            A.CallTo(() => environment.GetEnvironmentVariable("USERPROFILE")).Returns(useHome ? testHomePath : null);
            A.CallTo(() => environment.UserProfilePath).Returns(useUserProfilePath ? testUserProfilePath : string.Empty);

            var host = A.Fake<ITemplateEngineHost>();
            A.CallTo(() => host.HostIdentifier).Returns("hostID");
            A.CallTo(() => host.Version).Returns("1.0.0");

            if (!useHome && !useDotnetCliHome && !useUserProfilePath)
            {
                Assert.Throws<NotSupportedException>(() => new DefaultPathInfo(environment, host));
                return;
            }

            DefaultPathInfo pathInfo = new DefaultPathInfo(environment, host);

            Assert.NotNull(pathInfo.UserProfileDir);

            if (useDotnetCliHome)
            {
                Assert.Equal(testDotnetCliHomePath, pathInfo.UserProfileDir);
            }
            else if (useHome)
            {
               Assert.Equal(testHomePath, pathInfo.UserProfileDir);
            }
            else
            {
               Assert.True(useUserProfilePath);
               Assert.Equal(testUserProfilePath, pathInfo.UserProfileDir);
            }
        }

        [Fact]
        public void DefaultLocationTest()
        {
            var environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("HOME")).Returns("/home/path");
            A.CallTo(() => environment.GetEnvironmentVariable("USERPROFILE")).Returns("C:\\users\\user");

            var host = A.Fake<ITemplateEngineHost>();
            A.CallTo(() => host.HostIdentifier).Returns("hostID");
            A.CallTo(() => host.Version).Returns("1.0.0");

            var pathInfo = new DefaultPathInfo(environment, host);

            var homeFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user" : "/home/path";

            Assert.Equal(homeFolder, pathInfo.UserProfileDir);
            Assert.Equal(Path.Combine(homeFolder, ".templateengine"), pathInfo.GlobalSettingsDir);
            Assert.Equal(Path.Combine(homeFolder, ".templateengine", "hostID"), pathInfo.HostSettingsDir);
            Assert.Equal(Path.Combine(homeFolder, ".templateengine", "hostID", "1.0.0"), pathInfo.HostVersionSettingsDir);
        }

        [Fact]
        public void DefaultLocationTest_ExpectedExceptions()
        {
            var environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("HOME")).Returns("/home/path");
            A.CallTo(() => environment.GetEnvironmentVariable("USERPROFILE")).Returns("C:\\users\\user");

            var host = A.Fake<ITemplateEngineHost>();
            A.CallTo(() => host.HostIdentifier).Returns("hostID");
            A.CallTo(() => host.Version).Returns("");
            Assert.Throws<ArgumentException>(() => new DefaultPathInfo(environment, host));

            A.CallTo(() => host.HostIdentifier).Returns("");
            A.CallTo(() => host.Version).Returns("ver");
            Assert.Throws<ArgumentException>(() => new DefaultPathInfo(environment, host));
        }

        [Theory]
        [InlineData ("global", "host", "version")]
        [InlineData (null, "host", "version")]
        [InlineData ("global", null, "version")]
        [InlineData ("global", "host", null)]
        public void CustomLocationTest(string? global, string? hostDir, string? hostVersion)
        {
            var environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("HOME")).Returns("/home/path");
            A.CallTo(() => environment.GetEnvironmentVariable("USERPROFILE")).Returns("C:\\users\\user");

            var host = A.Fake<ITemplateEngineHost>();
            A.CallTo(() => host.HostIdentifier).Returns("hostID");
            A.CallTo(() => host.Version).Returns("1.0.0");

            var pathInfo = new DefaultPathInfo(environment, host, global, hostDir, hostVersion);

            var homeFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user" : "/home/path";
            var defaultGlobal = Path.Combine(homeFolder, ".templateengine");
            var defaultHost = Path.Combine(homeFolder, ".templateengine", "hostID");
            var defaultHostVesrion = Path.Combine(homeFolder, ".templateengine", "hostID", "1.0.0");

            Assert.Equal(homeFolder, pathInfo.UserProfileDir);
            Assert.Equal(string.IsNullOrWhiteSpace(global) ? defaultGlobal : global, pathInfo.GlobalSettingsDir);
            Assert.Equal(string.IsNullOrWhiteSpace(hostDir) ? defaultHost : hostDir, pathInfo.HostSettingsDir);
            Assert.Equal(string.IsNullOrWhiteSpace(hostVersion) ? defaultHostVesrion : hostVersion, pathInfo.HostVersionSettingsDir);
        }

        [Theory]
        [InlineData("custom")]
        [InlineData(null)]
        public void CustomHiveLocationTest(string? hiveLocation)
        {
            var environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("HOME")).Returns("/home/path");
            A.CallTo(() => environment.GetEnvironmentVariable("USERPROFILE")).Returns("C:\\users\\user");

            var host = A.Fake<ITemplateEngineHost>();
            A.CallTo(() => host.HostIdentifier).Returns("hostID");
            A.CallTo(() => host.Version).Returns("1.0.0");

            var envSettings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => envSettings.Host).Returns(host);
            A.CallTo(() => envSettings.Environment).Returns(environment);

            var pathInfo = new DefaultPathInfo(envSettings, hiveLocation);

            var homeFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\users\\user" : "/home/path";
            var expectedGlobal = string.IsNullOrWhiteSpace(hiveLocation)
                ? Path.Combine(homeFolder, ".templateengine")
                : Path.Combine(hiveLocation);
            var expectedHost = string.IsNullOrWhiteSpace(hiveLocation)
                ? Path.Combine(homeFolder, ".templateengine", "hostID")
                : Path.Combine(hiveLocation, "hostID");
            var expectedHostVesrion = string.IsNullOrWhiteSpace(hiveLocation)
                ? Path.Combine(homeFolder, ".templateengine", "hostID", "1.0.0")
                : Path.Combine(hiveLocation, "hostID", "1.0.0");

            Assert.Equal(homeFolder, pathInfo.UserProfileDir);
            Assert.Equal(expectedGlobal, pathInfo.GlobalSettingsDir);
            Assert.Equal(expectedHost, pathInfo.HostSettingsDir);
            Assert.Equal(expectedHostVesrion, pathInfo.HostVersionSettingsDir);
        }
    }
}
