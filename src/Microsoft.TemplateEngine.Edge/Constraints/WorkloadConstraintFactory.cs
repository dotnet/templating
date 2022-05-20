// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Components;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Constraints
{
    public class WorkloadConstraintFactory : ITemplateConstraintFactory
    {
        Guid IIdentifiedComponent.Id { get; } = Guid.Parse("{F8BA5B13-7BD6-47C8-838C-66626526817B}");

        string ITemplateConstraintFactory.Type => "workload";

        async Task<ITemplateConstraint> ITemplateConstraintFactory.CreateTemplateConstraintAsync(IEngineEnvironmentSettings environmentSettings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // need to await due to lack of covariance on Tasks
            return await WorkloadConstraint.CreateAsync(environmentSettings, this, cancellationToken).ConfigureAwait(false);
        }

        internal class WorkloadConstraint : ConstraintBase
        {
            private readonly HashSet<string> _installedWorkloads;
            private readonly string _installedWorkloadsString;

            private WorkloadConstraint(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory, IReadOnlyList<WorkloadInfo> workloads)
                : base(environmentSettings, factory)
            {
                _installedWorkloads = new HashSet<string>(workloads.Select(w => w.Id), StringComparer.InvariantCultureIgnoreCase);
                _installedWorkloadsString = workloads.Select(w => $"{w.Id} \"{w.Description}\"").ToCsvString();
            }

            public override string DisplayName => "Workload";

            internal static async Task<WorkloadConstraint> CreateAsync(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory, CancellationToken cancellationToken)
            {
                IReadOnlyList<WorkloadInfo> workloads =
                    await ExtractWorkloadInfoAsync(
                        environmentSettings.Components.OfType<IWorkloadsInfoProvider>(),
                        environmentSettings.Host.Logger,
                        cancellationToken).ConfigureAwait(false);
                return new WorkloadConstraint(environmentSettings, factory, workloads);
            }

            protected override TemplateConstraintResult EvaluateInternal(string? args)
            {
                IReadOnlyList<string> supportedWorkloads = ParseArgs(args).ToList();

                bool isSupportedWorkload = supportedWorkloads.Any(_installedWorkloads.Contains);

                if (isSupportedWorkload)
                {
                    return TemplateConstraintResult.CreateAllowed(Type);
                }

                return TemplateConstraintResult.CreateRestricted(
                    Type,
                    string.Format(
                        LocalizableStrings.WorkloadConstraint_Message_Restricted,
                        string.Join(", ", supportedWorkloads),
                        string.Join(", ", _installedWorkloadsString)),
                    LocalizableStrings.Workload_Message_CTA);
            }

            //supported configuration:
            // "args": "maui-mobile"  // workload expected
            // "args": [ "maui-mobile", "maui-desktop" ] // any of workloads expected
            private static IEnumerable<string> ParseArgs(string? args)
            {
                return args.ParseArrayOfConstraintStrings();
            }

            private static async Task<IReadOnlyList<WorkloadInfo>> ExtractWorkloadInfoAsync(IEnumerable<IWorkloadsInfoProvider> workloadsInfoProviders, ILogger logger, CancellationToken token)
            {
                List<IWorkloadsInfoProvider> providers = workloadsInfoProviders.ToList();
                List<WorkloadInfo>? workloads = null;

                if (providers.Count == 0)
                {
                    throw new ConfigurationException(LocalizableStrings.WorkloadConstraint_Error_MissingProvider);
                }

                if (providers.Count > 1)
                {
                    throw new ConfigurationException(
                        string.Format(LocalizableStrings.WorkloadConstraint_Error_MismatchedProviders, providers.Select(p => p.Id).ToCsvString()));
                }

                token.ThrowIfCancellationRequested();
                IEnumerable<WorkloadInfo> currentProviderWorkloads = await providers[0].GetInstalledWorkloadsAsync(token).ConfigureAwait(false);
                workloads = currentProviderWorkloads.ToList();

                if (workloads!.Select(w => w.Id).HasDuplicities(StringComparer.InvariantCultureIgnoreCase))
                {
                    logger.LogWarning(string.Format(
                        LocalizableStrings.WorkloadConstraint_Warning_DuplicateWorkloads,
                        workloads.Select(w => w.Id).GetDuplicities(StringComparer.InvariantCultureIgnoreCase).ToCsvString()));
                    workloads = workloads
                        .GroupBy(w => w.Id, StringComparer.InvariantCultureIgnoreCase)
                        .Select(g => g.First())
                        .ToList();
                }

                return workloads!;
            }
        }
    }
}
