// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    //this partial class contains the implememntation of ITemplate interface.
    internal abstract partial class DirectoryBasedTemplate : ITemplate
    {
        IDirectory ITemplate.TemplateSourceRoot => TemplateSourceRoot;

        string ITemplateInfo.Identity => TemplateIdentity;

        Guid ITemplateInfo.GeneratorId => Generator.Id;

        string? ITemplateInfo.Author => ConfigModel.Author;

        string? ITemplateInfo.Description => ConfigModel.Description;

        IReadOnlyList<string> ITemplateInfo.Classifications => ConfigModel.Classifications;

        string? ITemplateInfo.DefaultName => ConfigModel.DefaultName;

        IGenerator ITemplate.Generator => Generator;

        string? ITemplateInfo.GroupIdentity => ConfigModel.GroupIdentity;

        int ITemplateInfo.Precedence => ConfigModel.Precedence;

        string ITemplateInfo.Name => ConfigModel.Name ?? throw new TemplateValidationException("Template configuration should have name defined");

        [Obsolete]
        string ITemplateInfo.ShortName
        {
            get
            {
                if (((ITemplateInfo)this).ShortNameList.Count > 0)
                {
                    return ((ITemplateInfo)this).ShortNameList[0];
                }

                return string.Empty;
            }
        }

        IReadOnlyList<string> ITemplateInfo.ShortNameList => ConfigModel.ShortNameList ?? new List<string>();

        [Obsolete]
        IReadOnlyDictionary<string, ICacheTag> ITemplateInfo.Tags
        {
            get
            {
                Dictionary<string, ICacheTag> tags = new Dictionary<string, ICacheTag>();
                foreach (KeyValuePair<string, string> tag in ((ITemplateInfo)this).TagsCollection)
                {
                    tags[tag.Key] = new CacheTag(null, null, new Dictionary<string, ParameterChoice> { { tag.Value, new ParameterChoice(null, null) } }, tag.Value);
                }
                foreach (ITemplateParameter parameter in ((ITemplateInfo)this).ParameterDefinitions.Where(TemplateParameterExtensions.IsChoice))
                {
                    IReadOnlyDictionary<string, ParameterChoice> choices = parameter.Choices ?? new Dictionary<string, ParameterChoice>();
                    tags[parameter.Name] = new CacheTag(parameter.DisplayName, parameter.Description, choices, parameter.DefaultValue);
                }
                return tags;
            }
        }

        [Obsolete]
        IReadOnlyDictionary<string, ICacheParameter> ITemplateInfo.CacheParameters
        {
            get
            {
                Dictionary<string, ICacheParameter> cacheParameters = new Dictionary<string, ICacheParameter>();
                foreach (ITemplateParameter parameter in ((ITemplateInfo)this).ParameterDefinitions.Where(TemplateParameterExtensions.IsChoice))
                {
                    cacheParameters[parameter.Name] = new CacheParameter()
                    {
                        DataType = parameter.DataType,
                        DefaultValue = parameter.DefaultValue,
                        Description = parameter.Documentation,
                        DefaultIfOptionWithoutValue = parameter.DefaultIfOptionWithoutValue,
                        DisplayName = parameter.DisplayName

                    };
                }
                return cacheParameters;
            }
        }

        public IParameterDefinitionSet ParameterDefinitions => Parameters;

        [Obsolete("Use ParameterDefinitionSet instead.")]
        IReadOnlyList<ITemplateParameter> ITemplateInfo.Parameters => ParameterDefinitions;

        IFileSystemInfo ITemplate.Configuration => ConfigFile ?? throw new InvalidOperationException($"{nameof(ConfigFile)} should be set in order to continue");

        string ITemplateInfo.MountPointUri => ConfigFile?.MountPoint.MountPointUri ?? throw new InvalidOperationException($"{nameof(ConfigFile)} should be set in order to continue");

        string ITemplateInfo.ConfigPlace => ConfigFile?.FullPath ?? throw new InvalidOperationException($"{nameof(ConfigFile)} should be set in order to continue");

        //read in simple template model instead
        bool ITemplate.IsNameAgreementWithFolderPreferred => ConfigModel.PreferNameDirectory;

        //read in simple template model instead
        string? ITemplateInfo.ThirdPartyNotices => ConfigModel.ThirdPartyNotices;

        IReadOnlyDictionary<string, IBaselineInfo> ITemplateInfo.BaselineInfo => ConfigModel.BaselineInfo;

        IReadOnlyDictionary<string, string> ITemplateInfo.TagsCollection => ConfigModel.Tags;

        bool ITemplateInfo.HasScriptRunningPostActions { get; set; }

        IReadOnlyList<Guid> ITemplateInfo.PostActions => ConfigModel.PostActionModels.Select(pam => pam.ActionId).ToArray();

        IReadOnlyList<TemplateConstraintInfo> ITemplateInfo.Constraints => ConfigModel.Constraints;

        public IReadOnlyList<IValidationEntry> ValidationErrors => throw new NotImplementedException();

        public abstract IReadOnlyDictionary<string, ILocalizationLocator>? Localizations { get; }

        public abstract string? LocaleConfigPlace { get; }

        public abstract string? HostConfigPlace { get; }

        [Obsolete]
        public abstract IFileSystemInfo? LocaleConfiguration { get; }

        public void Dispose()
        {
            SourceMountPoint?.Dispose();
        }
    }
}
