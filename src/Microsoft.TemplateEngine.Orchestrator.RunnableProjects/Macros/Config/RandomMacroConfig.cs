// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config
{
    internal class RandomMacroConfig : IMacroConfig
    {
        internal RandomMacroConfig(string variableName, string? dataType, int low, int? high)
        {
            DataType = dataType;
            VariableName = variableName;
            Type = "random";
            Low = low;
            High = high ?? int.MaxValue;
        }

        public string VariableName { get; private set; }

        public string Type { get; private set; }

        internal string? DataType { get; }

        internal int Low { get; private set; }

        internal int High { get; private set; }
    }
}
