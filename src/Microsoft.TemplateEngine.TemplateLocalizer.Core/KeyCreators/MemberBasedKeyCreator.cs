﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyCreators
{
    /// <summary>
    /// Creates a key for a given element using the value of one of its members.
    /// The type of the value of the member should be string.
    /// </summary>
    internal sealed class MemberBasedKeyCreator : IJsonKeyCreator
    {
        public MemberBasedKeyCreator(string memberPropertyName)
        {
            MemberPropertyName = memberPropertyName;
        }

        /// <summary>
        /// Gets the name of the property to be searched in the member list
        /// of the <see cref="JsonElement"/> that the key will be created for.
        /// </summary>
        public string MemberPropertyName { get; }

        /// <inheritdoc/>
        public string CreateKey(JsonElement element, string? elementName, string? parentElementName, int indexInParent)
        {
            if (!element.TryGetProperty(MemberPropertyName, out JsonElement keyProperty) || keyProperty.ValueKind != JsonValueKind.String)
            {
                // TODO throw?
                return string.Empty;
            }

            string key = keyProperty.GetString()?.Replace('.', '_') ?? string.Empty;
            return parentElementName == null ? key : string.Concat(parentElementName, ".", key);
        }
    }
}
