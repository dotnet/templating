[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/templating/templating-ci?branchName=main)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=302&branchName=main) [![Join the chat at https://gitter.im/dotnet/templating](https://badges.gitter.im/dotnet/templating.svg)](https://gitter.im/dotnet/templating?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Overview

This repository is the home for the .NET Core Template Engine. It contains the brains for `dotnet new`. 
When `dotnet new` is invoked, it will call the Template Engine to create the artifacts on disk.
Template Engine is a library for manipulating streams, including operations to replace values, include/exclude 
regions and process `if`, `else if`, `else` and `end if` style statements.

# Content Repositories
* [Class Library/Console App](https://github.com/dotnet/templating/tree/main/template_feed)
* [Test projects](https://github.com/dotnet/test-templates/tree/master/template_feed)
* [ASP.NET project and items](https://github.com/aspnet/AspNetCore/tree/main/src/ProjectTemplates)

# Template Samples

We have created a [dotnet template samples repo](https://github.com/dotnet/dotnet-template-samples), which shows how you can use
the Template Engine to create new templates. The samples are setup to be stand alone for specific examples. 

# Info for `dotnet new` users

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

You can install additional templates that can be used by `dotnet new`. See [Available templates for dotnet new](https://github.com/dotnet/templating/wiki/Available-templates-for-dotnet-new).

# What to expect when working with this repo

The instructions below enable a new command at the `dotnet` CLI, `dotnet new3`, that uses the bits and templates contained in this repo. Think of it as a "preview" version of `dotnet new` for trying out new switches, interactions and display styles before rolling them in to the product.

Commands executed against `dotnet new3` won't impact the behavior of `dotnet new`, Visual Studio for Mac, Visual Studio, nor any other environment.

# How to build, run & debug

Check out our [contributing](docs/Contributing.md) page to learn how you can build, run and debug.

# Installing templates

Templates can be installed from packages in any NuGet feed, directories on the file system or ZIP type archives (zip, nupkg, vsix, etc.)
To install a new template use the command:

    dotnet .\dotnet-new3.dll -i {the path to the folder containing the template or *.nupkg file or nuget package name}
    dotnet .\dotnet-new3.dll -i "Boxed.Templates::*"
    dotnet .\dotnet-new3.dll -i <your repo location>\artifacts\packages\Debug\Shipping\Microsoft.DotNet.Common.ProjectTemplates.3.1.6.0.0-dev.nupkg

# Basic Commands
## Showing help

    dotnet .\dotnet-new3.dll --help
    dotnet .\dotnet-new3.dll -h
    dotnet .\dotnet-new3.dll

## Listing templates

    dotnet .\dotnet-new3.dll --list
    dotnet .\dotnet-new3.dll -l
    dotnet .\dotnet-new3.dll mvc -l            Lists all templates containing the text "mvc"

## Template parameter help

    dotnet .\dotnet-new3.dll mvc --help
    dotnet .\dotnet-new3.dll mvc -h

## Template creation

    dotnet .\dotnet-new3.dll MvcWebTemplate --name MyProject --output src --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN
    dotnet .\dotnet-new3.dll MvcWebTemplate -n MyProject -o src --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN

