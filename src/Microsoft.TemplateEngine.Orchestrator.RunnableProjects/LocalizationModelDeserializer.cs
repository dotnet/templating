// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Localization;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal static class LocalizationModelDeserializer
    {
        /// <summary>
        /// Character to be used when separating a key into parts.
        /// </summary>
        private const char KeySeparator = '/';

        /// <summary>
        /// Deserializes the given json data into an <see cref="ILocalizationModel"/>.
        /// </summary>
        /// <param name="data">Json data to be deserialized.</param>
        /// <param name="logger"><see cref="ILogger"/> to be used for logging diagnostics messages.</param>
        /// <param name="localizationModel">Deserialized model. Null, if the operation failed.</param>
        /// <returns>True if deserialization succeeded. False, otherwise.</returns>
        public static bool TryDeserialize(JObject data, ILogger logger, out ILocalizationModel? localizationModel)
        {
            try
            {
                var parameterLocalizations = new Dictionary<string, ParameterSymbolLocalizationModel>();

                List<(string Key, string Value)> localizedStrings = data.Properties()
                    .Select(p => p.Value.Type == JTokenType.String ? (p.Name, p.Value.ToString()) : throw new Exception(LocalizableStrings.Authoring_InvalidJsonElementInLocalizationFile))
                    .ToList();

                var symbols = LoadSymbolModels(localizedStrings);
                var postActions = LoadPostActionModels(localizedStrings);

                localizationModel = new LocalizationModel(
                    name: localizedStrings.FirstOrDefault(s => s.Key == "name").Value,
                    description: localizedStrings.FirstOrDefault(s => s.Key == "description").Value,
                    author: localizedStrings.FirstOrDefault(s => s.Key == "author").Value,
                    symbols,
                    postActions);
                return true;
            }
            catch
            {
                localizationModel = null;
                return false;
            }
        }

        /// <summary>
        /// Verifies that the given localization model was correctly constructed
        /// to localize the given template.
        /// </summary>
        /// <param name="model">The localization model to be verified.</param>
        /// <param name="template">The template that the model should be compatible with.</param>
        /// <param name="logger"><see cref="ILogger"/> to be used for logging.</param>
        /// <returns>True if the verification succeeds. False otherwise.
        /// Check logs for details in case of a failed verification.</returns>
        public static bool VerifyLocalizationModel(ILocalizationModel model, SimpleConfigModel template, ILogger logger)
        {
            int unusedPostActionLocs = model.PostActions.Count;
            foreach (var postAction in template.PostActionModel)
            {
                if (postAction.Id != null && model.PostActions.ContainsKey(postAction.Id))
                {
                    unusedPostActionLocs--;
                }
            }

            if (unusedPostActionLocs > 0)
            {
                // Localizations provide more translations than the number of post actions we have.
                string excessPostActionLocalizationIds = string.Join(", ", model.PostActions.Keys.Where(k => !template.PostActionModel.Any(p => p.Id == k)).Select(k => k.ToString()));
                logger.LogWarning(LocalizableStrings.Authoring_InvalidPostActionLocalizationIndex, excessPostActionLocalizationIds);
            }

            // TODO rest of the validation

            return false;
        }

        /// <summary>
        /// Generates parameter symbol localization models from the given localized strings.
        /// </summary>
        private static IReadOnlyDictionary<string, IParameterSymbolLocalizationModel> LoadSymbolModels(List<(string Key, string Value)> localizedStrings)
        {
            var results = new Dictionary<string, IParameterSymbolLocalizationModel>();

            // Property names are in format: symbols/framework/choices/net5.0/description
            // Split them using '/' and store together with the localized string.
            IEnumerable<(IEnumerable<string> NameParts, string LocalizedString)> strings = localizedStrings
                .Where(s => s.Key.StartsWith("symbols" + KeySeparator))
                .Select(s => (s.Key.Split(KeySeparator).AsEnumerable().Skip(1), s.Value))
                .ToList();

            // Group by symbol name
            foreach (var parameterParts in strings.GroupBy(p => p.NameParts.FirstOrDefault()))
            {
                if (string.IsNullOrEmpty(parameterParts.Key))
                {
                    // Symbol with no name. Ignore.
                    continue;
                }

                string symbolName = parameterParts.Key;
                string? displayName = parameterParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "displayName").LocalizedString;
                string? description = parameterParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "description").LocalizedString;

                IReadOnlyDictionary<string, ParameterChoiceLocalizationModel>? choiceModels = LoadChoiceModels(strings
                    .Where(s => s.NameParts.Skip(1).FirstOrDefault() == "choices")
                    .Select(s => (s.NameParts.Skip(2), s.LocalizedString)));

                ParameterSymbolLocalizationModel paramLoc = new ParameterSymbolLocalizationModel(
                    symbolName,
                    displayName,
                    description,
                    choiceModels);

                results[symbolName] = paramLoc;
            }

            return results;
        }

        /// <summary>
        /// Generates post action localization models. The given parts should begin with the choice name
        /// as shown below (prior parts of the name such as "symbols" and parameter name shouldn't be included).
        /// <list type="table">
        /// <item>net5.0/displayName</item>
        /// <item>net5.0/description</item>
        /// <item>netstandard2.0/description</item>
        /// </list>
        /// </summary>
        private static IReadOnlyDictionary<string, ParameterChoiceLocalizationModel> LoadChoiceModels(IEnumerable<(IEnumerable<string> NameParts, string LocalizedString)> strings)
        {
            var results = new Dictionary<string, ParameterChoiceLocalizationModel>();

            foreach (var choiceParts in strings.GroupBy(p => p.NameParts.FirstOrDefault()))
            {
                if (string.IsNullOrEmpty(choiceParts.Key))
                {
                    // Choice with no name. Ignore
                    continue;
                }

                string? displayName = choiceParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "displayName").LocalizedString;
                string? description = choiceParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "description").LocalizedString;

                results.Add(choiceParts.Key, new ParameterChoiceLocalizationModel(displayName, description));
            }

            return results;
        }

        /// <summary>
        /// Generates post action localization models from the given localized strings.
        /// </summary>
        private static IReadOnlyDictionary<string, IPostActionLocalizationModel> LoadPostActionModels(List<(string Key, string Value)> localizedStrings)
        {
            var results = new Dictionary<string, IPostActionLocalizationModel>();

            // Property names are in format: postActions/actionId/manualInstructions/instructionId/description
            // Split them using '/' and store together with the localized string.
            IEnumerable<(IEnumerable<string> NameParts, string LocalizedString)> strings = localizedStrings
                .Where(s => s.Key.StartsWith("postActions" + KeySeparator))
                .Select(s => (s.Key.Split(KeySeparator).AsEnumerable().Skip(1), s.Value))
                .ToList();

            foreach (var postActionParts in strings.GroupBy(p => p.NameParts.FirstOrDefault()))
            {
                string postActionId = postActionParts.Key;
                string? description = postActionParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "description").LocalizedString;
                var instructions = LoadManualInstructionModels(postActionParts
                    .Where(s => s.NameParts.Skip(1).FirstOrDefault().StartsWith("manualInstructions"))
                    .Select(s => (s.NameParts.Skip(2), s.LocalizedString)));

                results[postActionId] = new PostActionLocalizationModel()
                {
                    Description = description,
                    Instructions = instructions,
                };
            }

            return results;
        }

        /// <summary>
        /// Generates manual instruction localization models.
        /// The given parts should begin with the manual instruction id.
        /// <list type="table">
        /// <item>instructionToRestore/text</item>
        /// <item>instructionToRestart/text</item>
        /// </list>
        /// </summary>
        /// <returns>The localized manual instructions where each key represents the instruction id.</returns>
        private static IReadOnlyDictionary<string, string> LoadManualInstructionModels(IEnumerable<(IEnumerable<string> NameParts, string LocalizedString)> strings)
        {
            var results = new Dictionary<string, string>();

            foreach (var instructionParts in strings.GroupBy(p => p.NameParts.FirstOrDefault()))
            {
                string id = instructionParts.Key;
                string? text = instructionParts.SingleOrDefault(p => p.NameParts.Skip(1).FirstOrDefault() == "text").LocalizedString;
                results[id] = text;
            }

            return results;
        }
    }
}
