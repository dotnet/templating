using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class RangeVersionSpecification : IVersionSpecification
    {
        public static bool TryParse(string range, out IVersionSpecification specification)
        {
            bool startInclusive = false;
            bool endInclusive = false;

            if (range.StartsWith("["))
            {
                startInclusive = true;
            }
            else if (!range.StartsWith("("))
            {
                specification = null;
                return false;
            }

            if (range.EndsWith("]"))
            {
                endInclusive = true;
            }
            else if (!range.EndsWith(")"))
            {
                specification = null;
                return false;
            }

            string[] parts = range.Split('-');
            if (parts.Length != 2)
            {
                //If this is a semver range, the first part must end with a space
                int firstPartEnd = 0;
                for (; firstPartEnd < parts.Length - 1 && !parts[firstPartEnd].EndsWith(" "); ++firstPartEnd)
                {
                }

                if (firstPartEnd < parts.Length - 1)
                {
                    parts = new string[] { string.Join("-", parts.Take(firstPartEnd + 1)), string.Join("-", parts.Skip(firstPartEnd + 1)) };
                }
                else
                {
                    specification = null;
                    return false;
                }
            }

            string startVersion = parts[0].Substring(1).Trim();
            string endVersion = parts[1].Substring(0, parts[1].Length - 1).Trim();

            if (IsWildcardVersion(startVersion) && IsWildcardVersion(endVersion))
            {
                specification = null;
                return false;
            }
            else if (!IsWildcardVersion(startVersion) && !VersionStringHelpers.IsVersionWellFormed(startVersion) && !SemanticVersion.TryParse(startVersion, out SemanticVersion _))
            {
                specification = null;
                return false;
            }
            else if (!IsWildcardVersion(endVersion) && !VersionStringHelpers.IsVersionWellFormed(endVersion) && !SemanticVersion.TryParse(endVersion, out SemanticVersion _))
            {
                specification = null;
                return false;
            }

            specification = new RangeVersionSpecification(startVersion, endVersion, startInclusive, endInclusive);
            return true;
        }

        public RangeVersionSpecification(string min, string max, bool isStartInclusive, bool isEndInclusive)
        {
            MinVersion = min;
            MaxVersion = max;
            IsStartInclusive = isStartInclusive;
            IsEndInclusive = isEndInclusive;
        }

        public string MinVersion { get; }

        public string MaxVersion { get; }

        public bool IsStartInclusive { get; }

        public bool IsEndInclusive { get; }

        private static bool IsWildcardVersion(string version)
        {
            return string.Equals(version, "*");
        }

        public bool CheckIfVersionIsValid(string versionToCheck)
        {
            bool isStartValid;
            bool isEndValid;

            if (!IsWildcardVersion(MinVersion))
            {
                int? startComparison;

                if (!SemanticVersion.TryParse(MinVersion, out SemanticVersion minSemVer) || !SemanticVersion.TryParse(versionToCheck, out SemanticVersion semVerToCheck))
                {
                    startComparison = VersionStringHelpers.CompareVersions(MinVersion, versionToCheck);
                }
                else
                {
                    startComparison = minSemVer.CompareTo(semVerToCheck);
                }

                if (startComparison == null)
                {
                    return false;
                }

                if (IsStartInclusive)
                {
                    isStartValid = startComparison.Value <= 0;
                }
                else
                {
                    isStartValid = startComparison.Value < 0;
                }
            }
            else
            {
                isStartValid = true;
            }

            if (!IsWildcardVersion(MaxVersion))
            {
                int? endComparison;

                if (!SemanticVersion.TryParse(MaxVersion, out SemanticVersion maxSemVer) || !SemanticVersion.TryParse(versionToCheck, out SemanticVersion semVerToCheck))
                {
                    endComparison = VersionStringHelpers.CompareVersions(versionToCheck, MaxVersion);
                }
                else
                {
                    endComparison = semVerToCheck.CompareTo(maxSemVer);
                }

                if (endComparison == null)
                {
                    return false;
                }

                if (IsEndInclusive)
                {
                    isEndValid = endComparison.Value <= 0;
                }
                else 
                {
                    isEndValid = endComparison.Value < 0;
                }
            }
            else
            {
                isEndValid = true;
            }

            return isStartValid && isEndValid;
        }
    }
}
