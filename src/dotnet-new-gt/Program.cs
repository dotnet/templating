using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli;
using Microsoft.TemplateEngine.Cli.PostActionProcessors;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Utils;

namespace dotnet_new
{
    /// <summary>
    /// Entry point for dotnet new as a global tool ref.
    /// </summary>
    public class Program
    {
        // TODO: finalize these values
        private const string HostIdentifier = "dotnet-new-gt";
        private const string HostVersion = "1.0.0";
        private const string CommandName = "new-gt";
        private static readonly string PathEnvironmentVariableName = "DNGT";    // DotnetNewGlobalTool

        static int Main(string[] args)
        {
            if (args.Any(x => string.Equals(x, "--debug:attach", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Attach the debugger to processID: {System.Diagnostics.Process.GetCurrentProcess().Id} and hit return.");
                Console.ReadLine();
            }

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
                typeof(RunnableProjectGenerator).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Orchestrator.RunnableProjects
                typeof(NupkgInstallUnitDescriptorFactory).GetTypeInfo().Assembly,   // for assembly: Microsoft.TemplateEngine.Edge
                typeof(DotnetRestorePostActionProcessor).GetTypeInfo().Assembly     // for assembly: Microsoft.TemplateEngine.Cli
            });

            DefaultTemplateEngineHost host = new DefaultTemplateEngineHost(HostIdentifier, HostVersion, CultureInfo.CurrentCulture.Name, preferences, builtIns, new[] { "dotnetcli" });

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
            string baseDir = Environment.ExpandEnvironmentVariables($"%{PathEnvironmentVariableName}%");

            if (baseDir.Contains('%'))
            {
                Assembly a = typeof(Program).GetTypeInfo().Assembly;
                string path = new Uri(a.CodeBase, UriKind.Absolute).LocalPath;
                path = Path.GetDirectoryName(path);
                Environment.SetEnvironmentVariable(PathEnvironmentVariableName, path);
            }

            List<string> toInstallList = new List<string>();
            Paths paths = new Paths(environmentSettings);

            if (paths.FileExists(paths.Global.DefaultInstallTemplateList))
            {
                toInstallList.AddRange(paths.ReadAllText(paths.Global.DefaultInstallTemplateList).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (toInstallList.Count > 0)
            {
                for (int i = 0; i < toInstallList.Count; i++)
                {
                    toInstallList[i] = toInstallList[i].Replace("\r", "")
                                                        .Replace('\\', Path.DirectorySeparatorChar);
                }

                installer.InstallPackages(toInstallList);
            }
        }
    }
}
