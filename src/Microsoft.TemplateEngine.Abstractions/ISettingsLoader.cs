using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ISettingsLoader
    {
        IComponentManager Components { get; }

        IEngineEnvironmentSettings EnvironmentSettings { get; }

        IGlobalSettings GlobalSettings { get; }

        ITemplatesSourcesManager TemplatesSourcesManager { get; }

        void AddProbingPath(string probeIn);

        ITemplate LoadTemplate(ITemplateInfo info, string baselineName);

        Task<IReadOnlyList<ITemplateInfo>> GetTemplatesAsync(CancellationToken token);

        void Save();

        bool TryGetFileFromIdAndPath(string mountPointUri, string filePathInsideMount, out IFile file, out IMountPoint mountPoint);

        bool TryGetMountPoint(string mountPointUri, out IMountPoint mountPoint);

        IFile FindBestHostTemplateConfigFile(IFileSystemInfo config);

        Task RebuildCacheFromSettingsIfNotCurrent(bool forceRebuild);
    }
}
