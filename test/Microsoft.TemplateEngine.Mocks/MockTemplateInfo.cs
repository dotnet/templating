using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockTemplateInfo : ITemplateInfo, IShortNameList
    {
        public MockTemplateInfo()
        {
            ShortNameList = new List<string>();
            Classifications = new List<string>();
            BaselineInfo = new Dictionary<string, IBaselineInfo>(StringComparer.Ordinal);
        }

        public MockTemplateInfo WithParameters(params string[] parameters)
        {
            _cacheParameters = parameters;
            return this;
        }
        public MockTemplateInfo WithTag(string tagName, params string[] values)
        {
            _tags.Add(tagName, values);
            return this;
        }

        protected string[] _cacheParameters = new string[0];
        protected Dictionary<string, string[]> _tags = new Dictionary<string, string[]>();

        public string Author { get; }

        public string Description { get; set; }

        public IReadOnlyList<string> Classifications { get; }

        public string DefaultName { get;  }

        public string Identity { get; set; }

        public Guid GeneratorId { get; }

        public string GroupIdentity { get; set; }

        public int Precedence { get; set; }

        public string Name { get; set; }

        public string ShortName
        {
            get
            {
                if (ShortNameList.Count > 0)
                {
                    return ShortNameList[0];
                }

                return String.Empty;
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

        public virtual IReadOnlyDictionary<string, ICacheTag> Tags
        {
            get
            {
                return _tags.ToDictionary(kvp => kvp.Key, kvp => CreateTestCacheTag(kvp.Value));
            }
        }

        public virtual IReadOnlyDictionary<string, ICacheParameter> CacheParameters
        {
            get
            {
                return _cacheParameters.ToDictionary(param => param, kvp => (ICacheParameter)new CacheParameter());
            }
        }

        private IReadOnlyList<ITemplateParameter> _parameters;
        public IReadOnlyList<ITemplateParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    List<ITemplateParameter> parameters = new List<ITemplateParameter>();

                    foreach (KeyValuePair<string, ICacheTag> tagInfo in Tags)
                    {
                        ITemplateParameter param = new TemplateParameter
                        {
                            Name = tagInfo.Key,
                            Documentation = tagInfo.Value.Description,
                            DefaultValue = tagInfo.Value.DefaultValue,
                            Choices = tagInfo.Value.ChoicesAndDescriptions,
                            DataType = "choice"
                        };

                        if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                            && tagInfo.Value is IAllowDefaultIfOptionWithoutValue tagWithNoValueDefault)
                        {
                            paramWithNoValueDefault.DefaultIfOptionWithoutValue = tagWithNoValueDefault.DefaultIfOptionWithoutValue;
                            parameters.Add(paramWithNoValueDefault as TemplateParameter);
                        }
                        else
                        {
                            parameters.Add(param);
                        }
                    }

                    foreach (KeyValuePair<string, ICacheParameter> paramInfo in CacheParameters)
                    {
                        ITemplateParameter param = new TemplateParameter
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
                            parameters.Add(paramWithNoValueDefault as TemplateParameter);
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
            set
            {
                _parameters = value;
            }
        }

        public Guid ConfigMountPointId { get; }

        public string ConfigPlace { get; }

        public Guid LocaleConfigMountPointId { get; }

        public string LocaleConfigPlace { get; }

        public Guid HostConfigMountPointId { get; }

        public string HostConfigPlace { get;  }

        public string ThirdPartyNotices { get; }

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; }

        public bool HasScriptRunningPostActions { get; set;  }

        public DateTime? ConfigTimestampUtc { get; }

        private static ICacheTag CreateTestCacheTag(IReadOnlyList<string> choiceList, string tagDescription = null, string defaultValue = null, string defaultIfOptionWithoutValue = null)
        {
            Dictionary<string, string> choicesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string choice in choiceList)
            {
                choicesDict.Add(choice, null);
            };

            return new CacheTag(tagDescription, choicesDict, defaultValue, defaultIfOptionWithoutValue);
        }
    }
}
