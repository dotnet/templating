using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.TemplateUpdates;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Edge.TemplateUpdates
{
    public class NupkgInstallUnitDescriptor : IInstallUnitDescriptor
    {
        public NupkgInstallUnitDescriptor(Guid descriptorId, Guid mountPointId, string identifier, string version)
        {
            DescriptorId = descriptorId;
            MountPointId = mountPointId;
            Identifier = identifier;
            Version = version;
        }

        [JsonProperty]
        public Guid DescriptorId { get; }

        [JsonIgnore]
        public string Identifier { get; }

        [JsonProperty]
        public Guid FactoryId => NupkgInstallUnitDescriptorFactory.FactoryId;

        [JsonProperty]
        public Guid MountPointId { get; }

        [JsonIgnore]
        public string Version { get; }

        [JsonProperty]
        public IReadOnlyDictionary<string, string> Details
        {
            get
            {
                Dictionary<string, string> detailsInfo = new Dictionary<string, string>()
                {
                    { nameof(Version), Version }
                };

                return detailsInfo;
            }
        }

        [JsonIgnore]
        public string UserReadableIdentifier => string.Join(".", Identifier, Version);

        [JsonIgnore]
        public string UninstallString => string.Join("::", Identifier, Version);
    }
}
