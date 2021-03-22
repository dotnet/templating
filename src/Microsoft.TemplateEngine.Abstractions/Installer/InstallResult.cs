using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class InstallResult : Result
    {
        public InstallRequest InstallRequest { get; private set; }

        public static InstallResult CreateSuccess(InstallRequest request, IManagedTemplatesPackage source)
        {
            return new InstallResult()
            {
                InstallRequest = request,
                Error = InstallerErrorCode.Success,
                Source = source
            };
        }

        public static InstallResult CreateFailure(InstallRequest request, InstallerErrorCode error, string localizedFailureMessage)
        {
            return new InstallResult()
            {
                InstallRequest = request,
                Error = error,
                ErrorMessage = localizedFailureMessage
            };
        }
    }
}
