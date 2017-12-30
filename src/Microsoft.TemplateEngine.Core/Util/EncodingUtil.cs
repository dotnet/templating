using System.Text;

namespace Microsoft.TemplateEngine.Core.Util
{
    //TODO: Remove this eventually & refer only to the one in utils
    public class EncodingUtil
    {
        public static Encoding Detect(byte[] buffer, int currentBufferLength, out byte[] bom) => Utils.EncodingUtil.Detect(buffer, currentBufferLength, out bom);
    }
}
