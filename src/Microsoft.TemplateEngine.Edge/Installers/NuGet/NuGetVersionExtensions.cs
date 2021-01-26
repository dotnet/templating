using NuGet.Versioning;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal static class VersionExtensions
    {
        internal static Abstractions.SemanticVersion ToSemanticVersion(this NuGetVersion nuGetVersion)
        {
            if (Abstractions.SemanticVersion.TryParse(nuGetVersion.ToNormalizedString(), out Abstractions.SemanticVersion semanticVersion))
            {
                return semanticVersion;
            }
            else
            {
                return default;
            }
        }

        internal static NuGetVersion ToNuGetVersion(this Abstractions.SemanticVersion semanticVersion)
        {
            if (NuGetVersion.TryParse(semanticVersion.ToString(), out NuGetVersion nuGetVersion))
            {
                return nuGetVersion;
            }
            else
            {
                return default;
            }
        }
    }
}
