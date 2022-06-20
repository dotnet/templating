// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    internal class AddReferencePostActionProcessor : PostActionProcessor2Base
    {
        internal static readonly Guid ActionProcessorId = new Guid("B17581D1-C5C9-4489-8F0A-004BE667B814");

        public override Guid Id => ActionProcessorId;

        internal IReadOnlyList<string> FindProjFileAtOrAbovePath(IPhysicalFileSystem fileSystem, string startPath, HashSet<string> extensionLimiters)
        {
            if (extensionLimiters.Count == 0)
            {
                return FileFindHelpers.FindFilesAtOrAbovePath(fileSystem, startPath, "*.*proj");
            }
            else
            {
                return FileFindHelpers.FindFilesAtOrAbovePath(fileSystem, startPath, "*.*proj", (filename) => extensionLimiters.Contains(Path.GetExtension(filename)));
            }
        }

        protected override bool ProcessInternal(IEngineEnvironmentSettings environment, IPostAction action, ICreationEffects creationEffects, ICreationResult templateCreationResult, string outputBasePath)
        {
            IReadOnlyList<string>? projectsToProcess = GetConfiguredFiles(action.Args, creationEffects, "targetFiles", outputBasePath);

            if (projectsToProcess is null)
            {
                //If the author didn't opt in to the new behavior by specifying "targetFiles", search for project file in current output directory or above.
                HashSet<string> extensionLimiters = new HashSet<string>(StringComparer.Ordinal);
                if (action.Args.TryGetValue("projectFileExtensions", out string? projectFileExtensions))
                {
                    if (projectFileExtensions.Contains("/") || projectFileExtensions.Contains("\\") || projectFileExtensions.Contains("*"))
                    {
                        // these must be literals
                        Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionMisconfigured);
                        return false;
                    }

                    extensionLimiters.UnionWith(projectFileExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                }
                projectsToProcess = FindProjFileAtOrAbovePath(environment.Host.FileSystem, outputBasePath, extensionLimiters);
                if (projectsToProcess.Count > 1)
                {
                    // multiple projects at the same level. Error.
                    Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionUnresolvedProjFile);
                    Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionProjFileListHeader);
                    foreach (string projectFile in projectsToProcess)
                    {
                        Reporter.Error.WriteLine(string.Format("\t{0}", projectFile));
                    }
                    return false;
                }
            }
            if (projectsToProcess is null || !projectsToProcess.Any())
            {
                // no projects found. Error.
                Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionUnresolvedProjFile);
                return false;
            }

            bool success = true;
            foreach (string projectFile in projectsToProcess)
            {
                success &= AddReference(environment, action, projectFile, outputBasePath);

                if (!success)
                {
                    return false;
                }
            }
            return true;
        }

        private bool AddReference(IEngineEnvironmentSettings environment, IPostAction actionConfig, string projectFile, string outputBasePath)
        {
            if (actionConfig.Args == null || !actionConfig.Args.TryGetValue("reference", out string? referenceToAdd))
            {
                Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionMisconfigured);
                return false;
            }

            if (!actionConfig.Args.TryGetValue("referenceType", out string? referenceType))
            {
                Reporter.Error.WriteLine(LocalizableStrings.AddRefPostActionMisconfigured);
                return false;
            }
            Dotnet.Result commandResult;

            if (string.Equals(referenceType, "project", StringComparison.OrdinalIgnoreCase))
            {
                // actually do the add ref
                referenceToAdd = Path.GetFullPath(referenceToAdd, outputBasePath);
                Dotnet addReferenceCommand = Dotnet.AddProjectToProjectReference(projectFile, referenceToAdd);
                addReferenceCommand.CaptureStdOut();
                addReferenceCommand.CaptureStdErr();
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.AddRefPostActionAddProjectRef, projectFile, referenceToAdd));
                commandResult = addReferenceCommand.Execute();
            }
            else if (string.Equals(referenceType, "package", StringComparison.OrdinalIgnoreCase))
            {
                actionConfig.Args.TryGetValue("version", out string? version);

                Dotnet addReferenceCommand = Dotnet.AddPackageReference(projectFile, referenceToAdd, version);
                addReferenceCommand.CaptureStdOut();
                addReferenceCommand.CaptureStdErr();
                if (string.IsNullOrEmpty(version))
                {
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AddRefPostActionAddPackageRef, projectFile, referenceToAdd));
                }
                else
                {
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AddRefPostActionAddPackageRefWithVersion, projectFile, referenceToAdd, version));
                }
                commandResult = addReferenceCommand.Execute();
            }
            else if (string.Equals(referenceType, "framework", StringComparison.OrdinalIgnoreCase))
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.AddRefPostActionFrameworkNotSupported, referenceToAdd));
                return false;
            }
            else
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.AddRefPostActionUnsupportedRefType, referenceType));
                return false;
            }

            if (commandResult.ExitCode != 0)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.AddRefPostActionFailed, referenceToAdd, projectFile));
                Reporter.Error.WriteCommandOutput(commandResult);
                Reporter.Error.WriteLine(string.Empty);
                return false;
            }
            else
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.AddRefPostActionSucceeded, referenceToAdd, projectFile));
                return true;
            }

        }
    }
}
