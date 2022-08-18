﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests
{
    public class BootstrapperTestBase : TestBase
    {
        private const string HostIdentifier = "IDE.IntegrationTests";
        private const string HostVersion = "v1.0.0";

        internal static Bootstrapper GetBootstrapper(IEnumerable<string>? additionalVirtualLocations = null, bool loadTestTemplates = false)
        {
            ITemplateEngineHost host = CreateHost(loadTestTemplates);
            if (additionalVirtualLocations != null)
            {
                foreach (string virtualLocation in additionalVirtualLocations)
                {
                    host.VirtualizeDirectory(virtualLocation);
                }
            }
            return new Bootstrapper(host, virtualizeConfiguration: true, loadDefaultComponents: true);
        }

        internal static async Task InstallTestTemplateAsync(Bootstrapper bootstrapper, params string[] templates)
        {
            List<InstallRequest> installRequests = new List<InstallRequest>();

            foreach (string template in templates)
            {
                string path = GetTestTemplateLocation(template);
                installRequests.Add(new InstallRequest(Path.GetFullPath(path)));
            }

            IReadOnlyList<InstallResult> installationResults = await bootstrapper.InstallTemplatePackagesAsync(installRequests).ConfigureAwait(false);
            if (installationResults.Any(result => !result.Success))
            {
                throw new Exception($"Failed to install templates: {string.Join(";", installationResults.Select(result => $"path: {result.InstallRequest.PackageIdentifier}, details:{result.ErrorMessage}"))}");
            }
        }

        internal static async Task InstallTemplateAsync(Bootstrapper bootstrapper, params string[] templates)
        {
            List<InstallRequest> installRequests = new List<InstallRequest>();
            foreach (string template in templates)
            {
                installRequests.Add(new InstallRequest(Path.GetFullPath(template)));
            }

            IReadOnlyList<InstallResult> installationResults = await bootstrapper.InstallTemplatePackagesAsync(installRequests).ConfigureAwait(false);
            if (installationResults.Any(result => !result.Success))
            {
                throw new Exception($"Failed to install templates: {string.Join(";", installationResults.Select(result => $"path: {result.InstallRequest.PackageIdentifier}, details:{result.ErrorMessage}"))}");
            }
        }

        private static ITemplateEngineHost CreateHost(bool loadTestTemplates = false)
        {
            var preferences = new Dictionary<string, string>
            {
                { "prefs:language", "C#" }
            };

            var builtIns = new List<(Type, IIdentifiedComponent)>();
            if (loadTestTemplates)
            {
                builtIns.AddRange(BuiltInTemplatePackagesProviderFactory.GetComponents(TestTemplatesLocation));
            }
            return new DefaultTemplateEngineHost(HostIdentifier + Guid.NewGuid().ToString(), HostVersion, preferences, builtIns, Array.Empty<string>());
        }
    }
}
