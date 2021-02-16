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
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

#nullable enable

namespace Microsoft.TemplateEngine.Utils
{
    public class DefaultTemplatesSourceProvider : ITemplatesSourcesProvider
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly IEnumerable<string> _nupkgs;
        private readonly IEnumerable<string> _folders;

        public ITemplatesSourcesProviderFactory Factory { get; }

        public DefaultTemplatesSourceProvider(ITemplatesSourcesProviderFactory factory, IEngineEnvironmentSettings environmentSettings, IEnumerable<string>? nupkgs = null, IEnumerable<string>? folders = null)
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

        public Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
        {
            var expandedNupkgs = _nupkgs.SelectMany(p => InstallRequestPathResolution.Expand(p, _environmentSettings));
            var expandedFolders = _folders.SelectMany(p => InstallRequestPathResolution.Expand(p, _environmentSettings));

            var list = new List<ITemplatesSource>();
            foreach (var nupkg in expandedNupkgs)
            {
                list.Add(new TemplatesSource(this, nupkg, GetLastWriteTimeUtc(nupkg)));
            }
            foreach (var nupkg in expandedFolders)
            {
                list.Add(new TemplatesSource(this, nupkg, GetLastWriteTimeUtc(nupkg)));
            }
            return Task.FromResult<IReadOnlyList<ITemplatesSource>>(list);
        }

        private DateTime GetLastWriteTimeUtc(string path)
        {
            if (_environmentSettings.Host.FileSystem is IFileLastWriteTimeSource fileSystem)
                return fileSystem.GetLastWriteTimeUtc(path);
            return File.GetLastWriteTimeUtc(path);
        }
    }
}
