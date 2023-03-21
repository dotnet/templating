// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class InsecureNuGetFeedException : Exception
    {
        public InsecureNuGetFeedException(string message) : base(message) { }

        public InsecureNuGetFeedException(string message, IEnumerable<string> sources)
            : base(message + ", insecure sources: " + string.Join(", ", sources) + ".")
        {
            SourcesList = sources;
        }

        public InsecureNuGetFeedException(string message, Exception inner) : base(message, inner) { }

        public InsecureNuGetFeedException(string message, IEnumerable<string> sources, Exception inner)
            : base(message + ", insecure sources: " + string.Join(", ", sources) + ".", inner)
        {
            SourcesList = sources;
        }

        public IEnumerable<string>? SourcesList { get; private set; }
    }
}
