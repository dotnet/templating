using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.GlobalSettings
{
    public class TemplatesSourceData
    {
        public DateTime LastChangeTime { get; set; }

        public string MountPointUri { get; set; }

        public Guid InstallerId { get; set; }

        public Dictionary<string, string> Details { get; set; }
    }
}
