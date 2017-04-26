using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Expressions.Cpp;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Core.Util;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class GlobalRunSpec : IGlobalRunSpec
    {
        private static IReadOnlyDictionary<string, IOperationConfig> _operationConfigLookup;

        private static void EnsureOperationConfigs(IComponentManager componentManager)
        {
            if (_operationConfigLookup == null)
            {
                List<IOperationConfig> operationConfigReaders = new List<IOperationConfig>(componentManager.OfType<IOperationConfig>());
                Dictionary<string, IOperationConfig> operationConfigLookup = new Dictionary<string, IOperationConfig>();

                foreach (IOperationConfig opConfig in operationConfigReaders)
                {
                    operationConfigLookup[opConfig.Key] = opConfig;
                }

                _operationConfigLookup = operationConfigLookup;
            }
        }

        public IReadOnlyList<IPathMatcher> Include { get; private set; }

        public IReadOnlyList<IPathMatcher> Exclude { get; private set; }

        public IReadOnlyList<IPathMatcher> CopyOnly { get; private set; }

        public IReadOnlyDictionary<string, string> Rename { get; private set; }

        public IReadOnlyList<IOperationProvider> Operations { get; }

        public IVariableCollection RootVariableCollection { get; }

        public IReadOnlyList<KeyValuePair<IPathMatcher, IRunSpec>> Special { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<IOperationProvider>> LocalizationOperations { get; }

        public IReadOnlyList<string> IgnoreFileNames { get; }

        public bool TryGetTargetRelPath(string sourceRelPath, out string targetRelPath)
        {
            return Rename.TryGetValue(sourceRelPath, out targetRelPath);
        }

        public GlobalRunSpec(
            IDirectory templateRoot,
            IComponentManager componentManager,
            IParameterSet parameters, 
            IVariableCollection variables,
            IGlobalRunConfig globalConfig,
            IReadOnlyList<KeyValuePair<string, IGlobalRunConfig>> fileGlobConfigs,
            IReadOnlyDictionary<string, IReadOnlyList<IOperationProvider>> localizationOperations,
            IReadOnlyList<string> ignoreFileNames)
        {
            EnsureOperationConfigs(componentManager);

            RootVariableCollection = variables;
            LocalizationOperations = localizationOperations;
            IgnoreFileNames = ignoreFileNames;
            Operations = ResolveOperations(globalConfig, templateRoot, variables, parameters);
            List<KeyValuePair<IPathMatcher, IRunSpec>> specials = new List<KeyValuePair<IPathMatcher, IRunSpec>>();

            if (fileGlobConfigs != null)
            {
                foreach (KeyValuePair<string, IGlobalRunConfig> specialEntry in fileGlobConfigs)
                {
                    IReadOnlyList<IOperationProvider> specialOps = null;
                    IVariableCollection specialVariables = variables;

                    if (specialEntry.Value != null)
                    {
                        specialOps = ResolveOperations(specialEntry.Value, templateRoot, variables, parameters);
                        specialVariables = VariableCollection.SetupVariables(templateRoot.MountPoint.EnvironmentSettings, parameters, specialEntry.Value.VariableSetup);
                    }

                    RunSpec spec = new RunSpec(specialOps, specialVariables ?? variables, specialEntry.Value.VariableSetup.FallbackFormat);
                    specials.Add(new KeyValuePair<IPathMatcher, IRunSpec>(new GlobbingPatternMatcher(specialEntry.Key), spec));
                }
            }

            Special = specials;
        }

        public void SetupFileSource(FileSourceMatchInfo source)
        {
            FileSourceHierarchicalPathMatcher matcher = new FileSourceHierarchicalPathMatcher(source);
            Include = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Include, matcher) };
            Exclude = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Exclude, matcher) };
            CopyOnly = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.CopyOnly, matcher) };
            Rename = source.Renames ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Returns a list of operations which contains the custom operations and the default operations.
        // If there are custom Conditional operations, don't include the default Conditionals.
        //
        // Note: we may need a more robust filtering mechanism in the future.
        private static IReadOnlyList<IOperationProvider> ResolveOperations(IGlobalRunConfig runConfig, IDirectory templateRoot, IVariableCollection variables, IParameterSet parameters)
        {
            IReadOnlyList<IOperationProvider> customOperations = SetupCustomOperations(runConfig.CustomOperations, templateRoot, variables);
            IReadOnlyList<IOperationProvider> defaultOperations = SetupOperations(templateRoot.MountPoint.EnvironmentSettings, parameters, runConfig);

            List<IOperationProvider> operations = new List<IOperationProvider>(customOperations);

            if (customOperations.Any(x => x is Conditional))
            {
                operations.AddRange(defaultOperations.Where(op => !(op is Conditional)));
            }
            else
            {
                operations.AddRange(defaultOperations);
            }

            return operations;
        }

        private static IReadOnlyList<IOperationProvider> SetupOperations(IEngineEnvironmentSettings environmentSettings, IParameterSet parameters, IGlobalRunConfig runConfig)
        {
            // default operations
            List<IOperationProvider> operations = new List<IOperationProvider>();
            operations.AddRange(runConfig.Operations);

            // replacements
            if (runConfig.Replacements != null)
            {
                foreach (IReplacementTokens replaceSetup in runConfig.Replacements)
                {
                    IOperationProvider replacement = ReplacementConfig.Setup(environmentSettings, replaceSetup, parameters);
                    if (replacement != null)
                    {
                        operations.Add(replacement);
                    }
                }
            }

            if (runConfig.VariableSetup.Expand)
            {
                operations?.Add(new ExpandVariables(null, true));
            }

            return operations;
        }

        private static IReadOnlyList<IOperationProvider> SetupCustomOperations(IReadOnlyList<ICustomOperationModel> customModel, IDirectory templateRoot, IVariableCollection variables)
        {
            ITemplateEngineHost host = templateRoot.MountPoint.EnvironmentSettings.Host;
            List<IOperationProvider> customOperations = new List<IOperationProvider>();

            foreach (ICustomOperationModel opModelUntyped in customModel)
            {
                CustomOperationModel opModel = opModelUntyped as CustomOperationModel;
                if (opModel == null)
                {
                    host.LogMessage($"Operation type = [{opModelUntyped.Type}] could not be cast as a CustomOperationModel");
                    continue;
                }
                    
                string opType = opModel.Type;
                string condition = opModel.Condition;

                if (string.IsNullOrEmpty(condition)
                    || Cpp2StyleEvaluatorDefinition.EvaluateFromString(templateRoot.MountPoint.EnvironmentSettings, condition, variables))
                {
                    IOperationConfig realConfigObject;
                    if (_operationConfigLookup.TryGetValue(opType, out realConfigObject))
                    {
                        customOperations.AddRange(
                            realConfigObject.ConfigureFromJObject(opModel.Configuration, templateRoot));
                    }
                    else
                    {
                        host.LogMessage($"Operation type = [{opType}] from configuration is unknown.");
                    }
                }
            }

            return customOperations;
        }

        internal class ProcessorState : IProcessorState
        {
            public ProcessorState(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, byte[] buffer, Encoding encoding)
            {
                Config = new EngineConfig(environmentSettings, vars);
                CurrentBuffer = buffer;
                CurrentBufferPosition = 0;
                Encoding = encoding;
                EncodingConfig = new EncodingConfig(Config, encoding);
            }

            public IEngineConfig Config { get; }

            public byte[] CurrentBuffer { get; private set; }

            public int CurrentBufferLength => CurrentBuffer.Length;

            public int CurrentBufferPosition { get; }

            public Encoding Encoding { get; set; }

            public IEncodingConfig EncodingConfig { get; }

            public int CurrentSequenceNumber => throw new NotImplementedException();

            public bool AdvanceBuffer(int bufferPosition)
            {
                byte[] tmp = new byte[CurrentBufferLength - bufferPosition];
                Buffer.BlockCopy(CurrentBuffer, bufferPosition, tmp, 0, CurrentBufferLength - bufferPosition);
                CurrentBuffer = tmp;

                return true;
            }

            public void SeekBackUntil(ITokenTrie match)
            {
                throw new NotImplementedException();
            }

            public void SeekBackUntil(ITokenTrie match, bool consume)
            {
                throw new NotImplementedException();
            }

            public void SeekBackWhile(ITokenTrie match)
            {
                throw new NotImplementedException();
            }

            public void SeekForwardUntil(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
            {
                throw new NotImplementedException();
            }

            public void SeekForwardThrough(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
            {
                throw new NotImplementedException();
            }

            public void SeekForwardWhile(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
            {
                throw new NotImplementedException();
            }
        }
    }
}
