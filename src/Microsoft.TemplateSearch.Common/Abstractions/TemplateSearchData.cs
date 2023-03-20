﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateSearch.Common.Abstractions;

namespace Microsoft.TemplateSearch.Common
{
    /// <summary>
    /// Template searchable data.
    /// </summary>
    public partial class TemplateSearchData : ITemplateInfo
    {
        public TemplateSearchData(ITemplateInfo templateInfo, IDictionary<string, object>? data = null)
        {
            if (templateInfo is null)
            {
                throw new ArgumentNullException(nameof(templateInfo));
            }

#pragma warning disable CS0618 // Type or member is obsolete. The code will be moved to TemplateSearchData.Json when BlobStorageTemplateInfo is ready to be removed.
            TemplateInfo = new BlobStorageTemplateInfo(templateInfo);
#pragma warning restore CS0618 // Type or member is obsolete
            AdditionalData = data ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the additional data available for template package.
        /// </summary>
        /// <remarks>
        /// Additional data may be read by additional readers provider to <see cref="ITemplateSearchProviderFactory"/> when creating the <see cref="ITemplateSearchProvider"/>.
        /// </remarks>
        public IDictionary<string, object> AdditionalData { get; }

        /// <inheritdoc/>
        public string Identity => TemplateInfo.Identity;

        /// <inheritdoc/>
        public string? GroupIdentity => TemplateInfo.GroupIdentity;

        /// <inheritdoc/>
        public string Name => TemplateInfo.Name;

        /// <inheritdoc/>
        public IReadOnlyList<string> ShortNameList => TemplateInfo.ShortNameList;

        /// <inheritdoc/>
        bool ITemplateMetadata.PreferDefaultName => TemplateInfo.PreferDefaultName;

        /// <inheritdoc/>
        public string? Author => TemplateInfo.Author;

        /// <inheritdoc/>
        public string? Description => TemplateInfo.Description;

        /// <inheritdoc/>
        public IReadOnlyList<string> Classifications => TemplateInfo.Classifications;

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, string> TagsCollection => TemplateInfo.TagsCollection;

        /// <inheritdoc/>
        public IParameterDefinitionSet ParameterDefinitions => TemplateInfo.ParameterDefinitions;

        /// <inheritdoc/>
        [Obsolete("Use ParameterDefinitionSet instead.")]
        public IReadOnlyList<ITemplateParameter> Parameters => ParameterDefinitions;

        /// <inheritdoc/>
        public int Precedence => TemplateInfo.Precedence;

        /// <inheritdoc/>
        public string? ThirdPartyNotices => TemplateInfo.ThirdPartyNotices;

        #region implicit ITemplateInfo implementation
        string? ITemplateMetadata.DefaultName => TemplateInfo.DefaultName;

        Guid ITemplateLocator.GeneratorId => TemplateInfo.GeneratorId;

        [Obsolete]
        string ITemplateInfo.ShortName => TemplateInfo.ShortName;

        [Obsolete]
        IReadOnlyDictionary<string, ICacheTag> ITemplateInfo.Tags => TemplateInfo.Tags;

        [Obsolete]
        IReadOnlyDictionary<string, ICacheParameter> ITemplateInfo.CacheParameters => TemplateInfo.CacheParameters;

        string ITemplateLocator.MountPointUri => TemplateInfo.MountPointUri;

        string ITemplateLocator.ConfigPlace => TemplateInfo.ConfigPlace;

        string? IExtendedTemplateLocator.LocaleConfigPlace => TemplateInfo.LocaleConfigPlace;

        string? IExtendedTemplateLocator.HostConfigPlace => TemplateInfo.HostConfigPlace;

        IReadOnlyDictionary<string, IBaselineInfo> ITemplateMetadata.BaselineInfo => TemplateInfo.BaselineInfo;

        [Obsolete]
        bool ITemplateInfo.HasScriptRunningPostActions { get => TemplateInfo.HasScriptRunningPostActions; set => throw new NotImplementedException(); }

        IReadOnlyList<Guid> ITemplateMetadata.PostActions => TemplateInfo.PostActions;

        IReadOnlyList<TemplateConstraintInfo> ITemplateMetadata.Constraints => TemplateInfo.Constraints;
        #endregion

        private ITemplateInfo TemplateInfo { get; }
    }
}
