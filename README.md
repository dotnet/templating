[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/templating/templating-ci?branchName=main)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=302&branchName=main) 

# Overview

This repository is the home for the .NET Core Template Engine. It contains the brains for `dotnet new` and template instantiation in Visual Studio.
When `dotnet new` is invoked, it will call the Template Engine to create the artifacts on disk.
Template Engine is a library for manipulating streams, including operations to replace values, include/exclude 
regions and process `if`, `else if`, `else` and `end if` style statements.

# Template Content Repositories

[.NET default templates](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) are located in several repositories.

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

# Template Samples

We have created a [dotnet template samples repo](https://github.com/dotnet/dotnet-template-samples), which shows how you can use
the Template Engine to create new templates. The samples are setup to be stand alone for specific examples. 

# `dotnet new`

`dotnet new` CLI is now located in [dotnet/sdk](https://github.com/dotnet/sdk/tree/main/src/Cli/Microsoft.TemplateEngine.Cli) repo.

You can create new projects with `dotnet new`, this section will briefly describe that. For more info take a look at
[Announcing .NET Core Tools Updates in VS 2017 RC](https://blogs.msdn.microsoft.com/dotnet/2017/02/07/announcing-net-core-tools-updates-in-vs-2017-rc/).

To get started let's find out what options we have by executing `dotnet new --help`. The result is pasted in the block below.

```bash
$ dotnet new mvc --help

MVC ASP.NET Web Application (C#)
Author: Microsoft
Options:
  -au|--auth           The type of authentication to use
                           None          - No authentication
                           Individual    - Individual authentication
                       Default: None

  -uld|--use-local-db  Whether or not to use LocalDB instead of SQLite
                       bool - Optional
                       Default: false

  -f|--framework
                           1.0    - Target netcoreapp1.0
                           1.1    - Target netcoreapp1.1
                       Default: 1.0
```

Let's create a new project named "MyAwesomeProject" in the "src/MyProject" directory. This project should be an ASP.NET MVC project with Individual Auth. To create that template
execute `dotnet new mvc -n MyAwesomeProject -o src/MyProject -au Individual`. Let's try that now, the result is below.

```bash
$ dotnet new mvc -n MyAwesomeProject -o src/MyProject -au Individual
The template "MVC Application" created successfully.
```

The project was successfully created on disk as expected in `src/MyProject`. From here, we can run normal `dotnet` commands like `dotnet restore` and `dotnet build`.

We have a pretty good help system built in, including template specific help (_for example `dotnet new mvc --help`_). If you're not sure the syntax please try that,
if you have any difficulties please file a new [issue](https://github.com/dotnet/templating/issues/new).

Now that we've covered the basics of using `dotnew new`, lets move on to info for template authors and contributors.

# Available templates

You can install additional templates that can be used by `dotnet new`. 
To search for the templates available on NuGet.org, use [`dotnet new --search`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new-search).
```
    dotnet new --search web
    dotnet new --search azure --type project
    dotnet new --search azure --author Microsoft
```

# What to expect when working with this repo

This repo only contains libraries and packages that are used by `dotnet new` and Visual Studio to instantiate the template. There is no UI for this libraries. 
To build, run and debug `dotnet new`, see the [instuctions in dotnet/sdk repo](https://github.com/dotnet/sdk#how-do-i-build-the-sdk).

# How to build, run & debug

Check out our [contributing](docs/Contributing.md) page to learn how you can build, run and debug.

# Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
