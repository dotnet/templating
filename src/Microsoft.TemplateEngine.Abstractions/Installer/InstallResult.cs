using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class InstallResult : Result
    {
        public static InstallResult CreateSuccess(IManagedTemplatesSource source)
        {
            return new InstallResult()
            {
                Error = InstallerErrorCode.Success,
                Source = source
            };
        }

        public static InstallResult CreateFailure(InstallerErrorCode error, string localizedFailureMessage)
        {
            return new InstallResult()
            {
                Error = error,
                ErrorMessage = localizedFailureMessage
            };
        }
    }
}
