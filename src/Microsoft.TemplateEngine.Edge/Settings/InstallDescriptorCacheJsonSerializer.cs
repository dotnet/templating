using System;
using Microsoft.TemplateEngine.Abstractions.TemplateUpdates;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class InstallDescriptorCacheJsonSerializer
    {
        public InstallDescriptorCacheJsonSerializer()
        {
        }

        public bool TrySerialize(InstallUnitDescriptorCache descriptorCache, out string serialized)
        {
            JObject cacheObject = new JObject();

            if (JsonSerializerHelpers.TrySerializeDictionary(descriptorCache.InstalledItems, JsonSerializerHelpers.GuidKeyConverter, JsonSerializerHelpers.StringValueConverter, out JObject installedItemsObject))
            {
                cacheObject.Add(nameof(descriptorCache.InstalledItems), installedItemsObject);
            }
            else
            {
                serialized = null;
                return false;
            }

            if (JsonSerializerHelpers.TrySerializeDictionary(descriptorCache.Descriptors, JsonSerializerHelpers.StringKeyConverter, InstallUnitDescriptorToJObjectConverter, out JObject descriptorObject))
            {
                cacheObject.Add(nameof(descriptorCache.Descriptors), descriptorObject);
            }
            else
            {
                serialized = null;
                return false;
            }

            serialized = cacheObject.ToString();
            return true;
        }

        private static Func<IInstallUnitDescriptor, JObject> InstallUnitDescriptorToJObjectConverter = descriptor =>
        {
            JObject descriptorObject = new JObject();

            descriptorObject.Add(nameof(IInstallUnitDescriptor.FactoryId), descriptor.FactoryId);

            if (JsonSerializerHelpers.TrySerializeStringDictionary(descriptor.Details, out JObject detailsObject))
            {
                descriptorObject.Add(nameof(IInstallUnitDescriptor.Details), detailsObject);
            }
            else
            {
                descriptorObject = null;
            }

            return descriptorObject;
        };
    }
}
