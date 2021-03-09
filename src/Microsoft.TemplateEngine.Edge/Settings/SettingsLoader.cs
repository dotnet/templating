using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Edge.Mount.FileSystem;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class SettingsLoader : ISettingsLoader
    {
        private const int MaxLoadAttempts = 20;
        public static readonly string HostTemplateFileConfigBaseName = ".host.json";

        private SettingsStore _userSettings;
        private TemplateCache _userTemplateCache;
        private IMountPointManager _mountPointManager;
        private IComponentManager _componentManager;
        private bool _isLoaded;
        private bool _templatesLoaded;
        private readonly Paths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private TemplatesSourcesManager _templatesSourcesManager;

        public SettingsLoader(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
            _userTemplateCache = new TemplateCache(environmentSettings);
            _templatesSourcesManager = new TemplatesSourcesManager(environmentSettings);
        }

        public SettingsLoader(IEngineEnvironmentSettings environmentSettings, IMountPointManager mockMountPointManager)
        {
        }

        public void Save()
        {
            JObject serialized = JObject.FromObject(_userSettings);
            _paths.WriteAllText(_paths.User.SettingsFile, serialized.ToString());
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            using (Timing.Over(_environmentSettings.Host, "Parse and deserialize global settings"))
                try
                {
                    GlobalSettings = new GlobalSettings(EnvironmentSettings, _paths.User.GlobalSettingsFile);
                }
                catch (Exception ex)
                {
                    throw new EngineInitializationException("Error parsing the user settings file", "Settings File", ex);
                }

            string userSettings = null;
            using (Timing.Over(_environmentSettings.Host, "Read settings"))
                for (int i = 0; i < MaxLoadAttempts; ++i)
                {
                    try
                    {
                        userSettings = _paths.ReadAllText(_paths.User.SettingsFile, "{}");
                        break;
                    }
                    catch (IOException)
                    {
                        if (i == MaxLoadAttempts - 1)
                        {
                            throw;
                        }

                        Task.Delay(2).Wait();
                    }
                }
            JObject parsed;
            using (Timing.Over(_environmentSettings.Host, "Parse settings"))
                try
                {
                    parsed = JObject.Parse(userSettings);
                }
                catch (Exception ex)
                {
                    throw new EngineInitializationException("Error parsing the user settings file", "Settings File", ex);
                }
            using (Timing.Over(_environmentSettings.Host, "Deserialize user settings"))
                _userSettings = new SettingsStore(parsed);

            using (Timing.Over(_environmentSettings.Host, "Init probing paths"))
                if (_userSettings.ProbingPaths.Count == 0)
                {
                    _userSettings.ProbingPaths.Add(_paths.User.Content);
                }

            using (Timing.Over(_environmentSettings.Host, "Init Component manager"))
                _componentManager = new ComponentManager(this, _userSettings);

            using (Timing.Over(_environmentSettings.Host, "Init MountPoint manager"))
                _mountPointManager = new MountPointManager(_environmentSettings, _componentManager);

            using (Timing.Over(_environmentSettings.Host, "Demand template load"))
                EnsureTemplatesLoaded();

            _isLoaded = true;
        }

        // Loads from the template cache
        private void EnsureTemplatesLoaded()
        {
            if (_templatesLoaded)
            {
                return;
            }

            string userTemplateCache;

            if (_paths.Exists(_paths.User.TemplateCacheFile))
            {
                using (Timing.Over(_environmentSettings.Host, "Read template cache"))
                    userTemplateCache = _paths.ReadAllText(_paths.User.TemplateCacheFile, "{}");
            }
            else
            {
                userTemplateCache = "{}";
            }

            JObject parsed;
            using (Timing.Over(_environmentSettings.Host, "Parse template cache"))
                parsed = JObject.Parse(userTemplateCache);
            using (Timing.Over(_environmentSettings.Host, "Init template cache"))
                _userTemplateCache = new TemplateCache(_environmentSettings, parsed);

            _templatesLoaded = true;
        }

        public async Task RebuildCacheFromSettingsIfNotCurrent(bool forceRebuild)
        {
            EnsureLoaded();
            forceRebuild |= _userTemplateCache.Locale != _environmentSettings.Host.Locale;

            var placesThatNeedScanning = new HashSet<string>();
            var allTemplatesSources = await _templatesSourcesManager.GetTemplatesSources(forceRebuild).ConfigureAwait(false);

            if (forceRebuild)
            {
                _userTemplateCache = new TemplateCache(_environmentSettings);
                foreach (var source in allTemplatesSources)
                {
                    placesThatNeedScanning.Add(source.MountPointUri);
                }
            }
            else
            {
                foreach (var source in allTemplatesSources)
                {
                    if (_userTemplateCache.MountPointsInfo.TryGetValue(source.MountPointUri, out var cachedLastChangeTime))
                    {
                        if (source.LastChangeTime > cachedLastChangeTime)
                            placesThatNeedScanning.Add(source.MountPointUri);
                    }
                    else
                    {
                        placesThatNeedScanning.Add(source.MountPointUri);
                    }
                }
            }

            foreach (var place in placesThatNeedScanning)
            {
                try
                {
                    _userTemplateCache.Scan(place);
                }
                catch (Exception ex)
                {
                    _environmentSettings.Host.OnNonCriticalError(null, $"Failed to scan \"{place}\":{Environment.NewLine}{ex}", null, 0);
                }
            }

            // When writing the template caches, we need the existing cache version to read the existing caches for before updating.
            // so don't update it until after the template caches are written.
            _userTemplateCache.WriteTemplateCaches(allTemplatesSources.ToDictionary(x => x.MountPointUri, x => x.LastChangeTime));
        }

        public ITemplate LoadTemplate(ITemplateInfo info, string baselineName)
        {
            IGenerator generator;
            if (!Components.TryGetComponent(info.GeneratorId, out generator))
            {
                return null;
            }

            IMountPoint mountPoint;
            if (!_mountPointManager.TryDemandMountPoint(info.MountPointUri, out mountPoint))
            {
                return null;
            }
            IFileSystemInfo config = mountPoint.FileSystemInfo(info.ConfigPlace);

            IFileSystemInfo localeConfig = null;
            if (!string.IsNullOrEmpty(info.LocaleConfigPlace)
                    && !string.IsNullOrEmpty(info.MountPointUri))
            {
                IMountPoint localeMountPoint;
                if (!_mountPointManager.TryDemandMountPoint(info.MountPointUri, out localeMountPoint))
                {
                    // TODO: decide if we should proceed without loc info, instead of bailing.
                    return null;
                }

                localeConfig = localeMountPoint.FileSystemInfo(info.LocaleConfigPlace);
            }

            IFile hostTemplateConfigFile = FindBestHostTemplateConfigFile(config);

            ITemplate template;
            using (Timing.Over(_environmentSettings.Host, "Template from config"))
                if (generator.TryGetTemplateFromConfigInfo(config, out template, localeConfig, hostTemplateConfigFile, baselineName))
                {
                    return template;
                }
                else
                {
                    //TODO: Log the failure to read the template info
                }

            return null;
        }

        public IFile FindBestHostTemplateConfigFile(IFileSystemInfo config)
        {
            IDictionary<string, IFile> allHostFilesForTemplate = new Dictionary<string, IFile>();

            foreach (IFile hostFile in config.Parent.EnumerateFiles($"*{HostTemplateFileConfigBaseName}", SearchOption.TopDirectoryOnly))
            {
                allHostFilesForTemplate.Add(hostFile.Name, hostFile);
            }

            string preferredHostFileName = string.Concat(_environmentSettings.Host.HostIdentifier, HostTemplateFileConfigBaseName);
            if (allHostFilesForTemplate.TryGetValue(preferredHostFileName, out IFile preferredHostFile))
            {
                return preferredHostFile;
            }

            foreach (string fallbackHostName in _environmentSettings.Host.FallbackHostTemplateConfigNames)
            {
                string fallbackHostFileName = string.Concat(fallbackHostName, HostTemplateFileConfigBaseName);

                if (allHostFilesForTemplate.TryGetValue(fallbackHostFileName, out IFile fallbackHostFile))
                {
                    return fallbackHostFile;
                }
            }

            return null;
        }

        public IComponentManager Components
        {
            get
            {
                EnsureLoaded();
                return _componentManager;
            }
        }

        public IEngineEnvironmentSettings EnvironmentSettings => _environmentSettings;

        public IGlobalSettings GlobalSettings { get; private set; }

        public ITemplatesSourcesManager TemplatesSourcesManager => _templatesSourcesManager;

        public async Task<IReadOnlyList<ITemplateInfo>> GetTemplatesAsync(CancellationToken token)
        {
            EnsureLoaded();
            await RebuildCacheFromSettingsIfNotCurrent(false).ConfigureAwait(false);
            return _userTemplateCache.TemplateInfo;
        }

        public void AddProbingPath(string probeIn)
        {
            const int maxAttempts = 10;
            int attemptCount = 0;
            bool successfulWrite = false;

            EnsureLoaded();
            while (!successfulWrite && attemptCount++ < maxAttempts)
            {
                if (!_userSettings.ProbingPaths.Add(probeIn))
                {
                    return;
                }

                try
                {
                    Save();
                    successfulWrite = true;
                }
                catch
                {
                    Task.Delay(10).Wait();
                }
            }
        }

        public bool TryGetFileFromIdAndPath(string mountPointUri, string place, out IFile file, out IMountPoint mountPoint)
        {
            if (!string.IsNullOrEmpty(place) && _mountPointManager.TryDemandMountPoint(mountPointUri, out mountPoint))
            {
                file = mountPoint.FileInfo(place);
                return file != null && file.Exists;
            }

            mountPoint = null;
            file = null;
            return false;
        }

        public bool TryGetMountPoint(string mountPointUri, out IMountPoint mountPoint)
        {
            return _mountPointManager.TryDemandMountPoint(mountPointUri, out mountPoint);
        }
    }
}
