# Overview
Template Engine is the next iteration of the replacement capabilities built in to [Template Builder](https://github.com/ligershark/template-builder) and will eventually be used as the replacement engine in [SideWaffle](https://github.com/ligershark/side-waffle), [PecanWaffle](https://github.com/ligershark/pecan-waffle) and DotNetWaffle.

Template Engine is a library for manipulating streams, including operations to replace values, include/exclude regions and process `if`/`else if`/`else`/`end if` style statements.

#Getting Started

    Step 1: Get the SDK for your platform from [dotnet/cli](https://github.com/dotnet/cli)
    Step 2: Get the source

    Windows:
      Step 3: At the command prompt, run 
              Setup.cmd
      Step 4: Done! You can now use dotnet new3 in that console session

    Other Platforms:
      Step 3: In src/dotnet-new3/project.json make sure that the runtime ID for your platform is included in the runtimes section
      Step 4: At the command prompt, run
              dotnet restore --infer-runtimes --ignore-failed-sources
      Step 5: Change the working directory to src/dotnet-new3 and run
              dotnet build -r {your RID here} -c Release
      Step 6: Change the working directory to src/Microsoft.TemplateEngine.Core and run
              dotnet build -c Release
              dotnet pack -c Release -o ../../feed
      Step 7-10: Repeat step 6 for the following directories
              src/Microsoft.TemplateEngine.Abstractions
              src/Microsoft.TemplateEngine.Runner
              src/Microsoft.TemplateEngine.Orchestrator.VsTemplates
              src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects
      Step 11: Prepend the following to the PATH environment variable
              {path to the source}/src/dotnet-new3/bin/Release/netcoreapp1.0/{your RID here}/
      Step 12: Create a directory in the root of your user profile called
              .netnew
      Step 13: Create a nuget.config file in the directory created in step 12 with the following contents
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                <packageSources>
                    <clear />
                    <!-- Add sources for templates and components here -->
                    <add key="Templates" value="{path to the source}\template_feed" />
                    <add key="Components" value="{path to the source}\feed" />
                    <add key="api.nuget.org" value="https://api.nuget.org/v3/index.json" />
                    <add key="CLI Deps" value="https://dotnet.myget.org/F/cli-deps/api/v3/index.json" />
                </packageSources>
                <packageRestore>
                    <add key="enabled" value="False" />
                    <add key="automatic" value="False" />
                </packageRestore>
            </configuration>
      Step 14: To make sure everything worked correctly, run
              dotnet new3 -c

#Installing Templates

Templates can be installed from packages in any NuGet feed, directories on the file system or ZIP type archives (zip, nupkg, vsix, etc.)
All templates are installed the command:

    dotnet new3 -i {the path to the folder containing the templates}

If installing templates from a NuGet feed, the template may specify any additional components required to instantiate it (the "generator" is the most common thing to require). The `template_feed` directory in the source contains several pre-build template packs that will automatically install the required generator (in this case, it's `Microsoft.TemplateEngine.Orchestrator.RunnableProjects` which gets placed in the `feed` directory during setup).

If installing templates from a ZIP or from a directory and a required component to make them is not already installed, run

    dotnet new3 --install-component {the ID of the NuGet package containing the required component}

#Basic Commands
##Showing help

    dotnet new3 --help
    dotnet new3 -h
    dotnet new3

##Listing templates

    dotnet new3 --list
    dotnet new3 -l
    dotnet new3 mvc -l            Lists all templates containing the text "mvc"

##Template parameter help

    dotnet new3 MvcWebTemplate --help
    dotnet new3 MvcWebTemplate -h

##Template creation

    dotnet new3 MvcWebTemplate --name MyProject --directory --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN
    dotnet new3 MvcWebTemplate -n MyProject -d --ParameterName1 Value1 --ParameterName2 Value2 ... --ParameterNameN ValueN

##Favoriting templates
Creates a shortcut (t1 in the example) for a template, after creating an alias, you can use it instead of the template name

    dotnet new3 MvcWebTemplate --alias t1
    dotnet new3 MvcWebTemplate -a t1

#Roadmap
* Integration packs for each of the Waffles
 * Given that the Waffles are already widely used, integration packs for each of the Waffles will be provided here so that no substantial changes will be required to use Template Engine
* Aliases that embed template parameters
* Easy setup for platforms other than Windows
* Cascading configuration
* Additional Operations
 * Suggestions welcome!