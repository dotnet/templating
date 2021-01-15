using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.GlobalSettings
{
    public interface IGlobalSettings
    {
        IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources { get; set; }

        void Add(TemplatesSourceData userInstalledTemplate);

        void Remove(TemplatesSourceData userInstalledTemplate);

        string DefaultLanguage { get; set; }

        event Action SettingsChanged;
    }
}
