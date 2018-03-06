using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings.TemplateInfoReaders
{
    public class TemplateInfoReaderVersion1_0_0_2 : TemplateInfoReaderVersion1_0_0_1
    {
        public static new TemplateInfo FromJObject(JObject jObject)
        {
            TemplateInfo info = new TemplateInfo();

            TemplateInfoReaderVersion1_0_0_2 reader = new TemplateInfoReaderVersion1_0_0_2();
            return reader.Read(jObject);
        }

        protected override void ReadShortNameInfo(JObject jObject, TemplateInfo info)
        {
            if (info is IShortNameList)
            {
                JToken shortNameToken = jObject.Get<JToken>(nameof(TemplateInfo.ShortNameList));
                info.ShortNameList = JTokenStringOrArrayToCollection(shortNameToken, new string[0]);

                if (info.ShortNameList.Count == 0)
                {
                    // template.json stores the short name(s) in ShortName
                    // but the cache will store it in ShortNameList
                    base.ReadShortNameInfo(jObject, info);
                }
            }
            else
            {
                base.ReadShortNameInfo(jObject, info);
            }
        }

        private static IReadOnlyList<string> JTokenStringOrArrayToCollection(JToken token, string[] defaultSet)
        {
            if (token == null)
            {
                return defaultSet;
            }

            if (token.Type == JTokenType.String)
            {
                string tokenValue = token.ToString();
                return new List<string>() { tokenValue };
            }

            return token.ArrayAsStrings();
        }

        protected override ICacheTag ReadOneTag(JProperty item)
        {
            ICacheTag cacheTag = base.ReadOneTag(item);

            if (cacheTag is IAllowDefaultIfOptionWithoutValue tagWithNoValueDefault)
            {
                tagWithNoValueDefault.DefaultIfOptionWithoutValue = item.Value.ToString(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue));
                return tagWithNoValueDefault as CacheTag;
            }

            return cacheTag;
        }

        protected override ICacheParameter ReadOneParameter(JProperty item)
        {
            ICacheParameter param = base.ReadOneParameter(item);

            if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault)
            {
                paramWithNoValueDefault.DefaultIfOptionWithoutValue = item.Value.ToString(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue));
                return paramWithNoValueDefault as CacheParameter;
            }

            return param;
        }
    }
}
