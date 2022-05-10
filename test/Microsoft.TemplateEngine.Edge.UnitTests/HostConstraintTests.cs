// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Constraints;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class HostConstraintTests
    {   
        [Fact]
        public async Task CanReadConfiguration_WithoutVersion()
        {
            var config = new
            {
                identity = "test",
                constraints = new
                {
                    host = new
                    {
                        type = "host",
                        args = new[]
                        {
                            new
                            {
                                hostName = "host1",
                                version = ""
                            },
                            new
                            {
                                hostName = "host2",
                                version = "1.0.0"
                            },
                            new
                            {
                                hostName = "host3",
                                version = "[1.0.0-*]"
                            },

                        }
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));
            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Host.HostIdentifier).Returns("host1");
            A.CallTo(() => settings.Host.Version).Returns("2.0.0");
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new HostConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);
            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(TemplateConstraintResult.Status.Allowed, evaluateResult.EvaluationStatus);
        }

        [Fact]
        public async Task CanReadConfiguration_ExactVersion()
        {
            var config = new
            {
                identity = "test",
                constraints = new
                {
                    host = new
                    {
                        type = "host",
                        args = new[]
                        {
                            new
                            {
                                hostName = "host1",
                                version = ""
                            },
                            new
                            {
                                hostName = "host2",
                                version = "1.0.0"
                            },
                            new
                            {
                                hostName = "host3",
                                version = "[1.0.0-*]"
                            },

                        }
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));
            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Host.HostIdentifier).Returns("host2");
            A.CallTo(() => settings.Host.Version).Returns("2.0.0");
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new HostConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);
            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(TemplateConstraintResult.Status.Restricted, evaluateResult.EvaluationStatus);
            Assert.Equal("Running template on host2 (version: 2.0.0) is not supported, supported hosts is/are: host1, host2(1.0.0), host3([1.0.0-*]).", evaluateResult.LocalizedErrorMessage);
            Assert.Null(evaluateResult.CallToAction);

        }

        [Fact]
        public async Task CanReadConfiguration_VersionRange()
        {
            var config = new
            {
                identity = "test",
                constraints = new
                {
                    host = new
                    {
                        type = "host",
                        args = new[]
                        {
                            new
                            {
                                hostName = "host1",
                                version = ""
                            },
                            new
                            {
                                hostName = "host2",
                                version = "1.0.0"
                            },
                            new
                            {
                                hostName = "host3",
                                version = "[1.0.0-*]"
                            },

                        }
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));

            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Host.HostIdentifier).Returns("host3");
            A.CallTo(() => settings.Host.Version).Returns("2.0.0");
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new HostConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);
            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(TemplateConstraintResult.Status.Allowed, evaluateResult.EvaluationStatus);
        }

        [Fact]
        public async Task FailsOnWrongConfiguration()
        {
            var config = new
            {
                identity = "test",
                constraints = new
                {
                    host = new
                    {
                        type = "host",
                        args =
                            new
                            {
                                hostName = "host2",
                                version = "1.0.0"
                            }
                    }
                }
            };

            var configModel = SimpleConfigModel.FromJObject(JObject.FromObject(config));

            IEngineEnvironmentSettings settings = A.Fake<IEngineEnvironmentSettings>();
            A.CallTo(() => settings.Host.HostIdentifier).Returns("host3");
            A.CallTo(() => settings.Host.Version).Returns("2.0.0");
            A.CallTo(() => settings.Components.OfType<ITemplateConstraintFactory>()).Returns(new[] { new HostConstraintFactory() });

            var constraintManager = new TemplateConstraintManager(settings);
            var evaluateResult = await constraintManager.EvaluateConstraintAsync(configModel.Constraints.Single().Type, configModel.Constraints.Single().Args, default).ConfigureAwait(false);
            Assert.Equal(TemplateConstraintResult.Status.NotEvaluated, evaluateResult.EvaluationStatus);
            Assert.Equal("'{\"hostName\":\"host2\",\"version\":\"1.0.0\"}' is not a valid JSON array.", evaluateResult.LocalizedErrorMessage);
            Assert.Equal("Check the constraint configuration in template.json.", evaluateResult.CallToAction);
        }
    }
}
