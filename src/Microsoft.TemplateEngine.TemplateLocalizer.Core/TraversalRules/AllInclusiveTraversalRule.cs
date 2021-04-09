﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.TemplateLocalizer.Core.KeyExtractors;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.TraversalRules
{
    /// <summary>
    /// Allows all identifiers to be traversed.
    /// </summary>
    internal sealed class AllInclusiveTraversalRule : TraversalRule
    {
        public AllInclusiveTraversalRule(IJsonKeyCreator? keyCreator = null)
            : base(keyCreator) { }

        /// <inheritdoc/>
        public override bool AllowsTraversalOfIdentifier(string identifier)
        {
            return true;
        }
    }
}
