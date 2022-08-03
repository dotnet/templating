# Conditions

## Table of contents

* [Overview](#overview)
  * [Generated Conditions](#generated-conditions)
  * [Example](#example)
* [Choice symbols](#choice-symbols)
  * [Quoteless literals](#quoteless-literals)
  * [Multichoice symbols](#multichoice-symbols)
  * [Using Computed Conditions to work with Multichoice Symbols](#using-computed-conditions-to-work-with-multichoice-symbols)
* [Conditional parameters](#conditional-parameters)
  * [Evaluation](#evaluation)
  * [Performing evaluation externally](#performing-evaluation-externally)

## Overview

Conditions are used to drive [dynamic content genarating or replacing](Conditional-processing-and-comment-syntax.md).

Conditions use C++ style of [conditional preprocessor expressions](https://docs.microsoft.com/en-us/cpp/preprocessor/hash-if-hash-elif-hash-else-and-hash-endif-directives-c-cpp?view=msvc-170). Expressions are composed from constant literals (strings, numbers, `true`, `false`), [operators](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Core/Expressions/Cpp/Operator.cs), [symbols](https://github.com/dotnet/templating/blob/main/docs/Available-Symbols-Generators.md), brackets and whitespaces. Only single line expressions are supported. Boolean and numerical expressions are supported (nonzero value is interpreted as `true`)

[Sample conditions in source code](https://github.com/dotnet/templating/blob/main/test/Microsoft.TemplateEngine.Core.UnitTests/ConditionalTests.CStyleEvaluator.cs)

### Generated Conditions
Unlike C++ preprocessor conditions, template engine allows ability for using conditional expressions that are based on results of other expressions. Specifically [Evaluate](Available-Symbols-Generators.md#evaluate) and [Computed](Reference-for-template.json.md#computed-symbol) symbols can be leveraged for this purpose.

### Example 
(other related sample in [GeneratorTest.json](https://github.com/dotnet/templating/blob/main/test/Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests/SchemaTests/GeneratorTest.json#L82-L84)):

`template.json`:
```json
"symbols":{
    "langVersion": {
      "type": "parameter",
      "datatype": "text",
      "description": "Sets the LangVersion property in the created project file",
      "defaultValue": "",
      "replaces": "$(ProjectLanguageVersion)",
      "displayName": "Language version"
    },
    "csharp10orLater": {
      "type": "generated",
      "generator": "regexMatch",
      "datatype": "bool",
      "parameters": {
        "pattern": "^(|10\\.0|10|preview|latest|default|latestMajor)$",
        "source": "langVersion"
      }
    },
    "csharpFeature_ImplicitUsings": {
      "type": "computed",
      "value": "csharp10orLater == \"true\""
    },
}
```

`Program.cs`:
```C#
#if (!csharpFeature_ImplicitUsings)
using System;
#endif
```

## Choice symbols

### Quoteless literals

[Choice Symbol](Reference-for-template.json.md#examples) can have one of N predefined values. Those predefined values can be referenced in the conditions as quoted literals. Unquoted literals are as well supported as opt-in feature via [`enableQuotelessLiterals`](Reference-for-template.json.md#enableQuotelessLiterals). Following 2 expressions are equivalent when opted in:

`#if (PLATFORM == "Windows")`

`#if (PLATFORM == Windows)`

This allows for easier authoring of nested generated conditions.

### Multichoice symbols

Information about multi-choice symbols can be found in [Reference for `template.json`](Reference-for-template.json.md#multichoice-symbols-specifics)

Comparison to multichoice symbol results in operation checking of a presence of any value of a multichoice parameter (meaning `==` operator behaves as `contains()` operation):

`template.json`:
```json
  "symbols": {
    "Platform": {
      "type": "parameter",
      "description": "The target platform for the project.",
      "datatype": "choice",
      "allowMultipleValues": true,
      "enableQuotelessLiterals": true,
      "choices": [
        {
          "choice": "Windows",
          "description": "Windows Desktop"
        },
        {
          "choice": "WindowsPhone",
          "description": "Windows Phone"
        },
        {
          "choice": "MacOS",
          "description": "Macintosh computers"
        },
        {
          "choice": "iOS",
          "description": "iOS mobile"
        },
        {
          "choice": "android",
          "description": "android mobile"
        },
        {
          "choice": "nix",
          "description": "Linux distributions"
        }
      ],
      "defaultValue": "MacOS|iOS"
    }
}
```

`Program.cs`:
```C#
#if (Platform = MacOS)
// MacOS choice flag specified here
#endif
```

In above example if `Platform` has it's default value (`MacOS` and `iOS`) or if those 2 values are passed to the engine (e.g. via commandline: `dotnet new MyTemplate --Platform MacOS --Platform iOS`), the condition in `Program.cs` file will be evaluated as true.

Order of operands doesn't matter - `PLATFORM == Windows` evaluates identical as `Windows == PLATFORM`. Comparing 2 multichoice symbols leads to standard equality check

### Using Computed Conditions to work with Multichoice Symbols

Cases that needs evaluation of different type of condition over multichoice symbols than 'contains' (e.g. exclusive equality or membership in subset of possible values) can be achieved with slightly more involved condition - so we recommend definition of aliases via computed conditions.

#### Example:

Lets consider following multichoice symbol:

`template.json`:
```json
  "symbols": {
    "PLATFORM": {
      "type": "parameter",
      "description": "The target platform for the project.",
      "datatype": "choice",
      "allowMultipleValues": true,
      "enableQuotelessLiterals": true,
      "choices": [
        {
          "choice": "Windows",
          "description": "Windows Desktop"
        },
        {
          "choice": "WindowsPhone",
          "description": "Windows Phone"
        },
        {
          "choice": "MacOS",
          "description": "Macintosh computers"
        },
        {
          "choice": "iOS",
          "description": "iOS mobile"
        },
        {
          "choice": "android",
          "description": "android mobile"
        },
        {
          "choice": "nix",
          "description": "Linux distributions"
        }
      ],
      "defaultValue": "WindowsPhone|iOS|android"
    }
}
```

Then Checking whether platform is a mobile platform can be performed with following condition: `(PLATFORM == android || PLATFORM == iOS || PLATFORM == WindowsPhone)  && PLATFORM != Windows && PLATFORM != MacOS && PLATFORM != nix`

Checking for one and only one platform needs similarly involved condition: `PLATFORM == android && PLATFORM != iOS && PLATFORM != WindowsPhone && PLATFORM != Windows && PLATFORM != MacOS`

This is given by the fact that we do not support exclusive equality operator (in the future, if needed, we can introduce dedicated operator for that - e.g. `===`).

To simplify templates and make them more readable - following computed conditions can be defined:

`template.json`:
```json
  "symbols": {
    "IsMobile": {
      "type": "computed",
      "value": "(PLATFORM == android || PLATFORM == iOS || PLATFORM == WindowsPhone)  && PLATFORM != Windows && PLATFORM != MacOS && PLATFORM != nix"
    },
    "IsAndroidOnly": {
      "type": "computed",
      "value": "PLATFORM == android && PLATFORM != iOS && PLATFORM != WindowsPhone && PLATFORM != Windows && PLATFORM != MacOS && PLATFORM != nix"
    },
}
```

Usage can then look as following:

`Program.cs`
```C#
#if IsAndroidOnly
// This renders for android only
#elseif IsMobile
// This renders for rest of mobile platforms
#else
// This renders for desktop platforms
#endif
```

## Conditional Parameters

[Parameter symbols in template](Reference-for-template.json.md#parameter-symbol) can be specified together with optional conditions:
* [`IsEnabled Condition`](Reference-for-template.json.md#isEnabled) - overwritting presence of input parameter. If condition is specified and evaluates to false (or a `false` constant is passed), passed parameter value (if any) is ignored and processing works as if the parameter value was not passed. This includes application of [default values](Reference-for-template.json.md#default), [verification of mandatory parameters](Reference-for-template.json.md#isRequired), [conditional processing of sources](Conditional-processing-and-comment-syntax.md) and [replacements](Reference-for-template.json.md#replaces).
* [`IsRequired Condition`](Reference-for-template.json.md#isRequired) - dictates if parameter is mandatory or optional.

### Evaluation

**Input** - currently only other parameter symbols from the template configuration are supported within the parameter conditions. Any other variables are not bound and replaced (they are considered part of literal string).

**Evaluation order** - Dependencies between parameters are detected and evaluation is peformed in order that guarantees that all dependencies are evaluated prior their dependant (see [Topological Sorting](https://en.wikipedia.org/wiki/Topological_sorting) for details).

 In case of cyclic dependency the evaluation proceeds only if current input values of parameters do not lead to indeterministic result (and the cycle is indicated in warning log message). Otherwise an error is reported, indicating the cycle.

 Example `template.json` with cyclic dependency:
```json
  "symbols": {
    "A": {
      "type": "parameter",
      "datatype": "bool",
      "isEnabled": "B != false",
    },
    "B": {
      "type": "parameter",
      "datatype": "bool",
      "isEnabled": "A != true",
    }
}
```

Following input parameter values can (and will) be evaluated deterministically: `--A false --B true`

Following input parameter values cannot be evaluated deterministically (and will lead to error): `--A true --B false`

**Applying user, host and default values** - All user passed, host and default values are applied before the conditions evaluation. After the evaluation defaults are reapplied to parameters that were evaluated as optional and that do not have user/host supplied values. After this an evaluation of presence of mandatory values takes place.

### Performing evaluation externally

It is possible to supply evaluation results of parameters conditions when instantiating template via Edge API [`TemplateCreator.InstantiateAsync`](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/Template/TemplateCreator.cs#L89). Example use case is instantiation from Visual Studio host, that will leverage condition evaluator integrated within the New Project Dialog. 

This can be achieved by passing the structured [`InputDataSet`](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/Template/InputDataSet.cs) argument that is populated with [`EvaluatedInputParameterData`](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/Template/EvaluatedInputParameterData.cs) objects for evaluated parameters. 

It is currently not possible to provide just partial external evaluation - meaning that the template engine evaluates either all the parameter conditions or none. If the [`InputDataSet`](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/Template/InputDataSet.cs) collection contains at least one [`EvaluatedInputParameterData`](https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/Template/EvaluatedInputParameterData.cs) element, results of all parameter conditions are expected to be passed.

Template engine cross checks externally passed evaluations. If it encounteres mismatch between externally passed result and internal evaluation result a failed `ITemplateCreationResult` is returned from `InstantiateAsync` API. Failure is indicated by [`CondtionsEvaluationMismatch`](https://github.com/dotnet/templating/blob/6f2da67d94a86fa752e336f2611797f9483e44f9/src/Microsoft.TemplateEngine.Edge/Template/CreationResultStatus.cs#L61) in [`Status`](https://github.com/dotnet/templating/blob/6f2da67d94a86fa752e336f2611797f9483e44f9/src/Microsoft.TemplateEngine.Edge/Template/ITemplateCreationResult.cs#L41) property.