using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IGlobalSettings

    {
        List<UserInstalledTemplatesSource> UserInstalledTemplatesSources { get; }

        public string DefaultLanguage { get; set; }
    }
}
