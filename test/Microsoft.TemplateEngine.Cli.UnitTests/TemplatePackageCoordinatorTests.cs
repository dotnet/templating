// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class TemplatePackageCoordinatorTests : IClassFixture<TemplatePackageCoordinatorTests.ConsoleRedirector>
    {
        public class ConsoleRedirector
        {
            StringWriter stderr = new();
            StringWriter stdout = new();

            public StringWriter StdErr => stderr;

            public StringWriter StdOut => stdout;

            public ConsoleRedirector()
            {
                Console.SetError(stderr);
                Console.SetOut(stdout);
            }

            public void Clear()
            {
                stderr.GetStringBuilder().Clear();
                stdout.GetStringBuilder().Clear();
            }
        }

        private readonly ConsoleRedirector _consoleRedirector;

        // This constructor serves as a combination of one time test fixture setup combined with per test setup.
        //   It is called prior each test case, while the passed argument is allways identical (IClassFixture defintion)
        public TemplatePackageCoordinatorTests(ConsoleRedirector consoleRedirector)
        {
            _consoleRedirector = consoleRedirector;
            _consoleRedirector.Clear();
        }

        [Theory]
        [InlineData(true, InstallerErrorCode.PackageNotFound, false)]
        [InlineData(false, InstallerErrorCode.PackageNotFound, true)]
        [InlineData(true, InstallerErrorCode.GenericError, true)]
        [InlineData(false, InstallerErrorCode.GenericError, true)]
        public void CanConditionallyReportErrors_BasedOnErrorTypeAndPackageSource(bool isLocalPackage, InstallerErrorCode errorCode, bool shouldHaveError)
        {
            TemplatePackageManager packageManager = A.Fake<TemplatePackageManager>();
            ITelemetryLogger telemetryLogger = A.Fake<ITelemetryLogger>();
            IEngineEnvironmentSettings environmentSettings = A.Fake<IEngineEnvironmentSettings>();
            TemplatePackageCoordinator tpc = new TemplatePackageCoordinator(telemetryLogger, environmentSettings, packageManager);

            IManagedTemplatePackage templatePackage = A.Fake<IManagedTemplatePackage>();
            ICommandArgs commandArgs = A.Fake<ICommandArgs>();
            A.CallTo(() => templatePackage.IsLocalPackage).Returns(isLocalPackage);
            string packageDisplayName = "foo.bar";
            A.CallTo(() => templatePackage.DisplayName).Returns(packageDisplayName);

            CheckUpdateResult result = CheckUpdateResult.CreateFailure(templatePackage, errorCode, string.Empty);

            tpc.DisplayUpdateCheckResult(result, commandArgs);
            if (shouldHaveError == string.IsNullOrEmpty(_consoleRedirector.StdErr.ToString()))
            {
                throw new Exception($"{isLocalPackage} {errorCode} {shouldHaveError}");
            }
            Assert.Equal(shouldHaveError, !string.IsNullOrEmpty(_consoleRedirector.StdErr.ToString()));
            Assert.Empty(_consoleRedirector.StdOut.ToString());
        }
    }
}
