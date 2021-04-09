// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyExtractors;
using Microsoft.TemplateEngine.TemplateLocalizer.Core.TraversalRules;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    /// <summary>
    /// Extracts localizable strings from template.json files.
    /// </summary>
    internal sealed class TemplateStringExtractor
    {
        private readonly JsonDocument _jsonDocument;

        private readonly IJsonKeyCreator _defaultArrayKeyExtractor = new IndexBasedKeyCreator();
        private readonly IJsonKeyCreator _defaultObjectKeyExtractor = new PropertyNameBasedKeyCreator();

        private readonly List<TemplateString> _extractedStrings = new List<TemplateString>();
        private readonly HashSet<string> _extractedStringIds = new HashSet<string>();

        /// <summary>
        /// The list of rules that define which fields in the template json should be extracted to a templatestrings.json file.
        /// </summary>
        private readonly List<TraversalRule> _traversalRules = new List<TraversalRule>()
        {
            new StringFilteredTraversalRule(string.Empty).WithChildren(
                new StringFilteredTraversalRule("name"),
                new StringFilteredTraversalRule("description"),
                new StringFilteredTraversalRule("symbols").WithChild(
                    new AllInclusiveTraversalRule().WithChildren(
                        new StringFilteredTraversalRule("displayName"),
                        new StringFilteredTraversalRule("description"),
                        new StringFilteredTraversalRule("choices", new StringPropertyBasedKeyCreator("choice")).WithChild(
                            new RegexFilteredTraversalRule("[^\\.]*").WithChildren(
                                new StringFilteredTraversalRule("displayName"),
                                new StringFilteredTraversalRule("description"))))),
                new RegexFilteredTraversalRule("postActions").WithChild(
                    new AllInclusiveTraversalRule().WithChildren(
                        new StringFilteredTraversalRule("description"),
                        new RegexFilteredTraversalRule("manualInstructions\\[([0-9]+)\\]").WithChild(
                            new StringFilteredTraversalRule("text")))))
        };

        public TemplateStringExtractor(JsonDocument document)
        {
            _jsonDocument = document;
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
                _traversalRules);
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
            List<TraversalRule> complyingRules = rules.Where(r => r.AllowsTraversalOfIdentifier(elementName)).ToList();

            if (complyingRules.Count == 0)
            {
                // This identifier was filtered out.
                return;
            }

            JsonValueKind valueKind = element.ValueKind;
            if (valueKind == JsonValueKind.String)
            {
                string identifier = identifierPrefix + "." + elementName;
                if (_extractedStringIds.Contains(identifier))
                {
                    // This string was already included by an earlier rule, possibly with a different key. Skip.
                    return;
                }

                // First complying rule always has the precedence.
                TraversalRule leadingRule = complyingRules[0];
                string finalKey = keyPrefix == null ? key : (keyPrefix + "." + key);
                _extractedStringIds.Add(identifier);
                _extractedStrings.Add(new TemplateString(identifier, finalKey, element.GetString() ?? string.Empty));
                return;
            }

            identifierPrefix = identifierPrefix + "." + elementName;

            if (valueKind == JsonValueKind.Array)
            {
                foreach (TraversalRule rule in complyingRules)
                {
                    int childIndex = 0;
                    foreach (var child in element.EnumerateArray())
                    {
                        string childElementName = childIndex.ToString();
                        string childKey = (rule.KeyCreator ?? _defaultArrayKeyExtractor).CreateKey(child, childElementName, elementName, childIndex);

                        TraverseJsonElements(child, childElementName, identifierPrefix, childKey, keyPrefix, rule.ChildRules);
                        childIndex++;
                    }
                }
                return;
            }

            if (valueKind == JsonValueKind.Object)
            {
                keyPrefix = keyPrefix + "." + key;

                foreach (TraversalRule rule in complyingRules)
                {
                    int childIndex = 0;
                    foreach (JsonProperty child in element.EnumerateObject())
                    {
                        string childElementName = child.Name;
                        string childKey = (rule.KeyCreator ?? _defaultObjectKeyExtractor).CreateKey(child.Value, childElementName, elementName, childIndex);

                        TraverseJsonElements(child.Value, childElementName, identifierPrefix, childKey, keyPrefix, rule.ChildRules);
                        childIndex++;
                    }
                }
            }
        }
    }
}
