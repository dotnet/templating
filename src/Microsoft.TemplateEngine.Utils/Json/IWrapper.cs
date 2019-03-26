using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal interface IWrapper<T, TConcrete>
        where TConcrete : T
    {
        T Value { get; set; }

        TConcrete Deserialize(IJsonToken o);

        IJsonToken Serialize(IJsonDocumentObjectModelFactory f, T o);
    }
}
