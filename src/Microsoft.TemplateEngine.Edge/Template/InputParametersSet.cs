// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public class InputParametersSet : IEnumerable<InputParameter>
    {
        private readonly IReadOnlyList<InputParameter> _inputParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputParametersSet"/> class.
        /// </summary>
        public InputParametersSet() => _inputParameters = Array.Empty<InputParameter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InputParametersSet"/> class.
        /// </summary>
        /// <param name="inputParameters"></param>
        public InputParametersSet(IReadOnlyList<InputParameter> inputParameters) => _inputParameters = inputParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputParametersSet"/> class.
        /// </summary>
        /// <param name="dict"></param>
        public InputParametersSet(IReadOnlyDictionary<string, string?> dict)
            : this(dict.Select(p => (InputParameter)p).ToList()) { }

        /// <summary>
        /// Indicates whether the templating engine should perform the evaluation of the IsRequiredCondition and IsEnabledCondtion
        ///  on symbols or if it should rely on <see cref="InputParameter.IsEnabledConditionResult"/> and <see cref="InputParameter.IsRequiredConditionResult"/>
        ///  contained within this set.
        /// </summary>
        public bool SkipParametersConditionsEvaluation { get; set; }

        /// <summary>
        /// Checks whether this set contains parameter with given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public bool ContainsParameterWithName(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
            _inputParameters.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc/>
        public IEnumerator<InputParameter> GetEnumerator() => _inputParameters.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
