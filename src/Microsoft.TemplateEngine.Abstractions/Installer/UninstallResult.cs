using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class UninstallResult : Result
    {
        public static UninstallResult CreateSuccess(IManagedTemplatesSource source)
        {
            return new UninstallResult()
            {
                Error = InstallerErrorCode.Success,
                Source = source
            };
        }

        public static UninstallResult CreateFailure(IManagedTemplatesSource source, InstallerErrorCode code, string localizedFailureMessage)
        {
            return new UninstallResult()
            {
                Source = source,
                Error = code,
                ErrorMessage = localizedFailureMessage
            };
        }
    }
}
