﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests
{
    public class ValidationTests : TestBase, IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;

        public ValidationTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _environmentSettingsHelper = environmentSettingsHelper;
        }

        [Fact]
        public async Task ValidateAllTestTemplates()
        {
            string[] exceptions = new[] { "MissingConfigTest" };

            IEngineEnvironmentSettings environmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            using IMountPoint sourceMountPoint = environmentSettings.MountPath(TestTemplatesLocation);
            RunnableProjectGenerator generator = new();
            IReadOnlyList<ScannedTemplateInfo> loadedTemplates = await generator.GetTemplatesFromMountPointInternalAsync(sourceMountPoint, default).ConfigureAwait(false);

            IEnumerable<ScannedTemplateInfo> filteredTemplates = loadedTemplates.Where(t => !exceptions.Contains(t.ConfigurationModel.Identity));

            Assert.True(filteredTemplates.All(t => t.IsValid));
        }

        [Fact]
        public async Task ValidateInvalidTemplate()
        {
            IEngineEnvironmentSettings environmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            using IMountPoint sourceMountPoint = environmentSettings.MountPath(GetTestTemplateLocation("Invalid/MissingMandatoryConfig"));
            RunnableProjectGenerator generator = new();
            IReadOnlyList<ScannedTemplateInfo> loadedTemplates = await generator.GetTemplatesFromMountPointInternalAsync(sourceMountPoint, default).ConfigureAwait(false);

            ScannedTemplateInfo loadedTemplate = Assert.Single(loadedTemplates);
            Assert.False(loadedTemplate.IsValid);
            Assert.Equal(2, loadedTemplate.ValidationErrors.Count(ve => ve.Severity == IValidationEntry.SeverityLevel.Error));
        }
    }
}
