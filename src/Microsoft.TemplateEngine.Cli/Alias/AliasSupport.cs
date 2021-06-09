// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;

namespace Microsoft.TemplateEngine.Cli.Alias
{
    internal static class AliasSupport
    {
        // Matches on any non-word character (letter, number, or underscore)
        // Almost the same as \W, except \W has some quirks with unicode characters, and we allow '.'
        private static readonly Regex InvalidAliasRegex = new Regex("[^a-z0-9_.]", RegexOptions.IgnoreCase);

        // The first token must be a valid template short name. This naively tests for it by checking the first character.
        // TODO: make this test more robust.
        private static readonly Regex ValidFirstTokenRegex = new Regex("^[a-z0-9]", RegexOptions.IgnoreCase);

        internal static New3CommandStatus CoordinateAliasExpansion(
            INewCommandInput commandInput,
            AliasRegistry aliasRegistry,
            TemplateInformationCoordinator templateInformationCoordinator)
        {
            AliasExpansionStatus aliasExpansionStatus = AliasSupport.TryExpandAliases(commandInput, aliasRegistry);
            if (aliasExpansionStatus == AliasExpansionStatus.ExpansionError)
            {
                Reporter.Output.WriteLine(LocalizableStrings.AliasExpansionError);
                return New3CommandStatus.InvalidParamValues;
            }
            else if (aliasExpansionStatus == AliasExpansionStatus.Expanded)
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasCommandAfterExpansion, string.Join(" ", commandInput.Tokens)));

                if (commandInput.HasParseError)
                {
                    Reporter.Output.WriteLine(LocalizableStrings.AliasExpandedCommandParseError);
                    return templateInformationCoordinator.HandleParseError(commandInput);
                }
            }

            // this is both for success and for no action.
            return New3CommandStatus.Success;
        }

        internal static AliasExpansionStatus TryExpandAliases(INewCommandInput commandInput, AliasRegistry aliasRegistry)
        {
            List<string> inputTokens = commandInput.Tokens.ToList();
            inputTokens.RemoveAt(0);    // remove the command name

            if (aliasRegistry.TryExpandCommandAliases(inputTokens, out IReadOnlyList<string> expandedTokens))
            {
                // TryExpandCommandAliases() return value indicates error (cycle) or not error. It doesn't indicate whether or not expansions actually occurred.
                if (!expandedTokens.SequenceEqual(inputTokens))
                {
                    commandInput.ResetArgs(expandedTokens.ToArray());
                    return AliasExpansionStatus.Expanded;
                }

                return AliasExpansionStatus.NoChange;
            }

            return AliasExpansionStatus.ExpansionError;
        }

        internal static New3CommandStatus ManipulateAliasIfValid(AliasRegistry aliasRegistry, string aliasName, List<string> inputTokens, HashSet<string> reservedAliasNames)
        {
            if (reservedAliasNames.Contains(aliasName))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasCannotBeShortName, aliasName));
                return New3CommandStatus.CreateFailed;
            }
            else if (InvalidAliasRegex.IsMatch(aliasName))
            {
                Reporter.Output.WriteLine(LocalizableStrings.AliasNameContainsInvalidCharacters);
                return New3CommandStatus.InvalidParamValues;
            }

            inputTokens.RemoveAt(0);    // remove the command name
            IReadOnlyList<string> aliasTokens = FilterForAliasTokens(inputTokens); // remove '-a' or '--alias', and the alias name

            // The first token refers to a template name, or another alias.
            if (aliasTokens.Count > 0 && !ValidFirstTokenRegex.IsMatch(aliasTokens[0]))
            {
                Reporter.Output.WriteLine(LocalizableStrings.AliasValueFirstArgError);
                return New3CommandStatus.InvalidParamValues;
            }

            // create, update, or delete an alias.
            return ManipulateAliasValue(aliasName, aliasTokens, aliasRegistry);
        }

        internal static New3CommandStatus DisplayAliasValues(IEngineEnvironmentSettings environment, INewCommandInput commandInput, AliasRegistry aliasRegistry, string commandName)
        {
            IReadOnlyDictionary<string, IReadOnlyList<string>> aliasesToShow;

            if (!string.IsNullOrEmpty(commandInput.ShowAliasesAliasName))
            {
                if (aliasRegistry.AllAliases.TryGetValue(commandInput.ShowAliasesAliasName, out IReadOnlyList<string>? aliasValue))
                {
                    aliasesToShow = new Dictionary<string, IReadOnlyList<string>>()
                    {
                        { commandInput.ShowAliasesAliasName, aliasValue }
                    };
                }
                else
                {
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasShowErrorUnknownAlias, commandInput.ShowAliasesAliasName, commandName));
                    return New3CommandStatus.InvalidParamValues;
                }
            }
            else
            {
                aliasesToShow = aliasRegistry.AllAliases;
                Reporter.Output.WriteLine(LocalizableStrings.AliasShowAllAliasesHeader);
            }

            HelpFormatter<KeyValuePair<string, IReadOnlyList<string>>> formatter =
                new HelpFormatter<KeyValuePair<string, IReadOnlyList<string>>>(
                    environment,
                    commandInput,
                    aliasesToShow,
                    columnPadding: 2,
                    headerSeparator: '-',
                    blankLineBetweenRows: false)
                .DefineColumn(t => t.Key, LocalizableStrings.AliasName, showAlways: true)
                .DefineColumn(t => string.Join(" ", t.Value), LocalizableStrings.AliasValue, showAlways: true);

            Reporter.Output.WriteLine(formatter.Layout());
            return New3CommandStatus.Success;
        }

        private static New3CommandStatus ManipulateAliasValue(string aliasName, IReadOnlyList<string> aliasTokens, AliasRegistry aliasRegistry)
        {
            AliasManipulationResult result = aliasRegistry.TryCreateOrRemoveAlias(aliasName, aliasTokens);

            switch (result.Status)
            {
                case AliasManipulationStatus.Created:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasCreated, result.AliasName, string.Join(" ", result.AliasTokens)));
                    return New3CommandStatus.Success;
                case AliasManipulationStatus.Removed:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasRemoved, result.AliasName, string.Join(" ", result.AliasTokens)));
                    return New3CommandStatus.Success;
                case AliasManipulationStatus.RemoveNonExistentFailed:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasRemoveNonExistentFailed, result.AliasName));
                    return New3CommandStatus.AliasFailed;
                case AliasManipulationStatus.Updated:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasUpdated, result.AliasName, string.Join(" ", result.AliasTokens)));
                    return New3CommandStatus.Success;
                case AliasManipulationStatus.WouldCreateCycle:
                    Reporter.Output.WriteLine(LocalizableStrings.AliasCycleError);
                    return New3CommandStatus.AliasFailed;
                case AliasManipulationStatus.InvalidInput:
                    Reporter.Output.WriteLine(LocalizableStrings.AliasNotCreatedInvalidInput);
                    return New3CommandStatus.InvalidParamValues;
                default:
                    return New3CommandStatus.OperationNotSpecified;
            }
        }

        private static IReadOnlyList<string> FilterForAliasTokens(IReadOnlyList<string> inputTokens)
        {
            List<string> aliasTokens = new List<string>();
            bool nextIsAliasName = false;
            bool skipNextToken = false;
            string? aliasName = null;

            foreach (string token in inputTokens)
            {
                if (nextIsAliasName)
                {
                    aliasName = token;
                    nextIsAliasName = false;
                }
                else if (skipNextToken)
                {
                    skipNextToken = false;
                    continue;
                }
                else if (string.Equals(token, "-a", StringComparison.Ordinal) || string.Equals(token, "--alias", StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(aliasName))
                    {
                        // found multiple alias names, which is invalid.
                        aliasTokens.Clear();
                        aliasName = null;
                        return aliasTokens;
                    }

                    nextIsAliasName = true;
                }
                else if (token.Equals("--debug:custom-hive", StringComparison.Ordinal))
                {
                    skipNextToken = true;
                }
                else if (!token.StartsWith("--debug:", StringComparison.Ordinal))
                {
                    aliasTokens.Add(token);
                }
            }

            return aliasTokens;
        }
    }
}
