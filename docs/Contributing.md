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

## Running ##
1. Run `dotnet new3` at the command line

[Top](#top)

# Coding Style #

Most of the styling is enforced by analyzers and the rules covered by the analyzers are not listed in this section. Therefore, it is highly recommended to use an IDE with Roslyn analyzers support (such as Visual Studio or Visual Studio Code).

* We only use var when the variable type is obvious.
* We avoid this, unless absolutely necessary.
* We use `_camelCase` for private fields.
* Use readonly where possible.
* We use PascalCasing to name all our methods, properties, constant local variables, and static readonly fields.
* We use `nameof(...)` instead of `"..."` whenever possible and relevant.
* We use [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) to make conscious decisions on the nullability of references, be more clear with our intent and reduce `NullReferenceException`s. Add `#nullable enabled` to the top of all the modified files unless:
  * Nullable reference types are already enabled for the file
  * The file is in one of the test projects
  * The changes you are introducing to the file are negligable in size compared to the size of the whole file,
  * You don't have enough context on the code to make decisions on nullability of types.

Some of the analyzer rules are currently being treated as "info/suggestion"s instead of "warning"s, because we have not yet done a solution wide refactoring to comply with the rules. Although it would be most welcome, you are not required to fix any of the existing suggestions. However, any code that you introduce should be free of suggestions.

[Top](#top)

# Branching information #

We do development in *master* branch. After a release branch is created, any new changes that should be included in that release are cherry-picked from *master*.

We follow the same versioning as https://github.com/dotnet/sdk and release branches are named after the version numbers. For instance, `release/5.0.2xx` branch ships with .Net SDK 5.0.202.

| Topic | Branch |
|-------|-------|
| Development | *master* |
| Release | *release/** |

[Top](#top)
