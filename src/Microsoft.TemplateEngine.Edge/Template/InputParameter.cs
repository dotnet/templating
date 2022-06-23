// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public class InputParameter
    {
        public InputParameter(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Constructor for <see cref="InputParameter"/> type, that allows specification of results of external evaluation of conditions.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value">A stringified value of parameter or null for explicit unset. It's possible to indicate missing of parameter on input via <see cref="ParameterNotPassed"/> argument.</param>
        /// <param name="isEnabledConditionResult"></param>
        /// <param name="isRequiredConditionResult"></param>
        /// <param name="parameterNotPassed">
        /// If true indicates a situation that parameter was not specified on input (distinct situation from explicit null).
        ///  This would normally be achieved by not passing the parameter ate all into the <see cref="InputParametersSet"/>, however then it would not be possible
        ///  to specify the results of conditions calculations.
        /// </param>
        public InputParameter(string name, string? value, bool? isEnabledConditionResult, bool? isRequiredConditionResult, bool parameterNotPassed = false)
        {
            Name = name;
            Value = value;
            IsEnabledConditionResult = isEnabledConditionResult;
            IsRequiredConditionResult = isRequiredConditionResult;
            ParameterNotPassed = parameterNotPassed;
        }

        public string Name { get; }

        public string? Value { get; }

        public bool ParameterNotPassed { get; }

        public bool? IsEnabledConditionResult { get; }

        public bool? IsRequiredConditionResult { get; }

        public static implicit operator InputParameter(KeyValuePair<string, string?> pair) => new InputParameter(pair.Key, pair.Value);
    }
}
