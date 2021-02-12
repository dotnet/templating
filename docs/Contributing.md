| Table of Contents |
|-------------------|
| [Getting Started](#getting-started) |
| [Coding Style](#coding-style) |
| [Branching Information](#branching-information) |

# Getting Started #
To contribute, please fork the repo and develop in a feature branch off the current [development branch](#branching-information). When you've prepared the change you wish to make, please rebase against the development branch before submitting your PR.

## Prerequisites ##

### For Windows ###

1. git (available from http://www.git-scm.com/) on the PATH.

### For Linux ###

1. git (available from http://www.git-scm.com/) on the PATH.

## Building ##

### For Windows ###
1. Run `build.cmd`

### For non-Windows ###
1. Run `build.sh`

### Changing build configurations ###
To build in `DEBUG` mode - set the environment variable `DN3B` to `DEBUG`
To build in `RELEASE` mode (default) - set the environment variable `DN3B` to `RELEASE`

## Running ##
1. Run `dotnet new3` at the command line

### Note for Windows users ###
The location `dotnet-new3.exe` gets built to will be placed at the start of the `PATH` environment variable, so it won't become available in console windows (other than the one you've built in) that are already open. To run in already open windows, you can add the `dev` directory (created during the build) to the `PATH` environment variable, or run `dotnet-new3.exe` from that directory.

### Note for non-Windows users ###
`setup.sh` attempts to create a symlink to the `dotnet-new3` executable in `/usr/local/bin/`, if the attempt to elevate to do that is denied, you can still run the `dotnet-new3` executable directly in the `dev` directory created during the build.

[Top](#top)

# Coding Style #

Most of the styling is enforced by analyzers and the rules covered by the analyzers are not listed in this section. Therefore, it is highly recommended to use an IDE with Roslyn analyzers support (such as Visual Studio or Visual Studio Code).

* We use explicit types (no usages of `var`)
* We avoid this, unless absolutely necessary.
* We use `_camelCase` for private fields.
* Use readonly where possible.
* We use PascalCasing to name all our methods, properties, constant local variables, and static readonly fields.
* We use `nameof(...)` instead of `"..."` whenever possible and relevant.
* We use [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) to make conscious decisions on the nullability of references, be more clear with our intent and reduce `NullReferenceException`s. Add `#nullable enabled` to the top of all the modified files unless:
  * Nullable reference types are already enabled for the file
  * The changes you are introducing to a file is negligable in size compared to the size of the whole file,
  * You don't have enough context on the code to make decisions on nullability of types.

Some of the analyzer rules are currently being treated as "info/suggestion"s instead of "warning"s, because we have not yet done a solution wide refactoring to comply with the rules. Although it would be most welcome, you are not required to fix any of the existing suggestions. However, any code that you introduce should be free of suggestions.

[Top](#top)

# Branching information #

| Topic | Branch |
|-------|-------|
| Development | *stabilize* |
| Next | *master* |
| Current Release | *release/2.1* |

[Top](#top)
