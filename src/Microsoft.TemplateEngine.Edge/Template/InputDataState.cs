// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Edge.Template;

public enum InputDataState
{
    /// <summary>
    /// Parameter is represented in input with nonempty value.
    /// </summary>
    Set,

    /// <summary>
    /// Parameter is not represented in input.
    /// </summary>
    Unset,

    /// <summary>
    /// Parameter is represented in input data with a null or empty value - this can e.g. indicate multichoice with no option selected.
    /// In CLI this is represented by option with explicit null string ('dotnet new mytemplate --myoptionA ""').
    /// In TemplateCreator this is represented by explicit null value.
    /// </summary>
    ExplicitNull
}
