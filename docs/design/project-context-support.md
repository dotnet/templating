## Project context support

Though some item templates exist at the moment, they are barely usable as they are missing the context of the project they are being adding to.  As the result, the simplest item template for class still cannot be implement as it is not possible to evaluate project default namespace.
This feature allows to get information about project context for .NET projects.

For evaluating project context, the separate component will be introduced. Any host may opt-in to provide this component.
Both project determination and project evaluation will be done by separate component (`IDotNetProjectContext`). The interface of the component instance TBD, however at least includes:
- path to the project file (if found) - sync
- the way to get a MSBuild property value - sync
- the way to get project capabilities - sync
- the way to get project namespace - sync. Project namespace will be evaluated as value of `RootNamespace` MSBuild property, subfolders will not be considered.

The component will be implemented by the factory that creates actual instance. Creating actual instance will be async, while retrieving the information should be done in a sync way.
The implementation of this component will be done for `dotnet CLI` only. Visual Studio may opt-in to use this component to share constraint/additional variables (preferred), or have own implementation for similar constraint.

Note: not in scope
- the way to get a MSBuild item built-in and custom metadata
- the way to get a MSBuild property metadata

## Project determination for dotnet CLI
In CLI context, the project context is not known. The project will be determined as:
- `*.*proj` file in current directory,
- if `*.*proj` file is not found in the current directory, the parent directory/ies are checked until project is found.
- if there are several `*.*proj` files, it is not possible to define the one. Additional option (`--project`) will be introduced allowing to specify the path to the project the command should be working with.

Current directory is:
- the directory defined via `--output` option
- if `--output` option is not specified, the current working directory

## Project evaluation for dotnet CLI
Project evaluation will be done using MSBuild (Microsoft.Build).
MSBuild is already available in .NET SDK.
Found project file will be loaded and evaluated using MSBuild. 

### Project capabiltiies
In addition to evaluated information, the following project capabilities will be added:
- `DotNet`, `CSharp` (for C# projects),`FSharp` (for F# projects), `VisualBasic` (for VB projects),
- `WPF` in case (`UseWPF` property is `true`)
- `WindowsForms` in case (`UseWindowsForms` property is `true` and always for non-SDK style projects)
- `COMReferences`: `'$(TargetFrameworkIdentifier)' == '.NETFramework' Or ('$(TargetFrameworkIdentifier)' == '.NETCoreApp' And '$(_TargetFrameworkVersionWithoutV)' >= '3.0'`
- `AppSettings`: `'$(TargetFrameworkIdentifier)' == '.NETFramework' Or '$(UseWPF)' == 'true' Or '$(UseWindowsForms)' == 'true'`

`MAUI` and `TestContainer` capabilities should be available from MSBuild evaluation.

The timeouts should be considered as MSBuild evaluation may take time.

## The constraint based on `ProjectCapability`
Visual Studio uses `ProjectCapability` items to determine which item templates to show for certain project. Same functionality to be implemented in template engine while constraint.
The constraint will create `IDotNetProjectContext` component to get information on project capabilities. 
If the host implements `IDotNetProjectContext`, the host may re-use same constraint or alternatively define own constraint.
For this release, the constraint will be limited only to evaluate `ProjectCapability`. For future releases, evaluating other MSBuild properties or items may be considered.

Other ideas:
- we may also support programming language constraint that will be based on project file extension (`csproj`, `fsproj`, `vbproj`). By tests, not all the projects provide equivalent capability.

## Using MSBuild properties and items as variables
Another use case for MSBuild properties and items may be to use them in conditions when processing replacements or as actual replacement value.
The following way is suggested for this purpose:

1. The template author should define which values are to be used in template by defining `bind` symbol.
The syntax follows:
```json
   "Namespace": {
      "type": "bind",
      "binding": "<source name>:<property name>",
      "replaces": "%%NAMESPACE%%"
    }

```
Only the defined values will be available to be used in as variables in further processing. For MSBuild, source name is `msbuild`.

2. The author may use defined symbol in conditions or as replacement for a value defined in `replaces`.

This will be achieved by introducing yet another component type: `IExternalParameterSource`. When evaluating `bind` parameters, template engine will be able to query those sources for parameter values.
The component will be implemented using factory that creates actual instance. Creating actual instance will be async, while retrieving the parameter value should be done in a sync way.
Potentially, other sources for `bind` parameters such as environmental variables, host specific parameters should be transformed to this approach.
`IExternalParameterSource` at least includes the following:
- `type` property that corresponds to `source name` to be defined in `bind` parameter symbol
- `GetParameterValues(params string[] parameterNames)` that returns the `string?` values for parameters with `parameterNames`. If the parameter is not available, `null` to be returned.

Notes: 
- Visual Studio already has this feature implemented via host specific parameters. It may be retained or migrated to new way. We need to ensure that the syntax is compatible though.
- evaluating bind parameters should not fail template instantiation (TBD).


Other ideas:
- consider an API for `IDotNetProjectContext` in a singleton way to avoid double evaluation for constraints and external parameters. Might be done via extending `IComponentManager` for shared instances or a separate API in `Edge`.