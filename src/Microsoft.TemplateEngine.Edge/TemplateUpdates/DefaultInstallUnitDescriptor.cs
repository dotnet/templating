using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.TemplateUpdates;

namespace Microsoft.TemplateEngine.Edge.TemplateUpdates
{
    public class DefaultInstallUnitDescriptor : IInstallUnitDescriptor
    {
        public DefaultInstallUnitDescriptor(Guid descriptorId, Guid mountPointId, string identifier)
        {
            DescriptorId = descriptorId;
            MountPointId = mountPointId;
            Identifier = identifier;
            Details = new Dictionary<string, string>();
        }

        public Guid DescriptorId { get; }

        public string Identifier { get; }

        public Guid FactoryId => DefaultInstallUnitDescriptorFactory.FactoryId;

        public Guid MountPointId { get; }

        public IReadOnlyDictionary<string, string> Details { get; }

        public string UserReadableIdentifier => Identifier;

        public string UninstallString => Identifier;
    }
}
