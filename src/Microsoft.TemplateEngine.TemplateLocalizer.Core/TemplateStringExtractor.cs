﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyCreators;
using Microsoft.TemplateEngine.TemplateLocalizer.Core.TraversalRules;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    /// <summary>
    /// Extracts localizable strings from template.json files.
    /// </summary>
    internal sealed class TemplateStringExtractor
    {
        private const string _defaultTemplateJsonLanguage = "en";
        private static readonly IJsonKeyCreator _defaultArrayKeyExtractor = new IndexBasedKeyCreator();
        private static readonly IJsonKeyCreator _defaultObjectKeyExtractor = new NameKeyCreator();

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly JsonDocument _jsonDocument;

        /// <summary>
        /// The rules that define which fields in the template json should be extracted to a templatestrings.json file.
        /// </summary>
        private readonly TraversalRule _documentRootTraversalRule =
            // Root element should be included in any case.
            new AllInclusiveTraversalRule().WithChildren(
                // Include "name" under the root.
                new StringFilteredTraversalRule("name"),
                // Include "description" under the root.
                new StringFilteredTraversalRule("description"),
                // Include "symbols" under the root, if they also comply with child rules.
                new StringFilteredTraversalRule("symbols").WithChild(
                    // Any symbol is included, skip none.
                    new AllInclusiveTraversalRule().WithChildren(
                        // Include "displayName" of each symbol.
                        new StringFilteredTraversalRule("displayName"),
                        // Include "description" of each symbol.
                        new StringFilteredTraversalRule("description"),
                        // Include "choices" of symbols, if they also comply with child rules.
                        new StringFilteredTraversalRule("choices", new ChildValueKeyCreator("choice")).WithChild(
                            // Include any element of the "choices" array. No choice will be skipped.
                            new AllInclusiveTraversalRule().WithChildren(
                                // Include "displayName" of each choice.
                                new StringFilteredTraversalRule("displayName"),
                                // Include "description" of each choice.
                                new StringFilteredTraversalRule("description"))))),
                // Include "postActions" under the root, if they also comply with child rules.
                new StringFilteredTraversalRule("postActions").WithChild(
                    // Any post action in "postActions" array should be included. Skip none.
                    new AllInclusiveTraversalRule().WithChildren(
                        // Include "description" of the post action.
                        new StringFilteredTraversalRule("description"),
                        // Include "manualInstructions" of the post action, if they also comply with child rules.
                        new RegexFilteredTraversalRule("manualInstructions").WithChild(
                            // Include all the manual instructions in the array. Skip none.
                            new AllInclusiveTraversalRule().WithChild(
                                // Include "text" of the post action.
                                new StringFilteredTraversalRule("text"))))));

        public TemplateStringExtractor(JsonDocument document, ILoggerFactory? loggerFactory = null)
        {
            _jsonDocument = document;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<TemplateStringExtractor>();
        }

        /// <summary>
        /// Extracts localizable strings from the json document as well as the language of the strings.
        /// </summary>
        /// <param name="language">The language of the extracted strings</param>
        /// <returns>The list of localizable strings.</returns>
        public IReadOnlyList<TemplateString> ExtractStrings(out string language)
        {
            List<TemplateString> extractedStrings = new ();

            TraversalArgs traversalArgs = new (
                    identifierPrefix: string.Empty,
                    keyPrefix: string.Empty,
                    rules: new List<TraversalRule>() { _documentRootTraversalRule },
                    extractedStrings,
                    extractedStringIds: new HashSet<string>());

            TraverseJsonElements(
                _jsonDocument.RootElement,
                elementName: string.Empty,
                localizationKey: string.Empty,
                traversalArgs);

            language = GetTemplateLanguage(_jsonDocument);

            return extractedStrings;
        }

        private void TraverseJsonElements(
            JsonElement element,
            string elementName,
            string localizationKey,
            TraversalArgs args)
        {
            using IDisposable? _ = _logger.BeginScope(elementName);
            List<TraversalRule> complyingRules = args.Rules.Where(r => r.AllowsTraversalOfIdentifier(elementName)).ToList();

            if (complyingRules.Count == 0)
            {
                // This identifier was filtered out.
                _logger.LogDebug(
                    "The following element in the template.json will not be included in the localizations" +
                    " because it does not match any of the rules for localizable elements: {0}",
                    args.IdentifierPrefix + "." + elementName);
                return;
            }

            JsonValueKind valueKind = element.ValueKind;
            if (valueKind == JsonValueKind.String)
            {
                ProcessStringElement(element, elementName, localizationKey, args);
                return;
            }

            string newIdentifierPrefix = args.IdentifierPrefix + "." + elementName;
            if (valueKind == JsonValueKind.Array)
            {
                TraversalArgs newData = args;
                newData.IdentifierPrefix = newIdentifierPrefix;
                newData.Rules = complyingRules;
                ProcessArrayElement(element, elementName, newData);
                return;
            }

            if (valueKind == JsonValueKind.Object)
            {
                TraversalArgs newData = args;
                newData.IdentifierPrefix = newIdentifierPrefix;
                newData.Rules = complyingRules;
                newData.KeyPrefix = args.KeyPrefix + "." + localizationKey;
                ProcessObjectElement(element, elementName, newData);
            }
        }

        private void ProcessStringElement(JsonElement element, string elementName, string key, TraversalArgs data)
        {
            string identifier = (data.IdentifierPrefix + "." + elementName).ToLowerInvariant();

            if (data.ExtractedStringIds.Contains(identifier))
            {
                // This string was already included by an earlier rule, possibly with a different key. Skip.
                _logger.LogWarning(
                    "The following element in the template.json will be skipped" +
                    " since it was already added to the list of localizable strings: {0}",
                    identifier);
                return;
            }

            string finalKey = data.KeyPrefix == null ? key : (data.KeyPrefix + "." + key);

            if (finalKey.StartsWith(".."))
            {
                // Omit the dots generated by the root element and the initial empty prefix.
                finalKey = finalKey.Substring(2);
            }

            data.ExtractedStringIds.Add(identifier);
            data.ExtractedStrings.Add(new TemplateString(identifier, finalKey, element.GetString() ?? string.Empty));
            _logger.LogTrace("Adding into localizable strings: {0}", identifier);
        }

        private void ProcessArrayElement(JsonElement element, string elementName, TraversalArgs args)
        {
            foreach (TraversalRule rule in args.Rules)
            {
                int childIndex = 0;
                foreach (var child in element.EnumerateArray())
                {
                    string childElementName = childIndex.ToString();
                    string? childKey = (rule.KeyCreator ?? _defaultArrayKeyExtractor).CreateKey(child, childElementName, elementName, childIndex);

                    TraversalArgs nextArgs = args;
                    nextArgs.Rules = rule.ChildRules;

                    using IDisposable? _ = _logger.BeginScope(childElementName);
                    TraverseJsonElements(child, childElementName, childKey, nextArgs);
                    childIndex++;
                }
            }
        }

        private void ProcessObjectElement(JsonElement element, string elementName, TraversalArgs args)
        {
            foreach (TraversalRule rule in args.Rules)
            {
                int childIndex = 0;
                foreach (JsonProperty child in element.EnumerateObject())
                {
                    string childElementName = child.Name;
                    string childKey = (rule.KeyCreator ?? _defaultObjectKeyExtractor).CreateKey(child.Value, childElementName, elementName, childIndex);

                    TraversalArgs nextArgs = args;
                    nextArgs.Rules = rule.ChildRules;

                    using IDisposable? _ = _logger.BeginScope(childElementName);
                    TraverseJsonElements(child.Value, childElementName, childKey, nextArgs);
                    childIndex++;
                }
            }
        }

        private string GetTemplateLanguage(JsonDocument jsonDocument)
        {
            string? language = null;

            if (_jsonDocument.RootElement.TryGetProperty("authoringLanguage", out JsonElement langElement) &&
                langElement.ValueKind == JsonValueKind.String)
            {
                language = langElement.GetString();
                return string.IsNullOrWhiteSpace(language) ? _defaultTemplateJsonLanguage : language!;
            }

            return _defaultTemplateJsonLanguage;
        }
    }
}
