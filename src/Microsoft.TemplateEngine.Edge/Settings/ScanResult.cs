using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class ScanResult
    {
        public ScanResult()
        {
            _localizations = new List<ILocalizationLocator>();
            _templates = new List<ITemplate>();
            _installedMountPointIds = new HashSet<string>();
        }

        private List<ILocalizationLocator> _localizations;
        private List<ITemplate> _templates;
        private HashSet<string> _installedMountPointIds;

        public void AddLocalization(ILocalizationLocator locater)
        {
            _localizations.Add(locater);
        }

        public void AddTemplate(ITemplate template)
        {
            _templates.Add(template);
        }

        public void AddInstalledMountPointId(string mountPointUri)
        {
            _installedMountPointIds.Add(mountPointUri);
        }

        public IReadOnlyList<ILocalizationLocator> Localizations => _localizations;

        public IReadOnlyList<ITemplate> Templates => _templates;

        public IReadOnlyList<string> InstalledMountPointUris => _installedMountPointIds.ToList();
    }
}
