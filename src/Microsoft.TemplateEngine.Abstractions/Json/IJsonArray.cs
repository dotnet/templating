using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonArray : IReadOnlyList<IJsonToken>, IJsonToken
    {
        IJsonArray RemoveAt(int index);

        IJsonArray Add(IJsonToken value);
    }
}
