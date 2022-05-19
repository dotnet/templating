// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Components;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Edge.Constraints;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class SdkVersionConstraintTests
    {
        [Theory]
        [InlineData("1.2.3", true)]
        [InlineData("1.2.3-dev", true)]
        [InlineData("1.2.4", false)]
        [InlineData("4.5.3-dev", false)]
        [InlineData("4.5.3", true)]
        [InlineData("4.5.0", true)]
        [InlineData("4.6.0", false)]
        public async Task Evaluate_ArrayOfVersions(string sdkVersion, bool allowed)
        {
            var config = new
            {
                identity = "test-constraint-01",
                constraints = new
                {
                    specVersions = new
                    {
                        type = "sdk-version",
                        args = new[] { "1.2.3-*", "4.5.*" }
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));
            ISdkInfoProvider sdkInfoProvider = A.Fake<ISdkInfoProvider>();
            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Components.OfType<ISdkInfoProvider>()).Returns(new[] { sdkInfoProvider });
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new SdkVersionConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);

            A.CallTo(() => sdkInfoProvider.GetVersionAsync(A<CancellationToken>._)).Returns(Task.FromResult(sdkVersion));

            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(allowed ? TemplateConstraintResult.Status.Allowed : TemplateConstraintResult.Status.Restricted, evaluateResult.EvaluationStatus);
        }

        [Theory]
        [InlineData("1.2.2", false)]
        [InlineData("1.2.3", true)]
        [InlineData("1.2.3-dev", true)]
        [InlineData("1.2.4", true)]
        [InlineData("4.5.3-dev", false)]
        [InlineData("4.5.3", false)]
        [InlineData("4.5.0", true)]
        [InlineData("4.4.0-dev", true)]
        public async Task Evaluate_SingleVersionRange(string sdkVersion, bool allowed)
        {
            var config = new
            {
                identity = "test-constraint-01",
                constraints = new
                {
                    specVersions = new
                    {
                        type = "sdk-version",
                        args = "(1.2.3-*, 4.5]"
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));
            ISdkInfoProvider sdkInfoProvider = A.Fake<ISdkInfoProvider>();
            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Components.OfType<ISdkInfoProvider>()).Returns(new[] { sdkInfoProvider });
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new SdkVersionConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);

            A.CallTo(() => sdkInfoProvider.GetVersionAsync(A<CancellationToken>._)).Returns(Task.FromResult(sdkVersion));

            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(allowed ? TemplateConstraintResult.Status.Allowed : TemplateConstraintResult.Status.Restricted, evaluateResult.EvaluationStatus);
        }
    }
}
