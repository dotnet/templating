[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/templating/templating-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=302&branchName=master) [![Join the chat at https://gitter.im/dotnet/templating](https://badges.gitter.im/dotnet/templating.svg)](https://gitter.im/dotnet/templating?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Overview

This repository is the home for the .NET Core Template Engine. It contains the brains for `dotnet new`. 
When `dotnet new` is invoked, it will call the Template Engine to create the artifacts on disk.
Template Engine is a library for manipulating streams, including operations to replace values, include/exclude 
regions and process `if`, `else if`, `else` and `end if` style statements.

# Content Repositories
* [Class Library/Console App](https://github.com/dotnet/templating/tree/master/template_feed)
* [Test projects](https://github.com/dotnet/test-templates/tree/master/template_feed)
* [ASP.NET project and items](https://github.com/aspnet/AspNetCore/tree/master/src/ProjectTemplates)

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

# How to build, run & debug the latest

If you're authoring templates, or interested in contributing to this repo, then you're likely interested in how to use the latest version of this experience.
The steps required are outlined below.

## Acquire

- Fork this repository.
- Clone the forked repository to your local machine.
  - **master** is a build branch and does not accept contributions directly.
  - The default branch is the active development branch that accepts contributions and flows to master to produce packages.

## Build & Run

- Open up a command prompt and navigation to the root of your source code.
- Run the build script appropriate for your environment.
     - **Windows:** [build.cmd](https://github.com/dotnet/templating/blob/master/build.cmd)
     - **Mac/Linux**: [build.sh](https://github.com/dotnet/templating/blob/master/build.sh) 
- When running the build script, the existing built-in command `dotnet new` will be preserved. To run `dotnet new3`, run `dotnet <your repo location>\artifacts\bin\dotnet-new3\<configuration>\<target framework>\dotnet-new3.dll` (root path to `dotnet-new3.dll` is skipped in all commands below).

For example, here is the result of running `dotnet .\dotnet-new3.dll --help` (_truncated to save space here_).

```bash
$ dotnet .\dotnet-new3.dll --help
Usage: new3 [options]

Options:
  -h, --help          Displays help for this command.
  -l, --list          Lists templates containing the specified template name. If no name is specified, lists all templates.
  -n, --name          The name for the output being created. If no name is specified, the name of the output directory is used.
...
```
After first installation there are no templates installed. See [Installing templates](#Installing-templates) on how to install the templates and [Available templates](#Available-templates) for the list of available templates.
This repository features the templates for Class Library/Console App and they are located in `<your repo location>\artifacts\packages\Debug\Shipping\Microsoft.DotNet.Common.ProjectTemplates.*.nupkg` after build run.
    
## Debugging
Debugging code requires your current `dotnet new3` session to have its active build session configured to DEBUG, and a debugger from your application of choice to be attached to the current running `dotnet new3` process. The steps required to accomplish this are outlined below.

### Notes 

- When working with the source inside Visual Studio, it is recommended you use the latest available version.

### Setup

- Open the **Microsoft.TemplatingEngine.sln** solution in the application you will use to attach your debugger.
  - This solution contains the projects needed to run, modify & debug the Template Engine.

### Execution

Run the following command.
```bash
dotnet .\dotnet-new3.dll --debug:attach {{additonal args}}
```
By supplying the `--debug:attach` argument with any other argument(s) you are running, you are triggering a ` Console.ReadLine();` request which pauses execution of the Template Engine at an early point in its execution.

Once the engine is "paused", you have the opportunity to attach a debugger to the running `dotnet new3` process. 

In the application you are using to attach a debugger...

- Open the **Microsoft.TemplateEngine.Cli.New3Command** class and locate the following function.
  - `New3Command.Run()`
- Set a breakpoint at any point after the following block of code.

```csharp
if (args.Any(x => string.Equals(x, "--debug:attach", StringComparison.Ordinal)))
{
    // This is the line that is executed when --debug:attach is passed as 
    // an argument. 
    Console.ReadLine();
}
```
- Attach the debugger to the current running 'dotnet new 3' process.
- For example, if you are using **Visual Studio** you can perform the following.
  - Execute the keyboard shortcut - `ctrl + alt + p`.
  - This will open up a dialog that allows you to search for the **dotnet-new3.exe** process.
  - Locate the desired process, select it and hit the **Attach** button.
    
Now that you have a debug session attached to your properly configured `dotnet new3` process, head back to the command line and hit `enter`.  This will trigger `Console.Readline()` to execute and your proceeding breakpoint to be hit inside the application you are using to debug. 

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

