# Contributing

* [Overview](#overview)
* [Reporting the issues](#reporting-the-issues)
	* [Identify where to report](#identify-where-to-report)
		* [Template Content Repositories](#template-content-repositories)
	* [Finding existing issues](#finding-existing-issues)
	* [Use bug report / feature request template](#use-bug-report--feature-request-template)
* [Contributing changes](#contributing-changes)
	* [Suggested workflow](#suggested-workflow)
	* [Specific guidelines](#specific-guidelines)
* [Working with the repo](#working-with-the-repo)
	* [What to expect when working with this repo](#what-to-expect-when-working-with-this-repo)
	* [Build & Run](#build--run)
	* [Debugging](#debugging)
	* [Unit testing inside virtualized environment](#unit-testing-inside-virtualized-environment)
	* [Coding Style](#coding-style)
	* [Releases and branching](#releases-and-branching)

## Overview

We welcome contributions! You can contribute by:
- [creating the issue](https://github.com/dotnet/templating/issues/new/choose) 
- [starting the discussion](https://github.com/dotnet/templating/discussions)
- contributing by [creating the PR](#contributing-changes) that fixes the issue or implements new feature

[Contributing changes](#contributing-changes) is greatly appreciated.

See our [good first issue](https://github.com/dotnet/templating/contribute) candidates for the list of issues we consider as good starting point for first contribution to the repo.

See our [help wanted](https://github.com/dotnet/templating/issues?q=is%3Aopen+is%3Aissue+label%3Ahelp-wanted) issues for a list of issues we think are great for community contribution.

We have a number of features where we are actively looking for the feedback. They are marked with [`gathering-feedback` label](https://github.com/dotnet/templating/issues?q=is%3Aissue+is%3Aopen+label%3Agathering-feedback). 
If you think they are useful for your templates, please let us know in comments or by reacting on those.

## Reporting the issues

We always welcome bug reports, feature request and overall feedback. Here are a few tips on how you can make reporting your issue as effective as possible.

### Identify where to report

The templates and templating functionality are distributed across multiple repositories in the organization. Depending on the feedback you might want to file the issue on a different repo:
- dotnet/templating - the issue with template authoring on content generation which occurs during `dotnet new` execution and/or Visual Studio. The issues with NuGet packages and APIs.
- dotnet/sdk - `dotnet new` or `dotnet` issue. If in doubt, please file issue to dotnet/templating - we can transfer the issue if the cause is `dotnet` or `dotnet new` CLI.
- existing template content / request for new templates - see [Template Content Repositories](#template-content-repositories) for template ownership. 
- issue with New Project Dialog or New Project Item in Visual Studio or Visual Studio for Mac - use [feedback ticket](https://developercommunity.visualstudio.com/home) instead.

If you have an question on how to author the template in a specific way, opt to use [discussions](https://github.com/dotnet/templating/discussions) instead. 
Also use discussions for providing the feedback.

#### Template Content Repositories

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


### Finding existing issues 

Before filing a new issue, please search [open issues](https://github.com/dotnet/templating/issues) to check if it already exists.
If you do find an existing issue, please include your own feedback in the discussion. Do consider upvoting (👍 reaction) the original post, as this helps us prioritize popular issues in our backlog.

We have a number of features where we are actively looking for the feedback. They are marked with [`gathering-feedback` label](https://github.com/dotnet/templating/issues?q=is%3Aissue+is%3Aopen+label%3Agathering-feedback). 
If you think they are useful for your templates, please let us know in comments or by reacting on those.

### Use bug report / feature request template

Good bug reports make it easier for maintainers to verify and root cause the underlying problem. The better a bug report, the faster the problem will be resolved. Ideally, a bug report should contain the following information:
- A high-level description of the problem.
- A minimal reproduction. The template that reproduces the issue is desirable.
- A description of the expected behavior, contrasted with the actual behavior observed.
- Information on the environment: OS, `dotnet --info` output, Visual Studio version.
- Additional information, e.g. is it a regression from previous versions? are there any known workarounds?

To submit the bug report, use [Bug Report](https://github.com/dotnet/templating/issues/new?assignees=&labels=&template=bug_report.yml) template.
To suggest a feature, use [Feature Request](https://github.com/dotnet/templating/issues/new?assignees=&labels=&template=feature_request.yml) template.


## Contributing changes

All contributions to dotnet/templating repo are made via pull requests (PRs) rather than through direct commits. The Wiki documentation is mirrored from `docs` repo folder and adjustments should be requested via PR changing the content of `docs` folder. The pull requests are reviewed and merged by the repository maintainers after a review and approval from at least one area maintainer. Please create the PR to `main` branch only. If the fix is intended for a specific version, please let us know in the description. 

Do not mix unrelated changes in one pull request. All changes should follow the existing [coding style](#coding-style). CI should pass.

Contributions must maintain API signature compatibility. Contributions that include breaking changes without motivation will be rejected. We use Public API analyzer to keep track of new and changed APIs.

See our [good first issue](https://github.com/dotnet/templating/contribute) candidates for the list of issues we consider as good starting point for first contribution to the repo.

See our [help wanted](https://github.com/dotnet/templating/issues?q=is%3Aopen+is%3Aissue+label%3Ahelp-wanted) issues for a list of issues we think are great for community contribution.

### Suggested workflow
1) Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - Get agreement from the team and the community that your proposed change is a good one.
2) Create a personal fork of the repository on GitHub (if you don't already have one).
3) In your fork, create a branch off of main.
    - Name the branch so that it clearly communicates your intentions, such as `issue-123`c.
    - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4) Make and commit your changes to your branch.
5) Add new tests corresponding to your change
    - Typically unit tests are located in `test/ProjectName.UnitTests` and integration end-to-end tests are located in `test/ProjectName.IntegrationTests`.
    - If changed member already has a test class, consider using it.
6) [If applicable] Update documentation in `docs` folder for your change. In particular, it is required when template authoring behavior is changed, for example new generated symbol implementation is being added.
7) Build the repository with your changes. See the information [below](#working-with-the-repo) on how to build the repo.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
8) Create a pull request (PR) against the dotnet/templating repository's main branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing.
9) Wait for feedback or approval of your changes from the area owners.
10) When area owners have signed off, and all checks are green, your PR will be merged.
    - The next official build will automatically include your change.
    - You can delete the branch you used for making the change.

### Specific guidelines
Below are the specific guidelines on how to make new implementation for generated symbol and new value form:
- [Create a new generated symbol type](./contributing/how-to-create-new-generated-symbol.md)
- [Create a new value form](./contributing/how-to-create-new-value-form.md)

## Working with the repo

### What to expect when working with this repo

This repo only contains libraries and packages that are used by `dotnet new` and Visual Studio to instantiate the template. There is no UI for these libraries. 
To build, run and debug `dotnet new`, see the [instructions in dotnet/sdk repo](https://github.com/dotnet/sdk#how-do-i-build-the-sdk).

### Build & Run

- Open up a command prompt and navigate to the root of your local repo.
- Run the build script appropriate for your environment.
     - **Windows:** [build.cmd](https://github.com/dotnet/templating/blob/main/build.cmd)
     - **Mac/Linux**: [build.sh](https://github.com/dotnet/templating/blob/main/build.sh) 

### Debugging

This repo doesn't contain an executable application anymore. We recommend to do debugging using tests.
Add your test scenario to the [test project](https://github.com/dotnet/templating/tree/main/test/Microsoft.TemplateEngine.IDE.IntegrationTests) and debug it from IDE.
To build, run and debug `dotnet new`, see the [instructions in dotnet/sdk repo](https://github.com/dotnet/sdk#how-do-i-build-the-sdk).

### Unit testing inside virtualized environment

Unit tests can be run and debugged on a local virtualized environment supported by [Visual Studio Remote Testing](https://learn.microsoft.com/en-us/visualstudio/test/remote-testing?view=vs-2022).
Initial configurations have been added for `WSL` and net 7.0 linux docker via [`testenvironments.json`](../testenvironments.json).
Upon opening the Tests Explorer the advanced environments are available in the GUI: 

![TestExplorerEnvironments](docs/TestExplorerEnvironments.png)

This readme will not discuss definitive list of details for proper setup of the environments instead we defer reader to the following information sources and warn about particular gotchas:

 * WSL runs
   * Install [WSL](https://learn.microsoft.com/en-us/windows/wsl/about).
   * Install the [distribution](https://aka.ms/wslstore) of your choice.
   * [Install .NET Runtime](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu)
 * Docker runs
   * Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
   * First run of docker scenario might need elevation ([Test project does not reference any .NET NuGet adapter](https://developercommunity.visualstudio.com/t/test-project-does-not-reference-any-net-nuget-adap/1311698) error)  
 * Third party test runners might not support this feature. Use [Visual Studio Test Explorer](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer).


### Coding Style

Most of the styling is enforced by analyzer settings in `.editorconfig` and the rules covered by the analyzers are not listed in this section. Therefore, it is highly recommended to use an IDE with Roslyn analyzers support (such as Visual Studio or Visual Studio Code).

* We only use var when the variable type is obvious.
* We avoid this, unless absolutely necessary.
* We use `_camelCase` for private fields.
* Use readonly where possible.
* We use PascalCasing to name all our methods, properties, constant local variables, and static readonly fields.
* We use `nameof(...)` instead of `"..."` whenever possible and relevant.
* We use [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) to make conscious decisions on the nullability of references, be more clear with our intent and reduce `NullReferenceException`s. Add `#nullable enabled` to the top of all the modified files unless:
  * Nullable reference types are already enabled for the file or project-wide
  * The file is in one of the test projects
  * The changes you are introducing to the file are negligible in size compared to the size of the whole file,
  * You don't have enough context on the code to make decisions on nullability of types.

Some of the analyzer rules are currently being treated as "info/suggestion"s instead of "warning"s, because we have not yet done a solution wide refactoring to comply with the rules. Although it would be most welcome, you are not required to fix any of the existing suggestions. However, any code that you introduce should be free of suggestions.

### Releases and branching

We do development in *main* branch. After a release branch is created, any new changes that should be included in that release are cherry-picked from *main*.

Template engine is released together with .NET SDK and follows .NET SDK versioning (i.e. 7.0.1xx schema). 

We follow the same branching approach as [dotnet/sdk](https://github.com/dotnet/sdk) and release branches are named after the version numbers. For instance, `release/5.0.2xx` branch ships with .NET SDK 5.0.2xx.

| Topic | Branch |
|-------|-------|
| Development | *main* |
| Release | *release/** |

[Top](#top)