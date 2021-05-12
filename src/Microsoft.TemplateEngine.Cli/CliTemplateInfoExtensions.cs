// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli
{
    /// <summary>
    /// The class provides ITemplateInfo extension methods.
    /// </summary>
    internal static class CliTemplateInfoExtensions
    {
        /// <summary>
        /// Gets parameter collection for <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>the enumerator to parameter collection.</returns>
        internal static IEnumerable<CliTemplateParameter> GetParameters (this ITemplateInfo template)
        {
            List<CliTemplateParameter> parameters = new List<CliTemplateParameter>();
            foreach (KeyValuePair<string, ICacheTag> tagInfo in template.Tags)
            {
                CliTemplateParameter param = new CliTemplateParameter (tagInfo.Key)
                {
                    Name = tagInfo.Key,
                    Documentation = tagInfo.Value.Description,
                    DefaultValue = tagInfo.Value.DefaultValue,
                    Choices = tagInfo.Value.Choices,
                    DataType = "choice",
                    DefaultIfOptionWithoutValue = tagInfo.Value.DefaultIfOptionWithoutValue
                };
                parameters.Add(param);
            }

            foreach (KeyValuePair<string, ICacheParameter> paramInfo in template.CacheParameters)
            {
                CliTemplateParameter param = new CliTemplateParameter (paramInfo.Key)
                {
                    Name = paramInfo.Key,
                    Documentation = paramInfo.Value.Description,
                    DataType = paramInfo.Value.DataType,
                    DefaultValue = paramInfo.Value.DefaultValue,
                    DefaultIfOptionWithoutValue = paramInfo.Value.DefaultIfOptionWithoutValue
                };
                parameters.Add(param);
            }
            return parameters;
        }
    }
}
