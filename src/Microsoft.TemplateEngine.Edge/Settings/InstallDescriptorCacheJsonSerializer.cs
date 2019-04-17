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

            if (JsonSerializerHelpers.TrySerializeDictionary(descriptorCache.Descriptors, JsonSerializerHelpers.GuidKeyConverter, InstallUnitDescriptorToJObjectConverter, out JObject descriptorObject))
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

            descriptorObject[nameof(IInstallUnitDescriptor.FactoryId)] = descriptor.FactoryId;
            descriptorObject[nameof(IInstallUnitDescriptor.Identifier)] = descriptor.Identifier;
            descriptorObject[nameof(IInstallUnitDescriptor.MountPointId)] = descriptor.MountPointId;

            if (JsonSerializerHelpers.TrySerializeStringDictionary(descriptor.Details, out JObject detailsObject))
            {
                descriptorObject.Add(nameof(IInstallUnitDescriptor.Details), detailsObject);
            }

            return descriptorObject;
        };
    }
}
