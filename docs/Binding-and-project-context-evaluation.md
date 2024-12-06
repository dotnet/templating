# Binding and project context evaluation

## Overview

Since .NET SDK 7.0.100 template engine supports binding of symbols to various external sources, including MSBuild properties of the current project.
The feature is available for both `dotnet new` and Visual Studio.

## `bind` symbols

The symbol binds value from external sources. 

By default, the following sources are available:
- host parameters - parameters defined by each individual Template Engine host, for example the .NET CLI, Visual Studio, and C# Dev Kit. See [Host parameters](#host-parameters) for more details.
- environment variables - allows to bind environment variables. Binding syntax is `env:<environment variable name>`, example: `env:MYENVVAR`.
- msbuild properties - allows to bind to arbitrary MSBuild Properties. Binding syntax is `msbuild:<property name>`. See [Binding to MSBuild Properties](#binding-to-msbuild-properties) for more details.

It is also possible to bind the parameter without the prefix as a fallback behavior: `HostIdentifier`, `MYENVVAR`.

The priority of the sources are following:
- host parameters: 100
- environment variables: 0

The higher value indicates higher priority.


|Name|Description|Mandatory|
|---|---|---|
|`type`|`bind`|yes|
|`binding`| Mandatory. The name of the source and parameter in the source to take the value from. The syntax follows: `<source prefix>:<parameter name>`.|yes|
|`replaces`|The text to be replaced by the symbol value in the template files content.|no|
|`fileRename`|The portion of template filenames to be replaced by the symbol value.|no| 	 
|`defaultValue`|The value assigned to the symbol if no value was provided from external source(s). Recommended to be used when `replaces` and/or `fileRename` is used. In case default value is not specified and no value was provided from external source(s) the replacement won't be performed. |no|
|`dataType`|The value assigned to the symbol if no value was provided from external source(s). Allowed values are: "bool", "float", "int", "hex", "text", "string". If not specified, the value type will be inferred. In case the type of values might be ambiguous, consider specifying the desired datatype for processing. In case the conversion of value to the type fails, the symbol will be skipped from further processing. For more information about supported data types and their restrictions, refer to [data type description](Reference-for-template.json.md#parameter-symbol) for parameter symbols. |no|
 
##### Example  

```json
"symbols": {
   "HostIdentifier": {
      "type": "bind",
      "binding": "host:HostIdentifier"
    },
   "WorkingDirectory": {
      "type": "bind",
      "binding": "host:WorkingDirectory"
    }
}
```  
### Host parameters

As mentioned above, different hosts may expose different bindable properties for you to use: 

#### .NET CLI

| Symbol Name | Default value | Meaning |
| - | - | - |
| HostIdentifier | dotnetcli | Uniquely identifies this host |
| WorkingDirectory | System.Environment.CurrentDirectory | The full path that the template engine was invoked from |
| prefs:language | C# | For templates that support multiple programming languages, which one should be chosen if no explicit choice is made |
| dotnet-cli-version | <varies> | The exact SemVer version of the .NET SDK being run for this invocation of the Template Engine - for example 8.0.404 or 9.0.100-rc.2 |
| RuntimeFrameworkVersion | <varies> | The exact SemVer version of the Microsoft.NETCore.App Runtime (aka the .NET Runtime) being used by the .NET SDK for this invocation of the Template Engine - for example 8.0.10 or 9.0.0-rc.2.24473.5 |
| NetStandardImplicitPackageVersion | <varies> | The version of the NetStandard.Library that would be inserted by the .NET SDK - this is deprecated and seems to be mostly wrong, do not use it. | 

#### Visual Studio

| Symbol Name | Default value | Meaning |
| - | - | - |
| HostIdentifier | dotnetcli | Uniquely identifies this host |
| WorkingDirectory | System.Environment.CurrentDirectory | The full path that the template engine was invoked from |

#### C# Dev Kit

TODO: add details about the Dev Kit template engine host

### Binding to MSBuild properties

It is possible to bind symbols to MSBuild properties of the current project. The prefix to be used: `msbuild` and it is mandatory.
Commonly used with item templates to get the information about the project it is added to.

`dotnet new` attempts to find the closest project file using following rules:
- The project in current directory or `--output` directory (matching `*.*proj` extension).
- If not found, the parent of above and so on.
- The path to the project can be explicitly specified using `--project` instantiation option. This path takes precedence - so it can be used in case of ambiguity.

Once project is located, its MSBuild properties are evaluated. The project should be restored, otherwise evaluation fails.
Only .NET [SDK-style projects](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/overview) are supported.

It is recommended to configure `defaultValue` for `bind` symbol that will be used in case evaluation fails. 
If applicable, it is also recommended to use [project capability constraint](https://github.com/dotnet/templating/wiki/Constraints#Project-capabilities) to define the projects that the template can be added to.

Example - binds `DefaultNamespace` symbol to `RootNamespace` of the project:
```json
"symbols": {
  "DefaultNamespace": {
    "type": "bind",
    "binding": "msbuild:RootNamespace",
    "replaces": "%NAMESPACE%",
    "defaultValue": "TestNamespace"
  }
},
"constraints": {
  "csharp-only": {
    "type": "project-capability",
    "args": "CSharp + TestContainer" // only allowed in C# test project
  }
}
```

## Visual Studio specifics

Visual Studio supports binding to host parameters, environment variables and MSBuild properties.
In addition to that, there is additional `context` source supporting:
- `context:cratesolutiondirectory` - indicates whether a solution directory is to be created as a result of project creation (Place solution and project in same directory is UNCHECKED in NPD).
- `context:isexclusive` - indicates whether the template instantiation is a result of a new project being created (true) vs result of adding to an existing solution (false).
- `context:solutionname` - the name of the solution, which may be different from the project name.

Visual Studio also provides a way to bind to "namespace" via host parameters source:
```json
  "type": "bind"
  "binding": "namespace"
```

or 

```json
  "type": "bind"
  "binding": "host:namespace"
```
