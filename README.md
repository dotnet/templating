[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/templating/templating-ci?branchName=main)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=24&branchName=main) 

* [Overview](#overview)
    * [`dotnet new`](#dotnet-new)
    * [Template Content Repositories](#template-content-repositories)
* [How to author the templates](#how-to-author-the-templates)
    * [Template Samples](#template-samples)
* [Contributing](#contributing)
* [How to build, run & debug](#how-to-build-run--debug)
* [Trademarks](#trademarks)

## Overview

This repository is the home for the .NET Template Engine. It contains the libraries for template instantiation  and template package management used in [`dotnet new`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new) and [New Project Dialog](https://learn.microsoft.com/en-us/visualstudio/ide/create-new-project?view=vs-2022) and New Item Dialog in Visual Studio and Visual Studio for Mac. The libraries are also distributed as the NuGet packages on nuget.org.

The key packages are:
|Package name| Description|
|---|---|
| `Microsoft.TemplateEngine.Edge` | The template engine infrastructure:  managing template packages, templates, components, executing template. Main API surface for the products aiming to use template engine. See ['Inside Template Engine'](docs/api/Inside-the-Template-Engine.md) article for more information. |
| `Microsoft.TemplateEngine.Abstractions` | Contains the main contracts between `Edge` and components |
| `Microsoft.TemplateEngine.Orchestrator.RunnableProjects` | The template generator based on `template.json` configuration |
| `Microsoft.TemplateSearch.Common` | Facilitates template packages search on nuget.org |
| `Microsoft.TemplateEngine.IDE` | Lightweight API overlay over `Microsoft.TemplateEngine.Edge`. |
| `Microsoft.TemplateEngine.Authoring.Tasks` | Authoring tools: MSBuild tasks for template authoring |
| `Microsoft.TemplateEngine.Authoring.CLI` | Authoring tools: dotnet CLI tool with utilities for template authoring |
| `Microsoft.TemplateEngine.Authoring.TemplateVerifier` | Authoring tools: [snapshot testing framework](docs/authoring-tools/Templates-Testing-Tooling.md) for the templates |

### `dotnet new`

`dotnet new` CLI is now located in [dotnet/sdk](https://github.com/dotnet/sdk/tree/main/src/Cli/Microsoft.TemplateEngine.Cli) repo.

The issues for `dotnet new` CLI UX should be opened in the that repository.

### Template Content Repositories

[.NET default templates](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) are not located in this repository.
The templates are located in the following repositories:

| Templates | Repository |
|---|---|
|Common project and item templates|[dotnet/sdk](https://github.com/dotnet/sdk)|
|ASP.NET and Blazor templates|[dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)|
|ASP.NET Single Page Application templates| [dotnet/spa-templates](https://github.com/dotnet/spa-templates)|
|WPF templates|[dotnet/wpf](https://github.com/dotnet/wpf)|
|Windows Forms templates|[dotnet/winforms](https://github.com/dotnet/winforms)|
|Test templates|[dotnet/test-templates](https://github.com/dotnet/test-templates)|
|MAUI templates|[dotnet/maui](https://github.com/dotnet/maui)|

Issues for the template content should be opened in the corresponding repository. 
Suggestions for new templates should be opened in closest repository from the list above.  For example, if you have a suggestion for new web template, please create an issue in [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) repository.

## How to author the templates

This repository contains a lot of useful information on how to create the templates supported by `dotnet new`, `Visual Studio` and other tools that uses template engine. 

The starting point tutorial on how to create new templates is available in [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tutorials/cli-templates-create-project-template).
More advanced information can be found in the [Wiki](https://github.com/dotnet/templating/wiki) or [docs](https://github.com/dotnet/templating/tree/main/docs) folder in the repo.

Still have a question about template authoring? Do not hesitate to [open new discussion](https://github.com/dotnet/templating/discussions) in GitHub Template Authoring.

### Authoring Tools

Besides the actual implementation of .NET Template Engine, the repo contains various tools that help to author the templates.
They are not shipped together with .NET SDK, but available on NuGet.org. More information can be found [here](docs/authoring-tools/Authoring-Tools.md)

### Template Samples

We have created [dotnet template samples](https://github.com/dotnet/templating/tree/main/dotnet-template-samples), which shows how you can use the template engine to create new templates. The samples are setup to be stand alone for specific examples. 
More documentation can be found in the [Wiki](https://github.com/dotnet/templating/wiki).

## Contributing

We welcome contributions! You can contribute by:
- [creating the issue](https://github.com/dotnet/templating/issues/new/choose) 
- [starting the discussion](https://github.com/dotnet/templating/discussions)
- contributing by creating the PR that fixes the issue or implements new feature

See our [good first issue](https://github.com/dotnet/templating/contribute) candidates for the list of issues we consider as good starting point for first contribution to the repo.

See our [help wanted](https://github.com/dotnet/templating/issues?q=is%3Aopen+is%3Aissue+label%3Ahelp-wanted) issues for a list of issues we think are great for community contribution.

We have a number of features where we are actively looking for the feedback. They are marked with [`gathering-feedback` label](https://github.com/dotnet/templating/issues?q=is%3Aissue+is%3Aopen+label%3Agathering-feedback). 
If you think they are useful for your templates, please let us know in comments or by reacting on those.

Check out our [contributing](CONTRIBUTING.md) page to learn more details.

## How to build, run & debug

Check out our [contributing](CONTRIBUTING.md#working-with-the-repo) page to learn how you can build, run and debug.

## Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
