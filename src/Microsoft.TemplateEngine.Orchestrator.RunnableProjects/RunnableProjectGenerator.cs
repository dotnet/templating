// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class RunnableProjectGenerator : IGenerator
    {
        internal const string HostTemplateFileConfigBaseName = ".host.json";
        internal const string TemplateConfigDirectoryName = ".template.config";
        internal const string TemplateConfigFileName = "template.json";
        internal const string LocalizationFilePrefix = "templatestrings.";
        internal const string LocalizationFileExtension = ".json";
        internal const string GeneratorVersion = "1.0.0.0";
        private static readonly Guid GeneratorId = new Guid("0C434DF7-E2CB-4DEE-B216-D7C58C8EB4B3");

        public Guid Id => GeneratorId;

        /// <summary>
        /// Converts the raw, string version of a parameter to a strongly typed value. If the parameter has a datatype specified, use that. Otherwise attempt to infer the type.
        /// Throws a TemplateParamException if the conversion fails for any reason.
        /// </summary>
        public object? ConvertParameterValueToType(IEngineEnvironmentSettings environmentSettings, ITemplateParameter parameter, string untypedValue, out bool valueResolutionError)
        {
            return ParameterConverter.ConvertParameterValueToType(environmentSettings.Host, parameter, untypedValue, out valueResolutionError);
        }

        public Task<ICreationResult> CreateAsync(
            IEngineEnvironmentSettings environmentSettings,
            ITemplate templateData,
            IEvaluatedParameterSetData parameters,
            string targetDirectory,
            CancellationToken cancellationToken)
        {
            RunnableProjectConfig templateConfig = (RunnableProjectConfig)templateData;

            if (templateData.TemplateSourceRoot is null)
            {
                throw new InvalidOperationException($"{nameof(templateData.TemplateSourceRoot)} cannot be null to continue.");
            }
            return CreateAsync(
                environmentSettings,
                templateConfig,
                templateData.TemplateSourceRoot,
                parameters,
                targetDirectory,
                cancellationToken);
        }

        public async Task<ICreationResult> CreateAsync(
            IEngineEnvironmentSettings environmentSettings,
            IRunnableProjectConfig runnableProjectConfig,
            IDirectory templateSourceRoot,
            IEvaluatedParameterSetData parameters,
            string targetDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RemoveDisabledParamsFromTemplate((ITemplate)runnableProjectConfig, parameters);

            IVariableCollection variables = SetupVariables(environmentSettings, parameters, runnableProjectConfig.OperationConfig.VariableSetup);
            await runnableProjectConfig.EvaluateBindSymbolsAsync(environmentSettings, variables, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            ProcessMacros(environmentSettings, runnableProjectConfig.OperationConfig, variables);
            runnableProjectConfig.Evaluate(variables);

            IOrchestrator basicOrchestrator = new Core.Util.Orchestrator(environmentSettings.Host.Logger, environmentSettings.Host.FileSystem);
            RunnableProjectOrchestrator orchestrator = new RunnableProjectOrchestrator(basicOrchestrator);

            GlobalRunSpec runSpec = new GlobalRunSpec(templateSourceRoot, environmentSettings.Components, variables, runnableProjectConfig.OperationConfig, runnableProjectConfig.SpecialOperationConfig, runnableProjectConfig.IgnoreFileNames);

            foreach (FileSourceMatchInfo source in runnableProjectConfig.Sources)
            {
                runSpec.SetupFileSource(source);
                string target = Path.Combine(targetDirectory, source.Target);
                orchestrator.Run(runSpec, templateSourceRoot.DirectoryInfo(source.Source), target);
            }

            return GetCreationResult(environmentSettings.Host.Logger, runnableProjectConfig, variables);
        }

        /// <summary>
        /// Performs the dry-run of the template instantiation to evaluate the primary outputs, post actions to be applied and file changes to be made when executing the template with specified parameters.
        /// </summary>
        /// <param name="environmentSettings">environment settings.</param>
        /// <param name="templateData">the template to be executed.</param>
        /// <param name="parameters">the parameters to be used on template execution.</param>
        /// <param name="targetDirectory">the output path for the template.</param>
        /// <param name="cancellationToken">cancellationToken.</param>
        /// <returns>the primary outputs, post actions and file changes that will be made when executing the template with specified parameters.</returns>
        public async Task<ICreationEffects> GetCreationEffectsAsync(
            IEngineEnvironmentSettings environmentSettings,
            ITemplate templateData,
            IEvaluatedParameterSetData parameters,
            string targetDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RunnableProjectConfig templateConfig = (RunnableProjectConfig)templateData;
            if (templateData.TemplateSourceRoot is null)
            {
                throw new InvalidOperationException($"{nameof(templateData.TemplateSourceRoot)} cannot be null to continue.");
            }

            RemoveDisabledParamsFromTemplate(templateData, parameters);

            IVariableCollection variables = SetupVariables(environmentSettings, parameters, templateConfig.OperationConfig.VariableSetup);
            await templateConfig.EvaluateBindSymbolsAsync(environmentSettings, variables, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ProcessMacros(environmentSettings, templateConfig.OperationConfig, variables);
            templateConfig.Evaluate(variables);

            IOrchestrator basicOrchestrator = new Core.Util.Orchestrator(environmentSettings.Host.Logger, environmentSettings.Host.FileSystem);
            RunnableProjectOrchestrator orchestrator = new RunnableProjectOrchestrator(basicOrchestrator);

            GlobalRunSpec runSpec = new GlobalRunSpec(templateData.TemplateSourceRoot, environmentSettings.Components, variables, templateConfig.OperationConfig, templateConfig.SpecialOperationConfig, templateConfig.IgnoreFileNames);
            List<IFileChange2> changes = new List<IFileChange2>();

            foreach (FileSourceMatchInfo source in templateConfig.Sources)
            {
                runSpec.SetupFileSource(source);
                string target = Path.Combine(targetDirectory, source.Target);
                IReadOnlyList<IFileChange2> fileChanges = orchestrator.GetFileChanges(runSpec, templateData.TemplateSourceRoot.DirectoryInfo(source.Source), target);

                //source and target paths in the file changes are returned relative to source passed
                //GetCreationEffects method should return the source paths relative to template source root (location of .template.config folder) and target paths relative to output path and not relative to certain source
                //add source and target used to file changes to be returned as the result
                changes.AddRange(
                    fileChanges.Select(
                        fileChange => new FileChange(
                            Path.Combine(source.Source, fileChange.SourceRelativePath),
                            Path.Combine(source.Target, fileChange.TargetRelativePath),
                            fileChange.ChangeKind,
#pragma warning disable CS0618 // Type or member is obsolete
                            fileChange.Contents)));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return new CreationEffects2(changes, GetCreationResult(environmentSettings.Host.Logger, templateConfig, variables));
        }

        public IParameterSetBuilder GetParametersForTemplate(IEngineEnvironmentSettings environmentSettings, ITemplate template)
        {
            RunnableProjectConfig templateConfig = (RunnableProjectConfig)template;
            return GetParametersForTemplate(templateConfig);
        }

        /// <summary>
        /// Scans the <paramref name="source"/> for the available templates.
        /// </summary>
        /// <param name="source">the mount point to scan for the templates.</param>
        /// <param name="localizations">found localization definitions.</param>
        /// <returns>the list of found templates and list of found localizations via <paramref name="localizations"/>.</returns>
        public IList<ITemplate> GetTemplatesAndLangpacksFromDir(IMountPoint source, out IList<ILocalizationLocator> localizations)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            ILogger logger = source.EnvironmentSettings.Host.LoggerFactory.CreateLogger<RunnableProjectGenerator>();

            IDirectory folder = source.Root;
            IList<ITemplate> templateList = new List<ITemplate>();
            localizations = new List<ILocalizationLocator>();

            foreach (IFile file in folder.EnumerateFiles(TemplateConfigFileName, SearchOption.AllDirectories))
            {
                logger.LogDebug($"Found {TemplateConfigFileName} at {file.GetDisplayPath()}.");
                try
                {
                    IFile? hostConfigFile = FindBestHostTemplateConfigFile(source.EnvironmentSettings, file);
                    logger.LogDebug($"Found *{HostTemplateFileConfigBaseName} at {hostConfigFile?.GetDisplayPath()}.");

                    // issue here: we need to pass locale as parameter
                    // consider passing current locale file here if exists
                    // tracking issue: https://github.com/dotnet/templating/issues/3255
                    var templateConfiguration = new RunnableProjectConfig(source.EnvironmentSettings, this, file, hostConfigFile);

                    IDirectory? localizeFolder = file.Parent?.DirectoryInfo("localize");
                    if (localizeFolder != null && localizeFolder.Exists)
                    {
                        foreach (IFile locFile in localizeFolder.EnumerateFiles(LocalizationFilePrefix + "*" + LocalizationFileExtension, SearchOption.AllDirectories))
                        {
                            string locale = locFile.Name.Substring(LocalizationFilePrefix.Length, locFile.Name.Length - LocalizationFilePrefix.Length - LocalizationFileExtension.Length);

                            try
                            {
                                ILocalizationModel locModel = LocalizationModelDeserializer.Deserialize(locFile);
                                if (templateConfiguration.VerifyLocalizationModel(locModel, locFile))
                                {
                                    localizations.Add(new LocalizationLocator(
                                        locale,
                                        locFile.FullPath,
                                        templateConfiguration.Identity,
                                        locModel.Author ?? string.Empty,
                                        locModel.Name ?? string.Empty,
                                        locModel.Description ?? string.Empty,
                                        locModel.ParameterSymbols));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(LocalizableStrings.LocalizationModelDeserializer_Error_FailedToParse, locFile.GetDisplayPath());
                                logger.LogDebug("Details: {0}", ex);
                            }
                        }
                    }
                    templateList.Add(templateConfiguration);

                }
                catch (TemplateValidationException)
                {
                    //do nothing
                    //template validation prints all required information
                }
                catch (NotSupportedException ex)
                {
                    //do not print stack trace for this type.
                    logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, file.GetDisplayPath(), ex.Message);
                }
                catch (TemplateAuthoringException ex)
                {
                    //do not print stack trace for this type.
                    logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, file.GetDisplayPath(), ex.Message);
                }
                catch (Exception ex)
                {
                    //unexpected error - print details
                    logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, file.GetDisplayPath(), ex);
                }
            }

            return templateList;
        }

        /// <summary>
        /// Attempts to load template configuration from <paramref name="templateFileConfig"/>.
        /// </summary>
        /// <param name="templateFileConfig">the template configuration entry, should be a file.</param>
        /// <param name="template">loaded template.</param>
        /// <param name="localeFileConfig">the template localization configuration entry, should be a file.</param>
        /// <param name="hostTemplateConfigFile">the template host configuration entry, should be a file.</param>
        /// <param name="baselineName">the baseline to load.</param>
        /// <returns>true when template can be loaded, false otherwise. The loaded template is returned in <paramref name="template"/>.</returns>
        public bool TryGetTemplateFromConfigInfo(IFileSystemInfo templateFileConfig, out ITemplate? template, IFileSystemInfo? localeFileConfig = null, IFile? hostTemplateConfigFile = null, string? baselineName = null)
        {
            _ = templateFileConfig ?? throw new ArgumentNullException(nameof(templateFileConfig));
            ILogger logger = templateFileConfig.MountPoint.EnvironmentSettings.Host.LoggerFactory.CreateLogger<RunnableProjectGenerator>();
            try
            {
                IFile templateFile = templateFileConfig as IFile
                    ?? throw new NotSupportedException(string.Format(LocalizableStrings.RunnableProjectGenerator_Exception_ConfigShouldBeFile, templateFileConfig.GetDisplayPath()));

                IFile? localeFile = null;
                if (localeFileConfig != null)
                {
                    localeFile = localeFileConfig as IFile
                      ?? throw new NotSupportedException(string.Format(LocalizableStrings.RunnableProjectGenerator_Exception_LocaleConfigShouldBeFile, localeFileConfig.GetDisplayPath()));
                }

                var templateConfiguration = new RunnableProjectConfig(templateFileConfig.MountPoint.EnvironmentSettings, this, templateFile, hostTemplateConfigFile, localeFile, baselineName);
                template = templateConfiguration;
                return true;
            }
            catch (TemplateValidationException)
            {
                //do nothing
                //template validation prints all required information
            }
            catch (NotSupportedException ex)
            {
                //do not print stack trace for this type.
                logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, templateFileConfig.GetDisplayPath(), ex.Message);
            }
            catch (TemplateAuthoringException ex)
            {
                //do not print stack trace for this type.
                logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, templateFileConfig.GetDisplayPath(), ex.Message);
            }
            catch (Exception ex)
            {
                //unexpected error - print details
                logger.LogError(LocalizableStrings.Authoring_TemplateNotInstalled_Message, templateFileConfig.GetDisplayPath(), ex);
            }
            template = null;
            return false;
        }

        internal static IParameterSetBuilder GetParametersForTemplate(IRunnableProjectConfig templateConfig)
        {
            return new ParameterSetBuilder(templateConfig.Parameters.ToDictionary(p => p.Key, p => (ITemplateParameter)p.Value));
        }

        // In the future the RunableProjectConfig should be refactored so that it doesn't access symbols for information that should be possible
        //  to be disabled (so file renames, operation configs etc - those use parameterSymbols, but instead should look into PrameterSet)
        private static void RemoveDisabledParamsFromTemplate(ITemplate template, IEvaluatedParameterSetData evaluatedParameterSetData)
        {
            // Remove the disabled symbols from the config as well (as if they was never defined on the template)
            RunnableProjectConfig config = (RunnableProjectConfig)template;
            evaluatedParameterSetData.EvaluatedParametersData.Values
                .Where(d => d.IsEnabledConditionResult.HasValue && !d.IsEnabledConditionResult.Value)
                .Select(p => p.ParameterDefinition.Name)
                .ForEach(config.RemoveSymbol);
        }

        private static IVariableCollection SetupVariables(IEngineEnvironmentSettings environmentSettings, IEvaluatedParameterSetData parameters, IVariableConfig variableConfig)
        {
            IVariableCollection variables = VariableCollection.SetupVariables(environmentSettings, parameters, variableConfig);

            foreach (Parameter param in parameters.Values.OfType<Parameter>())
            {
                // Add choice values to variables - to allow them to be recognizable unquoted
                if (param.EnableQuotelessLiterals && param.IsChoice() && param.Choices != null)
                {
                    foreach (string choiceKey in param.Choices.Keys)
                    {
                        if (
                            variables.TryGetValue(choiceKey, out object existingValueObj) &&
                            existingValueObj is string existingValue &&
                            !string.Equals(choiceKey, existingValue, StringComparison.CurrentCulture)
                        )
                        {
                            throw new InvalidOperationException(string.Format(LocalizableStrings.RunnableProjectGenerator_CannotAddImplicitChoice, choiceKey, existingValue));
                        }
                        variables[choiceKey] = choiceKey;
                    }
                }
            }

            return variables;
        }

        // Note the deferred-config macros (generated) are part of the runConfig.Macros
        //      and not in the ComputedMacros.
        //  Possibly make a separate property for the deferred-config macros
        private static void ProcessMacros(IEngineEnvironmentSettings environmentSettings, IGlobalRunConfig runConfig, IVariableCollection variableCollection)
        {
            MacrosOperationConfig? macroProcessor = null;
            if (runConfig.Macros != null)
            {
                macroProcessor = new MacrosOperationConfig();
                macroProcessor.ProcessMacros(environmentSettings, runConfig.Macros, variableCollection);
            }

            if (runConfig.ComputedMacros != null)
            {
                macroProcessor = macroProcessor ?? new MacrosOperationConfig();
                macroProcessor.ProcessMacros(environmentSettings, runConfig.ComputedMacros, variableCollection);
            }
        }

        private static ICreationResult GetCreationResult(ILogger logger, IRunnableProjectConfig runnableProjectConfig, IVariableCollection variables)
        {
            return new CreationResult(
                postActions: PostAction.ListFromModel(logger, runnableProjectConfig.PostActionModels, variables),
                primaryOutputs: CreationPath.ListFromModel(logger, runnableProjectConfig.PrimaryOutputs, variables));
        }

        private IFile? FindBestHostTemplateConfigFile(IEngineEnvironmentSettings engineEnvironment, IFile config)
        {
            IDictionary<string, IFile> allHostFilesForTemplate = new Dictionary<string, IFile>();

            if (config.Parent is null)
            {
                return null;
            }

            foreach (IFile hostFile in config.Parent.EnumerateFiles($"*{HostTemplateFileConfigBaseName}", SearchOption.TopDirectoryOnly))
            {
                allHostFilesForTemplate.Add(hostFile.Name, hostFile);
            }

            string preferredHostFileName = string.Concat(engineEnvironment.Host.HostIdentifier, HostTemplateFileConfigBaseName);
            if (allHostFilesForTemplate.TryGetValue(preferredHostFileName, out IFile preferredHostFile))
            {
                return preferredHostFile;
            }

            foreach (string fallbackHostName in engineEnvironment.Host.FallbackHostTemplateConfigNames)
            {
                string fallbackHostFileName = string.Concat(fallbackHostName, HostTemplateFileConfigBaseName);

                if (allHostFilesForTemplate.TryGetValue(fallbackHostFileName, out IFile fallbackHostFile))
                {
                    return fallbackHostFile;
                }
            }

            return null;
        }

        internal class ParameterSetBuilder : ParametersDefinition, IParameterSetBuilder
        {
            private readonly Dictionary<ITemplateParameter, EvalData> _resolvedValues;
            private IEvaluatedParameterSetData? _result;

            public ParameterSetBuilder(IReadOnlyDictionary<string, ITemplateParameter> parameters) : base(parameters)
            {
                _resolvedValues = parameters.ToDictionary(p => p.Value, p => new EvalData(p.Value));
            }

            public void SetParameterValue(ITemplateParameter parameter, object value)
            {
                _resolvedValues[parameter].Value = value;
                _result = null;
            }

            public void SetParameterEvaluation(ITemplateParameter parameter, EvaluatedParameterData evaluatedParameterData)
            {
                var old = _resolvedValues[parameter];
                _resolvedValues[parameter] = new EvalData(evaluatedParameterData);
                if (old.InputDataState != InputDataState.Unset)
                {
                    _resolvedValues[parameter].Value = old.Value;
                }

                _result = null;
            }

            public bool HasParameterValue(ITemplateParameter parameter) => _resolvedValues[parameter].InputDataState != InputDataState.Unset;

            public void EvaluateConditionalParameters(ILogger logger)
            {
                List<EvalData> evaluatedParameters = _resolvedValues.Values.ToList();

                var variableCollection = new VariableCollection(
                    null,
                    evaluatedParameters
                        .Where(p => p.Value != null)
                        .ToDictionary(p => p.ParameterDefinition.Name, p => p.Value));

                EvalData[] variableCollectionIdxToParametersMap =
                    evaluatedParameters.Where(p => p.Value != null).Select(p => p).ToArray();

                EvaluateEnablementConditions(evaluatedParameters, variableCollection, variableCollectionIdxToParametersMap, logger);
                EvaluateRequirementCondition(evaluatedParameters, variableCollection, logger);
            }

            public IEvaluatedParameterSetData Build()
            {
                if (_result == null)
                {
                    _result = new EvaluatedParameterSetData(
                        this,
                        _resolvedValues.Select(p => p.Value.ToParameterData()).ToList());
                }

                return _result!;
            }

            private void EvaluateEnablementConditions(
                IReadOnlyList<EvalData> parameters,
                VariableCollection variableCollection,
                EvalData[] variableCollectionIdxToParametersMap,
                ILogger logger)
            {
                Dictionary<EvalData, HashSet<EvalData>> parametersDependencies = new();

                // First parameters traversal.
                //   - evaluate all IsEnabledCondition - and get the dependecies between the parameters during doing so
                foreach (EvalData parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.ParameterDefinition.Precedence.IsEnabledCondition))
                    {
                        HashSet<int> referencedVariablesIndexes = new HashSet<int>();
                        // Do not remove from the variable collection though - we want to capture all dependencies between parameters in the first traversal.
                        // Those will be bulk removed before second traversal (traversing only the required dependencies).
                        parameter.IsEnabledConditionResult =
                            Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsEnabledCondition, variableCollection, referencedVariablesIndexes);

                        if (referencedVariablesIndexes.Any())
                        {
                            parametersDependencies[parameter] = new HashSet<EvalData>(
                                referencedVariablesIndexes.Select(idx => variableCollectionIdxToParametersMap[idx]));
                        }
                    }
                }

                // No dependencies between parameters detected - no need to process further the second evaluation
                if (parametersDependencies.Count == 0)
                {
                    return;
                }

                DirectedGraph<EvalData> parametersDependenciesGraph = new(parametersDependencies);
                // Get the transitive closure of parameters that need to be recalculated, based on the knowledge of params that
                IReadOnlyList<EvalData> disabledParameters = parameters.Where(p => p.IsEnabledConditionResult.HasValue && !p.IsEnabledConditionResult.Value).ToList();
                DirectedGraph<EvalData> parametersToRecalculate =
                    parametersDependenciesGraph.GetSubGraphDependandOnVertices(disabledParameters, includeSeedVertices: false);

                // Second traversal - for transitive dependencies of parameters that need to be disabled
                if (parametersToRecalculate.TryGetTopologicalSort(out IReadOnlyList<EvalData> orderedParameters))
                {
                    disabledParameters.ForEach(p => variableCollection.Remove(p.ParameterDefinition.Name));

                    if (parametersDependenciesGraph.HasCycle(out var cycle))
                    {
                        logger.LogWarning(LocalizableStrings.RunnableProjectGenerator_Warning_ParamsCycle, cycle.Select(p => p.ParameterDefinition.Name).ToCsvString());
                    }

                    foreach (EvalData parameter in orderedParameters)
                    {
                        bool isEnabled = Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsEnabledCondition, variableCollection);
                        parameter.IsEnabledConditionResult = isEnabled;
                        if (!isEnabled)
                        {
                            variableCollection.Remove(parameter.ParameterDefinition.Name);
                        }
                    }
                }
                else if (parametersToRecalculate.HasCycle(out var cycle))
                {
                    throw new TemplateAuthoringException(
                        string.Format(
                            LocalizableStrings.RunnableProjectGenerator_Error_ParamsCycle,
                            cycle.Select(p => p.ParameterDefinition.Name).ToCsvString()),
                        "Conditional Parameters");
                }
                else
                {
                    throw new Exception(LocalizableStrings.RunnableProjectGenerator_Error_TopologicalSort);
                }
            }

            private void EvaluateRequirementCondition(
                IReadOnlyList<EvalData> parameters,
                VariableCollection variableCollection,
                ILogger logger
            )
            {
                foreach (EvalData parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.ParameterDefinition.Precedence.IsRequiredCondition))
                    {
                        parameter.IsRequiredConditionResult =
                            Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsRequiredCondition, variableCollection);
                    }
                }
            }

            private class EvalData
            {
                private object? _value;

                public EvalData(
                    ITemplateParameter parameterDefinition,
                    object? value,
                    bool? isEnabledConditionResult,
                    bool? isRequiredConditionResult)
                {
                    ParameterDefinition = parameterDefinition;
                    _value = value;
                    IsEnabledConditionResult = isEnabledConditionResult;
                    IsRequiredConditionResult = isRequiredConditionResult;
                }

                public EvalData(ITemplateParameter parameterDefinition)
                {
                    ParameterDefinition = parameterDefinition;
                }

                public EvalData(EvaluatedParameterData other)
                    : this(other.ParameterDefinition, other.Value, other.IsEnabledConditionResult, other.IsRequiredConditionResult)
                { }

                public ITemplateParameter ParameterDefinition { get; }

                public object? Value
                {
                    get { return _value; }

                    set
                    {
                        _value = value;
                        InputDataState = (value == null) ? InputDataState.ExplicitNull : InputDataState.Set;
                    }
                }

                public InputDataState InputDataState { get; private set; } = InputDataState.Unset;

                public bool? IsEnabledConditionResult { get; set; }

                public bool? IsRequiredConditionResult { get; set; }

                public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";

                public EvaluatedParameterData ToParameterData()
                {
                    return new EvaluatedParameterData(
                        this.ParameterDefinition,
                        this.Value,
                        this.IsEnabledConditionResult,
                        this.IsRequiredConditionResult,
                        InputDataState);
                }
            }
        }
    }
}
