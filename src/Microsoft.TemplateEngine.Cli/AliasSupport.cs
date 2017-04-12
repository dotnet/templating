﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli
{
    public static class AliasSupport
    {
        public static AliasExpansionStatus TryExpandAliases(INewCommandInput commandInput, AliasRegistry aliasRegistry)
        {
            List<string> inputTokens = commandInput.Tokens.ToList();
            inputTokens.RemoveAt(0);    // remove the command name

            if (aliasRegistry.TryExpandCommandAliases(inputTokens, out IReadOnlyList<string> expandedTokens))
            {
                if (!expandedTokens.SequenceEqual(inputTokens))
                {
                    commandInput.ResetArgs(expandedTokens.ToArray());
                    return AliasExpansionStatus.Expanded;
                }

                return AliasExpansionStatus.NoChange;
            }

            return AliasExpansionStatus.ExpansionError;
        }

        // Matches on any non-word character (letter, number, or underscore)
        // Almost the same as \W, except \W has some quirks with unicode characters, and we allow '.'
        private static readonly Regex InvalidAliasRegex = new Regex("[^a-z0-9_.]", RegexOptions.IgnoreCase);
        // The first token must be a valid template short name. This naively tests for it by checking the first character.
        // TODO: make this test more robust.
        private static readonly Regex ValidFirstTokenRegex = new Regex("^[a-z0-9]", RegexOptions.IgnoreCase);

        public static CreationResultStatus ManipulateAliasIfValid(AliasRegistry aliasRegistry, string aliasName, List<string> inputTokens, HashSet<string> reservedAliasNames)
        {
            if (reservedAliasNames.Contains(aliasName))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasCannotBeShortName, aliasName));
                return CreationResultStatus.CreateFailed;
            }
            else if (InvalidAliasRegex.IsMatch(aliasName))
            {
                Reporter.Output.WriteLine(LocalizableStrings.AliasNameContainsInvalidCharacters);   // TODO - change this string
                return CreationResultStatus.InvalidParamValues;
            }

            inputTokens.RemoveAt(0);    // remove the command name
            IReadOnlyList<string> aliasTokens = FilterForAliasTokens(inputTokens); // remove '-a' or '--alias', and the alias name

            // The first token refers to a template name, or another alias.
            if (aliasTokens.Count > 0 && !ValidFirstTokenRegex.IsMatch(aliasTokens[0]))
            {
                Reporter.Output.WriteLine(LocalizableStrings.AliasValueFirstArgError);
                return CreationResultStatus.InvalidParamValues;
            }

            // create, update, or delete an alias.
            return ManipulateAliasValue(aliasName, aliasTokens, aliasRegistry);
        }

        private static CreationResultStatus ManipulateAliasValue(string aliasName, IReadOnlyList<string> aliasTokens, AliasRegistry aliasRegistry)
        {
            AliasManipulationResult result = aliasRegistry.TryCreateOrRemoveAlias(aliasName, aliasTokens);
            CreationResultStatus returnStatus = CreationResultStatus.OperationNotSpecified;

            switch (result.Status)
            {
                case AliasManipulationStatus.Created:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasCreated, result.AliasName, result.AliasValue));
                    returnStatus = CreationResultStatus.Success;
                    break;
                case AliasManipulationStatus.Removed:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasRemoved, result.AliasName, result.AliasValue));
                    returnStatus = CreationResultStatus.Success;
                    break;
                case AliasManipulationStatus.RemoveNonExistentFailed:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasRemoveNonExistentFailed, result.AliasName));
                    break;
                case AliasManipulationStatus.Updated:
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasUpdated, result.AliasName, result.AliasValue));
                    returnStatus = CreationResultStatus.Success;
                    break;
                case AliasManipulationStatus.WouldCreateCycle:
                    Reporter.Output.WriteLine(LocalizableStrings.AliasCycleError);
                    returnStatus = CreationResultStatus.CreateFailed;
                    break;
                case AliasManipulationStatus.InvalidInput:
                    Reporter.Output.WriteLine(LocalizableStrings.AliasNotCreatedInvalidInput);
                    returnStatus = CreationResultStatus.InvalidParamValues;
                    break;
            }

            return returnStatus;
        }

        private static IReadOnlyList<string> FilterForAliasTokens(IReadOnlyList<string> inputTokens)
        {
            List<string> aliasTokens = new List<string>();
            bool nextIsAliasName = false;
            string aliasName = null;

            foreach (string token in inputTokens)
            {
                if (nextIsAliasName)
                {
                    aliasName = token;
                    nextIsAliasName = false;
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
                else if (!token.StartsWith("--debug:", StringComparison.Ordinal))
                {
                    aliasTokens.Add(token);
                }
            }

            return aliasTokens;
        }

        public static CreationResultStatus DisplayAliasValues(IEngineEnvironmentSettings environment, INewCommandInput commandInput, AliasRegistry aliasRegistry, string commandName)
        {
            IReadOnlyDictionary<string, string> aliasesToShow;

            if (!string.IsNullOrEmpty(commandInput.ShowAliasesAliasName))
            {
                if (aliasRegistry.AllAliases.TryGetValue(commandInput.ShowAliasesAliasName, out string aliasValue))
                {
                    aliasesToShow = new Dictionary<string, string>()
                    {
                        { commandInput.ShowAliasesAliasName, aliasValue }
                    };
                }
                else
                {
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.AliasShowErrorUnknownAlias, commandInput.ShowAliasesAliasName, commandName));
                    return CreationResultStatus.InvalidParamValues;
                }
            }
            else
            {
                aliasesToShow = aliasRegistry.AllAliases;
                Reporter.Output.WriteLine(LocalizableStrings.AliasShowAllAliasesHeader);
            }

            HelpFormatter<KeyValuePair<string, string>> formatter = new HelpFormatter<KeyValuePair<string, string>>(environment, aliasesToShow, 2, '-', false)
                            .DefineColumn(t => t.Key, LocalizableStrings.AliasName)
                            .DefineColumn(t => t.Value, LocalizableStrings.AliasValue);
            Reporter.Output.WriteLine(formatter.Layout());
            return CreationResultStatus.Success;
        }
    }
}
