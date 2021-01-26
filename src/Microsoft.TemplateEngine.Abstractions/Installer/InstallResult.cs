using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class InstallResult
    {
        public enum ErrorCode { Success = 0, PackageNotFound = 1, InvalidSource = 2, DownloadFailed = 3, UnsupportedRequest = 4, GenericError = 5 }
        public ErrorCode Error { get; private set; }

        public bool Success => Error == 0;

        public string ErrorMessage { get; private set; }
        public IManagedTemplatesSource ManagedTemplateSource { get; private set; }

        public static InstallResult CreateSuccess(IManagedTemplatesSource source)
        {
            return new InstallResult()
            {
                Error = ErrorCode.Success,
                ManagedTemplateSource = source
            };
        }

        public static InstallResult CreateFailure(ErrorCode error, string localizedFailureMessage)
        {
            return new InstallResult()
            {
                Error = error,
                ErrorMessage = localizedFailureMessage
            };
        }
    }
}
