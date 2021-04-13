// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly ILogger _logger;
        private readonly JsonDocument _jsonDocument;

        private static readonly IJsonKeyCreator _defaultArrayKeyExtractor = new IndexBasedKeyCreator();
        private static readonly IJsonKeyCreator _defaultObjectKeyExtractor = new NameBasedKeyCreator();

        private readonly List<TemplateString> _extractedStrings = new List<TemplateString>();
        private readonly HashSet<string> _extractedStringIds = new HashSet<string>();

        /// <summary>
        /// The rules that define which fields in the template json should be extracted to a templatestrings.json file.
        /// </summary>
        private readonly TraversalRule _documentRootTraversalRule =
            new StringFilteredTraversalRule(string.Empty).WithChildren(
                new StringFilteredTraversalRule("name"),
                new StringFilteredTraversalRule("description"),
                new StringFilteredTraversalRule("symbols").WithChild(
                    new AllInclusiveTraversalRule().WithChildren(
                        new StringFilteredTraversalRule("displayName"),
                        new StringFilteredTraversalRule("description"),
                        new StringFilteredTraversalRule("choices", new MemberBasedKeyCreator("choice")).WithChild(
                            new RegexFilteredTraversalRule("[^\\.]*").WithChildren(
                                new StringFilteredTraversalRule("displayName"),
                                new StringFilteredTraversalRule("description"))))),
                new StringFilteredTraversalRule("postActions").WithChild(
                    new AllInclusiveTraversalRule().WithChildren(
                        new StringFilteredTraversalRule("description"),
                        new RegexFilteredTraversalRule("manualInstructions\\[([0-9]+)\\]").WithChild(
                            new StringFilteredTraversalRule("text")))));

        public TemplateStringExtractor(JsonDocument document, ILoggerFactory? loggerFactory = null)
        {
            _jsonDocument = document;
            _logger = loggerFactory?.CreateLogger<TemplateStringExtractor>() ?? (ILogger)NullLogger.Instance;
        }

        public IReadOnlyList<TemplateString> ExtractStrings()
        {
            _extractedStringIds.Clear();
            _extractedStrings.Clear();

            TraverseJsonElements(
                _jsonDocument.RootElement,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new List<TraversalRule>() { _documentRootTraversalRule });
            return _extractedStrings;
        }

        private void TraverseJsonElements(
            JsonElement element,
            string elementName,
            string? identifierPrefix,
            string key,
            string? keyPrefix,
            IEnumerable<TraversalRule> rules)
        {
            using IDisposable? _ = _logger.BeginScope(elementName);
            List<TraversalRule> complyingRules = rules.Where(r => r.AllowsTraversalOfIdentifier(elementName)).ToList();

            if (complyingRules.Count == 0)
            {
                // This identifier was filtered out.
                _logger.LogInformation(
                    "The following element in the template.json will not be included in the localizations" +
                    " because it does not match any of the rules for localizable elements: {0}",
                    identifierPrefix + "." + elementName);
                return;
            }

            JsonValueKind valueKind = element.ValueKind;
            if (valueKind == JsonValueKind.String)
            {
                ProcessStringElement(element, elementName, identifierPrefix, key, keyPrefix);
                return;
            }

            identifierPrefix = identifierPrefix + "." + elementName;
            if (valueKind == JsonValueKind.Array)
            {
                ProcessArrayElement(element, elementName, identifierPrefix, keyPrefix, complyingRules);
                return;
            }

            if (valueKind == JsonValueKind.Object)
            {
                keyPrefix = keyPrefix + "." + key;
                ProcessObjectElement(element, elementName, identifierPrefix, keyPrefix, complyingRules);
            }
        }

        private void ProcessStringElement(JsonElement element, string elementName, string? identifierPrefix, string key, string? keyPrefix)
        {
            string identifier = identifierPrefix + "." + elementName;
            if (_extractedStringIds.Contains(identifier))
            {
                // This string was already included by an earlier rule, possibly with a different key. Skip.
                _logger.LogWarning(
                    "The following element in the template.json will be skipped" +
                    " since it was already added to the list of localizable strings: {0}",
                    identifier);
                return;
            }

            string finalKey = keyPrefix == null ? key : (keyPrefix + "." + key);
            _extractedStringIds.Add(identifier);
            _extractedStrings.Add(new TemplateString(identifier, finalKey, element.GetString() ?? string.Empty));
            _logger.LogInformation("Adding into localizable strings: {0}", identifier);
        }

        private void ProcessArrayElement(JsonElement element, string elementName, string? identifierPrefix, string? keyPrefix, List<TraversalRule> complyingRules)
        {
            foreach (TraversalRule rule in complyingRules)
            {
                int childIndex = 0;
                foreach (var child in element.EnumerateArray())
                {
                    string childElementName = childIndex.ToString();
                    string childKey = (rule.KeyCreator ?? _defaultArrayKeyExtractor).CreateKey(child, childElementName, elementName, childIndex);

                    using IDisposable? __ = _logger.BeginScope(childElementName);
                    TraverseJsonElements(child, childElementName, identifierPrefix, childKey, keyPrefix, rule.ChildRules);
                    childIndex++;
                }
            }
        }

        private void ProcessObjectElement(JsonElement element, string elementName, string? identifierPrefix, string? keyPrefix, List<TraversalRule> complyingRules)
        {
            foreach (TraversalRule rule in complyingRules)
            {
                int childIndex = 0;
                foreach (JsonProperty child in element.EnumerateObject())
                {
                    string childElementName = child.Name;
                    string childKey = (rule.KeyCreator ?? _defaultObjectKeyExtractor).CreateKey(child.Value, childElementName, elementName, childIndex);

                    using IDisposable? __ = _logger.BeginScope(childElementName);
                    TraverseJsonElements(child.Value, childElementName, identifierPrefix, childKey, keyPrefix, rule.ChildRules);
                    childIndex++;
                }
            }
        }
    }
}
