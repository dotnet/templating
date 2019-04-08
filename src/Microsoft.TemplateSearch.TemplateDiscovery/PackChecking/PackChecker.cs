using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking.Reporting;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackChecking
{
    public class PackChecker
    {
        private static readonly string HostIdentifierBase = "dotnetcli-discovery-";

        public PackChecker()
        {
        }

        public PackCheckResult TryGetTemplatesInPack(IPackInfo packInfo, IReadOnlyList<IAdditionalDataProducer> additionalDataProducers, HashSet<string> alreadySeenTemplateIdentities, bool persistHive = false)
        {
            ITemplateEngineHost host = CreateHost(packInfo);
            EngineEnvironmentSettings environment = new EngineEnvironmentSettings(host, x => new SettingsLoader(x));
            PackCheckResult checkResult;

            try
            {
                if (TryInstallPackage(packInfo.Path, environment, out IReadOnlyList<ITemplateInfo> installedTemplates))
                {
                    IReadOnlyList<ITemplateInfo> filteredInstalledTemplates = installedTemplates.Where(t => !alreadySeenTemplateIdentities.Contains(t.Identity)).ToList();
                    checkResult = new PackCheckResult(packInfo, filteredInstalledTemplates);
                    ProduceAdditionalDataForPack(additionalDataProducers, checkResult, environment);
                }
                else
                {
                    IReadOnlyList<ITemplateInfo> foundTemplates = new List<ITemplateInfo>();
                    checkResult = new PackCheckResult(packInfo, foundTemplates);
                }
            }
            catch // (Exception ex)
            {
                // TODO: abstract the logging away from the console.
                //Console.WriteLine($"Error attempting to install template pack {packInfo.Id}.");
                //Console.WriteLine(ex.Message);
                IReadOnlyList<ITemplateInfo> foundTemplates = new List<ITemplateInfo>();
                checkResult = new PackCheckResult(packInfo, foundTemplates);
            }

            if (!persistHive)
            {
                TryCleanup(environment);
            }

            return checkResult;
        }

        private void ProduceAdditionalDataForPack(IReadOnlyList<IAdditionalDataProducer> additionalDataProducers, PackCheckResult packCheckResult, EngineEnvironmentSettings environment)
        {
            if (!packCheckResult.AnyTemplates)
            {
                return;
            }

            foreach (IAdditionalDataProducer dataProducer in additionalDataProducers)
            {
                dataProducer.CreateDataForTemplatePack(packCheckResult.PackInfo, packCheckResult.FoundTemplates, environment);
            }
        }

        private bool TryInstallPackage(string packageFile, EngineEnvironmentSettings environment, out IReadOnlyList<ITemplateInfo> installedTemplates)
        {
            ((SettingsLoader)(environment.SettingsLoader)).UserTemplateCache.Scan(packageFile);
            environment.SettingsLoader.Save();

            if (((SettingsLoader)environment.SettingsLoader).UserTemplateCache.TemplateInfo.Count > 0)
            {
                installedTemplates = ((SettingsLoader)environment.SettingsLoader).UserTemplateCache.TemplateInfo;
            }
            else
            {
                installedTemplates = new List<ITemplateInfo>();
            }

            return installedTemplates.Count > 0;
        }

        private static ITemplateEngineHost CreateHost(IPackInfo packInfo)
        {
            string hostIdentifier = HostIdentifierBase + packInfo.Id;

            ITemplateEngineHost host = TemplateEngineHostHelper.CreateHost(hostIdentifier);

            return host;
        }

        private void TryCleanup(EngineEnvironmentSettings environment)
        {
            Paths paths = new Paths(environment);

            try
            {
                paths.Delete(paths.User.BaseDir);
            }
            catch // (Exception ex)
            {
                //Console.WriteLine($"Error deleting BaseDir = {paths.User.BaseDir} under the temporary hive. Error: {ex.Message}");
                //foreach (ITemplateInfo template in installedTemplates)
                //{
                //    Console.WriteLine($"\ttemplate id = {template.Identity}");
                //}
                //Console.WriteLine();
            }

            // remove the temporary hive
            string hiveDir = Directory.GetParent(paths.User.BaseDir).FullName;
            try
            {
                paths.Delete(hiveDir);
            }
            catch // (Exception ex)
            {
                //Console.WriteLine($"Error deleting temporary hive {hiveDir}. Error: {ex.Message}");
                //foreach (ITemplateInfo template in installedTemplates)
                //{
                //    Console.WriteLine($"\ttemplate id = {template.Identity}");
                //}
                //Console.WriteLine();
            }
        }
    }
}
