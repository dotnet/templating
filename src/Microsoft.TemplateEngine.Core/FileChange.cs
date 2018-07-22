using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Core
{
    public class FileChange : IFileChange2
    {
        public string SourceRelativePath { get; }

        public string TargetRelativePath { get; }

        public ChangeKind ChangeKind { get; }

        public byte[] Contents { get; }

        public FileChange(string sourceRelativePath, string targetRelativePath, ChangeKind changeKind, byte[] contents = null)
        {
            SourceRelativePath = sourceRelativePath;
            TargetRelativePath = targetRelativePath;
            ChangeKind = changeKind;
            Contents = contents;
        }
    }
}
