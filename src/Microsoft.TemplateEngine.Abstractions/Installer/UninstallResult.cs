namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class UninstallResult : Result
    {
        public static UninstallResult CreateSuccess()
        {
            return new UninstallResult()
            {
                Error = InstallerErrorCode.Success
            };
        }

        public static UninstallResult CreateFailure(InstallerErrorCode code, string localizedFailureMessage)
        {
            return new UninstallResult()
            {
                Error = code,
                ErrorMessage = localizedFailureMessage
            };
        }
    }
}
