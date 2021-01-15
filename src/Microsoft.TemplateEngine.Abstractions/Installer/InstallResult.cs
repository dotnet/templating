using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class InstallResult
    {
        public bool Success { get; private set; }
        public string FailureMessage { get; private set; }
        public IManagedTemplatesSource ManagedTemplateSource { get; private set; }

        public static InstallResult CreateSuccess(IManagedTemplatesSource source)
        {
            return new InstallResult()
            {
                Success = true,
                ManagedTemplateSource = source
            };
        }

        public static InstallResult CreateFailure(string localizedFailureMessage)
        {
            return new InstallResult()
            {
                Success = false,
                FailureMessage = localizedFailureMessage
            };
        }
    }
}
