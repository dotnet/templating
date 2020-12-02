using System;
using System.Collections.Generic;
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
            Tags = new Dictionary<string, ICacheTag>(StringComparer.Ordinal);
            CacheParameters = new Dictionary<string, ICacheParameter>(StringComparer.Ordinal);
            BaselineInfo = new Dictionary<string, IBaselineInfo>(StringComparer.Ordinal);
        }

        public string Author { get; set; }

        public string Description { get; set; }

        public IReadOnlyList<string> Classifications { get; set; }

        public string DefaultName { get; set; }

        public string Identity { get; set; }

        public Guid GeneratorId { get; set; }

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

        public IReadOnlyList<string> ShortNameList { get; set; }

        public virtual IReadOnlyDictionary<string, ICacheTag> Tags { get; set; }

        public virtual IReadOnlyDictionary<string, ICacheParameter> CacheParameters { get; set; }

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

        public Guid ConfigMountPointId { get; set; }

        public string ConfigPlace { get; set; }

        public Guid LocaleConfigMountPointId { get; set; }

        public string LocaleConfigPlace { get; set; }

        public Guid HostConfigMountPointId { get; set; }

        public string HostConfigPlace { get; set; }

        public string ThirdPartyNotices { get; set; }

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; set; }

        public bool HasScriptRunningPostActions { get; set; }

        public DateTime? ConfigTimestampUtc { get; set; }
    }
}
