# Template localization 

The templates are localizable. If the template is localized for the language matching the current locale, its elements will appear in that language in UX including `dotnet new` and Visual Studio.
Localizable elements are:
- name 
- author
- description
- symbols
  - description
  - display name
  - description and display name for choices for  choice parameters
- post actions
  - description
  - manual instructions

Localization files should be put to `localize` folder inside `.template.config`. Format of localization file is JSON, there is one file per language.
Naming convention for localization files is: `templatestrings.<lang code>.json`. Language code should match name of [cultures](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.name?view=net-6.0#system-globalization-cultureinfo-name).
Content of JSON are key-value pairs, where:
- key is the reference to element of `template.json` to be localized. if the element is child, the full path using `/` delimiter should be given, example: `symbols/Framework/choices/netstandard2.1/description`.
- value is localization of the element given in the key. 

Examples of localization files can be found [here](https://github.com/dotnet/templating/tree/main/template_feed/Microsoft.DotNet.Common.ProjectTemplates.7.0/content/ClassLibrary-CSharp/.template.config/localize).

These files can be parsed by template engine when loading information about the template. Template engine API returns the information for localizable properties in language matching the current UI culture (if localization is available).

### Post action localization
Ensure to add `id` property for all post actions in `template.json` prior setting up the localization. `id` should be unique within the template. 
Without the `id` property the localization files cannot be created.
In case the post action uses more than one `manualInstructions`, `id` should be also added for each manual instruction.

## Automatic generation of localization files

The localization files can be generated automatically on the build using `LocalizeTemplates` MSBuild task from `Microsoft.TemplateEngine.Tasks` package.
The task is meant to be used in template package project and will create the JSON files after the project is build.
The task supports the configuration using following properties:
- `LocalizableTemplatesPath` - the folder containing templates to be localized. Default value: `.`.
- `TemplateLanguages` - the languages to create files for. Default value: `cs;de;en;es;fr;it;ja;ko;pl;pt-BR;ru;tr;zh-Hans;zh-Hant`.


Example of usage:
```xml
 <ItemGroup>
    <PackageReference Include="Microsoft.TemplateEngine.Tasks" Version="1.0.0.0" PrivateAssets="all" IsImplicitlyDefined="true" />
 </ItemGroup>
 <LocalizeTemplates>true</LocalizeTemplates>
 <TemplateLanguages>en;de</TemplateLanguages>
```

This example will generate localization files for English and German languages for all templates in the package.

## 1st party templates localization

Prerequisite: the repo should be onboarded for OneLocBuild to localize the template content.
See the [link](https://aka.ms/AllAboutLoc) for more details.

### Repo is using arcade

`Microsoft.TemplateEngine.Tasks` is included to `Microsoft.DotNet.Arcade.Sdk`.
To start localizing the templates, enable the following property in template package project: <UsingToolTemplateLocalizer>true</UsingToolTemplateLocalizer>. This enables generating localization JSON files on the build.
Arcade has all the configuration applied that allows to automatically detect template package project and handover JSON files for localization and receive the back.
After the translation is done, the repo receives the PR replacing original English values in generated files with localized one.

### Repo is not using arcade
`Microsoft.TemplateEngine.Tasks` may be still manually added to engineering or directly to template package project as explained above.
In addition to that, localization handover process should be changed as following:
- `LocalizeTemplates` task generates ready files to be used including `<lang code>` in filename. OneLocBuild should receive the file without language code: `templatestrings.json`. The localization pipeline should rename the `templatestrings.en.json` to `templatestrings.json` and include it to localization project JSON to be translated.
- please work with your localization developer advocate to ensure that
  - localization files are handed back in JSON format and to original location
After the translation is done, the repo should receive the PR replacing original English values in generated JSON files with localized ones. It is important to ensure that files are handed over to original location (`.template.config\localize` folder).

It is recommended to setup a way to update `Microsoft.TemplateEngine.Tasks` package version, however it is not recommended to update it on each daily build. 
The package is released with any build of dotnet/templating repo, though it might have no changes, so updating version on each build might be superfluous. It is recommended to update the version when the next preview or stable version is released.

## 3rd party templates localization

- Include `LocalizeTemplates` task from `Microsoft.TemplateEngine.Tasks` as explained above. `Microsoft.TemplateEngine.Tasks` is available on [NuGet.org](https://www.nuget.org/packages/Microsoft.TemplateEngine.Tasks).
- Control the languages to be used with `TemplateLanguages` property.
- Translate the content in generated JSON files using any suitable means prior to distributing template package.

 It is recommended to update `Microsoft.TemplateEngine.Tasks` package version when the next preview or stable version is released.