using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings.TemplateInfoReaders
{
    public class TemplateInfoReaderVersion1_0_0_1 : TemplateInfoReaderVersion1_0_0_0
    {
        public static new TemplateInfo FromJObject(JObject jObject)
        {
            TemplateInfoReaderVersion1_0_0_1 reader = new TemplateInfoReaderVersion1_0_0_1();
            return reader.Read(jObject);
        }

        public override TemplateInfo Read(JObject jObject)
        {
            TemplateInfo info = base.Read(jObject);
            info.HasScriptRunningPostActions = jObject.ToBool(nameof(TemplateInfo.HasScriptRunningPostActions));

            return info;
        }
    }
}
