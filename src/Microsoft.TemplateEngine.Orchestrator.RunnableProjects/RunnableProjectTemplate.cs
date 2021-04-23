// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class RunnableProjectTemplate : ITemplate
    {
        private readonly JObject _raw;

        private IReadOnlyDictionary<string, ICacheTag> _tags;

        private IReadOnlyDictionary<string, ICacheParameter> _cacheParameters;

        private IReadOnlyList<ITemplateParameter> _parameters;

        internal RunnableProjectTemplate(JObject raw, IGenerator generator, IFile configFile, IRunnableProjectConfig config, IFile localeConfigFile, IFile hostConfigFile)
        {
            config.SourceFile = configFile;
            ConfigFile = configFile;
            Generator = generator;
            Source = configFile.MountPoint;
            Config = config;
            DefaultName = config.DefaultName;
            Name = config.Name;
            Identity = config.Identity ?? config.Name;

            ShortNameList = config.ShortNameList ?? new List<string>();

            Author = config.Author;
            Tags = config.Tags ?? new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase);
            CacheParameters = config.CacheParameters ?? new Dictionary<string, ICacheParameter>(StringComparer.OrdinalIgnoreCase);
            Description = config.Description;
            Classifications = config.Classifications;
            GroupIdentity = config.GroupIdentity;
            Precedence = config.Precedence;
            LocaleConfigFile = localeConfigFile;
            IsNameAgreementWithFolderPreferred = raw.ToBool("preferNameDirectory", false);
            HostConfigPlace = hostConfigFile?.FullPath;
            ThirdPartyNotices = raw.ToString("thirdPartyNotices");
            _raw = raw;
            BaselineInfo = config.BaselineInfo;
        }

        public IDirectory TemplateSourceRoot
        {
            get
            {
                return ConfigFile?.Parent?.Parent;
            }
        }

        public string Identity { get; }

        public Guid GeneratorId => Generator.Id;

        public string Author { get; }

        public string Description { get; }

        public IReadOnlyList<string> Classifications { get; }

        public string DefaultName { get; }
        public IGenerator Generator { get; }
        public string GroupIdentity { get; }
        public int Precedence { get; set; }
        public string Name { get; }

        public string ShortName
        {
            get
            {
                if (ShortNameList.Count > 0)
                {
                    return ShortNameList[0];
                }

                return string.Empty;
            }
            set
            {
                if (ShortNameList.Count > 0)
                {
                    throw new Exception("Can't set the short name when the ShortNameList already has entries.");
                }

                ShortNameList = new List<string>() { value };
            }
        }

        public IReadOnlyList<string> ShortNameList { get; private set; }

        public IReadOnlyDictionary<string, ICacheTag> Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                _parameters = null;
            }
        }

        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters
        {
            get
            {
                return _cacheParameters;
            }
            set
            {
                _cacheParameters = value;
                _parameters = null;
            }
        }

        public IReadOnlyList<ITemplateParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    List<ITemplateParameter> parameters = new List<ITemplateParameter>();

                    foreach (KeyValuePair<string, ICacheTag> tagInfo in Tags)
                    {
                        ITemplateParameter param = new Parameter
                        {
                            Name = tagInfo.Key,
                            Documentation = tagInfo.Value.Description,
                            DefaultValue = tagInfo.Value.DefaultValue,
                            Choices = tagInfo.Value.Choices,
                            DataType = "choice"
                        };

                        if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                            && tagInfo.Value is IAllowDefaultIfOptionWithoutValue tagValueWithNoValueDefault)
                        {
                            paramWithNoValueDefault.DefaultIfOptionWithoutValue = tagValueWithNoValueDefault.DefaultIfOptionWithoutValue;
                            parameters.Add(paramWithNoValueDefault as Parameter);
                        }
                        else
                        {
                            parameters.Add(param);
                        }
                    }

                    foreach (KeyValuePair<string, ICacheParameter> paramInfo in CacheParameters)
                    {
                        ITemplateParameter param = new Parameter
                        {
                            Name = paramInfo.Key,
                            Documentation = paramInfo.Value.Description,
                            DataType = paramInfo.Value.DataType,
                            DefaultValue = paramInfo.Value.DefaultValue,
                        };

                        if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                            && paramInfo.Value is IAllowDefaultIfOptionWithoutValue infoWithNoValueDefault)
                        {
                            paramWithNoValueDefault.DefaultIfOptionWithoutValue = infoWithNoValueDefault.DefaultIfOptionWithoutValue;
                            parameters.Add(paramWithNoValueDefault as Parameter);
                        }
                        else
                        {
                            parameters.Add(param);
                        }
                    }

                    _parameters = parameters;
                }

                return _parameters;
            }
        }

        public IFileSystemInfo Configuration => ConfigFile;
        public string MountPointUri => Configuration.MountPoint.MountPointUri;
        public string ConfigPlace => Configuration.FullPath;
        public IFileSystemInfo LocaleConfiguration => LocaleConfigFile;
        public string LocaleConfigPlace => LocaleConfiguration.FullPath;
        public bool IsNameAgreementWithFolderPreferred { get; }
        public string HostConfigPlace { get; }
        public string ThirdPartyNotices { get; }
        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; set; }

        bool ITemplateInfo.HasScriptRunningPostActions { get; set; }
        internal IRunnableProjectConfig Config { get; private set; }
        internal IMountPoint Source { get; }
        internal IFile ConfigFile { get; }
        internal IFile LocaleConfigFile { get; }
    }
}
