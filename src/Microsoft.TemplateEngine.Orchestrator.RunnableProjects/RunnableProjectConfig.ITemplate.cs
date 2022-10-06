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
    internal partial class RunnableProjectConfig : ITemplate
    {
        IGenerator ITemplate.Generator => Generator;

        IFileSystemInfo ITemplate.Configuration => ConfigFile ?? throw new InvalidOperationException("Configuration file is not initialized, are you using test constructor?");

        string ITemplateInfo.MountPointUri => ConfigFile?.MountPoint.MountPointUri ?? throw new InvalidOperationException("Configuration file is not initialized, are you using test constructor?");

        string ITemplateInfo.ConfigPlace => ConfigFile?.FullPath ?? throw new InvalidOperationException("Configuration file is not initialized, are you using test constructor?");

        IFileSystemInfo? ITemplate.LocaleConfiguration => _localeConfigFile;

        IFileSystemInfo? ITemplate.HostSpecificConfiguration => _hostConfigFile;

        string? ITemplateInfo.LocaleConfigPlace => _localeConfigFile?.FullPath;

        string? ITemplateInfo.HostConfigPlace => _hostConfigFile?.FullPath;

        bool ITemplate.IsNameAgreementWithFolderPreferred => ConfigModel.PreferNameDirectory;

        IDirectory ITemplate.TemplateSourceRoot => TemplateSourceRoot;

        IReadOnlyList<IValidationEntry> IValidationInfo.ValidationErrors => throw new NotImplementedException();

        string? ITemplateInfo.Author => ConfigModel.Author;

        string? ITemplateInfo.Description => ConfigModel.Description;

        IReadOnlyList<string> ITemplateInfo.Classifications => ConfigModel.Classifications;

        string? ITemplateInfo.DefaultName => ConfigModel.DefaultName;

        string ITemplateInfo.Identity => ConfigModel.Identity;

        Guid ITemplateInfo.GeneratorId => Generator.Id;

        string? ITemplateInfo.GroupIdentity => ConfigModel.GroupIdentity;

        int ITemplateInfo.Precedence => ConfigModel.Precedence;

        string ITemplateInfo.Name => ConfigModel.Name ?? throw new TemplateAuthoringException("Template configuration should have 'name' defined.", "name");

        IReadOnlyList<string> ITemplateInfo.ShortNameList => ConfigModel.ShortNameList ?? new List<string>();

        IParameterDefinitionSet ITemplateInfo.ParameterDefinitions => new ParameterDefinitionSet(ConfigModel.ExtractParameters());

        string? ITemplateInfo.ThirdPartyNotices => ConfigModel.ThirdPartyNotices;

        IReadOnlyDictionary<string, IBaselineInfo> ITemplateInfo.BaselineInfo => ConfigModel.BaselineInfo;

        IReadOnlyDictionary<string, string> ITemplateInfo.TagsCollection => ConfigModel.Tags;

        IReadOnlyList<Guid> ITemplateInfo.PostActions => ConfigModel.PostActionModels.Select(pam => pam.ActionId).ToArray();

        IReadOnlyList<TemplateConstraintInfo> ITemplateInfo.Constraints => ConfigModel.Constraints;

        #region Obsolete implementation

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

        [Obsolete("Use ParameterDefinitionSet instead.")]
        IReadOnlyList<ITemplateParameter> ITemplateInfo.Parameters => ParameterDefinitions;

        [Obsolete]
        bool ITemplateInfo.HasScriptRunningPostActions { get; set; }

        #endregion

        void IDisposable.Dispose() => SourceMountPoint.Dispose();
    }
}
