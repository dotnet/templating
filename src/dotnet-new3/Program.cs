// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli;
using Microsoft.TemplateEngine.Cli.PostActionProcessors;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateSearch.Common.TemplateUpdate;

[assembly: InternalsVisibleTo("dotnet-new3.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb")]

namespace dotnet_new3
{
    public class Program
    {
        private const string HostIdentifier = "dotnetcli-preview";
        private const string HostVersion = "v1.0.0";
        private const string CommandName = "new3";
        private const string LanguageOverrideEnvironmentVar = "DOTNET_CLI_UI_LANGUAGE";
        private const string VsLanguageOverrideEnvironmentVar = "VSLANG";
        private const string CompilerLanguageEnvironmentVar = "PreferredUILang";

        public static int Main(string[] args)
        {
            bool emitTimings = args.Any(x => string.Equals(x, "--debug:emit-timings", StringComparison.OrdinalIgnoreCase));
            bool debugTelemetry = args.Any(x => string.Equals(x, "--debug:emit-telemetry", StringComparison.OrdinalIgnoreCase));

            DefaultTemplateEngineHost host = CreateHost(emitTimings);

            bool debugAuthoring = args.Any(x => string.Equals(x, "--trace:authoring", StringComparison.OrdinalIgnoreCase));
            bool debugInstall = args.Any(x => string.Equals(x, "--trace:install", StringComparison.OrdinalIgnoreCase));
            if (debugAuthoring)
            {
                AddAuthoringLogger(host);
                AddInstallLogger(host);
            }
            else if (debugInstall)
            {
                AddInstallLogger(host);
            }

            return New3Command.Run(CommandName, host, new TelemetryLogger(null, debugTelemetry), FirstRun, args);
        }

        private static DefaultTemplateEngineHost CreateHost(bool emitTimings)
        {
            var preferences = new Dictionary<string, string>
            {
                { "prefs:language", "C#" }
            };

            try
            {
                string versionString = Dotnet.Version().CaptureStdOut().Execute().StdOut;
                if (!string.IsNullOrWhiteSpace(versionString))
                {
                    preferences["dotnet-cli-version"] = versionString.Trim();
                }
            }
            catch
            { }

            var builtIns = new AssemblyComponentCatalog(new[]
            {
                // for assembly: Microsoft.TemplateEngine.Orchestrator.RunnableProjects
                typeof(RunnableProjectGenerator).GetTypeInfo().Assembly,
                // for assembly: Microsoft.TemplateEngine.Edge
                typeof(NupkgInstallUnitDescriptorFactory).GetTypeInfo().Assembly,
                // for assembly: Microsoft.TemplateEngine.Cli
                typeof(DotnetRestorePostActionProcessor).GetTypeInfo().Assembly,
                // for assembly: Microsoft.TemplateSearch.Common
                typeof(NupkgUpdater).GetTypeInfo().Assembly
            });

            ConfigureLocale();

            DefaultTemplateEngineHost host = new DefaultTemplateEngineHost(HostIdentifier, HostVersion, CultureInfo.CurrentUICulture.Name, preferences, builtIns, new[] { "dotnetcli" });

            if (emitTimings)
            {
                host.OnLogTiming = (label, duration, depth) =>
                {
                    string indent = string.Join("", Enumerable.Repeat("  ", depth));
                    Console.WriteLine($"{indent} {label} {duration.TotalMilliseconds}");
                };
            }

            return host;
        }

        private static void AddAuthoringLogger(DefaultTemplateEngineHost host)
        {
            Action<string, string[]> authoringLogger = (message, additionalInfo) =>
            {
                Console.WriteLine(string.Format("Authoring: {0}", message));
            };
            host.RegisterDiagnosticLogger("Authoring", authoringLogger);
        }

        private static void AddInstallLogger(DefaultTemplateEngineHost host)
        {
            Action<string, string[]> installLogger = (message, additionalInfo) =>
            {
                Console.WriteLine(string.Format("Install: {0}", message));
            };
            host.RegisterDiagnosticLogger("Install", installLogger);
        }

        private static void FirstRun(IEngineEnvironmentSettings environmentSettings, IInstaller installer)
        {
            string? dn3Path = Environment.GetEnvironmentVariable("DN3");
            if (string.IsNullOrEmpty(dn3Path))
            {
                string? path = typeof(Program).Assembly.Location;
                while (path != null && !File.Exists(Path.Combine(path, "Microsoft.TemplateEngine.sln")))
                {
                    path = Path.GetDirectoryName(path);
                }
                if (path == null)
                {
                    environmentSettings.Host.LogDiagnosticMessage("Couldn't the setup package location, because \"Microsoft.TemplateEngine.sln\" is not in any of parent directories.", "Install");
                    return;
                }
                Environment.SetEnvironmentVariable("DN3", path);
            }

            List<string> toInstallList = new List<string>();
            Paths paths = new Paths(environmentSettings);

            if (paths.FileExists(paths.Global.DefaultInstallTemplateList))
            {
                toInstallList.AddRange(paths.ReadAllText(paths.Global.DefaultInstallTemplateList).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (toInstallList.Count > 0)
            {
                for (int i = 0; i < toInstallList.Count; i++)
                {
                    toInstallList[i] = toInstallList[i].Replace('\\', Path.DirectorySeparatorChar);
                }

                installer.InstallPackages(toInstallList);
            }

            // copy the in-box nuget template search metadata file.
            if (paths.FileExists(paths.Global.DefaultInstallTemplateSearchData))
            {
                paths.Copy(paths.Global.DefaultInstallTemplateSearchData, paths.User.NuGetScrapedTemplateSearchFile);
            }
        }

        private static void ConfigureLocale()
        {
            CultureInfo? selectedCulture = null;

            // VSLANG=<lcid> is set by VS and we respect that as well so that we will respect the VS
            // language preference if we're invoked by VS.
            string? vsLang = Environment.GetEnvironmentVariable(VsLanguageOverrideEnvironmentVar);
            if (vsLang != null && int.TryParse(vsLang, out int vsLcid))
            {
                try
                {
                    selectedCulture = new CultureInfo(vsLcid);
                }
                catch (ArgumentOutOfRangeException) { }
                catch (CultureNotFoundException) { }
            }

            // DOTNET_CLI_UI_LANGUAGE=<culture name> is the main way for users to customize the CLI's UI language.
            // This overrides the locale set by VS.
            string? dotnetCliLanguage = Environment.GetEnvironmentVariable(LanguageOverrideEnvironmentVar);
            if (!string.IsNullOrWhiteSpace(dotnetCliLanguage))
            {
                try
                {
                    selectedCulture = new CultureInfo(dotnetCliLanguage);
                }
                catch (CultureNotFoundException)
                {
                    Console.WriteLine("Environment variable \"" + LanguageOverrideEnvironmentVar + "\" does not correspond to a valid culture." +
                        " Value is: \"" + dotnetCliLanguage + "\"");
                }
            }

            if (selectedCulture != null)
            {
                CultureInfo.DefaultThreadCurrentUICulture = selectedCulture;
                ConfigureLocaleForChildProcesses(selectedCulture);
            }
        }

        private static void ConfigureLocaleForChildProcesses(CultureInfo language)
        {
            // Set language for child dotnetcli processes.
            Environment.SetEnvironmentVariable(LanguageOverrideEnvironmentVar, language.Name);

            // Set language for tools following VS guidelines to just work in CLI.
            Environment.SetEnvironmentVariable(VsLanguageOverrideEnvironmentVar, language.LCID.ToString());

            // Set languatefor C#/VB targets that pass $(PreferredUILang) to compiler.
            Environment.SetEnvironmentVariable(CompilerLanguageEnvironmentVar, language.Name);
        }
    }
}
