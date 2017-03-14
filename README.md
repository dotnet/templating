# Overview

This repository is the home for the .NET Core Template Engine. It contains the brains for `dotnet new`. 
When `dotnet new` is invoked, it will call the Template Engine to create the artifacts on disk.
Template Engine is a library for manipulating streams, including operations to replace values, include/exclude 
regions and process `if`, `else if`, `else` and `end if` style statements.

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

# How to build and run the latest

If you're authoring templates, or interested in contributing to this repo, then you're likely interested in how to use the latest version of this experience.
The steps required are outlined below.

Step 1: Get the SDK for your platform from [dotnet/cli](https://github.com/dotnet/cli)

Step 2: Clone this repository to your local machine

Step 3:  Run the setup script
 - **Windows:** [setup.cmd](https://github.com/dotnet/templating/blob/master/setup.cmd)
 - **Mac/Linux**: [setup.sh](https://github.com/dotnet/templating/blob/master/setup.sh) 

When running the setup script, the existing built in command `dotnet new` will be preserved. A new command `dotnet new3` will be enabled which allows you to create
files with the latest Template Engine.

Step 4: That's it! Now you can run `dotnet new3`.

For example, here is the result of running `dotnet new3 --help` on a Mac (_truncated to save space here_).

```bash
$ dotnet new3 --help
Template Instantiation Commands for .NET Core CLI.

Usage: dotnet new3 [arguments] [options]

Arguments:
  template  The template to instantiate.

<truncated>
```

# Installing templates

Templates can be installed from packages in any NuGet feed, directories on the file system or ZIP type archives (zip, nupkg, vsix, etc.)
To install a new template use the command:

    dotnet new3 -i {the path to the folder containing the templates}

# Basic Commands
## Showing help

    dotnet new3 --help
    dotnet new3 -h
    dotnet new3

## Listing templates

    dotnet new3 --list
    dotnet new3 -l
    dotnet new3 mvc -l            Lists all templates containing the text "mvc"

## Template parameter help

    dotnet new3 mvc --help
    dotnet new3 mvc -h

## Template creation

    dotnet new3 MvcWebTemplate --name MyProject --output src --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN
    dotnet new3 MvcWebTemplate -n MyProject -o src --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN

# Roadmap
* Create formal docs
* Interactive mode (i.e. interactive prompts similar to [`yo aspnet`](https://github.com/omnisharp/generator-aspnet)
* Integration with Visual Studio One ASP.NET dialog
* Integration with Visual Studio for Mac for .NET Core projects
* Integration with [`yo aspnet`](https://github.com/omnisharp/generator-aspnet)
* Template updates (both required and optional)
* Visual Studio wizard to enable community members to plug into VS as well
* Maybe: Visual Studio wizard which can display templates given a feed URL
* Suggestions welcome, please file [an issue](https://github.com/dotnet/templating/issues/new)
