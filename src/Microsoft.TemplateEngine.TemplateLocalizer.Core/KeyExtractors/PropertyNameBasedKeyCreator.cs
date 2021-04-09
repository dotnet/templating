﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyExtractors
{
    /// <summary>
    /// Creates a key for a given element using the name of the element.
    /// </summary>
    internal sealed class PropertyNameBasedKeyCreator : IJsonKeyCreator
    {
        /// <inheritdoc/>
        public string CreateKey(JsonElement element, string? elementName, string? parentElementName, int indexInParent)
        {
            return elementName ?? string.Empty;
        }
    }
}
