using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class ExactVersionSpecification : IVersionSpecification
    {
        public static bool TryParse(string version, out IVersionSpecification specification)
        {
            if (!VersionStringHelpers.IsVersionWellFormed(version))
            {
                specification = null;
                return false;
            }

            specification = new ExactVersionSpecification(version);
            return true;
        }

        public ExactVersionSpecification(string version)
        {
            RequiredVersion = version;
        }

        public string RequiredVersion { get; }

        public bool CheckIfVersionIsValid(string versionToCheck)
        {
            int? result;

            if (!SemanticVersion.TryParse(versionToCheck, out SemanticVersion semVerToCheck) || !SemanticVersion.TryParse(RequiredVersion, out SemanticVersion requiredSemVer))
            {
                result = VersionStringHelpers.CompareVersions(RequiredVersion, versionToCheck);
            }
            else
            {
                result = semVerToCheck.CompareTo(requiredSemVer);
            }

            return result.HasValue && result.Value == 0;
        }
    }
}
