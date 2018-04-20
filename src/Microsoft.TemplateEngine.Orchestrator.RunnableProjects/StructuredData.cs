using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    [JsonConverter(typeof(StructuredDataJsonConverter))]
    public class StructuredData : IStructuredData
    {
        private readonly IReadOnlyList<IStructuredData> _arrayData;
        private readonly IReadOnlyDictionary<string, IStructuredData> _objectData;

        public StructuredData(IReadOnlyDictionary<string, IStructuredData> objectData)
        {
            _objectData = objectData;
            Keys = new List<string>(objectData.Keys);
            IsObjectData = true;
        }

        public StructuredData(IReadOnlyList<IStructuredData> arrayData)
        {
            Keys = new string[0];
            _arrayData = arrayData;
            IsArrayData = true;
        }

        public StructuredData(object value)
        {
            Keys = new string[0];
            Value = value;
            IsPrimitive = true;
        }

        public int Count => IsArrayData ? _arrayData.Count : 0;

        public bool IsArrayData { get; }

        public bool IsObjectData { get; }

        public bool IsPrimitive { get; }

        public IReadOnlyList<string> Keys { get; }

        public object Value { get; }

        public bool TryGetNamedValue(string name, out IStructuredData value)
        {
            if (!IsObjectData)
            {
                value = null;
                return false;
            }

            return _objectData.TryGetValue(name, out value);
        }

        public bool TryGetValueByIndex(int index, out IStructuredData value)
        {
            if (!IsArrayData || index < 0 || index >= _arrayData.Count)
            {
                value = null;
                return false;
            }

            value = _arrayData[index];
            return true;
        }
    }
}
