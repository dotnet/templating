// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class InvalidNuGetSourceException : Exception
    {
        public InvalidNuGetSourceException(string message, IEnumerable<string> sources) : base(message)
        {
            SourcesList = sources;
        }

        public InvalidNuGetSourceException(string message, IEnumerable<string> sources, Exception inner) : base(message, inner)
        {
            SourcesList = sources;
        }

        public IEnumerable<string> SourcesList { get; private set; }
    }
}
