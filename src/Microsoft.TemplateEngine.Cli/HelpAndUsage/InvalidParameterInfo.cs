using Microsoft.TemplateEngine.Cli.TemplateResolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// <param name="templateGroup">the template group to use to get more information about parameters. Optional - if not provided the possible value for the parameters won't be included to the output.</param>
        /// <returns>the error string for the output</returns>
        public static string InvalidParameterListToString(IEnumerable<InvalidParameterInfo> invalidParameterList, TemplateGroup templateGroup = null)
        {
            if (!invalidParameterList.Any())
            {
                return string.Empty;
            }

            StringBuilder invalidParamsErrorText = new StringBuilder(LocalizableStrings.InvalidTemplateParameterValues);
            const int padWidth = 3;
            invalidParamsErrorText.AppendLine();
            foreach (InvalidParameterInfo invalidParam in invalidParameterList)
            {
                if (invalidParam.Kind == InvalidParameterInfoKind.InvalidParameterName)
                {
                    invalidParamsErrorText.AppendLine(string.Format("{0}", invalidParam.InputFormat));
                    invalidParamsErrorText.Append(' ', padWidth).AppendLine(string.Format(LocalizableStrings.InvalidParameterNameDetail, invalidParam.InputFormat));
                }
                else if (invalidParam.Kind == InvalidParameterInfoKind.AmbiguousValue)
                {
                    invalidParamsErrorText.AppendLine(string.Format("{0} {1}", invalidParam.InputFormat, invalidParam.SpecifiedValue));
                    string header = string.Format(LocalizableStrings.AmbiguousParameterDetail, invalidParam.InputFormat, invalidParam.SpecifiedValue);
                    if (templateGroup != null)
                    {
                        DisplayValidValues(invalidParamsErrorText, header, templateGroup.GetAmbiguousValuesForChoiceParameter(invalidParam.Canonical, invalidParam.SpecifiedValue), padWidth);
                    }
                    else
                    {
                        invalidParamsErrorText.Append(' ', padWidth).AppendLine(header);
                    }
                }
                else if (invalidParam.Kind == InvalidParameterInfoKind.InvalidValue)
                {
                    invalidParamsErrorText.AppendLine(string.Format("{0} {1}", invalidParam.InputFormat, invalidParam.SpecifiedValue));
                    string header = string.Format(LocalizableStrings.InvalidParameterDetail, invalidParam.InputFormat, invalidParam.SpecifiedValue);
                    if (templateGroup != null)
                    {
                        DisplayValidValues(invalidParamsErrorText, header, templateGroup.GetValidValuesForChoiceParameter(invalidParam.Canonical), padWidth);
                    }
                    else
                    {
                        invalidParamsErrorText.Append(' ', padWidth).AppendLine(header);
                    }
                }
                else
                {
                    invalidParamsErrorText.AppendLine(string.Format("{0} {1}", invalidParam.InputFormat, invalidParam.SpecifiedValue));
                    invalidParamsErrorText.Append(' ', padWidth).AppendLine(string.Format(LocalizableStrings.InvalidParameterDefault, invalidParam.InputFormat, invalidParam.SpecifiedValue));
                }
            }
            return invalidParamsErrorText.ToString();
        }

        private static void DisplayValidValues(StringBuilder text, string header, IDictionary<string,string> possibleValues, int padWidth)
        {
            text.Append(' ', padWidth).Append(header);

            if (!possibleValues.Any())
            {
                return;
            }

            text.Append(' ').AppendLine(LocalizableStrings.PossibleValuesHeader);
            int longestChoiceLength = possibleValues.Keys.Max(x => x.Length);
            foreach (KeyValuePair<string, string> choiceInfo in possibleValues)
            {
                text.Append(' ', padWidth * 2).Append(choiceInfo.Key.PadRight(longestChoiceLength + padWidth));

                if (!string.IsNullOrWhiteSpace(choiceInfo.Value))
                {
                    text.Append("- " + choiceInfo.Value);
                }

                text.AppendLine();
            }
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
