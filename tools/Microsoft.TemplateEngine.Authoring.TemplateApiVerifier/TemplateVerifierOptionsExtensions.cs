// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier.Commands;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateEngine.Authoring.TemplateApiVerifier
{
    public static class TemplateVerifierOptionsExtensions
    {
        private static readonly string ExpectedConfigLocation = Path.Combine(".template.config", "template.json");

        /// <summary>
        /// Adds custom template instantiator that runs template instantiation in-proc via TemplateCreator API.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="inputParameters"></param>
        /// <returns></returns>
        public static TemplateVerifierOptions WithInstantiationThroughTemplateCreatorApi(
            this TemplateVerifierOptions options,
            IReadOnlyDictionary<string, string?>? inputParameters)
        {
            return options.WithCustomInstatiation(async verifierOptions => await RunInstantiation(verifierOptions, inputParameters, null).ConfigureAwait(false));
        }

        /// <summary>
        /// Adds custom template instantiator that runs template instantiation in-proc via TemplateCreator API.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="inputDataSet"></param>
        /// <returns></returns>
        public static TemplateVerifierOptions WithInstantiationThroughTemplateCreatorApi(
            this TemplateVerifierOptions options,
            InputDataSet? inputDataSet)
        {
            return options.WithCustomInstatiation(async verifierOptions => await RunInstantiation(verifierOptions, null, inputDataSet).ConfigureAwait(false));
        }

        private static async Task<IInstantiationResult> RunInstantiation(
            TemplateVerifierOptions options,
            IReadOnlyDictionary<string, string?>? inputParameters,
            InputDataSet? inputDataSet)
        {
            if (!string.IsNullOrEmpty(options.DotnetExecutablePath))
            {
                throw new TemplateVerificationException(LocalizableStrings.Error_DotnetPath, TemplateVerificationErrorCode.InvalidOption);
            }

            if (string.IsNullOrEmpty(options.TemplatePath))
            {
                throw new TemplateVerificationException(LocalizableStrings.Error_TemplatePathMissing, TemplateVerificationErrorCode.InvalidOption);
            }

            if (!Path.Exists(options.TemplatePath))
            {
                throw new TemplateVerificationException(LocalizableStrings.Error_TemplatePathDoesNotExist, TemplateVerificationErrorCode.InvalidOption);
            }

            if (options.TemplateSpecificArgs != null)
            {
                throw new TemplateVerificationException(LocalizableStrings.Error_TemplateArgsDisalowed, TemplateVerificationErrorCode.InvalidOption);
            }

            string templateConfigPath = options.TemplatePath;
            if (!templateConfigPath.EndsWith(ExpectedConfigLocation, StringComparison.InvariantCultureIgnoreCase))
            {
                templateConfigPath = Path.Combine(templateConfigPath, ExpectedConfigLocation);

                if (!File.Exists(templateConfigPath))
                {
                    throw new TemplateVerificationException(
                        string.Format(LocalizableStrings.Error_ConfigDoesntExist, templateConfigPath, ExpectedConfigLocation),
                        TemplateVerificationErrorCode.InvalidOption);
                }
            }

            // the expected nesting is checked and ensured above
            string templateBasePath = new FileInfo(templateConfigPath).Directory!.Parent!.FullName;

            // Create temp folder and instantiate there
            string workingDir = options.OutputDirectory ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (Directory.Exists(workingDir) && Directory.EnumerateFileSystemEntries(workingDir).Any())
            {
                throw new TemplateVerificationException(TemplateVerifier.LocalizableStrings.VerificationEngine_Error_WorkDirExists, TemplateVerificationErrorCode.WorkingDirectoryExists);
            }

            if (!ExtractParameter(inputParameters, inputDataSet, "name", out string? name) &&
                !ExtractParameter(inputParameters, inputDataSet, "n", out name))
            {
                name = options.TemplateName;
            }

            if (!ExtractParameter(inputParameters, inputDataSet, "output", out string? output) &&
                !ExtractParameter(inputParameters, inputDataSet, "o", out output))
            {
                output = options.TemplateName;
            }

            string outputPath = Path.Combine(workingDir, output!);

            var builtIns = Edge.Components.AllComponents.Concat(Orchestrator.RunnableProjects.Components.AllComponents).ToList();
            // use "dotnetcli" as a fallback host so the correct host specific files are read.
            var host = new DefaultTemplateEngineHost(nameof(TemplateVerifierOptionsExtensions), "1.0.0", null, builtIns, new[] { "dotnetcli" });
            EngineEnvironmentSettings environment = new EngineEnvironmentSettings(
                host: host,
                virtualizeSettings: string.IsNullOrEmpty(options.SettingsDirectory),
                settingsLocation: options.SettingsDirectory,
                environment: new DefaultEnvironment(options.Environment));

            using IMountPoint sourceMountPoint = environment.MountPath(templateBasePath);
            IFile? templateConfig = sourceMountPoint.FileInfo(ExpectedConfigLocation);

            if (templateConfig == null)
            {
                throw new TemplateVerificationException(LocalizableStrings.Error_ConfigRetrieval, TemplateVerificationErrorCode.InternalError);
            }

            RunnableProjectGenerator rpg = new();
            var runnableConfig = new RunnableProjectConfig(environment, rpg, templateConfig);

            TemplateCreator creator = new(environment);

            ITemplateCreationResult result = await
                (inputDataSet != null
                    ? creator.InstantiateAsync(
                        templateInfo: runnableConfig,
                        name: name,
                        fallbackName: null,
                        inputParameters: inputDataSet,
                        outputPath: outputPath)
                    : creator.InstantiateAsync(
                        templateInfo: runnableConfig,
                        name: name,
                        fallbackName: null,
                        inputParameters: inputParameters ?? new Dictionary<string, string?>(),
                        outputPath: outputPath)).ConfigureAwait(false);

            return new CommandResultData(
                (int)result.Status,
                result.Status == CreationResultStatus.Success ? string.Format(LocalizableStrings.CreateSuccessful, result.TemplateFullName) : string.Empty,
                result.ErrorMessage ?? string.Empty,
                // We do not want ot use result.OutputBaseDirectory as it points to the base of template
                //  not a working dir of command (which is one level up - as we explicitly specify output subdir, as if '-o' was passed)
                workingDir);
        }

        private static bool ExtractParameter(
            IReadOnlyDictionary<string, string?>? inputParameters,
            InputDataSet? inputDataSet,
            string key,
            out string? value)
        {
            value = null;
            if (
                inputDataSet != null &&
                inputDataSet.ParameterDefinitionSet.TryGetValue(key, out ITemplateParameter parameter) &&
                inputDataSet.TryGetValue(parameter, out InputParameterData parameterValue) &&
                parameterValue.Value != null)
            {
                value = parameterValue.Value.ToString();
            }
            else
            {
                inputParameters?.TryGetValue(key, out value);
            }

            return !string.IsNullOrEmpty(value);
        }
    }
}
