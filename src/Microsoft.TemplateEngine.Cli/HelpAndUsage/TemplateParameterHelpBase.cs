// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    internal static class TemplateParameterHelpBase
    {
        // Note: This method explicitly filters out "type" and "language", in addition to other filtering.
        internal static IEnumerable<CliTemplateParameter> FilterParamsForHelp(IEnumerable<CliTemplateParameter> parameterDefinitions, HashSet<string> hiddenParams, bool showImplicitlyHiddenParams = false, bool hasPostActionScriptRunner = false, HashSet<string>? parametersToAlwaysShow = null)
        {
            IList<CliTemplateParameter> filteredParams = parameterDefinitions
                .Where(x => x.Priority != TemplateParameterPriority.Implicit
                        && !hiddenParams.Contains(x.Name)
                        && !string.Equals(x.Name, "type", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(x.Name, "language", StringComparison.OrdinalIgnoreCase)
                        && (showImplicitlyHiddenParams || x.DataType != "choice" || x.Choices?.Count > 1 || (parametersToAlwaysShow?.Contains(x.Name) ?? false))).ToList();    // for filtering "tags"

            if (hasPostActionScriptRunner)
            {
                CliTemplateParameter allowScriptsParam = new CliTemplateParameter("allow-scripts")
                {
                    Documentation = LocalizableStrings.WhetherToAllowScriptsToRun,
                    DataType = "choice",
                    DefaultValue = "prompt",
                    Choices = new Dictionary<string, ParameterChoice>()
                    {
                        { "yes", new ParameterChoice(string.Empty, LocalizableStrings.AllowScriptsYesChoice) },
                        { "no", new ParameterChoice(string.Empty, LocalizableStrings.AllowScriptsNoChoice) },
                        { "prompt", new ParameterChoice(string.Empty, LocalizableStrings.AllowScriptsPromptChoice) }
                    }
                };

                filteredParams.Add(allowScriptsParam);
            }

            return filteredParams;
        }
    }
}
