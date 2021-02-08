using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
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
        private Dictionary<Guid, MountPointInfo> _mountPoints;
        private bool _templatesLoaded;
        private InstallUnitDescriptorCache _installUnitDescriptorCache;
        private bool _installUnitDescriptorsLoaded;
        private readonly Paths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;

        public SettingsLoader(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
            _userTemplateCache = new TemplateCache(environmentSettings);
            _installUnitDescriptorCache = new InstallUnitDescriptorCache(environmentSettings);
        }

        internal SettingsLoader(IEngineEnvironmentSettings environmentSettings, IMountPointManager mountPointManager) : this(environmentSettings)
        {
            _mountPointManager = mountPointManager;
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

            WriteInstallDescriptorCache();

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

        // It's important to note that these are loaded on demand, not at initialization of SettingsLoader.
        // So the backing field shouldn't be directly accessed except during initialization.
        public InstallUnitDescriptorCache InstallUnitDescriptorCache
        {
            get
            {
                EnsureLoaded();
                EnsureInstallDescriptorsLoaded();

                return _installUnitDescriptorCache;
            }
        }

        private void EnsureInstallDescriptorsLoaded()
        {
            if (_installUnitDescriptorsLoaded)
            {
                return;
            }

            string descriptorFileContents = _paths.ReadAllText(_paths.User.InstallUnitDescriptorsFile, "{}");
            JObject parsed = JObject.Parse(descriptorFileContents);

            _installUnitDescriptorCache = InstallUnitDescriptorCache.FromJObject(_environmentSettings, parsed);
            _installUnitDescriptorsLoaded = true;
        }

        // Write the install unit descriptors.
        // Get them from the property to ensure they're loaded. Descriptors are loaded on demand, not at startup.
        private void WriteInstallDescriptorCache()
        {
            JObject installDescriptorsSerialized = JObject.FromObject(InstallUnitDescriptorCache);
            _paths.WriteAllText(_paths.User.InstallUnitDescriptorsFile, installDescriptorsSerialized.ToString());
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
                    GlobalSettings = JsonConvert.DeserializeObject<GlobalSettings>(globalSettings);
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

            _mountPoints = new Dictionary<Guid, MountPointInfo>();
            using (Timing.Over(_environmentSettings.Host, "Load mount points"))
                foreach (MountPointInfo info in _userSettings.MountPoints)
                {
                    _mountPoints[info.MountPointId] = info;
                }

            using (Timing.Over(_environmentSettings.Host, "Init Component manager"))
                _componentManager = new ComponentManager(this, _userSettings);

            if (_mountPointManager == null)
            {
                using (Timing.Over(_environmentSettings.Host, "Init Mount Point manager"))
                    _mountPointManager = new MountPointManager(_environmentSettings, _componentManager);
            }

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
            var removedMounts = new Dictionary<string, MountPointInfo>();
            var mountsPoints = new Dictionary<string, MountPointInfo>();

            var tasks = Components.OfType<ITemplatesInstallSourcesProviderFactory>().Select(p => Task.Run(() =>
            {
                var provider = p.CreateProvider(EnvironmentSettings);
                return provider.GetInstalledPackagesAsync(default);
            })).ToList();

            foreach (var mountPoint in mountsPoints.Values)
            {
                var key = mountPoint.MountPointFactoryId + mountPoint.Place;
                mountsPoints.Add(key, mountPoint);
                removedMounts.Add(key, mountPoint);
            }

            while (tasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                foreach (var source in completedTask.Result)
                {
                    var key = source.MountPointFactoryId + source.Place;
                    removedMounts.Remove(key);
                    if (mountsPoints.TryGetValue(key, out var mountPoint))
                    {
                        if (source.LastWriteTime > mountPoint.LastScanTime)
                            placesThatNeedScanning.Add(source.Place);
                    }
                    else
                    {
                        placesThatNeedScanning.Add(source.Place);
                    }
                }
            }

            foreach (var removedMount in removedMounts.Values)
            {
                _mountPoints.Remove(removedMount.MountPointId);
            }

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
            if (!_mountPointManager.TryDemandMountPoint(info.ConfigMountPointId, out mountPoint))
            {
                return null;
            }
            IFileSystemInfo config = mountPoint.FileSystemInfo(info.ConfigPlace);

            IFileSystemInfo localeConfig = null;
            if (!string.IsNullOrEmpty(info.LocaleConfigPlace)
                    && info.LocaleConfigMountPointId != null
                    && info.LocaleConfigMountPointId != Guid.Empty)
            {
                IMountPoint localeMountPoint;
                if (!_mountPointManager.TryDemandMountPoint(info.LocaleConfigMountPointId, out localeMountPoint))
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

        public IEnumerable<MountPointInfo> MountPoints
        {
            get
            {
                EnsureLoaded();
                return _mountPoints.Values;
            }
        }

        public IEngineEnvironmentSettings EnvironmentSettings => _environmentSettings;

        public IGlobalSettings GlobalSettings { get; private set; }

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
            bool hasMountPointChanges = false;

            for (int i = 0; i < toCache.Count; ++i)
            {
                if (!_mountPoints.ContainsKey(toCache[i].ConfigMountPointId))
                {
                    toCache.RemoveAt(i);
                    --i;
                    hasMountPointChanges = true;
                    continue;
                }

                if (!_mountPoints.ContainsKey(toCache[i].HostConfigMountPointId))
                {
                    toCache[i].HostConfigMountPointId = Guid.Empty;
                    toCache[i].HostConfigPlace = null;
                    hasMountPointChanges = true;
                }

                if (!_mountPoints.ContainsKey(toCache[i].LocaleConfigMountPointId))
                {
                    toCache[i].LocaleConfigMountPointId = Guid.Empty;
                    toCache[i].LocaleConfigPlace = null;
                    hasMountPointChanges = true;
                }
            }

            if (hasContentChanges || hasMountPointChanges)
            {
                TemplateCache cache = new TemplateCache(_environmentSettings, toCache);
                JObject serialized = JObject.FromObject(cache);
                _paths.WriteAllText(_paths.User.ExplicitLocaleTemplateCacheFile(locale), serialized.ToString());
            }

            bool isCurrentLocale = string.IsNullOrEmpty(locale)
                && string.IsNullOrEmpty(_environmentSettings.Host.Locale)
                || (locale == _environmentSettings.Host.Locale);

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

        public bool TryGetMountPointInfo(Guid mountPointId, out MountPointInfo info)
        {
            EnsureLoaded();
            using (Timing.Over(_environmentSettings.Host, "Mount point lookup"))
                return _mountPoints.TryGetValue(mountPointId, out info);
        }

        public bool TryGetMountPointInfoFromPlace(string mountPointPlace, out MountPointInfo info)
        {
            EnsureLoaded();
            using (Timing.Over(_environmentSettings.Host, "Mount point place lookup"))
                foreach (MountPointInfo mountInfoToCheck in _mountPoints.Values)
                {
                    if (mountPointPlace.Equals(mountInfoToCheck.Place, StringComparison.OrdinalIgnoreCase))
                    {
                        info = mountInfoToCheck;
                        return true;
                    }
                }

            info = null;
            return false;
        }

        public bool TryGetMountPointFromPlace(string mountPointPlace, out IMountPoint mountPoint)
        {
            if (!TryGetMountPointInfoFromPlace(mountPointPlace, out MountPointInfo info))
            {
                mountPoint = null;
                return false;
            }

            return _mountPointManager.TryDemandMountPoint(info.MountPointId, out mountPoint);
        }

        public void AddMountPoint(IMountPoint mountPoint)
        {
            if (_mountPoints.Values.Any(x => string.Equals(x.Place, mountPoint.Info.Place) && x.ParentMountPointId == mountPoint.Info.ParentMountPointId))
            {
                return;
            }

            _mountPoints[mountPoint.Info.MountPointId] = mountPoint.Info;
            _userSettings.MountPoints.Add(mountPoint.Info);
            JObject serialized = JObject.FromObject(_userSettings);
            _paths.WriteAllText(_paths.User.SettingsFile, serialized.ToString());
        }

        public bool TryGetFileFromIdAndPath(Guid mountPointId, string place, out IFile file, out IMountPoint mountPoint)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(place) && _mountPointManager.TryDemandMountPoint(mountPointId, out mountPoint))
            {
                file = mountPoint.FileInfo(place);
                return file != null && file.Exists;
            }

            mountPoint = null;
            file = null;
            return false;
        }

        public bool TryGetMountPointFromId(Guid mountPointId, out IMountPoint mountPoint)
        {
            return _mountPointManager.TryDemandMountPoint(mountPointId, out mountPoint);
        }

        public void RemoveMountPoints(IEnumerable<Guid> mountPoints)
        {
            foreach (Guid g in mountPoints)
            {
                if (_mountPoints.TryGetValue(g, out MountPointInfo info))
                {
                    _userSettings.MountPoints.Remove(info);
                    _mountPoints.Remove(g);
                }
            }
        }

        public void ReleaseMountPoint(IMountPoint mountPoint)
        {
            _mountPointManager.ReleaseMountPoint(mountPoint);
        }

        public void RemoveMountPoint(IMountPoint mountPoint)
        {
            _mountPointManager.ReleaseMountPoint(mountPoint);
            RemoveMountPoints(new[] { mountPoint.Info.MountPointId });
        }
    }
}
