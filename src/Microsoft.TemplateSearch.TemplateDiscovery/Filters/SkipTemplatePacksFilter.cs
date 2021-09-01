// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Filters
{
    internal sealed class SkipTemplatePacksFilter
    {
        private const string _FilterId = "Permanently skipped packages";

        private static readonly List<string> PackagesToBeSkipped = new List<string>
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

        internal static Func<IDownloadedPackInfo, PreFilterResult> SetupPackFilter()
        {
            Func<IDownloadedPackInfo, PreFilterResult> filter = (packInfo) =>
            {
                foreach (string package in PackagesToBeSkipped)
                {
                    if (packInfo.Name.StartsWith(package, StringComparison.OrdinalIgnoreCase))
                    {
                        return new PreFilterResult(_FilterId, isFiltered: true, $"Package {packInfo.Name} is skipped as it matches the package name to be permanently skipped.");
                    }
                }
                return new PreFilterResult(_FilterId, isFiltered: false);
            };

            return filter;
        }
    }
}
