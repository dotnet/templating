using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class DerivedSymbol : BaseValueSymbol
    {
        public const string TypeName = "derived";

        public string ValueTransform { get; set; }

        public string ValueSource { get; set; }

        public static ISymbolModel FromJObject(JObject jObject, IParameterSymbolLocalizationModel localization, string defaultOverride)
        {
            DerivedSymbol symbol = new DerivedSymbol();
            FromJObject(symbol, jObject, localization, defaultOverride);

            symbol.ValueTransform = jObject.ToString(nameof(ValueTransform));
            symbol.ValueSource = jObject.ToString(nameof(ValueSource));

            return symbol;
        }
    }
}
