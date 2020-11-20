using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Core.Util;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal static class FileRenameGenerator
    {
        // Creates the complete file rename mapping for the template invocation being processed.
        // Renames are based on:
        //  - parameters with a FileRename specified
        //  - the source & target names.
        // Any input fileRenames will be applied before the parameter symbol renames.
        public static IReadOnlyDictionary<string, string> AugmentFileRenames(IEngineEnvironmentSettings environmentSettings, string sourceName, IFileSystemInfo configFile, string sourceDirectory, ref string targetDirectory, object resolvedNameParamValue, IParameterSet parameterSet, Dictionary<string, string> fileRenames, IReadOnlyList<IReplacementTokens> symbolBasedFileRenames = null)
        {
            Dictionary<string, string> allRenames = new Dictionary<string, string>(StringComparer.Ordinal);

            IProcessor sourceRenameProcessor = SetupRenameProcessor(environmentSettings, fileRenames);
            IProcessor symbolRenameProcessor = SetupSymbolBasedRenameProcessor(environmentSettings, sourceName, ref targetDirectory, resolvedNameParamValue, parameterSet, symbolBasedFileRenames);

            IDirectory sourceBaseDirectoryInfo = configFile.Parent.Parent.DirectoryInfo(sourceDirectory.TrimEnd('/'));

            foreach (IFileSystemInfo fileSystemEntry in sourceBaseDirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                string sourceTemplateRelativePath = fileSystemEntry.PathRelativeTo(sourceBaseDirectoryInfo);

                // first apply the sources renames, then apply the symbol renames to that result.
                string renameFromSourcesValue = ApplyRenameProcessorToFilename(sourceRenameProcessor, sourceTemplateRelativePath);
                string renameFinalTargetValue = ApplyRenameProcessorToFilename(symbolRenameProcessor, renameFromSourcesValue);

                if (!string.Equals(sourceTemplateRelativePath, renameFinalTargetValue, StringComparison.Ordinal))
                {
                    allRenames[sourceTemplateRelativePath] = renameFinalTargetValue;
                }
            }

            return allRenames;
        }

        private static string ApplyRenameProcessorToFilename(IProcessor processor, string sourceFilename)
        {
            using (Stream source = new MemoryStream(Encoding.UTF8.GetBytes(sourceFilename)))
            using (Stream target = new MemoryStream())
            {
                processor.Run(source, target);

                byte[] targetData = new byte[target.Length];
                target.Position = 0;
                target.Read(targetData, 0, targetData.Length);
                return Encoding.UTF8.GetString(targetData);
            }
        }

        // Creates and returns the processor used to create the file rename mapping.
        private static IProcessor SetupRenameProcessor(IEngineEnvironmentSettings environmentSettings, IReadOnlyDictionary<string, string> substringReplacementMap)
        {
            List<IOperationProvider> operations = new List<IOperationProvider>();

            foreach (KeyValuePair<string, string> replacement in substringReplacementMap)
            {
                IOperationProvider replacementOperation = new Replacement(replacement.Key.TokenConfig(), replacement.Value, null, true);
                operations.Add(replacementOperation);
            }

            IVariableCollection variables = new VariableCollection();
            EngineConfig config = new EngineConfig(environmentSettings, variables);
            IProcessor processor = Processor.Create(config, operations);
            return processor;
        }

        private static IProcessor SetupSymbolBasedRenameProcessor(IEngineEnvironmentSettings environmentSettings, string sourceName, ref string targetDirectory, object resolvedNameParamValue, IParameterSet parameterSet, IReadOnlyList<IReplacementTokens> symbolBasedFileRenames)
        {
            List<IOperationProvider> operations = new List<IOperationProvider>();

            if (resolvedNameParamValue != null && sourceName != null)
            {
                string targetName = ((string)resolvedNameParamValue).Trim();
                targetDirectory = targetDirectory.Replace(sourceName, targetName);
                operations.Add(new Replacement(sourceName.TokenConfig(), targetName, null, true));
            }

            foreach (IReplacementTokens fileRenameToken in symbolBasedFileRenames)
            {
                if (parameterSet.TryGetRuntimeValue(environmentSettings, fileRenameToken.VariableName, out object value) && value is string valueString)
                {
                    IOperationProvider replacementOperation = new Replacement(fileRenameToken.OriginalValue, valueString, null, true);
                    operations.Add(replacementOperation);
                }
            }

            IVariableCollection variables = new VariableCollection();
            EngineConfig config = new EngineConfig(environmentSettings, variables);
            IProcessor processor = Processor.Create(config, operations);
            return processor;
        }
    }
}
