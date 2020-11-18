using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    internal enum InvalidParameterInfoKind
    {
        InvalidParameterName,
        InvalidValue,
        InvalidDefaultValue,
        AmbiguousValue
    }

    /// <summary>
    /// The class represents the information about the invalid template parameter used when executing the command
    /// </summary>
    internal class InvalidParameterInfo
    {
        public InvalidParameterInfo(InvalidParameterInfoKind kind, string inputFormat, string specifiedValue, string canonical)
        {
            InputFormat = inputFormat;
            SpecifiedValue = specifiedValue;
            Canonical = canonical;
            Kind = kind;
        }

        /// <summary>
        /// the option used in CLI for parameter
        /// </summary>
        public string InputFormat { get; }
        /// <summary>
        /// The value specified for the parameter in CLI
        /// </summary>
        public string SpecifiedValue { get; }
        /// <summary>
        /// The canonical name for the option
        /// </summary>
        public string Canonical { get; }
        /// <summary>
        /// - <see cref="InvalidParameterInfoKind.InvalidParameterName"/> - the option name is invalid <br />
        /// - <see cref="InvalidParameterInfoKind.InvalidValue"/> - the value is invalid <br />
        /// - <see cref="InvalidParameterInfoKind.InvalidDefaultValue"/> - the default name is invalid <br />
        /// - <see cref="InvalidParameterInfoKind.AmbiguousValue"/> - the value provided leads to ambiguous choice (for choice parameters only) <br />
        /// </summary>
        public InvalidParameterInfoKind Kind { get; }

        /// <summary>
        /// Provides the error string to use for the invalid parameters collection
        /// </summary>
        /// <param name="invalidParameterList">the invalid parameters collection to prepare output for</param>
        /// <returns>the error string for the output</returns>
        public static string InvalidParameterListToString(IEnumerable<InvalidParameterInfo> invalidParameterList)
        {
            if (!invalidParameterList.Any())
            {
                return string.Empty;
            }

            string invalidParamsErrorText = LocalizableStrings.InvalidTemplateParameterValues;
            foreach (InvalidParameterInfo invalidParam in invalidParameterList)
            {
                if (invalidParam.Kind == InvalidParameterInfoKind.InvalidParameterName)
                {
                    invalidParamsErrorText += Environment.NewLine + string.Format("{0}{1}    '{0}' is not a valid option", invalidParam.InputFormat, Environment.NewLine);
                }
                else if (invalidParam.Kind == InvalidParameterInfoKind.AmbiguousValue)
                {
                    invalidParamsErrorText += Environment.NewLine + string.Format(LocalizableStrings.AmbiguousParameterDetail, invalidParam.InputFormat, invalidParam.SpecifiedValue);
                }
                else if (invalidParam.Kind == InvalidParameterInfoKind.InvalidValue)
                {
                    invalidParamsErrorText += Environment.NewLine + string.Format(LocalizableStrings.InvalidParameterDetail, invalidParam.InputFormat, invalidParam.SpecifiedValue, invalidParam.Canonical);
                }
                else
                {
                    invalidParamsErrorText += Environment.NewLine + string.Format(LocalizableStrings.InvalidParameterDefault, invalidParam.Canonical, invalidParam.SpecifiedValue);
                }
            }

            return invalidParamsErrorText;
        }

        public static IDictionary<string, InvalidParameterInfo> IntersectWithExisting(IDictionary<string, InvalidParameterInfo> existing, IReadOnlyList<InvalidParameterInfo> newInfo)
        {
            Dictionary<string, InvalidParameterInfo> intersection = new Dictionary<string, InvalidParameterInfo>();

            foreach (InvalidParameterInfo info in newInfo)
            {
                if (existing.ContainsKey(info.Canonical))
                {
                    intersection.Add(info.Canonical, info);
                }
            }

            return intersection;
        }

        public override bool Equals(object obj)
        {
            if (obj is InvalidParameterInfo info)
            {
                //checking canonical name and kind is enough for invalid parameters to be the same
                if (Canonical.Equals(info.Canonical, StringComparison.OrdinalIgnoreCase) && Kind == info.Kind)
                {
                    return true;
                }
                return false;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return new { a = Canonical?.ToLowerInvariant(), Kind }.GetHashCode();
        }
    }
}
