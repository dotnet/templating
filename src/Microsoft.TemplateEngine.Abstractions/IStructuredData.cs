using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IStructuredData
    {
        int Count { get; }

        bool IsArrayData { get; }

        bool IsObjectData { get; }

        bool IsPrimitive { get; }

        IReadOnlyList<string> Keys { get; }

        object Value { get; }

        bool TryGetNamedValue(string name, out IStructuredData value);

        bool TryGetValueByIndex(int index, out IStructuredData value);
    }
}
