// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier;

public class ScrubbersDefinition
{
    public static readonly ScrubbersDefinition Empty = new();

    public ScrubbersDefinition() { }

    public ScrubbersDefinition(Action<StringBuilder> scrubber, string? extension = null)
    {
        this.AddScrubber(scrubber, extension);
    }

    public Dictionary<string, Action<StringBuilder>> ScrubersByExtension { get; private set; } = new Dictionary<string, Action<StringBuilder>>();

    public Action<StringBuilder>? GeneralScrubber { get; private set; }

    public ScrubbersDefinition AddScrubber(Action<StringBuilder> scrubber, string? extension = null)
    {
        if (object.ReferenceEquals(this, Empty))
        {
            return new ScrubbersDefinition().AddScrubber(scrubber, extension);
        }

        if (extension == null)
        {
            GeneralScrubber += scrubber;
        }
        else
        {
            ScrubersByExtension[extension] = scrubber;
        }

        return this;
    }
}
