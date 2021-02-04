using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Installers.Folder
{
    class FolderManagedTemplatesSource : IManagedTemplatesSource
    {
        public FolderManagedTemplatesSource(IEngineEnvironmentSettings settings, IManagedTemplatesSourcesProvider provider, string mountPointUri, DateTime? lastChangeTime)
        {
            Identifier = mountPointUri;
            ManagedProvider = provider;
            LastChangeTime = lastChangeTime ?? (settings.Host.FileSystem as IFileLastWriteTimeSource)?.GetLastWriteTimeUtc(mountPointUri) ?? File.GetLastWriteTime(mountPointUri);
        }

        public string Identifier { get; }

        public string Version => null;

        public IReadOnlyDictionary<string, string> Details => null;

        public IReadOnlyList<string> DetailKeysDisplayOrder => null;

        public IManagedTemplatesSourcesProvider ManagedProvider { get; }

        public DateTime LastChangeTime { get; }

        public string MountPointUri => Identifier;

        public ITemplatesSourcesProvider Provider => ManagedProvider;
    }
}
