﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.NET.TestFramework.Assertions;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet_new3.IntegrationTests
{
    public class DotnetNewList : IClassFixture<SharedHomeDirectory>
    {
        private readonly SharedHomeDirectory _sharedHome;
        private readonly ITestOutputHelper _log;

        public DotnetNewList(SharedHomeDirectory sharedHome, ITestOutputHelper log)
        {
            _sharedHome = sharedHome;
            _log = log;
        }

        [Fact]
        public void BasicTest()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library");

            new DotnetNewCommand(_log, "c", "--list")
             .WithCustomHive(_sharedHome.HomeDirectory)
             .Execute()
             .Should()
             .ExitWith(0)
             .And.NotHaveStdErr()
             .And.HaveStdOutContaining("These templates matched your input: 'c'")
             .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
             .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
             .And.NotHaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
             .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library");
        }

        [Fact]
        public void CanShowAllColumns()
        {
            new DotnetNewCommand(_log, "--list", "--columns-all")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Type\\s+Author\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+project\\s+Microsoft\\s+Common/Console");
        }

        [Fact]
        public void CanFilterTags()
        {
            new DotnetNewCommand(_log, "--list", "--tag", "Common")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: tag='Common'")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.NotHaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library");

            new DotnetNewCommand(_log, "app", "--list", "--tag", "Common")
                 .WithCustomHive(_sharedHome.HomeDirectory)
                 .Execute()
                 .Should()
                 .ExitWith(0)
                 .And.NotHaveStdErr()
                 .And.HaveStdOutContaining("These templates matched your input: 'app', tag='Common'")
                 .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                 .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                 .And.NotHaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
                 .And.NotHaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library");
        }

        [Fact]
        public void CanSortByName()
        {
            const string expectedOutput =
@"Template Name                                 Short Name     Language    Tags                  
--------------------------------------------  -------------  ----------  ----------------------
ASP.NET Core Empty                            web            [C#],F#     Web/Empty             
ASP.NET Core gRPC Service                     grpc           [C#]        Web/gRPC              
ASP.NET Core Web API                          webapi         [C#],F#     Web/WebAPI            
ASP.NET Core Web App                          webapp,razor   [C#]        Web/MVC/Razor Pages   
ASP.NET Core Web App (Model-View-Controller)  mvc            [C#],F#     Web/MVC               
Blazor Server App                             blazorserver   [C#]        Web/Blazor            
Blazor WebAssembly App                        blazorwasm     [C#]        Web/Blazor/WebAssembly
Class Library                                 classlib       [C#],F#,VB  Common/Library        
Console App                                   console        [C#],F#,VB  Common/Console        
dotnet gitignore file                         gitignore                  Config                
Dotnet local tool manifest file               tool-manifest              Config                
EditorConfig file                             editorconfig               Config                
global.json file                              globaljson                 Config                
NuGet Config                                  nugetconfig                Config                
Razor Class Library                           razorclasslib  [C#]        Web/Razor/Library     
Solution File                                 sln                        Solution              
Web Config                                    webconfig                  Config                
Worker Service                                worker         [C#],F#     Common/Worker/Web     ";

            string home = TestUtils.CreateTemporaryFolder();
            Helpers.InstallNuGetTemplate("Microsoft.DotNet.Web.ProjectTemplates.5.0::5.0.0", _log, null, home);

            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(expectedOutput)
                .And.NotHaveStdErr();
        }

        [Fact]
        public void CanShowMultipleShortNames()
        {
            string home = TestUtils.CreateTemporaryFolder("Home");
            string workingDirectory = TestUtils.CreateTemporaryFolder();

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0")
                  .WithCustomHive(home)
                  .WithWorkingDirectory(workingDirectory)
                  .Execute()
                  .Should()
                  .ExitWith(0)
                  .And
                  .NotHaveStdErr()
                  .And.HaveStdOutMatching("ASP\\.NET Core Web App\\s+webapp,razor\\s+\\[C#\\]\\s+Web/MVC/Razor Pages");

            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(home)
                .WithoutBuiltInTemplates()
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("ASP\\.NET Core Web App\\s+webapp,razor\\s+\\[C#\\]\\s+Web/MVC/Razor Pages");

            new DotnetNewCommand(_log, "webapp", "--list")
                .WithCustomHive(home)
                .WithoutBuiltInTemplates()
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'webapp'")
                .And.HaveStdOutMatching("ASP\\.NET Core Web App\\s+webapp,razor\\s+\\[C#\\]\\s+Web/MVC/Razor Pages");

            new DotnetNewCommand(_log, "razor", "--list")
                .WithCustomHive(home)
                .WithoutBuiltInTemplates()
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'razor'")
                .And.HaveStdOutMatching("ASP\\.NET Core Web App\\s+webapp,razor\\s+\\[C#\\]\\s+Web/MVC/Razor Pages");
        }

        [Fact]
        public void CanFilterByChoiceParameter()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "--framework")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'c', --framework")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "-f")
              .WithCustomHive(_sharedHome.HomeDirectory)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr()
              .And.HaveStdOutContaining("These templates matched your input: 'c', -f")
              .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
              .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
              .And.NotHaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
              .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
              .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--framework")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: --framework")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "-f")
              .WithCustomHive(_sharedHome.HomeDirectory)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr()
              .And.HaveStdOutContaining("These templates matched your input: -f")
              .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
              .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
              .And.NotHaveStdOutMatching("dotnet gitignore file\\s+gitignore\\s+Config")
              .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
              .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");
        }

        [Fact]
        public void CanFilterByNonChoiceParameter()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "--langVersion")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'c', --langVersion")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--langVersion")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: --langVersion")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");
        }

        [Fact]
        public void IgnoresValueForNonChoiceParameter()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "--no-restore", "invalid")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'c', --no-restore")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--no-restore", "invalid")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: --no-restore")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");
        }

        [Fact]
        public void CanFilterByChoiceParameterWithValue()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "--framework", "net5.0")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: 'c', --framework='net5.0'")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "c", "--list", "-f", "net5.0")
              .WithCustomHive(_sharedHome.HomeDirectory)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr()
              .And.HaveStdOutContaining("These templates matched your input: 'c', -f")
              .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
              .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
              .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
              .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--framework", "net5.0")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input: --framework")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "-f", "net5.0")
              .WithCustomHive(_sharedHome.HomeDirectory)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr()
              .And.HaveStdOutContaining("These templates matched your input: -f")
              .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
              .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
              .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
              .And.NotHaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");
        }

        [Fact]
        public void CannotListTemplatesWithUnknownParameter()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: --unknown.")
                .And.HaveStdErrContaining("9 template(s) partially matched, but failed on --unknown.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 <TEMPLATE_NAME> --search");

            new DotnetNewCommand(_log, "c", "--list", "--unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: 'c', --unknown.")
                .And.HaveStdErrContaining("6 template(s) partially matched, but failed on --unknown.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 c --search");

            new DotnetNewCommand(_log, "c", "--list", "--unknown", "--language", "C#")
              .WithCustomHive(_sharedHome.HomeDirectory)
              .Execute()
              .Should().Fail()
              .And.HaveStdErrContaining("No templates found matching: 'c', language='C#', --unknown.")
              .And.HaveStdErrContaining("6 template(s) partially matched, but failed on language='C#', --unknown.")
              .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 c --search");
        }

        [Fact]
        public void CannotListTemplatesWithUnknownValueForChoiceParameter()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--framework", "unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: --framework='unknown'.")
                .And.HaveStdErrContaining("9 template(s) partially matched, but failed on --framework='unknown'.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 <TEMPLATE_NAME> --search");

            new DotnetNewCommand(_log, "c", "--list", "--framework", "unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: 'c', --framework='unknown'.")
                .And.HaveStdErrContaining("6 template(s) partially matched, but failed on --framework='unknown'.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 c --search");
        }

        [Fact]
        public void CannotListTemplatesForInvalidFilters()
        {
            new DotnetNewCommand(_log, "--list")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Template Name\\s+Short Name\\s+Language\\s+Tags")
                .And.HaveStdOutMatching("Console App\\s+console\\s+\\[C#\\],F#,VB\\s+Common/Console")
                .And.HaveStdOutMatching("Class Library\\s+classlib\\s+\\[C#\\],F#,VB\\s+Common/Library")
                .And.HaveStdOutMatching("NuGet Config\\s+nugetconfig\\s+Config");

            new DotnetNewCommand(_log, "--list", "--language", "unknown", "--framework", "unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: language='unknown'.")
                .And.HaveStdErrContaining("9 template(s) partially matched, but failed on language='unknown'.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 <TEMPLATE_NAME> --search");

            new DotnetNewCommand(_log, "c", "--list", "--language", "unknown", "--framework", "unknown")
                .WithCustomHive(_sharedHome.HomeDirectory)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("No templates found matching: 'c', language='unknown'.")
                .And.HaveStdErrContaining("6 template(s) partially matched, but failed on language='unknown'.")
                .And.HaveStdErrContaining($"To search for the templates on NuGet.org, run:{Environment.NewLine}   dotnet new3 c --search");
        }

        [Fact]
        public void TemplateGroupingTest()
        {
            string home = TestUtils.CreateTemporaryFolder("Home");
            string workingDir = TestUtils.CreateTemporaryFolder();
            Helpers.InstallTestTemplate("TemplateGrouping", _log, workingDir, home);

            new DotnetNewCommand(_log, "--list", "--columns-all")
                .WithCustomHive(home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("These templates matched your input:")
                .And.HaveStdOutMatching("Basic FSharp +template-grouping +\\[C#],F# +item +Author1 +Test Asset +\\r?\\n +Q# +(item\\,project|project\\,item) +Author2 +Test Asset");
        }

        [Theory]
        [InlineData("c --list", "--list c")]
        [InlineData("c --list --language F#", "--list c --language F#")]
        [InlineData("c --list --columns-all", "--list c --columns-all")]
        public void CanFallbackToListOption(string command1, string command2)
        {
            var commandResult1 = new DotnetNewCommand(_log, command1.Split())
             .WithCustomHive(_sharedHome.HomeDirectory)
             .Execute();

            var commandResult2 = new DotnetNewCommand(_log, command2.Split())
               .WithCustomHive(_sharedHome.HomeDirectory)
               .Execute();

            Assert.Equal(commandResult1.StdOut, commandResult2.StdOut);
        }

        [Theory]
        [InlineData("--list foo --columns-all bar", "bar", "foo")]
        [InlineData("--list foo bar", "bar", "foo")]
        [InlineData("foo --list bar", "bar", "foo", true)]
        [InlineData("foo --list bar --language F#", "bar", "foo", true)]
        [InlineData("foo --list --columns-all bar", "bar", "foo")]
        [InlineData("foo --list --columns-all --framework net6.0 bar", "bar", "foo|--framework|net6.0")]
        [InlineData("foo --list --columns-all -other-param --framework net6.0 bar", "bar", "foo|--framework|net6.0|-other-param")]
        public void CannotShowListOnParseError(string command, string invalidArguments, string validArguments, bool invalidSyntax = false)
        {
            var commandResult = new DotnetNewCommand(_log, command.Split())
             .WithCustomHive(_sharedHome.HomeDirectory)
             .Execute();

            if (invalidSyntax)
            {
                commandResult.Should().Fail().And.HaveStdErrContaining("Invalid command syntax: use 'dotnet new3 --list [PARTIAL_NAME] [FILTER_OPTIONS]' instead.");
                return;
            }

            commandResult.Should().Fail()
                .And.HaveStdErrContaining("Error: Invalid option(s):");
            foreach (string arg in invalidArguments.Split('|'))
            {
                commandResult.Should().HaveStdErrContaining(arg);
            }

            foreach (string arg in validArguments.Split('|'))
            {
                commandResult.Should().NotHaveStdErrContaining(arg);
            }
        }
    }
}
