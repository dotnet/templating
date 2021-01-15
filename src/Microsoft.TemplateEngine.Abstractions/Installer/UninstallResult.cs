namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class UninstallResult
    {
        public bool Success { get; private set; }
        public string FailureMessage { get; private set; }

        public static UninstallResult CreateSuccess()
        {
            return new UninstallResult()
            {
                Success = true,
            };
        }

        public static UninstallResult CreateFailure(string localizedFailureMessage)
        {
            return new UninstallResult()
            {
                Success = false,
                FailureMessage = localizedFailureMessage
            };
        }
    }
}
