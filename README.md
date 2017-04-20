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

# Available templates

You can install additional templates that can be used by `dotnet new`. See [Available templates for dotnet new](https://github.com/dotnet/templating/wiki/Available-templates-for-dotnet-new).

# What to expect when working with this repo

The instructions below enable a new command at the `dotnet` CLI, `dotnet new3`, that uses the bits and templates contained in this repo. Think of it as a "preview" version of `dotnet new` for trying out new switches, interactions and display styles before rolling them in to the product.

Commands executed against `dotnet new3` won't impact the behavior of `dotnet new`, Visual Studio for Mac, Visual Studio, nor any other environment.

# How to build, run & debug the latest

If you're authoring templates, or interested in contributing to this repo, then you're likely interested in how to use the latest version of this experience.
The steps required are outlined below.

## Aquire

- Fork this repository.
- Clone the forked repository to your local machine.
  - **master** is a build branch and does not accept contributions directly.
  - The default branch is the active development branch that accepts contributions and flows to master to produce packages.

## Build & Run

- Open up a command prompt and navigation to the root of your source code.
- Run the setup script appropriate your environment.
     - **Windows:** [setup.cmd](https://github.com/dotnet/templating/blob/master/setup.cmd)
     - **Mac/Linux**: [setup.sh](https://github.com/dotnet/templating/blob/master/setup.sh) 
- When running the setup script, the existing built-in command `dotnet new` will be preserved. A new command `dotnet new3` will be enabled which allows you to create
files with the latest Template Engine.
- That's it! Now you can run `dotnet new3`.

For example, here is the result of running `dotnet new3 --help` on a Mac (_truncated to save space here_).

```bash
$ dotnet new3 --help
Template Instantiation Commands for .NET Core CLI.

Usage: dotnet new3 [arguments] [options]

Arguments:
  template  The template to instantiate.

<truncated>
```
### Template Nuget Packages
- The build the that produces the **template Nuget packages** currently has a dependency on **Nuget.exe**.
  - Because of this, those that wish to `install` using the **template Nuget packages** will need to be on Windows in order to produce the appropriate assets. 
    
## Debugging
Debugging code requires your current `dotnet new3` session to have its active build session configured to DEBUG, and a debugger from your application of choice to be attached to the current running `dotnet new3` process. The steps required to accomplish this are outlined below.

### Notes 

- When working with the source inside Visual Studio, it is recommended you use the latest available version.

### Setup

- Open the **Microsoft.Templating.sln** solution in the application you will use to attach your debugger.
  - This solution contains the projects needed to run, modify & debug the Template Engine.
- Once your solution is loaded, open up a new command prompt and navigate to the root of your repository.
- Execute the **dn3buildmode-debug.cmd** script.
  - This will set your `dotnet new3` build to a DEBUG mode.
- Run the "setup" script to build a new `dotnet new3` session. 

### Execution

Once "setup" has completed successfully, run the following command.
```bash
dotnet new3 --debug:attach {{additonal args}}
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
