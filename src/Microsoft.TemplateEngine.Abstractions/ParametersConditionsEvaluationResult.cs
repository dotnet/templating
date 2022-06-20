// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions
{
    public class ParametersConditionsEvaluationResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="ParametersConditionsEvaluationResult"/> class.
        /// </summary>
        /// <param name="disabledParameters"></param>
        /// <param name="parametersWithAlteredPriority"></param>
        public ParametersConditionsEvaluationResult(
            IReadOnlyList<ITemplateParameter> disabledParameters,
            IReadOnlyList<ITemplateParameter> parametersWithAlteredPriority)
        {
            DisabledParameters = disabledParameters;
            ParametersWithAlteredPriority = parametersWithAlteredPriority;
        }

        /// <summary>
        /// List of parameters that were disabled as a result of evaluating their conditions.
        /// </summary>
        public IReadOnlyList<ITemplateParameter> DisabledParameters { get; }

        /// <summary>
        /// List of parameters that have their priority altered as a result of evaluating their conditions.
        /// </summary>
        public IReadOnlyList<ITemplateParameter> ParametersWithAlteredPriority { get; }
    }
}
