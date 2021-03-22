// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;

#nullable enable

namespace Microsoft.TemplateEngine.Utils
{
    public class DefaultTemplatesPackageProvider : ITemplatesPackagesProvider
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly IEnumerable<string> _nupkgs;
        private readonly IEnumerable<string> _folders;

        public ITemplatesPackagesProviderFactory Factory { get; }

        public DefaultTemplatesPackageProvider(ITemplatesPackagesProviderFactory factory, IEngineEnvironmentSettings environmentSettings, IEnumerable<string>? nupkgs = null, IEnumerable<string>? folders = null)
        {
            Factory = factory;
            _environmentSettings = environmentSettings;
            _nupkgs = nupkgs ?? Array.Empty<string>();
            _folders = folders ?? Array.Empty<string>();
        }

        public event Action? SourcesChanged;

        public void TriggerSourcesChangedEvent()
        {
            SourcesChanged?.Invoke();
        }

        public Task<IReadOnlyList<ITemplatesPackage>> GetAllSourcesAsync(CancellationToken cancellationToken)
        {
            var expandedNupkgs = _nupkgs.SelectMany(p => InstallRequestPathResolution.Expand(p, _environmentSettings));
            var expandedFolders = _folders.SelectMany(p => InstallRequestPathResolution.Expand(p, _environmentSettings));

            var list = new List<ITemplatesPackage>();
            foreach (var nupkg in expandedNupkgs)
            {
                list.Add(new TemplatesPackage(this, nupkg, GetLastWriteTimeUtc(nupkg)));
            }
            foreach (var folder in expandedFolders)
            {
                list.Add(new TemplatesPackage(this, folder, GetLastWriteTimeUtc(folder)));
            }
            return Task.FromResult<IReadOnlyList<ITemplatesPackage>>(list);
        }

        private DateTime GetLastWriteTimeUtc(string path)
        {
            if (_environmentSettings.Host.FileSystem is IFileLastWriteTimeSource fileSystem)
                return fileSystem.GetLastWriteTimeUtc(path);
            return File.GetLastWriteTimeUtc(path);
        }
    }
}
