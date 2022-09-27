// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    /// <summary>
    /// This class represent the template discovered during scanning. It is not ready to be run.
    /// Use <see cref="RunnableProjectConfig"/> to load template for running.
    /// </summary>
    internal class ScannedTemplateInfo : DirectoryBasedTemplate
    {
        internal const string HostTemplateFileConfigBaseName = ".host.json";
        internal const string LocalizationFilePrefix = "templatestrings.";
        internal const string LocalizationFileExtension = ".json";

        private readonly IFile? _hostConfigFile;

        /// <summary>
        /// Creates instance of the class based on configuration from <paramref name="templateFile"/>.
        /// </summary>
        public ScannedTemplateInfo(IEngineEnvironmentSettings settings, IGenerator generator, IFile templateFile) : base(settings, generator, templateFile)
        {
            _hostConfigFile = FindBestHostTemplateConfigFile(settings, templateFile);
            if (_hostConfigFile != null)
            {
                Logger.LogDebug($"Found *{HostTemplateFileConfigBaseName} at {_hostConfigFile.GetDisplayPath()}.");
            }

            IDirectory? localizeFolder = templateFile.Parent!.DirectoryInfo("localize");
            if (localizeFolder != null && localizeFolder.Exists)
            {
                Dictionary<string, ILocalizationLocator> localizations = new();
                foreach (IFile locFile in localizeFolder.EnumerateFiles(LocalizationFilePrefix + "*" + LocalizationFileExtension, SearchOption.AllDirectories))
                {
                    string locale = locFile.Name.Substring(LocalizationFilePrefix.Length, locFile.Name.Length - LocalizationFilePrefix.Length - LocalizationFileExtension.Length);

                    try
                    {
                        ILocalizationModel locModel = LocalizationModelDeserializer.Deserialize(locFile);
                        if (VerifyLocalizationModel(locModel, locFile))
                        {
                            localizations[locale] = new LocalizationLocator(
                                locale,
                                locFile.FullPath,
                                TemplateIdentity,
                                locModel.Author ?? string.Empty,
                                locModel.Name ?? string.Empty,
                                locModel.Description ?? string.Empty,
                                locModel.ParameterSymbols);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(LocalizableStrings.LocalizationModelDeserializer_Error_FailedToParse, locFile.GetDisplayPath());
                        Logger.LogDebug("Details: {0}", ex);
                    }
                }
                Localizations = localizations;
            }
        }

        #region Overriden ITemplate members

        public override IReadOnlyDictionary<string, ILocalizationLocator>? Localizations { get; }

        [Obsolete]
        public override IFileSystemInfo? LocaleConfiguration => throw new NotSupportedException($"{nameof(LocaleConfiguration)} is not supported during template scanning.");

        public override string? LocaleConfigPlace => throw new NotSupportedException($"{nameof(LocaleConfigPlace)} is not supported during template scanning.");

        public override string? HostConfigPlace => _hostConfigFile?.FullPath;

        #endregion

        /// <summary>
        /// Attempts to find the host configuration file based on current template configuration <paramref name="config"/>.
        /// </summary>
        /// <returns>Host configuration <see cref="IFile"/> or <see langword="null"/> is the host file was not found.</returns>
        private static IFile? FindBestHostTemplateConfigFile(IEngineEnvironmentSettings settings, IFile config)
        {
            IDictionary<string, IFile> allHostFilesForTemplate = new Dictionary<string, IFile>();

            if (config.Parent is null)
            {
                return null;
            }

            foreach (IFile hostFile in config.Parent.EnumerateFiles($"*{HostTemplateFileConfigBaseName}", SearchOption.TopDirectoryOnly))
            {
                allHostFilesForTemplate.Add(hostFile.Name, hostFile);
            }

            string preferredHostFileName = string.Concat(settings.Host.HostIdentifier, HostTemplateFileConfigBaseName);
            if (allHostFilesForTemplate.TryGetValue(preferredHostFileName, out IFile preferredHostFile))
            {
                return preferredHostFile;
            }

            foreach (string fallbackHostName in settings.Host.FallbackHostTemplateConfigNames)
            {
                string fallbackHostFileName = string.Concat(fallbackHostName, HostTemplateFileConfigBaseName);

                if (allHostFilesForTemplate.TryGetValue(fallbackHostFileName, out IFile fallbackHostFile))
                {
                    return fallbackHostFile;
                }
            }

            return null;
        }
    }
}
