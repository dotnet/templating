using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public static class ParseResultExtensions
    {
        public static bool HasAppliedOption(this ParseResult parseResult, params string[] optionPath)
        {
            if (optionPath.Length == 0)
            {
                return false;
            }

            if (!parseResult.HasOption(optionPath[0]))
            {
                return false;
            }

            AppliedOption workingOptionPath = parseResult[optionPath[0]];

            for (int i = 1; i < optionPath.Length; i++)
            {
                if (!workingOptionPath.HasOption(optionPath[i]))
                {
                    return false;
                }

                workingOptionPath = workingOptionPath[optionPath[i]];
            }

            return true;
        }

        public static T GetAppliedOptionOrDefault<T>(this ParseResult parseResult, params string[] optionsPath)
        {
            parseResult.TryGetAppliedOption(out T value, optionsPath);
            return value;
        }

        public static T GetAppliedOptionOrSpecifiedValue<T>(this ParseResult parseResult, T specifiedValue, params string[] optionPath)
        {
            if (parseResult.TryGetAppliedOption(out T optionValue, optionPath))
            {
                return optionValue;
            }
            else
            {
                return specifiedValue;
            }
        }

        public static IReadOnlyCollection<string> GetArgumentsAtPath(this ParseResult parseResult, params string[] optionPath)
        {
            if (parseResult.TryTraversePath(out AppliedOption option, optionPath))
            {
                return option.Arguments;
            }

            return new List<string>();
        }

        public static bool TryGetAppliedOption<T>(this ParseResult parseResult, out T optionValue, params string[] optionPath)
        {
            if (parseResult.TryTraversePath(out AppliedOption option, optionPath))
            {
                optionValue = option.Value<T>();
                return true;
            }

            optionValue = default(T);
            return false;
        }

        private static bool TryTraversePath(this ParseResult parseResult, out AppliedOption option, params string[] optionPath)
        {
            if (optionPath.Length == 0)
            {
                option = null;
                return false; 
            }

            if (!parseResult.HasOption(optionPath[0]))
            {
                option = null;
                return false;
            }

            AppliedOption workingOptionPath = parseResult[optionPath[0]];

            for (int i = 1; i < optionPath.Length; i++)
            {
                if (!workingOptionPath.HasOption(optionPath[i]))
                {
                    option = null;
                    return false;
                }

                workingOptionPath = workingOptionPath[optionPath[i]];
            }

            option = workingOptionPath;
            return true;
        }
    }
}
