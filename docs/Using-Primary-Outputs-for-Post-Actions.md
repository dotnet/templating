## Primary outputs

Primary outputs define the list of template files for further processing. 
The primary outputs are returned by template engine API after template instantiation (`Microsoft.TemplateEngine.IDE.Bootstrapper.CreateAsync`, `Microsoft.TemplateEngine.Edge.Template.TemplateCreator.InstantiateAsync`) or dry run (`Microsoft.TemplateEngine.IDE.Bootstrapper.GetCreationEffectsAsync`).
The main use for primary outputs is to provide the list of files to perform post actions implemented by the host. `dotnet CLI` host and Visual Studio host implement the default set of post actions based on primary outputs returned by API, described in [post-action registry](https://github.com/dotnet/templating/wiki/Post-Action-Registry). In case the template files has to be used in post actions, they should be added as the primary outputs in `template.json` definition.

The template can define any number of primary outputs. Each primary output contains the following information:
- `path` - the path should contain the relative path to the file after template instantiation is done, however prior to symbol based renaming is applied.
- `condition` - if the condition evaluates to `true`, the corresponding primary output file will be generated. If `false`, the primary output is ignored. If no condition is provided for a path, the condition defaults to `true`.

Basic example:

template files:
- `MyTestProject.csproj`
- `Class.cs`

`template.json` definition
```
{
  "author": "Test Asset",
  "classifications": [ "Test Asset" ],
  "name": "BasicTemplate",
  "generatorVersions": "[1.0.0.0-*)",
  "groupIdentity": "TestAssets.BasicTemplate",
  "precedence": "100",
  "identity": "TestAssets.BasicTemplate",
  "shortName": "TestAssets.BasicTemplate",
  "tags": {
    "language": "C#",
    "type": "project"
  },
  "sourceName": "MyTestProject",
  "primaryOutputs": [
    {
      "path": "MyTestProject.csproj"        
    }
  ],
  "postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [
        { "text": "Run 'dotnet restore'" }
      ],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true,
      "args": {
        "files": [ "MyTestProject.csproj" ]
      }
    }
  ]
}
```
Note that the example above contains primary output with original name. The actual primary output returned by API, will contain the final file name after renames are applied.
Example:
```
dotnet new TestAssets.BasicTemplate --name Awesome 
```
The primary output returned by API is `Awesome.csproj` and actual file produced is `Awesome.csproj`.

## Using primary outputs with source modifiers
Source modifiers defined in `template.json` can override source or target location during template instantiation, or rename files during instantiation. It is important that primary outputs are specified correctly in combination with source modifiers.

### Changing source
When changing the source, make sure that primary output path contains final location of the file after instantiation relative to default target path (`./`)
Example:
template files:
- `/Custom/Path/MyTestProject.csproj`
- `/Custom/Path/Class.cs`

`template.json` definition
```
{
  "author": "Test Asset",
  "classifications": [ "Test Asset" ],
  "name": "TemplateWithSourceNameAndCustomSourcePath",
  "generatorVersions": "[1.0.0.0-*)",
  "groupIdentity": "TestAssets.TemplateWithSourceNameAndCustomSourcePath",
  "precedence": "100",
  "identity": "TestAssets.TemplateWithSourceNameAndCustomSourcePath",
  "shortName": "TestAssets.TemplateWithSourceNameAndCustomSourcePath",
  "sourceName": "foo",
  "sources": [
    {
      "source": "./Custom/Path/"
    }
  ],
  "primaryOutputs": [
    {
      "path": "MyTestProject.csproj"        
    }
  ],
  "postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [
        { "text": "Run 'dotnet restore'" }
      ],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true,
      "args": {
        "files": [ "Custom/Path/MyTestProject.csproj" ]
      }
    }
  ]
}
```
Note that the example above contains primary output with original name in the final location. The actual primary output returned by API, will contain the final file name after renames are applied.
Post action `files` should contain the original location in template definition.
Example:
```
dotnet new TestAssets.TemplateWithSourceNameAndCustomSourcePath --name Awesome 
```
The primary output returned by API is: `Awesome.csproj`.
The final location of the files is: `./Awesome.csproj, ./Class.cs`.

### Changing target
When changing the target for template instantiation, make sure that primary output path contains final location of the file after instantiation including target path.
Example:
template files:
- `MyTestProject.csproj`
- `Class.cs`

`template.json` definition
```
{
  "author": "Test Asset",
  "classifications": [ "Test Asset" ],
  "name": "TemplateWithSourceNameAndCustomTargetPath",
  "generatorVersions": "[1.0.0.0-*)",
  "groupIdentity": "TestAssets.TemplateWithSourceNameAndCustomTargetPath",
  "precedence": "100",
  "identity": "TestAssets.TemplateWithSourceNameAndCustomTargetPath",
  "shortName": "TestAssets.TemplateWithSourceNameAndCustomTargetPath",
  "sourceName": "foo",
  "sources": [
    {
      "target": "./Custom/Path/"
    }
  ],
  "primaryOutputs": [
    {
      "path": "/Custom/Path/MyTestProject.csproj"        
    }
  ],
  "postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [
        { "text": "Run 'dotnet restore'" }
      ],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true,
      "args": {
        "files": [ "MyTestProject.csproj" ]
      }
    }
  ]
}
```
Note that the example above contains primary output with original name in the final location. The actual primary output returned by API, will contain the final file name after renames are applied.
Post action `files` should contain the original location in template definition.
Example:
```
dotnet new TestAssets.TemplateWithSourceNameAndCustomTargetPath --name Awesome 
```
The primary output returned by API is: `/Custom/Path/Awesome.csproj`.
The final location of the files is: `./Custom/Path/Awesome.csproj, ./Custom/Path/Class.cs`.

### Changing source and target
When changing both source and target for template instantiation, make sure that primary output path contains final location of the file after instantiation including target path.
Example:
template files:
- `/Src/Custom/Path/MyTestProject.csproj`
- `/Target/Output/Class.cs`

`template.json` definition
```
{
  "author": "Test Asset",
  "classifications": [ "Test Asset" ],
  "name": "TemplateWithSourceNameAndCustomSourceAndTargetPaths",
  "generatorVersions": "[1.0.0.0-*)",
  "groupIdentity": "TestAssets.TemplateWithSourceNameAndCustomSourceAndTargetPaths",
  "precedence": "100",
  "identity": "TestAssets.TemplateWithSourceNameAndCustomSourceAndTargetPaths",
  "shortName": "TestAssets.TemplateWithSourceNameAndCustomSourceAndTargetPaths",
  "sourceName": "foo",
  "sources": [
    {
      "source": "./Src/Custom/Path/",
      "target": "./Target/Output/"
    }
  ],
  "primaryOutputs": [
    {
      "path": "Target/Output/MyTestProject.csproj"        
    }
  ],
  "postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [
        { "text": "Run 'dotnet restore'" }
      ],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true,
      "args": {
        "files": [ "Src/Custom/Path/MyTestProject.csproj" ]
      }
    }
  ]
}
```
Note that the example above contains primary output with original name in the final location. The actual primary output returned by API, will contain the final file name after renames are applied.
Post action `files` should contain the original location in template definition.
Example:
```
dotnet new TestAssets.TemplateWithSourceNameAndCustomSourceAndTargetPaths --name Awesome 
```
The primary output returned by API is: `Target/Output/Awesome.csproj`.
The final location of the files is: `./Target/Output/Awesome.csproj, .Target/Output/Class.cs`.

### Renaming
When using renames in source modifiers, make sure that primary output path contains the filename after source based file rename is applied, however before symbol based renames are applied.
Example:
template files:
- `MyTestProject.csproj`

`template.json` definition
```
{
  "author": "Test Asset",
  "classifications": [ "Test Asset" ],
  "name": "TemplateWithSourceBasedRenames",
  "generatorVersions": "[1.0.0.0-*)",
  "groupIdentity": "TestAssets.TemplateWithSourceBasedRenames",
  "precedence": "100",
  "identity": "TestAssets.TemplateWithSourceBasedRenames",
  "shortName": "TestAssets.TemplateWithSourceBasedRenames",
  "sourceName": "foo",
  "sources": [
    {
      "rename": {
        "MyTestProject.csproj": "MyFirstTestProject.csproj"
      }
    }
  ],
  "symbols": {
    "firstRename": {
      "type": "parameter",
      "dataType": "string",
      "fileRename": "First"
    }
  },
  "primaryOutputs": [
    {
      "path": "MyFirstTestProject.csproj"
    }
  ],
  "postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [
        { "text": "Run 'dotnet restore'" }
      ],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true,
      "args": {
        "files": [ "MyTestProject.csproj" ]
      }
    }
  ]
}
```
Note that the example above contains primary output with name <b>after source based rename is applied</b>. The actual primary output returned by API, contains the final file name after symbol based renames are applied. Post action `files` should contain the original location in template definition.
Example:
```
dotnet new TestAssets.TemplateWithSourceBasedRenames --firstRename Awesome 
```
The primary output returned by API is: `MyAwesomeTestProject.csproj`.
The final location of the files is: `./MyAwesomeTestProject.csproj`.