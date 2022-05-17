// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Constraints
{
    internal class SdkVersionConstraintFactory : ITemplateConstraintFactory
    {
        public Guid Id { get; } = Guid.Parse("{4E9721EF-0C02-4C09-A5A4-56C3D29BFC8E}");

        public string Type => "sdk";

        public Task<ITemplateConstraint> CreateTemplateConstraintAsync(IEngineEnvironmentSettings environmentSettings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult((ITemplateConstraint)new SdkVersionConstraint(environmentSettings, this));
        }

        internal class SdkVersionConstraint : ConstraintBase
        {
            private readonly NuGetVersionSpecification _installedSdkVersion;

            internal SdkVersionConstraint(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory)
                : base(environmentSettings, factory)
            {
                _installedSdkVersion = ExtractInstalledSdkVersion(environmentSettings.Components.OfType<ISdkInfoProvider>(), environmentSettings.Host.Logger);
            }

            public override string DisplayName => "Current dotnet SDK version";

            protected override TemplateConstraintResult EvaluateInternal(string? args)
            {
                IReadOnlyList<IVersionSpecification> supportedSdks = ParseArgs(args).ToList();

                foreach (IVersionSpecification supportedSdk in supportedSdks)
                {
                    if (supportedSdk.CheckIfVersionIsValid(_installedSdkVersion.ToString()))
                    {
                        return TemplateConstraintResult.CreateAllowed(Type);
                    }
                }

                return TemplateConstraintResult.CreateRestricted(
                    Type,
                    string.Format(LocalizableStrings.SdkConstraint_Message_Restricted, _installedSdkVersion.ToString(), supportedSdks.ToCsvString()));
            }

            //supported configuration:
            // "args": "[7-*]"  // single semver nuget compatible version expression
            // "args": [ "5.0.100", "6.0.100" ] // multiple version expression - all expressing supported versions
            private static IEnumerable<IVersionSpecification> ParseArgs(string? args)
            {
                return args.ParseConstraintStrings().Select(ConstraintsHelper.ParseVersionSpecification);
            }

            private static NuGetVersionSpecification ExtractInstalledSdkVersion(IEnumerable<ISdkInfoProvider> sdkInfoProviders, ILogger logger)
            {
                NuGetVersionSpecification? installedSdkVersion = null;
                List<Guid> previousComponentsGuids = new List<Guid>();

                foreach (ISdkInfoProvider sdkInfoProvider in sdkInfoProviders)
                {
                    if (installedSdkVersion == null)
                    {
                        if (!NuGetVersionSpecification.TryParse(sdkInfoProvider.VersionString, out installedSdkVersion))
                        {
                            throw new ConfigurationException(string.Format(LocalizableStrings.SdkConstraint_Error_InvalidVersion, sdkInfoProvider.VersionString));
                        }
                    }
                    else if (!installedSdkVersion.CheckIfVersionIsValid(sdkInfoProvider.VersionString))
                    {
                        throw new ConfigurationException(string.Format(
                            LocalizableStrings.SdkConstraint_Error_MismatchedProviders,
                            sdkInfoProvider.Id,
                            sdkInfoProvider.VersionString,
                            previousComponentsGuids.ToCsvString(),
                            installedSdkVersion));
                    }

                    previousComponentsGuids.Add(sdkInfoProvider.Id);
                }

                if (previousComponentsGuids.Count > 1)
                {
                    logger.LogWarning(LocalizableStrings.SdkConstraint_Warning_DuplicatedProviders, previousComponentsGuids.ToCsvString());
                }

                if (previousComponentsGuids.Count == 0)
                {
                    throw new ConfigurationException(LocalizableStrings.SdkConstraint_Error_MissingProvider);
                }

                return installedSdkVersion!;
            }
        }
    }
}
