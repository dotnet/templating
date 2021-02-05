using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
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
        public FolderManagedTemplatesSource(IEngineEnvironmentSettings settings, IInstaller installer, string mountPointUri)
        {
            Identifier = mountPointUri;
            Installer = installer;
            LastChangeTime = (settings.Host.FileSystem as IFileLastWriteTimeSource)?.GetLastWriteTimeUtc(mountPointUri) ?? File.GetLastWriteTime(mountPointUri);
        }

        public string Identifier { get; }

        public string Version => null;

        public IReadOnlyDictionary<string, string> Details => null;

        public IReadOnlyList<string> DetailKeysDisplayOrder => null;

        public DateTime LastChangeTime { get; }

        public string MountPointUri => Identifier;

        public ITemplatesSourcesProvider Provider => Installer.Provider;

        public IInstaller Installer { get; }
    }
}
