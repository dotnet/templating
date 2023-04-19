// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetManagedTemplatePackage : IManagedTemplatePackage
    {
        private const string AuthorKey = "Author";
        private const string LocalPackageKey = "LocalPackage";
        private const string OwnersKey = "Owners";
        private const string TrustedKey = "Trusted";
        private const string NuGetSourceKey = "NuGetSource";
        private const string PackageIdKey = "PackageId";
        private const string PackageVersionKey = "Version";
        private readonly IEngineEnvironmentSettings _settings;
        private readonly ILogger _logger;

        public NuGetManagedTemplatePackage(
          IEngineEnvironmentSettings settings,
          IInstaller installer,
          IManagedTemplatePackageProvider provider,
          string mountPointUri,
          string packageIdentifier)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} cannot be null or empty", nameof(mountPointUri));
            }
            if (string.IsNullOrWhiteSpace(packageIdentifier))
            {
                throw new ArgumentException($"{nameof(packageIdentifier)} cannot be null or empty", nameof(packageIdentifier));
            }
            MountPointUri = mountPointUri;
            Installer = installer ?? throw new ArgumentNullException(nameof(installer));
            ManagedProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = settings.Host.LoggerFactory.CreateLogger<NuGetInstaller>();

            Details = new Dictionary<string, string>
            {
                [PackageIdKey] = packageIdentifier
            };
        }

        /// <summary>
        /// Private constructor used for de-serialization only.
        /// </summary>
        private NuGetManagedTemplatePackage(
            IEngineEnvironmentSettings settings,
            IInstaller installer,
            IManagedTemplatePackageProvider provider,
            string mountPointUri,
            IReadOnlyDictionary<string, string> details)
        {
            if (string.IsNullOrWhiteSpace(mountPointUri))
            {
                throw new ArgumentException($"{nameof(mountPointUri)} cannot be null or empty", nameof(mountPointUri));
            }
            MountPointUri = mountPointUri;
            Installer = installer ?? throw new ArgumentNullException(nameof(installer));
            ManagedProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Details = details?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? throw new ArgumentNullException(nameof(details));
            if (Details.TryGetValue(PackageIdKey, out string packageId))
            {
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    throw new ArgumentException($"{nameof(details)} should contain key {PackageIdKey} with non-empty value", nameof(details));
                }
            }
            else
            {
                throw new ArgumentException($"{nameof(details)} should contain key {PackageIdKey}", nameof(details));
            }
            _logger = settings.Host.LoggerFactory.CreateLogger<NuGetInstaller>();
        }

        public ITemplatePackageProvider Provider => ManagedProvider;

        public IManagedTemplatePackageProvider ManagedProvider { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(Version) ? Identifier : $"{Identifier}::{Version}";

        public string Identifier => Details[PackageIdKey];

        public IInstaller Installer { get; }

        public string MountPointUri { get; }

        public DateTime LastChangeTime
        {
            get
            {
                try
                {
                    return _settings.Host.FileSystem.GetLastWriteTimeUtc(MountPointUri);
                }
                catch (Exception e)
                {
                    _logger.LogDebug($"Failed to get last changed time for {MountPointUri}, details: {e}");
                    return default;
                }
            }
        }

        public string? Trusted
        {
            get => Details.TryGetValue(TrustedKey, out string trusted) ? trusted : false.ToString();
            set => Details.UpdateOrRemoveValue(TrustedKey, value!, InsertionCondition);
        }

        public string? Author
        {
            get => Details.TryGetValue(AuthorKey, out string author) ? author : null;
            set => Details.UpdateOrRemoveValue(AuthorKey, value!, InsertionCondition);
        }

        public string? Owners
        {
            get => Details.TryGetValue(OwnersKey, out string owners) ? owners : null;
            set => Details.UpdateOrRemoveValue(OwnersKey, value!, InsertionCondition);
        }

        public bool IsLocalPackage
        {
            get
            {
                if (Details.TryGetValue(LocalPackageKey, out string val) && bool.TryParse(val, out bool isLocalPackage))
                {
                    return isLocalPackage;
                }
                return false;
            }
            set => Details.UpdateOrRemoveValue(LocalPackageKey, value.ToString(), (string value) => value == true.ToString());
        }

        public string? NuGetSource
        {
            get => Details.TryGetValue(NuGetSourceKey, out string nugetSource) ? nugetSource : null;
            set => Details.UpdateOrRemoveValue(NuGetSourceKey, value!, InsertionCondition);
        }

        public string? Version
        {
            get => Details.TryGetValue(PackageVersionKey, out string version) ? version : null;
            set => Details.UpdateOrRemoveValue(PackageVersionKey, value!, InsertionCondition);
        }

        internal Dictionary<string, string> Details { get; }

        public static NuGetManagedTemplatePackage Deserialize(
            IEngineEnvironmentSettings settings,
            IInstaller installer,
            IManagedTemplatePackageProvider provider,
            string mountPointUri,
            IReadOnlyDictionary<string, string> details)
        {
            return new NuGetManagedTemplatePackage(settings, installer, provider, mountPointUri, details);
        }

        public IReadOnlyDictionary<string, string> GetDetails()
        {
            var details = new Dictionary<string, string>();

            details.TryAdd(AuthorKey, Author ?? string.Empty, InsertionCondition);
            details.TryAdd(OwnersKey, Owners ?? string.Empty, InsertionCondition);
            details.TryAdd(TrustedKey, Trusted ?? string.Empty, InsertionCondition);
            details.TryAdd(NuGetSourceKey, NuGetSource ?? string.Empty, InsertionCondition);

            return details;
        }

        private bool InsertionCondition(string entry) => !string.IsNullOrEmpty(entry);
    }
}
