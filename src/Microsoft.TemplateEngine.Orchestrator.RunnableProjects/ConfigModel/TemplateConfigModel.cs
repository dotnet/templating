// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel
{
    /// <summary>
    /// The class represents model of template.json.
    /// </summary>
    public sealed class TemplateConfigModel
    {
        private const string NameSymbolName = "name";
        private readonly ILogger? _logger;
        private IReadOnlyDictionary<string, string> _tags = new Dictionary<string, string>();
        private Dictionary<string, BaseSymbol> _symbols = new Dictionary<string, BaseSymbol>();
        private IReadOnlyList<PostActionModel> _postActions = new List<PostActionModel>();
        private string? _author;
        private string? _name;
        private string? _description;
        private string? _sourceName;

        internal TemplateConfigModel()
        {
            Symbols = Array.Empty<BaseSymbol>();
        }

        private TemplateConfigModel(JObject source, ILogger? logger, ISimpleConfigModifiers? configModifiers = null, string? filename = null)
        {
            _logger = logger;

            //TODO: improve validation not to allow null values here
            Identity = source.ToString(nameof(Identity));
            Name = source.ToString(nameof(Name));

            Author = source.ToString(nameof(Author));
            Classifications = source.ArrayAsStrings(nameof(Classifications));
            DefaultName = source.ToString(nameof(DefaultName));
            Description = source.ToString(nameof(Description)) ?? string.Empty;
            GroupIdentity = source.ToString(nameof(GroupIdentity));
            Precedence = source.ToInt32(nameof(Precedence));
            Guids = source.ArrayAsGuids(nameof(Guids));

            SourceName = source.ToString(nameof(SourceName));
            PlaceholderFilename = source.ToString(nameof(PlaceholderFilename))!;
            GeneratorVersions = source.ToString(nameof(GeneratorVersions));
            ThirdPartyNotices = source.ToString(nameof(ThirdPartyNotices));
            PreferNameDirectory = source.ToBool(nameof(PreferNameDirectory));

            ShortNameList = source.ToStringReadOnlyList("ShortName");
            Forms = SetupValueFormMapForTemplate(source);

            var sources = new List<ExtendedFileSource>();
            Sources = sources;
            foreach (JObject item in source.Items<JObject>(nameof(Sources)))
            {
                ExtendedFileSource src = ExtendedFileSource.FromJObject(item);
                sources.Add(src);
            }

            IBaselineInfo? baseline = null;
            BaselineInfo = BaselineInfoFromJObject(source.PropertiesOf("baselines"));

            if (!string.IsNullOrEmpty(configModifiers?.BaselineName))
            {
                BaselineInfo.TryGetValue(configModifiers!.BaselineName, out baseline);
            }

            Dictionary<string, BaseSymbol> symbols = new(StringComparer.Ordinal);
            // create a name symbol. If one is explicitly defined in the template, it'll override this.
            NameSymbol = SetupDefaultNameSymbol(SourceName);
            symbols[NameSymbol.Name] = NameSymbol;

            // tags are being deprecated from template configuration, but we still read them for backwards compatibility.
            // They're turned into symbols here, which eventually become tags.
            _tags = source.ToStringDictionary(StringComparer.OrdinalIgnoreCase, "tags");
            foreach (KeyValuePair<string, string> tagInfo in _tags)
            {
                if (!symbols.ContainsKey(tagInfo.Key))
                {
                    symbols[tagInfo.Key] = ParameterSymbol.FromDeprecatedConfigTag(tagInfo.Key, tagInfo.Value);
                }
            }
            foreach (JProperty prop in source.PropertiesOf(nameof(Symbols)))
            {
                if (prop.Value is not JObject obj)
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(prop.Name))
                {
                    continue;
                }

                string? defaultOverride = null;
                if (baseline?.DefaultOverrides != null)
                {
                    baseline.DefaultOverrides.TryGetValue(prop.Name, out defaultOverride);
                }

                BaseSymbol? modelForSymbol = SymbolModelConverter.GetModelForObject(prop.Name, obj, logger, defaultOverride);

                if (modelForSymbol != null)
                {
                    // The symbols dictionary comparer is Ordinal, making symbol names case-sensitive.
                    if (string.Equals(prop.Name, NameSymbolName, StringComparison.Ordinal)
                            && symbols.TryGetValue(prop.Name, out BaseSymbol existingSymbol)
                            && existingSymbol is ParameterSymbol existingParameterSymbol
                            && modelForSymbol is ParameterSymbol modelForParameterSymbol)
                    {
                        // "name" symbol is explicitly defined above. If it's also defined in the template.json, it gets special handling here.
                        symbols[prop.Name] = new ParameterSymbol(modelForParameterSymbol, existingParameterSymbol.Forms);
                    }
                    else
                    {
                        // last in wins (in the odd case where a template.json defined a symbol multiple times)
                        symbols[prop.Name] = modelForSymbol;
                    }
                }
            }
            foreach (var symbol in ImplicitBindSymbols)
            {
                if (!symbols.ContainsKey(symbol.Name))
                {
                    symbols[symbol.Name] = symbol;
                }
            }
            _symbols = symbols;
            _postActions = PostActionModel.LoadListFromJArray(source.Get<JArray>("PostActions"), _logger, filename);
            PrimaryOutputs = PrimaryOutputModel.ListFromJArray(source.Get<JArray>(nameof(PrimaryOutputs)));

            // Custom operations at the global level
            JToken? globalCustomConfigData = source[nameof(GlobalCustomOperations)];
            if (globalCustomConfigData != null)
            {
                GlobalCustomOperations = CustomFileGlobModel.FromJObject((JObject)globalCustomConfigData, string.Empty);
            }

            // Custom operations for specials
            IReadOnlyDictionary<string, JToken> allSpecialOpsConfig = source.ToJTokenDictionary(StringComparer.OrdinalIgnoreCase, nameof(SpecialCustomOperations));
            List<CustomFileGlobModel> specialCustomSetup = new List<CustomFileGlobModel>();

            foreach (KeyValuePair<string, JToken> globConfigKeyValue in allSpecialOpsConfig)
            {
                string globName = globConfigKeyValue.Key;
                JToken globData = globConfigKeyValue.Value;

                CustomFileGlobModel globModel = CustomFileGlobModel.FromJObject((JObject)globData, globName);
                specialCustomSetup.Add(globModel);
            }

            SpecialCustomOperations = specialCustomSetup;

            List<TemplateConstraintInfo> constraints = new List<TemplateConstraintInfo>();
            foreach (JProperty prop in source.PropertiesOf(nameof(Constraints)))
            {
                if (prop.Value is not JObject obj)
                {
                    _logger?.LogWarning(LocalizableStrings.SimpleConfigModel_Error_Constraints_InvalidSyntax, nameof(Constraints).ToLowerInvariant());
                    continue;
                }

                string? type = obj.ToString(nameof(TemplateConstraintInfo.Type));
                if (string.IsNullOrWhiteSpace(type))
                {
                    _logger?.LogWarning(LocalizableStrings.SimpleConfigModel_Error_Constraints_MissingType, obj.ToString(), nameof(TemplateConstraintInfo.Type).ToLowerInvariant());
                    continue;
                }
                obj.TryGetValue(nameof(TemplateConstraintInfo.Args), StringComparison.OrdinalIgnoreCase, out JToken? args);
                constraints.Add(new TemplateConstraintInfo(type!, args.ToJSONString()));
            }
            Constraints = constraints;
        }

        /// <summary>
        /// Gets the template author ("author" JSON property).
        /// </summary>
        public string? Author
        {
            get
            {
                return _author;
            }

            internal init
            {
                _author = value;
            }
        }

        /// <summary>
        /// Gets the default name for the template ("defaultName" JSON property).
        /// </summary>
        public string? DefaultName { get; internal init; }

        /// <summary>
        /// Gets the description of the template ("description" JSON property).
        /// </summary>
        public string? Description
        {
            get
            {
                return _description;
            }

            internal init
            {
                _description = value;
            }
        }

        /// <summary>
        /// Gets the group identity of the template ("groupIdentity" JSON property).
        /// This allows multiple templates to be displayed as one, with the the decision for which one to use based on <see cref="Precedence"/> and other parameters added by user for the instantiation.
        /// </summary>
        public string? GroupIdentity { get; internal init; }

        /// <summary>
        /// Gets the precedence of the template in a group ("precedence" JSON property).
        /// </summary>
        public int Precedence { get; internal init; }

        /// <summary>
        /// Gets the template name ("name" JSON property).
        /// </summary>
        public string? Name
        {
            get
            {
                return _name;
            }

            internal init
            {
                _name = value;
            }
        }

        /// <summary>
        /// Gets the link to 3rd party notices ("thirdPartyNotices" JSON property).
        /// </summary>
        public string? ThirdPartyNotices { get; internal init; }

        /// <summary>
        /// Indicates whether to create a directory for the template if name is specified but an output directory is not set (instead of creating the content directly in the current directory) ("preferNameDirectory" JSON property).
        /// </summary>
        public bool PreferNameDirectory { get; internal init; }

        /// <summary>
        /// Gets the collection of template tags ("tags" JSON property).
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags
        {
            get
            {
                return _tags;
            }

            internal init
            {
                _tags = value;
            }
        }

        /// <summary>
        /// Gets the list of template short names ("shortName" JSON property).
        /// </summary>
        public IReadOnlyList<string> ShortNameList { get; internal init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the list of post actions defined for the template ("postActions" JSON property).
        /// </summary>
        public IReadOnlyList<PostActionModel> PostActionModels
        {
            get
            {
                return _postActions;
            }

            internal init
            {
                _postActions = value;
            }
        }

        /// <summary>
        /// Gets the list of template primary outputs ("primaryOutputs" JSON property).
        /// </summary>
        public IReadOnlyList<PrimaryOutputModel> PrimaryOutputs { get; internal init; } = Array.Empty<PrimaryOutputModel>();

        /// <summary>
        /// Gets version expression which defines which generator versions is supported by the template ("generatorVersions" JSON property).
        /// </summary>
        public string? GeneratorVersions { get; internal init; }

        /// <summary>
        /// Gets the list of baselines defined for the template ("baselines" JSON property).
        /// </summary>
        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; internal init; } = new Dictionary<string, IBaselineInfo>();

        /// <summary>
        /// Gets the template identity ("identity" JSON property) - a unique name for this template.
        /// </summary>
        public string? Identity { get; internal init; }

        /// <summary>
        /// Gets the list of classifications of the template ("classifications" JSON property).
        /// </summary>
        public IReadOnlyList<string> Classifications { get; internal init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the list of guids defined in the template ("guids" JSON property).
        /// </summary>
        public IReadOnlyList<Guid> Guids { get; internal init; } = Array.Empty<Guid>();

        /// <summary>
        /// Gets the source name defined in the template ("sourceName" JSON property).
        /// </summary>
        public string? SourceName
        {
            get
            {
                return _sourceName;
            }

            internal init
            {
                _sourceName = value;
                NameSymbol = SetupDefaultNameSymbol(value);
                _symbols[NameSymbolName] = NameSymbol;
            }
        }

        /// <summary>
        /// Gets the list of sources defined in the template ("sources" JSON property).
        /// </summary>
        public IReadOnlyList<ExtendedFileSource> Sources { get; internal init; } = Array.Empty<ExtendedFileSource>();

        /// <summary>
        /// Gets the list of constraints defined in the template ("constraints" JSON property).
        /// </summary>
        public IReadOnlyList<TemplateConstraintInfo> Constraints { get; internal init; } = Array.Empty<TemplateConstraintInfo>();

        /// <summary>
        /// Gets the list of symbols defined in the template ("symbols" JSON property).
        /// </summary>
        public IEnumerable<BaseSymbol> Symbols

        {
            get
            {
                return _symbols.Values;
            }

            internal init
            {
                _symbols = value.ToDictionary(s => s.Name, s => s);
                _symbols[NameSymbolName] = NameSymbol;
                foreach (var symbol in ImplicitBindSymbols)
                {
                    if (!_symbols.ContainsKey(symbol.Name))
                    {
                        _symbols[symbol.Name] = symbol;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of forms defined in the template ("forms" JSON property).
        /// </summary>
        public IReadOnlyDictionary<string, IValueForm> Forms { get; internal init; } = new Dictionary<string, IValueForm>();

        /// <summary>
        /// Gets the placeholder filename defined in the template ("placeholderFilename" JSON property).
        /// </summary>
        public string? PlaceholderFilename { get; internal init; }

        /// <summary>
        /// Gets the list of global custom operations defined for the template ("globalCustomOperations" JSON property).
        /// </summary>
        public CustomFileGlobModel? GlobalCustomOperations { get; internal init; }

        /// <summary>
        /// Gets the list of custom operations defined for the template for specific files ("specialCustomOperations" JSON property).
        /// </summary>
        public IReadOnlyList<CustomFileGlobModel> SpecialCustomOperations { get; internal init; } = Array.Empty<CustomFileGlobModel>();

        internal BaseSymbol NameSymbol { get; private set; } = SetupDefaultNameSymbol(null);

        private static IReadOnlyList<BindSymbol> ImplicitBindSymbols { get; } = SetupImplicitBindSymbols();

        /// <summary>
        /// Creates <see cref="TemplateConfigModel"/> from stream <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The stream containing template configuration in JSON format.</param>
        /// <param name="logger">The logger to use for reporting errors/messages.</param>
        /// <param name="filename">The filepath of template configuration (optional, used for logging).</param>
        public static TemplateConfigModel FromStream(Stream content, ILogger? logger = null, string? filename = null)
        {
            using (TextReader tr = new StreamReader(content, System.Text.Encoding.UTF8, true))
            {
                return FromTextReader(tr, logger, filename);
            }
        }

        /// <summary>
        /// Creates <see cref="TemplateConfigModel"/> from string <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The string containing template configuration in JSON format.</param>
        /// <param name="logger">The logger to use for reporting errors/messages.</param>
        /// <param name="filename">The filepath of template configuration (optional, used for logging).</param>
        public static TemplateConfigModel FromString(string content, ILogger? logger = null, string? filename = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new TemplateConfigModel();
            }
            using (TextReader tr = new StringReader(content))
            {
                return FromTextReader(tr, logger, filename);
            }
        }

        internal static TemplateConfigModel FromTextReader(TextReader content, ILogger? logger = null, string? filename = null)
        {
            using (JsonReader r = new JsonTextReader(content))
            {
                return new TemplateConfigModel(JObject.Load(r), logger, null, filename);
            }
        }

        internal static TemplateConfigModel FromJObject(JObject source, ILogger? logger = null, ISimpleConfigModifiers? configModifiers = null, string? filename = null)
        {
            return new TemplateConfigModel(source, logger, configModifiers, filename);
        }

        //TODO: create convertors to get proper json format if needed.
        internal JObject ToJObject()
        {
            return JObject.FromObject(this);
        }

        /// <summary>
        /// Localizes this <see cref="TemplateConfigModel"/> with given localization model.
        /// </summary>
        /// <param name="locModel">Localization model containing the localized strings.</param>
        /// <remarks>This method works on a best-effort basis. If the given model is invalid or incompatible,
        /// erroneous data will be skipped. No errors will be logged. Use <see cref="Localize(ILocalizationModel)"/>
        /// to validate localization models before calling this method.</remarks>
        internal void Localize(ILocalizationModel locModel)
        {
            _author = locModel.Author ?? Author;
            _name = locModel.Name ?? Name;
            _description = locModel.Description ?? Description;

            foreach (var postAction in _postActions)
            {
                if (postAction.Id != null && locModel.PostActions.TryGetValue(postAction.Id, out IPostActionLocalizationModel postActionLocModel))
                {
                    postAction.Localize(postActionLocModel);
                }
            }
        }

        private static BaseSymbol SetupDefaultNameSymbol(string? sourceName)
        {
            string? replaces = string.IsNullOrWhiteSpace(sourceName) ? null : sourceName;
            return new ParameterSymbol(NameSymbolName, replaces)
            {
                Description = "The default name symbol",
                DataType = "string",
                Forms = SymbolValueFormsModel.NameForms,
                Precedence = new TemplateParameterPrecedence(PrecedenceDefinition.Implicit)
            };
        }

        private static IReadOnlyDictionary<string, IValueForm> SetupValueFormMapForTemplate(JObject source)
        {
            Dictionary<string, IValueForm> formMap = new Dictionary<string, IValueForm>(StringComparer.Ordinal);

            // setup all the built-in default forms.
            // name of the form is form identifier
            // this is only possible for the forms that don't need configuration
            foreach (KeyValuePair<string, IValueFormFactory> builtInForm in ValueFormRegistry.FormLookup)
            {
                formMap[builtInForm.Key] = builtInForm.Value.Create();
            }

            // setup the forms defined by the template configuration.
            // if any have the same name as a default, the default is overridden.
            IReadOnlyDictionary<string, JToken> templateDefinedforms = source.ToJTokenDictionary(StringComparer.OrdinalIgnoreCase, nameof(Forms));

            foreach (KeyValuePair<string, JToken> form in templateDefinedforms)
            {
                if (form.Value is JObject o)
                {
                    formMap[form.Key] = ValueFormRegistry.GetForm(form.Key, o);
                }
            }

            return formMap;
        }

        private static IReadOnlyDictionary<string, IBaselineInfo> BaselineInfoFromJObject(IEnumerable<JProperty> baselineJProperties)
        {
            Dictionary<string, IBaselineInfo> allBaselines = new Dictionary<string, IBaselineInfo>();

            foreach (JProperty property in baselineJProperties)
            {
                JObject? obj = property.Value as JObject;

                if (obj == null)
                {
                    continue;
                }

                var defaultOverrides = obj.Get<JObject>(nameof(Utils.BaselineInfo.DefaultOverrides))?.ToStringDictionary() ?? new Dictionary<string, string>();
                BaselineInfo baseline = new BaselineInfo(defaultOverrides, obj.ToString(nameof(baseline.Description)));
                allBaselines[property.Name] = baseline;
            }

            return allBaselines;
        }

        private static IReadOnlyList<BindSymbol> SetupImplicitBindSymbols()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!isWindows)
            {
                return Array.Empty<BindSymbol>();
            }
            //on Windows we implicitly bind OS to avoid likely breaking change.
            //this environment variable is commonly used in conditions when using run script post action.
            return new[] { new BindSymbol("OS", "env:OS") };
        }
    }
}
