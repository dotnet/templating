using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public abstract class BaseValueSymbol : ISymbolModel
    {
        public string Binding { get; set; }

        public string DefaultValue { get; set; }

        public string Description { get; set; }

        public SymbolValueFormsModel Forms { get; set; }

        public bool IsRequired { get; set; }

        public string Type { get; protected set; }

        public string Replaces { get; set; }

        public string DataType { get; set; }

        public string FileRename { get; set; }

        public IReadOnlyList<IReplacementContext> ReplacementContexts { get; set; }

        protected static T FromJObject<T>(JObject jObject, IParameterSymbolLocalizationModel localization, string defaultOverride)
            where T: BaseValueSymbol, new()
        {
            T symbol = new T
            {
                Binding = jObject.ToString(nameof(Binding)),
                DefaultValue = defaultOverride ?? jObject.ToString(nameof(DefaultValue)),
                Description = localization?.Description ?? jObject.ToString(nameof(Description)) ?? string.Empty,
                FileRename = jObject.ToString(nameof(FileRename)),
                IsRequired = jObject.ToBool(nameof(IsRequired)),
                Type = jObject.ToString(nameof(Type)),
                Replaces = jObject.ToString(nameof(Replaces)),
                DataType = jObject.ToString(nameof(DataType)),
                ReplacementContexts = SymbolModelConverter.ReadReplacementContexts(jObject)
            };

            if (!jObject.TryGetValue(nameof(symbol.Forms), StringComparison.OrdinalIgnoreCase, out JToken formsToken) || !(formsToken is JObject formsObject))
            {
                // no value forms explicitly defined, use the default ("identity")
                symbol.Forms = SymbolValueFormsModel.Default;
            }
            else
            {
                // the config defines forms for the symbol. Use them.
                symbol.Forms = SymbolValueFormsModel.FromJObject(formsObject);
            }

            return symbol;
        }
    }
}
