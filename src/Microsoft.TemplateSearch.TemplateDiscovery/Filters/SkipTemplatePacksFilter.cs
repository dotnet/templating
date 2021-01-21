using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking.Reporting;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;
using System;
using System.Collections.Generic;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Filters
{
    internal sealed class SkipTemplatePacksFilter
    {
        private static readonly List<string> packagesToBeSkipped = new List<string>
        {
            "microsoft.dotnet.common.itemtemplates",
            "microsoft.dotnet.common.projecttemplates",
            "microsoft.dotnet.test.projecttemplates",
            "microsoft.dotnet.web.itemtemplates",
            "microsoft.dotnet.web.projecttemplates",
            "microsoft.dotnet.web.spa.projecttemplates",
            "microsoft.dotnet.winforms.projecttemplates",
            "microsoft.dotnet.wpf.projecttemplates",
            //NUnit package is included to SDK, however not managed by Microsoft - keep it in to check for updates
            //"nunit3.dotnetnew.template",
            "microsoft.aspnetcore.components.webassembly.template"
        };
        private static readonly string _FilterId = "Permanent pack blacklist";

        public static Func<IInstalledPackInfo, PreFilterResult> SetupPackFilter()
        {
            Func<IInstalledPackInfo, PreFilterResult> filter = (packInfo) =>
            {
                foreach (string package in packagesToBeSkipped)
                {
                    if (packInfo.Id.StartsWith(package, StringComparison.OrdinalIgnoreCase))
                    {
                        return new PreFilterResult()
                        {
                            FilterId = _FilterId,
                            IsFiltered = true,
                            Reason = $"Package {packInfo.Id} is skipped as it matches the package name to be permanently skipped."
                        };
                    }
                }
                return new PreFilterResult()
                {
                    FilterId = _FilterId,
                    IsFiltered = false
                };
            };

            return filter;
        }
    }
}
