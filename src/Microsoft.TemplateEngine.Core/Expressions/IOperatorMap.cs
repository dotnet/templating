using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Core.Expressions
{
    public interface IOperatorMap<TOperator, TToken>
    {
        IReadOnlyDictionary<TOperator, Func<IEvaluable, IEvaluable>> OperatorScopeLookupFactory { get; }

        IReadOnlyDictionary<TToken, TOperator> TokensToOperatorsMap { get; }

        string Decode(string value);

        string Encode(string value);
    }
}