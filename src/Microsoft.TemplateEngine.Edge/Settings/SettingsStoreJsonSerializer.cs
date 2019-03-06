using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class SettingsStoreJsonSerializer
    {
        public SettingsStoreJsonSerializer()
        {
        }

        public bool TrySerialize(SettingsStore settingsStore, out string serialized)
        {
            JObject storeObject = new JObject();

            storeObject.Add(nameof(SettingsStore.Version), settingsStore.Version);

            if (JsonSerializerHelpers.TrySerializeIEnumerable(settingsStore.MountPoints, SerializeMountPoint, out JArray mountPointListObject)
                && JsonSerializerHelpers.TrySerializeStringDictionary(settingsStore.ComponentGuidToAssemblyQualifiedName, out JObject componentGuidToAssemblyQualifiedNameObject)
                && JsonSerializerHelpers.TrySerializeIEnumerable(settingsStore.ProbingPaths, JsonSerializerHelpers.StringValueConverter, out JArray probingPathsObject)
                && TrySerializeComponentTypeToGuidList(settingsStore.ComponentTypeToGuidList, out JObject componentTypeToGuidListObject))
            {
                storeObject.Add(nameof(SettingsStore.MountPoints), mountPointListObject);
                storeObject.Add(nameof(SettingsStore.ComponentGuidToAssemblyQualifiedName), componentGuidToAssemblyQualifiedNameObject);
                storeObject.Add(nameof(SettingsStore.ProbingPaths), probingPathsObject);
                storeObject.Add(nameof(SettingsStore.ComponentTypeToGuidList), componentTypeToGuidListObject);

                serialized = storeObject.ToString();
                return true;
            }

            serialized = null;
            return false;
        }

        private static Func<MountPointInfo, JObject> SerializeMountPoint = mountPointInfo =>
        {
            JObject mountPointObject = new JObject();

            mountPointObject.Add(nameof(MountPointInfo.ParentMountPointId), mountPointInfo.ParentMountPointId);
            mountPointObject.Add(nameof(MountPointInfo.MountPointFactoryId), mountPointInfo.MountPointFactoryId);
            mountPointObject.Add(nameof(MountPointInfo.MountPointId), mountPointInfo.MountPointId);
            mountPointObject.Add(nameof(MountPointInfo.Place), mountPointInfo.Place);

            return mountPointObject;
        };

        private static bool TrySerializeComponentTypeToGuidList(IReadOnlyDictionary<string, HashSet<Guid>> componentTypeToGuidList, out JObject componentTypeToGuidListObject)
        {
            try
            {
                componentTypeToGuidListObject = new JObject();

                foreach (KeyValuePair<string, HashSet<Guid>> componentTypeEntry in componentTypeToGuidList)
                {
                    if (JsonSerializerHelpers.TrySerializeIEnumerable(componentTypeEntry.Value, JsonSerializerHelpers.GuidValueConverter, out JArray componentTypeEntryObject))
                    {
                        componentTypeToGuidListObject.Add(componentTypeEntry.Key, componentTypeEntryObject);
                    }
                    else
                    {
                        componentTypeToGuidListObject = null;
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                componentTypeToGuidListObject = null;
                return false;
            }
        }
    }
}
