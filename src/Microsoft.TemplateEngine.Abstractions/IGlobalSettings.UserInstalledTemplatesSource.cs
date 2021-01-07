using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions
{
    public class UserInstalledTemplatesSource
    {
        public DateTime InstallTime { get; set; }
        public Guid MountPointFactoryId { get; set; }
        public string Place { get; set; }
    }
}
