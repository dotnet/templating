// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyExtractors
{
    /// <summary>
    /// Creates a key for a given element using one of its child properties.
    /// </summary>
    internal sealed class StringPropertyBasedKeyCreator : IJsonKeyCreator
    {
        public StringPropertyBasedKeyCreator(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public string CreateKey(JsonElement element, string? elementName, string? parentElementName, int indexInParent)
        {
            if (!element.TryGetProperty(PropertyName, out JsonElement keyProperty) || keyProperty.ValueKind != JsonValueKind.String)
            {
                // TODO throw?
                return string.Empty;
            }

            string key = keyProperty.GetString()?.Replace('.', '_') ?? string.Empty;
            return parentElementName == null ? key : string.Concat(parentElementName, ".", key);
        }
    }
}
