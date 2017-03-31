﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Cli
{
    internal class Installer : IInstaller
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly Paths _paths;

        public Installer(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
        }

        public void InstallPackages(IEnumerable<string> installationRequests)
        {
            List<string> localSources = new List<string>();
            List<Package> packages = new List<Package>();

            foreach (string request in installationRequests)
            {
                string req = request;

                //If the request string doesn't have any wild cards or probable path indicators,
                //  and doesn't have a "::" delimiter either, try to convert it to "latest package"
                //  form
                if (req.IndexOfAny(new[] { '*', '?', '/', '\\' }) < 0 && req.IndexOf("::", StringComparison.Ordinal) < 0)
                {
                    req += "::*";
                }

                if (Package.TryParse(req, out Package package))
                {
                    packages.Add(package);
                }
                else
                {
                    localSources.Add(request);
                }
            }

            if (localSources.Count > 0)
            {
                InstallLocalPackages(localSources);
            }

            if (packages.Count > 0)
            {
                InstallRemotePackages(packages);
            }

            _environmentSettings.SettingsLoader.Save();
        }

        private void InstallRemotePackages(List<Package> packages)
        {
            const string packageRef = @"    <PackageReference Include=""{0}"" Version=""{1}"" />";
            const string projectFile = @"<Project ToolsVersion=""15.0"" Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Remove=""Microsoft.NETCore.App"" />
{0}
  </ItemGroup>
</Project>";

            _paths.CreateDirectory(_paths.User.ScratchDir);
            string proj = Path.Combine(_paths.User.ScratchDir, "restore.csproj");
            StringBuilder references = new StringBuilder();

            foreach (Package pkg in packages)
            {
                references.AppendLine(string.Format(packageRef, pkg.Name, pkg.Version));
            }

            string content = string.Format(projectFile, references.ToString());
            _paths.WriteAllText(proj, content);

            _paths.CreateDirectory(_paths.User.Packages);
            string restored = Path.Combine(_paths.User.ScratchDir, "Packages");
            Command.CreateDotNet("restore", new[] { proj, "--packages", restored }).ForwardStdErr().Execute();

            List<string> newLocalPackages = new List<string>();
            foreach (string packagePath in _paths.EnumerateFiles(restored, "*.nupkg", SearchOption.AllDirectories))
            {
                string path = Path.Combine(_paths.User.Packages, Path.GetFileName(packagePath));
                _paths.Copy(packagePath, path);
                newLocalPackages.Add(path);
            }

            _paths.DeleteDirectory(_paths.User.ScratchDir);
            InstallLocalPackages(newLocalPackages);
        }

        private void InstallLocalPackages(IReadOnlyList<string> packageNames)
        {
            List<string> toInstall = new List<string>();

            foreach (string package in packageNames)
            {
                if (package == null)
                {
                    continue;
                }

                string pkg = package.Trim();
                pkg = _environmentSettings.Environment.ExpandEnvironmentVariables(pkg);
                string pattern = null;

                int wildcardIndex = pkg.IndexOfAny(new[] { '*', '?' });

                if (wildcardIndex > -1)
                {
                    int lastSlashBeforeWildcard = pkg.LastIndexOfAny(new[] { '\\', '/' });
                    pattern = pkg.Substring(lastSlashBeforeWildcard + 1);
                    pkg = pkg.Substring(0, lastSlashBeforeWildcard);
                }

                try
                {
                    if (pattern != null)
                    {
                        string fullDirectory = new DirectoryInfo(pkg).FullName;
                        string fullPathGlob = Path.Combine(fullDirectory, pattern);
                        ((SettingsLoader)(_environmentSettings.SettingsLoader)).UserTemplateCache.Scan(fullPathGlob);
                    }
                    else if (_environmentSettings.Host.FileSystem.DirectoryExists(pkg) || _environmentSettings.Host.FileSystem.FileExists(pkg))
                    {
                        string packageLocation = new DirectoryInfo(pkg).FullName;
                        ((SettingsLoader)(_environmentSettings.SettingsLoader)).UserTemplateCache.Scan(packageLocation);
                    }
                    else
                    {
                        _environmentSettings.Host.OnNonCriticalError("InvalidPackageSpecification", string.Format(LocalizableStrings.BadPackageSpec, pkg), null, 0);
                    }
                }
                catch
                {
                    _environmentSettings.Host.OnNonCriticalError("InvalidPackageSpecification", string.Format(LocalizableStrings.BadPackageSpec, pkg), null, 0);
                }
            }
        }
    }
}
