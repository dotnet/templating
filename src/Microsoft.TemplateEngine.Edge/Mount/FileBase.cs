using System.IO;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Edge.Mount
{
    public abstract class FileBase : FileSystemInfoBase, IFile
    {
        protected FileBase(IMountPoint mountPoint, string fullPath, string name)
            : base(mountPoint, fullPath, name, FileSystemInfoKind.File)
        {
        }

        public abstract Stream OpenRead();
    }
}
