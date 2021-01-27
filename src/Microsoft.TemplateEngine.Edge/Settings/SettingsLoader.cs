using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            Save(_userTemplateCache);
        }

        private void Save(TemplateCache cacheToSave)
        {
            _paths.WriteAllText(_paths.User.GlobalSettingsFile, JsonConvert.SerializeObject(GlobalSettings));

            // When writing the template caches, we need the existing cache version to read the existing caches for before updating.
            // so don't update it until after the template caches are written.
            cacheToSave.WriteTemplateCaches(_userSettings.Version);

            // now it's safe to update the cache version, which is written in the settings file.
            _userSettings.SetVersionToCurrent();
            JObject serialized = JObject.FromObject(_userSettings);
            _paths.WriteAllText(_paths.User.SettingsFile, serialized.ToString());

            if (_userTemplateCache != cacheToSave)  // object equals
            {
                ReloadTemplates();
            }
        }

        public TemplateCache UserTemplateCache
        {
            get
            {
                EnsureLoaded();
                return _userTemplateCache;
            }
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            string globalSettings = null;
            using (Timing.Over(_environmentSettings.Host, "Read global settings"))
                for (int i = 0; i < MaxLoadAttempts; ++i)
                {
                    try
                    {
                        globalSettings = _paths.ReadAllText(_paths.User.GlobalSettingsFile, "{}");
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
            using (Timing.Over(_environmentSettings.Host, "Parse and deserialize global settings"))
                try
                {
                    GlobalSettings = JsonConvert.DeserializeObject<GlobalSettings>(globalSettings) ?? new Settings.GlobalSettings();
                    GlobalSettings.SettingsChanged += OnGlobalSettingsChanged;
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

        private void OnGlobalSettingsChanged()
        {
            _paths.WriteAllText(_paths.User.GlobalSettingsFile, JsonConvert.SerializeObject(GlobalSettings));
        }

        // Loads from the template cache
        private void EnsureTemplatesLoaded()
        {
            if (_templatesLoaded)
            {
                return;
            }

            string userTemplateCache;

            if (_paths.Exists(_paths.User.CurrentLocaleTemplateCacheFile))
            {
                using (Timing.Over(_environmentSettings.Host, "Read template cache"))
                    userTemplateCache = _paths.ReadAllText(_paths.User.CurrentLocaleTemplateCacheFile, "{}");
            }
            else if (_paths.Exists(_paths.User.CultureNeutralTemplateCacheFile))
            {
                // clone the culture neutral cache
                // this should not occur if there are any langpacks installed for this culture.
                // when they got installed, the cache should have been created for that locale.
                using (Timing.Over(_environmentSettings.Host, "Clone cultural neutral cache"))
                {
                    userTemplateCache = _paths.ReadAllText(_paths.User.CultureNeutralTemplateCacheFile, "{}");
                    _paths.WriteAllText(_paths.User.CurrentLocaleTemplateCacheFile, userTemplateCache);
                }
            }
            else
            {
                userTemplateCache = "{}";
            }

            JObject parsed;
            using (Timing.Over(_environmentSettings.Host, "Parse template cache"))
                parsed = JObject.Parse(userTemplateCache);
            using (Timing.Over(_environmentSettings.Host, "Init template cache"))
                _userTemplateCache = new TemplateCache(_environmentSettings, parsed, _userSettings.Version);

            _templatesLoaded = true;
        }

        public void Reload()
        {
            _isLoaded = false;
            EnsureLoaded();
            ReloadTemplates();
        }

        private void UpdateTemplateListFromCache(TemplateCache cache, ISet<ITemplateInfo> templates)
        {
            using (Timing.Over(_environmentSettings.Host, "Enumerate infos"))
                templates.UnionWith(cache.TemplateInfo);
        }

        public async Task RebuildCacheFromSettingsIfNotCurrent(bool forceRebuild)
        {
            EnsureLoaded();

            var placesThatNeedScanning = new HashSet<string>();
            var mountsPoints = new Dictionary<string, DateTime?>();

            foreach (var template in _userTemplateCache.TemplateInfo)
            {
                if (mountsPoints.TryGetValue(template.MountPointUri, out var existingTime))
                    if (template.ConfigTimestampUtc < existingTime)
                        continue;//existing time in dictionary is newer, hence don't replace it
                mountsPoints[template.MountPointUri] = template.ConfigTimestampUtc;
            }

            var completedTask = await _templatesSourcesManager.GetTemplatesSources(forceRebuild).ConfigureAwait(false);
            var mountsPointsInUsedNow = new HashSet<string>();
            foreach (var source in completedTask)
            {
                mountsPointsInUsedNow.Add(source.MountPointUri);
                if (mountsPoints.TryGetValue(source.MountPointUri, out var lastChangeTime))
                {
                    if (source.LastChangeTime > lastChangeTime)
                        placesThatNeedScanning.Add(source.MountPointUri);
                }
                else
                {
                    placesThatNeedScanning.Add(source.MountPointUri);
                }
            }

            var removedMountPoints = mountsPoints.Keys.Except(mountsPointsInUsedNow);

            TemplateCache workingCache = new TemplateCache(_environmentSettings);
            foreach (var place in placesThatNeedScanning)
            {
                workingCache.Scan(place);
            }

            Save(workingCache);
        }

        private void ReloadTemplates()
        {
            _templatesLoaded = false;
            EnsureTemplatesLoaded();
        }

        public bool IsVersionCurrent
        {
            get
            {
                if (string.IsNullOrEmpty(_userSettings.Version) || !string.Equals(_userSettings.Version, SettingsStore.CurrentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
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

        public void GetTemplates(HashSet<ITemplateInfo> templates)
        {
            using (Timing.Over(_environmentSettings.Host, "Settings init"))
                EnsureLoaded();
            using (Timing.Over(_environmentSettings.Host, "Template load"))
                UpdateTemplateListFromCache(_userTemplateCache, templates);
        }

        public void WriteTemplateCache(IList<ITemplateInfo> templates, string locale)
        {
            WriteTemplateCache(templates, locale, true);
        }

        public void WriteTemplateCache(IList<ITemplateInfo> templates, string locale, bool hasContentChanges)
        {
            List<TemplateInfo> toCache = templates.Cast<TemplateInfo>().ToList();

            if (hasContentChanges)
            {
                TemplateCache cache = new TemplateCache(_environmentSettings, toCache);
                JObject serialized = JObject.FromObject(cache);
                _paths.WriteAllText(_paths.User.ExplicitLocaleTemplateCacheFile(locale), serialized.ToString());
            }

            CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
            bool isCurrentLocale = string.IsNullOrEmpty(locale) && currentUICulture == CultureInfo.InvariantCulture
                || locale == currentUICulture.Name;

            // TODO: determine if this reload is necessary if there wasn't a save (probably not needed)
            if (isCurrentLocale)
            {
                ReloadTemplates();
            }
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
                    Reload();
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
