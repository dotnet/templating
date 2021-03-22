using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class UninstallResult : Result
    {
        public static UninstallResult CreateSuccess(IManagedTemplatesPackage source)
        {
            return new UninstallResult()
            {
                Error = InstallerErrorCode.Success,
                Source = source
            };
        }

        public static UninstallResult CreateFailure(IManagedTemplatesPackage source, InstallerErrorCode code, string localizedFailureMessage)
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
