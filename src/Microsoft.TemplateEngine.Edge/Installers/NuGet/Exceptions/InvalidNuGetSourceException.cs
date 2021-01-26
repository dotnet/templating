using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
