// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Abstractions.Parameters;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal abstract partial class DirectoryBasedTemplate : ITemplateMetadata, ITemplateLocator
    {
        string ITemplateMetadata.Identity => TemplateIdentity;

        Guid ITemplateLocator.GeneratorId => Generator.Id;

        string? ITemplateMetadata.Author => ConfigModel.Author;

        string? ITemplateMetadata.Description => ConfigModel.Description;

        IReadOnlyList<string> ITemplateMetadata.Classifications => ConfigModel.Classifications;

        string? ITemplateMetadata.DefaultName => ConfigModel.DefaultName;

        string? ITemplateMetadata.GroupIdentity => ConfigModel.GroupIdentity;

        int ITemplateMetadata.Precedence => ConfigModel.Precedence;

        string ITemplateMetadata.Name => ConfigModel.Name ?? throw new TemplateValidationException("Template configuration should have name defined");

        IReadOnlyList<string> ITemplateMetadata.ShortNameList => ConfigModel.ShortNameList ?? new List<string>();

        public IParameterDefinitionSet ParameterDefinitions => Parameters;

        string ITemplateLocator.MountPointUri => ConfigFile?.MountPoint.MountPointUri ?? throw new InvalidOperationException($"{nameof(ConfigFile)} should be set in order to continue");

        string ITemplateLocator.ConfigPlace => ConfigFile?.FullPath ?? throw new InvalidOperationException($"{nameof(ConfigFile)} should be set in order to continue");

        string? ITemplateMetadata.ThirdPartyNotices => ConfigModel.ThirdPartyNotices;

        IReadOnlyDictionary<string, IBaselineInfo> ITemplateMetadata.BaselineInfo => ConfigModel.BaselineInfo;

        IReadOnlyDictionary<string, string> ITemplateMetadata.TagsCollection => ConfigModel.Tags;

        IReadOnlyList<Guid> ITemplateMetadata.PostActions => ConfigModel.PostActionModels.Select(pam => pam.ActionId).ToArray();

        IReadOnlyList<TemplateConstraintInfo> ITemplateMetadata.Constraints => ConfigModel.Constraints;

        public IReadOnlyList<IValidationEntry> ValidationErrors => throw new NotImplementedException();
    }
}
