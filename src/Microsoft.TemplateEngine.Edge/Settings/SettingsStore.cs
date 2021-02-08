using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class SettingsStore
    {
        // NOTE: when the current version changes, a corresponding change in TemplateInfo.cs is needed, to get the correct template cache version reader to fire.
        public static readonly string CurrentVersion = "1.0.0.3";

        public SettingsStore()
        {
            ComponentGuidToAssemblyQualifiedName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentTypeToGuidList = new Dictionary<string, HashSet<Guid>>();
            ProbingPaths = new HashSet<string>();
            Version = string.Empty;
        }

        public SettingsStore(JObject obj)
            : this()
        {
            JToken versionToken;
            if (obj.TryGetValue(nameof(Version), StringComparison.OrdinalIgnoreCase, out versionToken))
            {
                Version = versionToken.ToString();
            }

            JToken componentGuidToAssemblyQualifiedNameToken;
            if (obj.TryGetValue("ComponentGuidToAssemblyQualifiedName", StringComparison.OrdinalIgnoreCase, out componentGuidToAssemblyQualifiedNameToken))
            {
                JObject componentGuidToAssemblyQualifiedNameObject = componentGuidToAssemblyQualifiedNameToken as JObject;
                if (componentGuidToAssemblyQualifiedNameObject != null)
                {
                    foreach (JProperty entry in componentGuidToAssemblyQualifiedNameObject.Properties())
                    {
                        if (entry.Value != null && entry.Value.Type == JTokenType.String)
                        {
                            ComponentGuidToAssemblyQualifiedName[entry.Name] = entry.Value.ToString();
                        }
                    }
                }
            }

            JToken probingPathsToken;
            if (obj.TryGetValue("ProbingPaths", StringComparison.OrdinalIgnoreCase, out probingPathsToken))
            {
                JArray probingPathsArray = probingPathsToken as JArray;
                if (probingPathsArray != null)
                {
                    foreach (JToken path in probingPathsArray)
                    {
                        if (path != null && path.Type == JTokenType.String)
                        {
                            ProbingPaths.Add(path.ToString());
                        }
                    }
                }
            }

            JToken componentTypeToGuidListToken;
            if (obj.TryGetValue("ComponentTypeToGuidList", StringComparison.OrdinalIgnoreCase, out componentTypeToGuidListToken))
            {
                JObject componentTypeToGuidListObject = componentTypeToGuidListToken as JObject;
                if (componentTypeToGuidListObject != null)
                {
                    foreach (JProperty entry in componentTypeToGuidListObject.Properties())
                    {
                        JArray values = entry.Value as JArray;

                        if (values != null)
                        {
                            HashSet<Guid> set = new HashSet<Guid>();
                            ComponentTypeToGuidList[entry.Name] = set;

                            foreach (JToken value in values)
                            {
                                if (value != null && value.Type == JTokenType.String)
                                {
                                    Guid id;
                                    if (Guid.TryParse(value.ToString(), out id))
                                    {
                                        set.Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetVersionToCurrent()
        {
            Version = CurrentVersion;
        }

        [JsonProperty]
        public string Version { get; private set; }

        [JsonProperty]
        public Dictionary<string, string> ComponentGuidToAssemblyQualifiedName { get; }

        [JsonProperty]
        public HashSet<string> ProbingPaths { get; }

        [JsonProperty]
        public Dictionary<string, HashSet<Guid>> ComponentTypeToGuidList { get; }
    }
}
