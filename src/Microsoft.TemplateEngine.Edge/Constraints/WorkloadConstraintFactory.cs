﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Constraints
{
    internal class WorkloadConstraintFactory : ITemplateConstraintFactory
    {
        public Guid Id { get; } = Guid.Parse("{F8BA5B13-7BD6-47C8-838C-66626526817B}");

        public string Type => "workload";

        public Task<ITemplateConstraint> CreateTemplateConstraintAsync(IEngineEnvironmentSettings environmentSettings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult((ITemplateConstraint)new WorkloadConstraint(environmentSettings, this));
        }

        internal class WorkloadConstraint : ConstraintBase
        {
            private readonly HashSet<string> _installedWorkloads;
            private readonly string _installedWorkloadsString;

            internal WorkloadConstraint(IEngineEnvironmentSettings environmentSettings, ITemplateConstraintFactory factory)
                : base(environmentSettings, factory)
            {
                IReadOnlyList<WorkloadInfo> workloads = ExtractWorkloadInfo(environmentSettings.Components.OfType<IWorkloadsInfoProvider>(), environmentSettings.Host.Logger);
                _installedWorkloads = new HashSet<string>(workloads.Select(w => w.Id), StringComparer.InvariantCultureIgnoreCase);
                _installedWorkloadsString = workloads.Select(w => $"{w.Id} \"{w.Description}\"").ToCsvString();
            }

            public override string DisplayName => "Workload";

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
                return args.ParseConstraintStrings();
            }

            private static IReadOnlyList<WorkloadInfo> ExtractWorkloadInfo(IEnumerable<IWorkloadsInfoProvider> workloadsInfoProviders, ILogger logger)
            {
                List<WorkloadInfo>? workloads = null;
                List<Guid> previousComponentsGuids = new List<Guid>();

                foreach (IWorkloadsInfoProvider workloadsInfoProvider in workloadsInfoProviders)
                {
                    if (workloads == null)
                    {
                        workloads = workloadsInfoProvider.InstalledWorkloads.ToList();
                    }
                    else
                    {
                        if (
                            !workloads
                                .Select(w => w.Id)
                                .OrderBy(id => id)
                                .SequenceEqual(workloadsInfoProvider.InstalledWorkloads.Select(w => w.Id).OrderBy(id => id))
                            )
                        {
                            throw new ConfigurationException(string.Format(
                                LocalizableStrings.WorkloadConstraint_Error_MismatchedProviders, workloadsInfoProvider.Id, previousComponentsGuids.ToCsvString()));
                        }
                    }

                    previousComponentsGuids.Add(workloadsInfoProvider.Id);
                }

                if (previousComponentsGuids.Count > 1)
                {
                    logger.LogWarning(LocalizableStrings.WorkloadConstraint_Warning_DuplicatedProviders, previousComponentsGuids.ToCsvString());
                }

                if (previousComponentsGuids.Count == 0)
                {
                    throw new ConfigurationException(LocalizableStrings.WorkloadConstraint_Error_MissingProvider);
                }

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
