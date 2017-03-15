﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IEncodingConfig
    {
        Encoding Encoding { get; }

        ITokenTrie LineEndings { get; }

        IReadOnlyList<IToken> VariableKeys { get; }

        IReadOnlyList<Func<object>> VariableValues { get; }

        ITokenTrie Variables { get; }

        ITokenTrie Whitespace { get; }

        ITokenTrie WhitespaceOrLineEnding { get; }

        object this[int index] { get; }
    }
}
