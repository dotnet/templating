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
using Microsoft.TemplateEngine.Abstractions.Components;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Constraints
{
    internal class SdkVersionConstraintFactory : ITemplateConstraintFactory
    {
        public Guid Id { get; } = Guid.Parse("{4E9721EF-0C02-4C09-A5A4-56C3D29BFC8E}");

        public string Type => "sdk-version";

        public async Task<ITemplateConstraint> CreateTemplateConstraintAsync(IEngineEnvironmentSettings environmentSettings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // need to await due to lack of covariance on Tasks
            return await SdkVersionConstraint.CreateAsync(environmentSettings, this, cancellationToken).ConfigureAwait(false);
        }

        internal class SdkVersionConstraint : ConstraintBase
        {
            private readonly NuGetVersionSpecification _installedSdkVersion;

            private SdkVersionConstraint(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory, NuGetVersionSpecification installedSdkVersion)
                : base(environmentSettings, factory)
            {
                _installedSdkVersion = installedSdkVersion;
            }

            public override string DisplayName => ".NET SDK version";

            internal static async Task<SdkVersionConstraint> CreateAsync(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory, CancellationToken cancellationToken)
            {
                NuGetVersionSpecification installedSdkVersion =
                    await ExtractInstalledSdkVersionAsync(
                        environmentSettings.Components.OfType<ISdkInfoProvider>(),
                        environmentSettings.Host.Logger,
                        cancellationToken).ConfigureAwait(false);
                return new SdkVersionConstraint(environmentSettings, factory, installedSdkVersion);
            }

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
                return args.ParseArrayOfConstraintStrings().Select(ConstraintsExtensions.ParseVersionSpecification);
            }

            private static async Task<NuGetVersionSpecification> ExtractInstalledSdkVersionAsync(IEnumerable<ISdkInfoProvider> sdkInfoProviders, ILogger logger, CancellationToken cancellationToken)
            {
                NuGetVersionSpecification? installedSdkVersion = null;
                List<Guid> previousComponentsGuids = new List<Guid>();

                foreach (ISdkInfoProvider sdkInfoProvider in sdkInfoProviders)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string version = await sdkInfoProvider.GetVersionAsync(cancellationToken).ConfigureAwait(false);
                    if (installedSdkVersion == null)
                    {
                        if (!NuGetVersionSpecification.TryParse(version, out installedSdkVersion))
                        {
                            throw new ConfigurationException(string.Format(LocalizableStrings.SdkConstraint_Error_InvalidVersion, version));
                        }
                    }
                    else if (!installedSdkVersion.CheckIfVersionIsValid(version))
                    {
                        throw new ConfigurationException(string.Format(
                            LocalizableStrings.SdkConstraint_Error_MismatchedProviders,
                            sdkInfoProvider.Id,
                            version,
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
