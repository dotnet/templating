﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class RunnableProjectGenerator : IGenerator
    {
        public static readonly string TemplateConfigDirectoryName = ".template.config";
        public static readonly string TemplateConfigFileName = "template.json";
        private static readonly string AdditionalConfigFilesIndicator = "AdditionalConfigFiles";
        private static readonly Guid GeneratorId = new Guid("0C434DF7-E2CB-4DEE-B216-D7C58C8EB4B3");
        private static readonly string GeneratorVersion = "1.0.0.0";
        public Guid Id => GeneratorId;
        //
        // Converts the raw, string version of a parameter to a strongly typed value.
        // If the param has a datatype specified, use that. Otherwise attempt to infer the type.
        // Throws a TemplateParamException if the conversion fails for any reason.
        //
        public object ConvertParameterValueToType(IEngineEnvironmentSettings environmentSettings, ITemplateParameter parameter, string untypedValue, out bool valueResolutionError)
        {
            return InternalConvertParameterValueToType(environmentSettings, parameter, untypedValue, out valueResolutionError);
        }

        public Task<ICreationResult> CreateAsync(IEngineEnvironmentSettings environmentSettings, ITemplate templateData, IParameterSet parameters, IComponentManager componentManager, string targetDirectory)
        {
            RunnableProjectTemplate template = (RunnableProjectTemplate)templateData;
            ProcessMacros(environmentSettings, componentManager, template.Config.OperationConfig, parameters);

            IVariableCollection variables = VariableCollection.SetupVariables(environmentSettings, parameters, template.Config.OperationConfig.VariableSetup);
            template.Config.Evaluate(parameters, variables, template.ConfigFile);

            IOrchestrator2 basicOrchestrator = new Core.Util.Orchestrator();
            RunnableProjectOrchestrator orchestrator = new RunnableProjectOrchestrator(basicOrchestrator);

            GlobalRunSpec runSpec = new GlobalRunSpec(template.TemplateSourceRoot, componentManager, parameters, variables, template.Config.OperationConfig, template.Config.SpecialOperationConfig, template.Config.LocalizationOperations, template.Config.IgnoreFileNames);

            foreach (FileSourceMatchInfo source in template.Config.Sources)
            {
                runSpec.SetupFileSource(source);
                string target = Path.Combine(targetDirectory, source.Target);
                orchestrator.Run(runSpec, template.TemplateSourceRoot.DirectoryInfo(source.Source), target);
            }

            return Task.FromResult(GetCreationResult(environmentSettings, template, variables));
        }

        /// <summary>
        /// Performs the dry-run of the template instantiation to evaluate the primary outputs, post actions to be applied and file changes to be made when executing the template with specified parameters.
        /// </summary>
        /// <param name="environmentSettings">environment settings</param>
        /// <param name="templateData">the template to be executed</param>
        /// <param name="parameters">the parameters to be used on template execution</param>
        /// <param name="componentManager">the instance of component manager</param>
        /// <param name="targetDirectory">the output path for the template</param>
        /// <returns>the primary outputs, post actions and file changes that will be made when executing the template with specified parameters.</returns>
        public ICreationEffects GetCreationEffects(IEngineEnvironmentSettings environmentSettings, ITemplate templateData, IParameterSet parameters, IComponentManager componentManager, string targetDirectory)
        {
            RunnableProjectTemplate template = (RunnableProjectTemplate)templateData;
            ProcessMacros(environmentSettings, componentManager, template.Config.OperationConfig, parameters);

            IVariableCollection variables = VariableCollection.SetupVariables(environmentSettings, parameters, template.Config.OperationConfig.VariableSetup);
            template.Config.Evaluate(parameters, variables, template.ConfigFile);

            IOrchestrator2 basicOrchestrator = new Core.Util.Orchestrator();
            RunnableProjectOrchestrator orchestrator = new RunnableProjectOrchestrator(basicOrchestrator);

            GlobalRunSpec runSpec = new GlobalRunSpec(template.TemplateSourceRoot, componentManager, parameters, variables, template.Config.OperationConfig, template.Config.SpecialOperationConfig, template.Config.LocalizationOperations, template.Config.IgnoreFileNames);
            List<IFileChange2> changes = new List<IFileChange2>();

            foreach (FileSourceMatchInfo source in template.Config.Sources)
            {
                runSpec.SetupFileSource(source);
                string target = Path.Combine(targetDirectory, source.Target);
                IReadOnlyList<IFileChange2> fileChanges = orchestrator.GetFileChanges(runSpec, template.TemplateSourceRoot.DirectoryInfo(source.Source), target);

                //source and target paths in the file changes are returned relative to source passed
                //GetCreationEffects method should return the source paths relative to template source root (location of .template.config folder) and target paths relative to output path and not relative to certain source
                //add source and target used to file changes to be returned as the result
                changes.AddRange(
                    fileChanges.Select(
                        fileChange => new FileChange(
                            Path.Combine(source.Source, fileChange.SourceRelativePath),
                            Path.Combine(source.Target, fileChange.TargetRelativePath),
                            fileChange.ChangeKind,
                            fileChange.Contents)));
            }

            return new CreationEffects2
            {
                FileChanges = changes,
                CreationResult = GetCreationResult(environmentSettings, template, variables)
            };
        }

        public IParameterSet GetParametersForTemplate(IEngineEnvironmentSettings environmentSettings, ITemplate template)
        {
            RunnableProjectTemplate tmplt = (RunnableProjectTemplate)template;
            return new ParameterSet(tmplt.Config);
        }

        public IList<ITemplate> GetTemplatesAndLangpacksFromDir(IMountPoint source, out IList<ILocalizationLocator> localizations)
        {
            IDirectory folder = source.Root;

            Regex localeFileRegex = new Regex(@"
                ^
                (?<locale>
                    [a-z]{2}
                    (?:-[A-Z]{2})?
                )
                \."
                + Regex.Escape(TemplateConfigFileName)
                + "$"
                , RegexOptions.IgnorePatternWhitespace);

            IList<ITemplate> templateList = new List<ITemplate>();
            localizations = new List<ILocalizationLocator>();

            foreach (IFile file in folder.EnumerateFiles("*" + TemplateConfigFileName, SearchOption.AllDirectories))
            {
                if (string.Equals(file.Name, TemplateConfigFileName, StringComparison.OrdinalIgnoreCase))
                {
                    IFile hostConfigFile = file.MountPoint.EnvironmentSettings.SettingsLoader.FindBestHostTemplateConfigFile(file);

                    if (TryGetTemplateFromConfigInfo(file, out ITemplate template, hostTemplateConfigFile: hostConfigFile))
                    {
                        templateList.Add(template);
                    }

                    continue;
                }

                Match localeMatch = localeFileRegex.Match(file.Name);
                if (localeMatch.Success)
                {
                    string locale = localeMatch.Groups["locale"].Value;

                    if (TryGetLangPackFromFile(file, out ILocalizationModel locModel))
                    {
                        ILocalizationLocator locator = new LocalizationLocator()
                        {
                            Locale = locale,
                            MountPointUri = source.MountPointUri,
                            ConfigPlace = file.FullPath,
                            Identity = locModel.Identity,
                            Author = locModel.Author,
                            Name = locModel.Name,
                            Description = locModel.Description,
                            ParameterSymbols = locModel.ParameterSymbols
                        };
                        localizations.Add(locator);
                    }

                    continue;
                }
            }

            return templateList;
        }

        public bool TryGetTemplateFromConfigInfo(IFileSystemInfo templateFileConfig, out ITemplate template, IFileSystemInfo localeFileConfig = null, IFile hostTemplateConfigFile = null, string baselineName = null)
        {
            IFile templateFile = templateFileConfig as IFile;

            if (templateFile == null)
            {
                template = null;
                return false;
            }

            IFile localeFile = localeFileConfig as IFile;
            ITemplateEngineHost host = templateFileConfig.MountPoint.EnvironmentSettings.Host;

            try
            {
                JObject baseSrcObject = ReadJObjectFromIFile(templateFile);
                JObject srcObject = MergeAdditionalConfiguration(baseSrcObject, templateFileConfig);

                JObject localeSourceObject = null;
                if (localeFile != null)
                {
                    localeSourceObject = ReadJObjectFromIFile(localeFile);
                }

                ISimpleConfigModifiers configModifiers = new SimpleConfigModifiers()
                {
                    BaselineName = baselineName
                };
                SimpleConfigModel templateModel = SimpleConfigModel.FromJObject(templateFile.MountPoint.EnvironmentSettings, srcObject, configModifiers, localeSourceObject);

                if (!PerformTemplateValidation(templateModel, templateFile, host))
                {
                    template = null;
                    return false;
                }

                if (!CheckGeneratorVersionRequiredByTemplate(templateModel.GeneratorVersions))
                {   // template isn't compatible with this generator version
                    template = null;
                    return false;
                }

                RunnableProjectTemplate runnableProjectTemplate = new RunnableProjectTemplate(srcObject, this, templateFile, templateModel, null, hostTemplateConfigFile);
                if (!AreAllTemplatePathsValid(templateModel, runnableProjectTemplate))
                {
                    template = null;
                    return false;
                }

                template = runnableProjectTemplate;
                return true;
            }
            catch (Exception ex)
            {
                host.LogMessage($"Error reading template from file: {templateFile.FullPath} | Error = {ex.Message}");
            }

            template = null;
            return false;
        }

        // For explicitly data-typed variables, attempt to convert the variable value to the specified type.
        // Data type names:
        //     - choice
        //     - bool
        //     - float
        //     - int
        //     - hex
        //     - text
        // The data type names are case insensitive.
        //
        // Returns the converted value if it can be converted, throw otherwise
        internal static object DataTypeSpecifiedConvertLiteral(IEngineEnvironmentSettings environmentSettings, ITemplateParameter param, string literal, out bool valueResolutionError)
        {
            valueResolutionError = false;

            if (string.Equals(param.DataType, "bool", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(literal, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (string.Equals(literal, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                else
                {
                    bool boolVal = false;
                    // Note: if the literal is ever null, it is probably due to a problem in TemplateCreator.Instantiate()
                    // which takes care of making null bool -> true as appropriate.
                    // This else can also happen if there is a value but it can't be converted.
                    string val;
                    while (environmentSettings.Host.OnParameterError(param, null, "ParameterValueNotSpecified", out val) && !bool.TryParse(val, out boolVal))
                    {
                    }

                    valueResolutionError = !bool.TryParse(val, out boolVal);
                    return boolVal;
                }
            }
            else if (string.Equals(param.DataType, "choice", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResolveChoiceValue(literal, param, out string match))
                {
                    return match;
                }

                if (literal == null && param.Priority != TemplateParameterPriority.Required)
                {
                    return param.DefaultValue;
                }

                string val;
                while (environmentSettings.Host.OnParameterError(param, null, "ValueNotValid:" + string.Join(",", param.Choices.Keys), out val)
                        && !TryResolveChoiceValue(literal, param, out val))
                {
                }

                valueResolutionError = val == null;
                return val;
            }

            else if (string.Equals(param.DataType, "float", StringComparison.OrdinalIgnoreCase))
            {
                if (ParserExtensions.DoubleTryParseСurrentOrInvariant(literal, out double convertedFloat))
                {
                    return convertedFloat;
                }
                else
                {
                    string val;
                    while (environmentSettings.Host.OnParameterError(param, null, "ValueNotValidMustBeFloat", out val) && (val == null || !ParserExtensions.DoubleTryParseСurrentOrInvariant(val, out convertedFloat)))
                    {
                    }

                    valueResolutionError = !ParserExtensions.DoubleTryParseСurrentOrInvariant(val, out convertedFloat);
                    return convertedFloat;
                }
            }
            else if (string.Equals(param.DataType, "int", StringComparison.OrdinalIgnoreCase)
                || string.Equals(param.DataType, "integer", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(literal, out long convertedInt))
                {
                    return convertedInt;
                }
                else
                {
                    string val;
                    while (environmentSettings.Host.OnParameterError(param, null, "ValueNotValidMustBeInteger", out val) && (val == null || !long.TryParse(val, out convertedInt)))
                    {
                    }

                    valueResolutionError = !long.TryParse(val, out convertedInt);
                    return convertedInt;
                }
            }
            else if (string.Equals(param.DataType, "hex", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(literal.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long convertedHex))
                {
                    return convertedHex;
                }
                else
                {
                    string val;
                    while (environmentSettings.Host.OnParameterError(param, null, "ValueNotValidMustBeHex", out val) && (val == null || val.Length < 3 || !long.TryParse(val.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out convertedHex)))
                    {
                    }

                    valueResolutionError = !long.TryParse(val.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out convertedHex);
                    return convertedHex;
                }
            }
            else if (string.Equals(param.DataType, "text", StringComparison.OrdinalIgnoreCase)
                || string.Equals(param.DataType, "string", StringComparison.OrdinalIgnoreCase))
            {   // "text" is a valid data type, but doesn't need any special handling.
                return literal;
            }
            else
            {
                return literal;
            }
        }

        internal static object InferTypeAndConvertLiteral(string literal)
        {
            if (literal == null)
            {
                return null;
            }

            if (!literal.Contains("\""))
            {
                if (string.Equals(literal, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(literal, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.Equals(literal, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if ((literal.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    || literal.Contains(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
                    && ParserExtensions.DoubleTryParseСurrentOrInvariant(literal, out double literalDouble))
                {
                    return literalDouble;
                }

                if (long.TryParse(literal, out long literalLong))
                {
                    return literalLong;
                }

                if (literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(literal.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out literalLong))
                {
                    return literalLong;
                }
            }

            return literal;
        }

        internal static object InternalConvertParameterValueToType(IEngineEnvironmentSettings environmentSettings, ITemplateParameter parameter, string untypedValue, out bool valueResolutionError)
        {
            if (untypedValue == null)
            {
                valueResolutionError = false;
                return null;
            }

            if (!string.IsNullOrEmpty(parameter.DataType))
            {
                object convertedValue = DataTypeSpecifiedConvertLiteral(environmentSettings, parameter, untypedValue, out valueResolutionError);
                return convertedValue;
            }
            else
            {
                valueResolutionError = false;
                return InferTypeAndConvertLiteral(untypedValue);
            }
        }

        // TODO: localize the diagnostic strings
        // checks that all the template sources are under the template root, and they exist.
        internal bool AreAllTemplatePathsValid(IRunnableProjectConfig templateConfig, RunnableProjectTemplate runnableTemplate)
        {
            ITemplateEngineHost host = runnableTemplate.Source.EnvironmentSettings.Host;

            if (runnableTemplate.TemplateSourceRoot == null)
            {
                host.LogDiagnosticMessage(string.Empty, "Authoring");
                host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateRootOutsideInstallSource, runnableTemplate.Name), "Authoring");
                return false;
            }

            // check if any sources get out of the mount point
            bool allSourcesValid = true;
            foreach (FileSourceMatchInfo source in templateConfig.Sources)
            {
                try
                {
                    IFile file = runnableTemplate.TemplateSourceRoot.FileInfo(source.Source);

                    if (file?.Exists ?? false)
                    {
                        allSourcesValid = false;
                        host.LogDiagnosticMessage(string.Empty, "Authoring");
                        host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateNameDisplay, runnableTemplate.Name), "Authoring");
                        host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateSourceRoot, runnableTemplate.TemplateSourceRoot.FullPath), "Authoring");
                        host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_SourceMustBeDirectory, source.Source), "Authoring");
                    }
                    else
                    {
                        IDirectory sourceRoot = runnableTemplate.TemplateSourceRoot.DirectoryInfo(source.Source);

                        if (!(sourceRoot?.Exists ?? false))
                        {
                            // non-existant directory
                            allSourcesValid = false;
                            host.LogDiagnosticMessage(string.Empty, "Authoring");
                            host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateNameDisplay, runnableTemplate.Name), "Authoring");
                            host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateSourceRoot, runnableTemplate.TemplateSourceRoot.FullPath), "Authoring");
                            host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_SourceDoesNotExist, source.Source), "Authoring");
                            host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_SourceIsOutsideInstallSource, sourceRoot.FullPath), "Authoring");
                        }
                    }
                }
                catch
                {   // outside the mount point root
                    // TODO: after the null ref exception in DirectoryInfo is fixed, change how this check works.
                    allSourcesValid = false;
                    host.LogDiagnosticMessage(string.Empty, "Authoring");
                    host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateNameDisplay, runnableTemplate.Name), "Authoring");
                    host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateSourceRoot, runnableTemplate.TemplateSourceRoot.FullPath), "Authoring");
                    host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateRootOutsideInstallSource, source.Source), "Authoring");
                }
            }

            return allSourcesValid;
        }

        internal JObject ReadJObjectFromIFile(IFile file)
        {
            using (Stream s = file.OpenRead())
            using (TextReader tr = new StreamReader(s, true))
            using (JsonReader r = new JsonTextReader(tr))
            {
                return JObject.Load(r);
            }
        }

        private static ICreationResult GetCreationResult(IEngineEnvironmentSettings environmentSettings, RunnableProjectTemplate template, IVariableCollection variables)
        {
            return new CreationResult()
            {
                PostActions = PostAction.ListFromModel(environmentSettings, template.Config.PostActionModel, variables),
                PrimaryOutputs = CreationPath.ListFromModel(environmentSettings, template.Config.PrimaryOutputs, variables)
            };
        }

        // Note the deferred-config macros (generated) are part of the runConfig.Macros
        //      and not in the ComputedMacros.
        //  Possibly make a separate property for the deferred-config macros
        private static void ProcessMacros(IEngineEnvironmentSettings environmentSettings, IComponentManager componentManager, IGlobalRunConfig runConfig, IParameterSet parameters)
        {
            if (runConfig.Macros != null)
            {
                IVariableCollection varsForMacros = VariableCollection.SetupVariables(environmentSettings, parameters, runConfig.VariableSetup);
                MacrosOperationConfig macroProcessor = new MacrosOperationConfig();
                macroProcessor.ProcessMacros(environmentSettings, componentManager, runConfig.Macros, varsForMacros, parameters);
            }

            if (runConfig.ComputedMacros != null)
            {
                IVariableCollection varsForMacros = VariableCollection.SetupVariables(environmentSettings, parameters, runConfig.VariableSetup);
                MacrosOperationConfig macroProcessor = new MacrosOperationConfig();
                macroProcessor.ProcessMacros(environmentSettings, componentManager, runConfig.ComputedMacros, varsForMacros, parameters);
            }
        }
        private static bool TryResolveChoiceValue(string literal, ITemplateParameter param, out string match)
        {
            if (literal == null)
            {
                match = null;
                return false;
            }

            string partialMatch = null;

            foreach (string choiceValue in param.Choices.Keys)
            {
                if (string.Equals(choiceValue, literal, StringComparison.OrdinalIgnoreCase))
                {   // exact match is good, regardless of partial matches
                    match = choiceValue;
                    return true;
                }
                else if (choiceValue.StartsWith(literal, StringComparison.OrdinalIgnoreCase))
                {
                    if (partialMatch == null)
                    {
                        partialMatch = choiceValue;
                    }
                    else
                    {   // multiple partial matches, can't take one.
                        match = null;
                        return false;
                    }
                }
            }

            match = partialMatch;
            return match != null;
        }

        private bool CheckGeneratorVersionRequiredByTemplate(string generatorVersionsAllowed)
        {
            if (string.IsNullOrEmpty(generatorVersionsAllowed))
            {
                return true;
            }

            if (!VersionStringHelpers.TryParseVersionSpecification(generatorVersionsAllowed, out IVersionSpecification versionChecker))
            {
                return false;
            }

            return versionChecker.CheckIfVersionIsValid(GeneratorVersion);
        }

        // Checks the primarySource for additional configuration files.
        // If found, merges them all together.
        // Returns the merged JObject (or the original if there was nothing to merge).
        // Additional files must be in the same dir as the template file.
        private JObject MergeAdditionalConfiguration(JObject primarySource, IFileSystemInfo primarySourceConfig)
        {
            IReadOnlyList<string> otherFiles = primarySource.ArrayAsStrings(AdditionalConfigFilesIndicator);

            if (!otherFiles.Any())
            {
                return primarySource;
            }

            JObject combinedSource = (JObject)primarySource.DeepClone();

            foreach (string partialConfigFileName in otherFiles)
            {
                if (!partialConfigFileName.EndsWith("." + TemplateConfigFileName))
                {
                    throw new TemplateAuthoringException($"Split configuration error with file [{partialConfigFileName}]. Additional configuration file names must end with '.{TemplateConfigFileName}'.", partialConfigFileName);
                }

                IFile partialConfigFile = primarySourceConfig.Parent.EnumerateFiles(partialConfigFileName, SearchOption.TopDirectoryOnly).FirstOrDefault(x => string.Equals(x.Name, partialConfigFileName));

                if (partialConfigFile == null)
                {
                    throw new TemplateAuthoringException($"Split configuration file [{partialConfigFileName}] could not be found.", partialConfigFileName);
                }

                JObject partialConfigJson = ReadJObjectFromIFile(partialConfigFile);
                combinedSource.Merge(partialConfigJson);
            }

            return combinedSource;
        }

        private bool PerformTemplateValidation(SimpleConfigModel templateModel, IFile templateFile, ITemplateEngineHost host)
        {
            //Do some basic checks...
            List<string> errorMessages = new List<string>();
            List<string> warningMessages = new List<string>();
            if (string.IsNullOrEmpty(templateModel.Identity))
            {
                errorMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "identity", templateFile.FullPath));
            }

            if (string.IsNullOrEmpty(templateModel.Name))
            {
                errorMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "name", templateFile.FullPath));
            }

            if ((templateModel.ShortNameList?.Count ?? 0) == 0)
            {
                errorMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "shortName", templateFile.FullPath));
            }

            if (string.IsNullOrEmpty(templateModel.SourceName))
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "sourceName", templateFile.FullPath));
            }

            if (string.IsNullOrEmpty(templateModel.Author))
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "author", templateFile.FullPath));
            }

            if (string.IsNullOrEmpty(templateModel.GroupIdentity))
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "groupIdentity", templateFile.FullPath));
            }

            if (string.IsNullOrEmpty(templateModel.GeneratorVersions))
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "generatorVersions", templateFile.FullPath));
            }

            if (templateModel.Precedence == 0)
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "precedence", templateFile.FullPath));
            }

            if ((templateModel.Classifications?.Count ?? 0) == 0)
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MissingValue, "classifications", templateFile.FullPath));
            }

            if (templateModel.PostActionModel != null && templateModel.PostActionModel.Any(x => x.ManualInstructionInfo == null || x.ManualInstructionInfo.Count == 0))
            {
                warningMessages.Add(string.Format(LocalizableStrings.Authoring_MalformedPostActionManualInstructions, templateFile.FullPath));
            }

            if (warningMessages.Count > 0)
            {
                host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateMissingCommonInformation, templateFile.FullPath), "Authoring");

                foreach (string message in warningMessages)
                {
                    host.LogDiagnosticMessage("    " + message, "Authoring");
                }
            }

            if (errorMessages.Count > 0)
            {
                host.LogDiagnosticMessage(string.Format(LocalizableStrings.Authoring_TemplateNotInstalled, templateFile.FullPath), "Authoring");

                foreach (string message in errorMessages)
                {
                    host.LogDiagnosticMessage("    " + message, "Authoring");
                }

                return false;
            }

            return true;
        }

        private bool TryGetLangPackFromFile(IFile file, out ILocalizationModel locModel)
        {
            if (file == null)
            {
                locModel = null;
                return false;
            }

            try
            {
                JObject srcObject = ReadJObjectFromIFile(file);
                locModel = SimpleConfigModel.LocalizationFromJObject(srcObject);
                return true;
            }
            catch (Exception ex)
            {
                ITemplateEngineHost host = file.MountPoint.EnvironmentSettings.Host;
                host.LogMessage($"Error reading Langpack from file: {file.FullPath} | Error = {ex.ToString()}");
            }

            locModel = null;
            return false;
        }
        internal class ParameterSet : IParameterSet
        {
            private readonly IDictionary<string, ITemplateParameter> _parameters = new Dictionary<string, ITemplateParameter>(StringComparer.OrdinalIgnoreCase);

            public ParameterSet(IRunnableProjectConfig config)
            {
                foreach (KeyValuePair<string, Parameter> p in config.Parameters)
                {
                    p.Value.Name = p.Key;
                    _parameters[p.Key] = p.Value;
                }
            }

            public IEnumerable<ITemplateParameter> ParameterDefinitions => _parameters.Values;

            public IEnumerable<string> RequiredBrokerCapabilities => Enumerable.Empty<string>();
            public IDictionary<ITemplateParameter, object> ResolvedValues { get; } = new Dictionary<ITemplateParameter, object>();
            public void AddParameter(ITemplateParameter param)
            {
                _parameters[param.Name] = param;
            }

            public bool TryGetParameterDefinition(string name, out ITemplateParameter parameter)
            {
                if (_parameters.TryGetValue(name, out parameter))
                {
                    return true;
                }

                parameter = new Parameter
                {
                    Name = name,
                    Requirement = TemplateParameterPriority.Optional,
                    IsVariable = true,
                    Type = "string"
                };

                return true;
            }
        }
    }
}
