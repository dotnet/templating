﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal static class Extensions
    {
        internal static string? GetValueForOptionOrNull(this ParseResult parseResult, Option option)
        {
            OptionResult? result = parseResult.FindResultFor(option);
            if (result == null)
            {
                return null;
            }
            return result.GetValueOrDefault()?.ToString();
        }

        /// <summary>
        /// Checks if <paramref name="parseResult"/> contains an error for <paramref name="option"/>.
        /// </summary>
        internal static bool HasErrorFor(this ParseResult parseResult, Option option)
        {
            if (!parseResult.Errors.Any())
            {
                return false;
            }

            if (parseResult.Errors.Any(e => e.SymbolResult?.Symbol == option))
            {
                return true;
            }

            if (parseResult.Errors.Any(e => e.SymbolResult?.Parent?.Symbol == option))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Case insensitive version for <see cref="System.CommandLine.OptionExtensions.FromAmong{TOption}(TOption, string[])"/>.
        /// </summary>
        internal static void FromAmongCaseInsensitive(this Option<string> option, params string[] allowedValues)
        {
            option.AddValidator(optionResult => ValidateAllowedValues(optionResult, allowedValues));
            option.AddCompletions(allowedValues);
        }

        private static void ValidateAllowedValues(OptionResult optionResult, string[] allowedValues)
        {
            var invalidArguments = optionResult.Tokens.Where(token => !allowedValues.Contains(token.Value, StringComparer.OrdinalIgnoreCase));
            if (invalidArguments.Any())
            {
                optionResult.ErrorMessage = string.Format(
                    LocalizableStrings.Commands_Validator_WrongArgumentValue,
                    string.Join(", ", invalidArguments.Select(arg => $"'{arg.Value}'")),
                    string.Join(", ", allowedValues.Select(allowedValue => $"'{allowedValue}'")));
            }
        }
    }
}
