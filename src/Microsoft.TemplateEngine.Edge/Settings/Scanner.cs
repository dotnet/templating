// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !NETFULL
using System.Runtime.Loader;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Edge.Mount.FileSystem;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    /// <summary>
    /// Utility for scanning <see cref="IMountPoint"/> for templates, localizations and components.
    /// </summary>
    public class Scanner
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly SettingsFilePaths _paths;
        private readonly ILogger _logger;

        public Scanner(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new SettingsFilePaths(environmentSettings);
            _logger = environmentSettings.Host.LoggerFactory.CreateLogger<Scanner>();
        }

        /// <summary>
        /// Scans mount point for templates, localizations and components.
        /// </summary>
        /// <remarks>
        /// The mount point will not be disposed by the <see cref="Scanner"/>. Use <see cref="ScanResult.Dispose"/> to dispose mount point.
        /// </remarks>
        [Obsolete("Use ScanAsync instead.")]
        public ScanResult Scan(string mountPointUri)
        {
            return Scan(mountPointUri, scanForComponents: true);
        }

        /// <summary>
        /// Same as <see cref="Scan(string)"/>, however allows to enable or disable components scanning via <paramref name="scanForComponents"/>.
        /// </summary>
        /// <remarks>
        /// The mount point will not be disposed by the <see cref="Scanner"/>. Use <see cref="ScanResult.Dispose"/> to dispose mount point.
        /// </remarks>
        ///
        [Obsolete("Use ScanAsync instead.")]
        public ScanResult Scan(string mountPointUri, bool scanForComponents)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} should not be null or empty");
            }
            MountPointScanSource source = GetOrCreateMountPointScanInfoForInstallSource(mountPointUri);

            if (scanForComponents)
            {
                ScanForComponents(source);
            }
            return Task.Run(async () => await ScanMountPointForTemplatesAsync(source, default).ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Scans mount point for templates.
        /// </summary>
        /// <remarks>
        /// The mount point will not be disposed by the <see cref="Scanner"/>. Use <see cref="ScanResult.Dispose"/> to dispose mount point.
        /// </remarks>
        public Task<ScanResult> ScanAsync(string mountPointUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} should not be null or empty");
            }
            MountPointScanSource source = GetOrCreateMountPointScanInfoForInstallSource(mountPointUri);
            cancellationToken.ThrowIfCancellationRequested();
            return ScanMountPointForTemplatesAsync(source, cancellationToken);
        }

        private MountPointScanSource GetOrCreateMountPointScanInfoForInstallSource(string sourceLocation)
        {
            foreach (IMountPointFactory factory in _environmentSettings.Components.OfType<IMountPointFactory>().ToList())
            {
                if (factory.TryMount(_environmentSettings, null, sourceLocation, out IMountPoint? mountPoint))
                {
                    if (mountPoint is null)
                    {
                        throw new InvalidOperationException($"{nameof(mountPoint)} cannot be null when {nameof(factory.TryMount)} is 'true'");
                    }

                    // file-based and not originating in the scratch dir.
                    bool isLocalFlatFileSource = mountPoint is FileSystemMountPoint
                                                && !sourceLocation.StartsWith(_paths.ScratchDir);

                    return new MountPointScanSource(
                        location: sourceLocation,
                        mountPoint: mountPoint,
                        shouldStayInOriginalLocation: isLocalFlatFileSource,
                        foundComponents: false,
                        foundTemplates: false);
                }
            }
            throw new Exception(string.Format(LocalizableStrings.Scanner_Error_TemplatePackageLocationIsNotSupported, sourceLocation));
        }

        private void ScanForComponents(MountPointScanSource source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            bool isCopiedIntoContentDirectory;

            if (!source.MountPoint.Root.EnumerateFiles("*.dll", SearchOption.AllDirectories).Any())
            {
                return;
            }

            string? actualScanPath;
            if (!source.ShouldStayInOriginalLocation)
            {
                if (!TryCopyForNonFileSystemBasedMountPoints(source.MountPoint, source.Location, _paths.Content, true, out actualScanPath) || actualScanPath == null)
                {
                    return;
                }

                isCopiedIntoContentDirectory = true;
            }
            else
            {
                actualScanPath = source.Location;
                isCopiedIntoContentDirectory = false;
            }

            foreach (KeyValuePair<string, Assembly> asm in LoadAllFromPath(out _, actualScanPath))
            {
                try
                {
                    IReadOnlyList<Type> typeList = asm.Value.GetTypes();

                    if (typeList.Count > 0)
                    {
                        // TODO: figure out what to do with probing path registration when components are not found.
                        // They need to be registered for dependent assemblies, not just when an assembly can be loaded.
                        // We'll need to figure out how to know when that is.
#pragma warning disable CS0618 // Type or member is obsolete
                        _environmentSettings.Components.RegisterMany(typeList);
#pragma warning restore CS0618 // Type or member is obsolete
                        source.FoundComponents = true;
                    }
                }
                catch
                {
                    // exceptions here are ok, due to dependency errors, etc.
                }
            }

            if (!source.FoundComponents && isCopiedIntoContentDirectory)
            {
                try
                {
                    // The source was copied to content and then scanned for components.
                    // Nothing was found, and this is a copy that now has no use, so delete it.
                    // Note: no mount point was created for this copy, so no need to release it.
                    _environmentSettings.Host.FileSystem.DirectoryDelete(actualScanPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"During ScanForComponents() cleanup, couldn't delete source copied into the content dir: {actualScanPath}. Details: {ex}.");
                }
            }
        }

        private bool TryCopyForNonFileSystemBasedMountPoints(IMountPoint mountPoint, string sourceLocation, string targetBasePath, bool expandIfArchive, out string? diskPath)
        {
            string targetPath = Path.Combine(targetBasePath, Path.GetFileName(sourceLocation));

            try
            {
                if (expandIfArchive)
                {
                    mountPoint.Root.CopyTo(targetPath);
                }
                else
                {
                    _environmentSettings.Host.FileSystem.CreateDirectory(targetBasePath); // creates Packages/ or Content/ if needed
                    _paths.Copy(sourceLocation, targetPath);
                }
            }
            catch (IOException)
            {
                _logger.LogDebug($"Error copying scanLocation: {sourceLocation} into the target dir: {targetPath}");
                diskPath = null;
                return false;
            }

            diskPath = targetPath;
            return true;
        }

        private async Task<ScanResult> ScanMountPointForTemplatesAsync(MountPointScanSource source, CancellationToken cancellationToken)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            var templates = new List<IScanTemplateInfo>();
            foreach (IGenerator generator in _environmentSettings.Components.OfType<IGenerator>())
            {
                IReadOnlyList<IScanTemplateInfo> templateList = await generator.GetTemplatesFromMountPointAsync(source.MountPoint, cancellationToken).ConfigureAwait(false);
                LogScanningResults(source, templateList, generator);

                IEnumerable<IScanTemplateInfo> validTemplates = templateList.Where(t => t.IsValid);
                templates.AddRange(validTemplates);
                source.FoundTemplates |= validTemplates.Any();
            }

            //backward compatibility
            var localizationLocators = templates.SelectMany(t => t.Localizations.Values.Where(li => li.IsValid)).ToList();
            return new ScanResult(source.MountPoint, templates, localizationLocators, Array.Empty<(string, Type, IIdentifiedComponent)>());
        }

        /// <summary>
        /// Loads assemblies for components from the given <paramref name="path"/>.
        /// </summary>
        /// <param name="loadFailures">Errors happened when loading assemblies.</param>
        /// <param name="path">The path to load assemblies from.</param>
        /// <param name="pattern">Filename pattern to use when searching for files.</param>
        /// <param name="searchOption"><see cref="SearchOption"/> to use when searching for files.</param>
        /// <returns>The list of loaded assemblies in format (filename, loaded assembly).</returns>
        private IEnumerable<KeyValuePair<string, Assembly>> LoadAllFromPath(
            out IEnumerable<string> loadFailures,
            string path,
            string pattern = "*.dll",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            List<KeyValuePair<string, Assembly>> loaded = new List<KeyValuePair<string, Assembly>>();
            List<string> failures = new List<string>();

            foreach (string file in _paths.EnumerateFiles(path, pattern, searchOption))
            {
                try
                {
                    Assembly? assembly = null;

#if !NETFULL
                    if (file.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) > -1 || file.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        using (Stream fileStream = _environmentSettings.Host.FileSystem.OpenRead(file))
                        {
                            assembly = AssemblyLoadContext.Default.LoadFromStream(fileStream);
                        }
                    }
#else
                    if (file.IndexOf("net4", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        byte[] fileBytes = _environmentSettings.Host.FileSystem.ReadAllBytes(file);
                        assembly = Assembly.Load(fileBytes);
                    }
#endif

                    if (assembly != null)
                    {
                        loaded.Add(new KeyValuePair<string, Assembly>(file, assembly));
                    }
                }
                catch
                {
                    failures.Add(file);
                }
            }

            loadFailures = failures;
            return loaded;
        }

        private void LogScanningResults(MountPointScanSource source, IReadOnlyList<IScanTemplateInfo> foundTemplates, IGenerator generator)
        {
            ILogger logger = _environmentSettings.Host.Logger;
            logger.LogDebug("Scanning mount point '{0}' by generator '{1}': found {2} templates", source.MountPoint.MountPointUri, generator.Id, foundTemplates.Count);
            foreach (IScanTemplateInfo template in foundTemplates)
            {
                string templateDisplayName = GetTemplateDisplayName(template);
                logger.LogDebug("Found template {0}", templateDisplayName);

                LogValidationEntries(
                    logger,
                    string.Format(LocalizableStrings.Scanner_Validation_Error_Header, templateDisplayName),
                    template.ValidationErrors,
                    IValidationEntry.SeverityLevel.Error);
                LogValidationEntries(
                    logger,
                    string.Format(LocalizableStrings.Scanner_Validation_Warning_Header, templateDisplayName),
                    template.ValidationErrors,
                    IValidationEntry.SeverityLevel.Warning);
                LogValidationEntries(
                    logger,
                    string.Format(LocalizableStrings.Scanner_Validation_Info_Header, templateDisplayName),
                    template.ValidationErrors,
                    IValidationEntry.SeverityLevel.Info);

                foreach (KeyValuePair<string, ILocalizationLocator> locator in template.Localizations)
                {
                    ILocalizationLocator localizationInfo = locator.Value;

                    LogValidationEntries(
                        logger,
                        string.Format(LocalizableStrings.Scanner_Validation_LocError_Header, templateDisplayName, localizationInfo.Locale),
                        localizationInfo.ValidationErrors,
                        IValidationEntry.SeverityLevel.Error);
                    LogValidationEntries(
                        logger,
                        string.Format(LocalizableStrings.Scanner_Validation_LocWarning_Header, templateDisplayName, localizationInfo.Locale),
                        localizationInfo.ValidationErrors,
                        IValidationEntry.SeverityLevel.Warning);
                    LogValidationEntries(
                        logger,
                        string.Format(LocalizableStrings.Scanner_Validation_LocInfo_Header, templateDisplayName, localizationInfo.Locale),
                        localizationInfo.ValidationErrors,
                        IValidationEntry.SeverityLevel.Info);
                }

                if (!template.IsValid)
                {
                    logger.LogError(LocalizableStrings.Scanner_Validation_InvalidTemplate, templateDisplayName);
                }
                foreach (ILocalizationLocator invalidLoc in template.Localizations.Values.Where(li => !li.IsValid))
                {
                    logger.LogWarning(LocalizableStrings.Scanner_Validation_InvalidTemplateLoc, invalidLoc.Locale, templateDisplayName);
                }
            }

            static string GetTemplateDisplayName(IScanTemplateInfo template)
            {
                string templateName = string.IsNullOrEmpty(template.Name) ? "<no name>" : template.Name;
                return $"'{templateName}' ({template.Identity})";
            }

            static string PrintError(IValidationEntry error) => $"   [{error.Severity}][{error.Code}] {error.ErrorMessage}";

            static void LogValidationEntries(ILogger logger, string header, IReadOnlyList<IValidationEntry> errors, IValidationEntry.SeverityLevel severity)
            {
                Action<string> log = severity switch
                {
                    IValidationEntry.SeverityLevel.None => (string s) => throw new NotSupportedException($"{IValidationEntry.SeverityLevel.None} severity is not supported."),
                    IValidationEntry.SeverityLevel.Info => (string s) => logger.LogDebug(s),
                    IValidationEntry.SeverityLevel.Warning => (string s) => logger.LogWarning(s),
                    IValidationEntry.SeverityLevel.Error => (string s) => logger.LogError(s),
                    _ => throw new InvalidOperationException($"{severity} is not expected value for {nameof(IValidationEntry.SeverityLevel)}."),
                };

                if (!errors.Any(e => e.Severity == severity))
                {
                    return;
                }

                StringBuilder sb = new();
                sb.AppendLine(header);
                foreach (IValidationEntry error in errors.Where(e => e.Severity == severity))
                {
                    sb.AppendLine(PrintError(error));
                }
                log(sb.ToString());
            }
        }

        private class MountPointScanSource
        {
            public MountPointScanSource(string location, IMountPoint mountPoint, bool shouldStayInOriginalLocation, bool foundComponents, bool foundTemplates)
            {
                Location = location;
                MountPoint = mountPoint;
                ShouldStayInOriginalLocation = shouldStayInOriginalLocation;
                FoundComponents = foundComponents;
                FoundTemplates = foundTemplates;
            }

            public string Location { get; }

            public IMountPoint MountPoint { get; }

            public bool ShouldStayInOriginalLocation { get; }

            public bool FoundComponents { get; set; }

            public bool FoundTemplates { get; set; }

            public bool AnythingFound => FoundTemplates || FoundComponents;
        }
    }
}
