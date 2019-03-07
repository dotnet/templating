using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class AliasJsonSerializer
    {
        public AliasJsonSerializer()
        {
        }

        public bool TrySerialize(AliasModel aliasModel, out string serialized)
        {
            JObject commandAliasesObject = new JObject();

            foreach (KeyValuePair<string, IReadOnlyList<string>> alias in aliasModel.CommandAliases)
            {
                if (JsonSerializerHelpers.TrySerializeIEnumerable(alias.Value, JsonSerializerHelpers.StringValueConverter, out JArray serializedAlias))
                {
                    commandAliasesObject.Add(alias.Key, serializedAlias);
                }
                else
                {
                    serialized = null;
                    return false;
                }
            }

            JObject serializedObject = new JObject();
            serializedObject.Add(nameof(aliasModel.CommandAliases), commandAliasesObject);

            serialized = serializedObject.ToString();
            return true;
        }
    }
}
