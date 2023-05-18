// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Core.Expressions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    internal interface IExpressionEvaluator
    {
        /// <summary>
        /// Creates the expression based on passed string.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="text">The string to be inspected and turned into expression.</param>
        /// <param name="variables">Variables to be substituted within the expression.</param>
        /// <param name="evaluableExpressionError">Error message detailing failing building evaluable expression.</param>
        /// <param name="referencedVariablesKeys">If passed (if not null) it will be populated with references to variables used within the inspected expression.</param>
        /// <returns></returns>
        IEvaluable GetEvaluableExpression(
            ILogger logger,
            string text,
            IDictionary<string, object> variables,
            out string evaluableExpressionError,
            HashSet<string>? referencedVariablesKeys);
    }
}
