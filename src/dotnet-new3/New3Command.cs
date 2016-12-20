﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
//using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
//using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Utils;

namespace dotnet_new3
{
    public class New3Command
    {
        private static readonly string HostIdentifier = "dotnetcli";
        private static readonly Version HostVersion = typeof(Program).GetTypeInfo().Assembly.GetName().Version;
        private static DefaultTemplateEngineHost Host;

        private static void SetupInternalCommands(ExtendedCommandParser appExt)
        {
            // visible
            appExt.InternalOption("-l|--list", "--list", LocalizableStrings.ListsTemplates, CommandOptionType.NoValue);
            appExt.InternalOption("-lang|--language", "--language", LocalizableStrings.LanguageParameter, CommandOptionType.SingleValue);
            appExt.InternalOption("-n|--name", "--name", LocalizableStrings.NameOfOutput, CommandOptionType.SingleValue);
            appExt.InternalOption("-o|--output", "--output", LocalizableStrings.OutputPath, CommandOptionType.SingleValue);
            appExt.InternalOption("-h|--help", "--help", LocalizableStrings.DisplaysHelp, CommandOptionType.NoValue);

            // hidden
            appExt.HiddenInternalOption("-a|--alias", "--alias", CommandOptionType.SingleValue);
            appExt.HiddenInternalOption("-x|--extra-args", "--extra-args", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("--locale", "--locale", CommandOptionType.SingleValue);
            appExt.HiddenInternalOption("--quiet", "--quiet", CommandOptionType.NoValue);
            appExt.HiddenInternalOption("-i|--install", "--install", CommandOptionType.MultipleValue);

            // reserved but not currently used
            appExt.HiddenInternalOption("-up|--update", "--update", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("-u|--uninstall", "--uninstall", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("--skip-update-check", "--skip-update-check", CommandOptionType.NoValue);
        }

        public static int Run(string[] args)
        {
            Dictionary<Guid, Func<Type>> builtIns = new Dictionary<Guid, Func<Type>>
            {
                //{ new Guid("0C434DF7-E2CB-4DEE-B216-D7C58C8EB4B3"), () => typeof(RunnableProjectGenerator) },
                //{ new Guid("3147965A-08E5-4523-B869-02C8E9A8AAA1"), () => typeof(BalancedNestingConfig) },
                //{ new Guid("3E8BCBF0-D631-45BA-A12D-FBF1DE03AA38"), () => typeof(ConditionalConfig) },
                //{ new Guid("A1E27A4B-9608-47F1-B3B8-F70DF62DC521"), () => typeof(FlagsConfig) },
                //{ new Guid("3FAE1942-7257-4247-B44D-2DDE07CB4A4A"), () => typeof(IncludeConfig) },
                //{ new Guid("3D33B3BF-F40E-43EB-A14D-F40516F880CD"), () => typeof(RegionConfig) },
                //{ new Guid("62DB7F1F-A10E-46F0-953F-A28A03A81CD1"), () => typeof(ReplacementConfig) },
                //{ new Guid("370996FE-2943-4AED-B2F6-EC03F0B75B4A"), () => typeof(ConstantMacro) },
                //{ new Guid("BB625F71-6404-4550-98AF-B2E546F46C5F"), () => typeof(EvaluateMacro) },
                //{ new Guid("10919008-4E13-4FA8-825C-3B4DA855578E"), () => typeof(GuidMacro) },
                //{ new Guid("F2B423D7-3C23-4489-816A-41D8D2A98596"), () => typeof(NowMacro) },
                //{ new Guid("011E8DC1-8544-4360-9B40-65FD916049B7"), () => typeof(RandomMacro) },
                //{ new Guid("8A4D4937-E23F-426D-8398-3BDBD1873ADB"), () => typeof(RegexMacro) },
                //{ new Guid("B57D64E0-9B4F-4ABE-9366-711170FD5294"), () => typeof(SwitchMacro) }
            };

            // Initial host setup has the current locale. May need to be changed based on inputs.
            Host = new DefaultTemplateEngineHost(HostIdentifier, HostVersion, CultureInfo.CurrentCulture.Name, new Dictionary<string, string> { { "prefs:language", "C#" } }, builtIns.ToList());
            EngineEnvironmentSettings.Host = Host;

            ExtendedCommandParser app = new ExtendedCommandParser()
            {
                Name = "dotnet new",
                FullName = LocalizableStrings.CommandDescription
            };
            SetupInternalCommands(app);
            CommandArgument templateNames = app.Argument("template", LocalizableStrings.TemplateArgumentHelp);

            app.OnExecute(async () =>
            {
                app.ParseArgs();
                if (app.InternalParamHasValue("--extra-args"))
                {
                    app.ParseArgs(app.InternalParamValueList("--extra-args"));
                }

                if (app.RemainingParameters.ContainsKey("--debug:attach"))
                {
                    Console.ReadLine();
                }

                if (app.InternalParamHasValue("--locale"))
                {
                    string newLocale = app.InternalParamValue("--locale");
                    if (!ValidateLocaleFormat(newLocale))
                    {
                        EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.BadLocaleError, newLocale));
                        return -1;
                    }

                    Host.UpdateLocale(newLocale);
                }

                int resultCode = InitializationAndDebugging(app, out bool shouldExit);
                if (shouldExit)
                {
                    return resultCode;
                }

                string language = app.InternalParamValue("--language");
                resultCode = ParseTemplateArgs(app, templateNames.Value, language, out shouldExit);
                if (shouldExit)
                {
                    return resultCode;
                }

                resultCode = MaintenanceAndInfo(app, templateNames.Value, language, out shouldExit);
                if (shouldExit)
                {
                    return resultCode;
                }

                return await CreateTemplateAsync(app, templateNames.Value, language);
            });

            int result;
            try
            {
                using (Timing.Over("Execute"))
                {
                    result = app.Execute(args);
                }
            }
            catch (Exception ex)
            {
                AggregateException ax = ex as AggregateException;

                while (ax != null && ax.InnerExceptions.Count == 1)
                {
                    ex = ax.InnerException;
                    ax = ex as AggregateException;
                }

                Reporter.Error.WriteLine(ex.Message.Bold().Red());

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    ax = ex as AggregateException;

                    while (ax != null && ax.InnerExceptions.Count == 1)
                    {
                        ex = ax.InnerException;
                        ax = ex as AggregateException;
                    }

                    Reporter.Error.WriteLine(ex.Message.Bold().Red());
                }

                Reporter.Error.WriteLine(ex.StackTrace.Bold().Red());
                result = 1;
            }

            return result;
        }

        private static async Task<int> CreateTemplateAsync(ExtendedCommandParser app, string templateName, string language)
        {
            string nameValue = app.InternalParamValue("--name");
            string outputPath = app.InternalParamValue("--output");
            string fallbackName = new DirectoryInfo(outputPath ?? Directory.GetCurrentDirectory()).Name;
            string aliasName = app.InternalParamValue("--alias");
            bool skipUpdateCheckValue = app.InternalParamHasValue("--skip-update-check");

            // TODO: refactor alias creation out of InstantiateAsync()
            TemplateCreationResult instantiateResult = await TemplateCreator.InstantiateAsync(templateName ?? "", language, nameValue, fallbackName, outputPath, aliasName, app.AllTemplateParams, skipUpdateCheckValue);

            string resultTemplateName = string.IsNullOrEmpty(instantiateResult.TemplateFullName) ? templateName : instantiateResult.TemplateFullName;

            switch (instantiateResult.Status)
            {
                case CreationResultStatus.AliasSucceeded:
                    EngineEnvironmentSettings.Host.LogMessage(LocalizableStrings.AliasCreated);
                    ListTemplates(templateName, language);
                    break;
                case CreationResultStatus.AliasFailed:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.AliasAlreadyExists, aliasName));
                    ListTemplates(templateName, language);
                    break;
                case CreationResultStatus.CreateSucceeded:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.CreateSuccessful, resultTemplateName));
                    break;
                case CreationResultStatus.CreateFailed:
                case CreationResultStatus.TemplateNotFound:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.CreateFailed, resultTemplateName, instantiateResult.Message));
                    ListTemplates(templateName, language);
                    break;
                case CreationResultStatus.InstallSucceeded:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.InstallSuccessful, resultTemplateName));
                    break;
                case CreationResultStatus.InstallFailed:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.InstallFailed, resultTemplateName, instantiateResult.Message));
                    break;
                case CreationResultStatus.MissingMandatoryParam:
                    EngineEnvironmentSettings.Host.LogMessage(string.Format(LocalizableStrings.MissingRequiredParameter, instantiateResult.Message, resultTemplateName));
                    break;
                case CreationResultStatus.InvalidParamValues:
                    // DisplayHelp() will figure out the details on the invalid params.
                    DisplayHelp(templateName, language, app, app.AllTemplateParams);
                    break;
                default:
                    break;
            }

            return instantiateResult.ResultCode;
        }

        private static int InitializationAndDebugging(ExtendedCommandParser app, out bool shouldExit)
        {
            bool reinitFlag = app.RemainingArguments.Any(x => x == "--debug:reinit");
            if (reinitFlag)
            {
                Paths.User.FirstRunCookie.Delete();
            }

            // Note: this leaves things in a weird state. Might be related to the localized caches.
            // not sure, need to look into it.
            if (reinitFlag || app.RemainingArguments.Any(x => x == "--debug:reset-config"))
            {
                Paths.User.AliasesFile.Delete();
                Paths.User.SettingsFile.Delete();
                TemplateCache.DeleteAllLocaleCacheFiles();
                shouldExit = true;
                return 0;
            }

            if (!Paths.User.BaseDir.Exists() || !Paths.User.FirstRunCookie.Exists())
            {
                if (!app.InternalParamHasValue("--quiet"))
                {
                    Reporter.Output.WriteLine(LocalizableStrings.GettingReady);
                }

                ConfigureEnvironment();
                Paths.User.FirstRunCookie.WriteAllText("");
            }

            if (app.RemainingArguments.Any(x => x == "--debug:showconfig"))
            {
                ShowConfig();
                shouldExit = true;
                return 0;
            }

            shouldExit = false;
            return 0;
        }

        private static int ParseTemplateArgs(ExtendedCommandParser app, string templateName, string language, out bool shouldExit)
        {
            try
            {
                IReadOnlyCollection<ITemplateInfo> templates = TemplateCreator.List(templateName, language);
                if (templates.Count == 1)
                {
                    ITemplateInfo templateInfo = templates.First();

                    ITemplate template = SettingsLoader.LoadTemplate(templateInfo);
                    IParameterSet allParams = template.Generator.GetParametersForTemplate(template);
                    IReadOnlyDictionary<string, string> parameterNameMap = template.Generator.ParameterMapForTemplate(template);
                    app.SetupTemplateParameters(allParams, parameterNameMap);
                }

                // re-parse after setting up the template params
                app.ParseArgs(app.InternalParamValueList("--extra-args"));
            }
            catch (Exception ex)
            {
                Reporter.Error.WriteLine(ex.Message.Red().Bold());
                app.ShowHelp();
                shouldExit = true;
                return -1;
            }

            if (app.RemainingParameters.Any(x => !x.Key.StartsWith("--debug:")))
            {
                EngineEnvironmentSettings.Host.LogMessage(LocalizableStrings.InvalidInputSwitch);
                foreach (string flag in app.RemainingParameters.Keys)
                {
                    EngineEnvironmentSettings.Host.LogMessage($"\t{flag}");
                }

                shouldExit = true;
                return DisplayHelp(templateName, language, app, app.AllTemplateParams);
            }

            shouldExit = false;
            return 0;
        }

        private static int MaintenanceAndInfo(ExtendedCommandParser app, string templateName, string language, out bool shouldExit)
        {
            if (app.InternalParamHasValue("--list"))
            {
                ListTemplates(templateName, language);
                shouldExit = true;
                return -1;
            }

            if (app.InternalParamHasValue("--help"))
            {
                shouldExit = true;
                return DisplayHelp(templateName, language, app, app.AllTemplateParams);
            }

            if (app.InternalParamHasValue("--install"))
            {
                InstallPackages(app.InternalParamValueList("--install").ToList(), app.InternalParamHasValue("--quiet"));
                shouldExit = true;
                return 0;
            }

            if (string.IsNullOrEmpty(templateName))
            {
                ListTemplates(string.Empty, language);
                shouldExit = true;
                return -1;
            }

            shouldExit = false;
            return 0;
        }

        private static Regex _localeFormatRegex = new Regex(@"
            ^
                [a-z]{2}
                (?:-[A-Z]{2})?
            $"
            , RegexOptions.IgnorePatternWhitespace);

        private static bool ValidateLocaleFormat(string localeToCheck)
        {
            return _localeFormatRegex.IsMatch(localeToCheck);
        }

        private static void ConfigureEnvironment()
        {
            string[] packageList;

            if (Paths.Global.DefaultInstallPackageList.FileExists())
            {
                packageList = Paths.Global.DefaultInstallPackageList.ReadAllText().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (packageList.Length > 0)
                {
                    InstallPackages(packageList, true);
                }
            }

            if (Paths.Global.DefaultInstallTemplateList.FileExists())
            {
                packageList = Paths.Global.DefaultInstallTemplateList.ReadAllText().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (packageList.Length > 0)
                {
                    InstallPackages(packageList, true);
                }
            }
        }

        private static void InstallPackages(IReadOnlyList<string> packageNames, bool quiet = false)
        {
            List<string> toInstall = new List<string>();

            foreach (string package in packageNames)
            {
                string pkg = package.Trim();
                pkg = Environment.ExpandEnvironmentVariables(pkg);
                string pattern = null;

                int wildcardIndex = pkg.IndexOfAny(new[] { '*', '?' });

                if(wildcardIndex > -1)
                {
                    int lastSlashBeforeWildcard = pkg.LastIndexOfAny(new[] { '\\', '/' });
                    pattern = pkg.Substring(lastSlashBeforeWildcard + 1);
                    pkg = pkg.Substring(0, lastSlashBeforeWildcard);
                }

                try
                {
                    if (pattern != null)
                    {
                        string fullDirectory = new DirectoryInfo(pkg).FullName;
                        string fullPathGlob = Path.Combine(fullDirectory, pattern);
                        TemplateCache.Scan(fullPathGlob);
                    }
                    else if (Directory.Exists(pkg) || File.Exists(pkg))
                    {
                        string packageLocation = new DirectoryInfo(pkg).FullName;
                        TemplateCache.Scan(packageLocation);
                    }
                    else
                    {
                        EngineEnvironmentSettings.Host.OnNonCriticalError("InvalidPackageSpecification", string.Format(LocalizableStrings.BadPackageSpec, pkg), null, 0);
                    }
                }
                catch
                {
                    EngineEnvironmentSettings.Host.OnNonCriticalError("InvalidPackageSpecification", string.Format(LocalizableStrings.BadPackageSpec, pkg), null, 0);
                }
            }

            TemplateCache.WriteTemplateCaches();

            if (!quiet)
            {
                ListTemplates(string.Empty, null);
            }
        }

        private static void ListTemplates(string templateNames, string language)
        {
            IEnumerable<ITemplateInfo> results = TemplateCreator.List(templateNames, language);
            IEnumerable<IGrouping<string, ITemplateInfo>> grouped = results.GroupBy(x => x.GroupIdentity);
            EngineEnvironmentSettings.Host.TryGetHostParamDefault("prefs:language", out string defaultLanguage);

            Dictionary<ITemplateInfo, string> templatesVersusLanguages = new Dictionary<ITemplateInfo, string>();
            
            foreach(IGrouping<string, ITemplateInfo> grouping in grouped)
            {
                using (IEnumerator<ITemplateInfo> enumerator = grouping.GetEnumerator())
                {
                    enumerator.MoveNext();
                    ITemplateInfo key = enumerator.Current;
                    StringBuilder languages = new StringBuilder();
                    bool anyLangs = false;
                    if (enumerator.Current.Tags.TryGetValue("language", out string lang))
                    {
                        anyLangs = true;

                        if(string.IsNullOrEmpty(language) && string.Equals(defaultLanguage, lang, StringComparison.OrdinalIgnoreCase))
                        {
                            lang = $"[{lang}]";
                        }

                        languages.Append(lang);
                    }

                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.Tags.TryGetValue("language", out lang))
                        {
                            if (string.IsNullOrEmpty(language) && string.Equals(defaultLanguage, lang, StringComparison.OrdinalIgnoreCase))
                            {
                                lang = $"[{lang}]";
                            }

                            if (!anyLangs)
                            {
                                anyLangs = true;
                                languages.Append(lang);
                            }
                            else
                            {
                                languages.Append($", {lang}");
                            }
                        }
                    }

                    templatesVersusLanguages[key] = languages.ToString();
                }
            }

            HelpFormatter<KeyValuePair<ITemplateInfo, string>> formatter = new HelpFormatter<KeyValuePair<ITemplateInfo, string>>(templatesVersusLanguages, 6, '-', false);
            formatter.DefineColumn(t => t.Key.Name, LocalizableStrings.Templates);
            formatter.DefineColumn(t => $"[{t.Key.ShortName}]", LocalizableStrings.ShortName);
            formatter.DefineColumn(t => t.Value, LocalizableStrings.Language);
            formatter.DefineColumn(t => t.Key.Classifications != null ? string.Join("/", t.Key.Classifications) : null, LocalizableStrings.Tags);
            Reporter.Output.WriteLine(formatter.Layout());
        }

        private static void ShowConfig()
        {
            Reporter.Output.WriteLine(LocalizableStrings.CurrentConfiguration);
            Reporter.Output.WriteLine(" ");
            TableFormatter.Print(SettingsLoader.MountPoints, LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<MountPointInfo, object>>
            {
                {LocalizableStrings.MountPoints, x => x.Place},
                {LocalizableStrings.Id, x => x.MountPointId},
                {LocalizableStrings.Parent, x => x.ParentMountPointId},
                {LocalizableStrings.Factory, x => x.MountPointFactoryId}
            });

            TableFormatter.Print(SettingsLoader.Components.OfType<IMountPointFactory>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IMountPointFactory, object>>
            {
                {LocalizableStrings.MountPointFactories, x => x.Id},
                {LocalizableStrings.Type, x => x.GetType().FullName},
                {LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName}
            });

            TableFormatter.Print(SettingsLoader.Components.OfType<IGenerator>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IGenerator, object>>
            {
                {LocalizableStrings.Generators, x => x.Id},
                {LocalizableStrings.Type, x => x.GetType().FullName},
                {LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName}
            });
        }

        private static int DisplayHelp(string templateNames, string language, ExtendedCommandParser app, IReadOnlyDictionary<string, string> userParameters)
        {
            if (string.IsNullOrWhiteSpace(templateNames))
            {   // no template specified
                app.ShowHelp();
                return 0;
            }

            IReadOnlyCollection<ITemplateInfo> templates = TemplateCreator.List(templateNames, language);

            if (templates.Count > 1)
            {
                ListTemplates(templateNames, language);
                return -1;
            }
            else if (templates.Count == 1)
            {
                ITemplateInfo templateInfo = templates.First();
                return TemplateHelp(templateInfo, app, userParameters);
            }
            else
            {
                // TODO: add a message indicating no templates matched the pattern. Requires LOC coordination
                ListTemplates(string.Empty, language);
                return -1;
            }
        }

        private static int TemplateHelp(ITemplateInfo templateInfo, ExtendedCommandParser app, IReadOnlyDictionary<string, string> userParameters)
        {
            Reporter.Output.WriteLine(templateInfo.Name);
            if (!string.IsNullOrWhiteSpace(templateInfo.Author))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.Author, templateInfo.Author));
            }

            if (!string.IsNullOrWhiteSpace(templateInfo.Description))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.Description, templateInfo.Description));
            }

            ITemplate template = SettingsLoader.LoadTemplate(templateInfo);
            IParameterSet allParams = TemplateCreator.SetupDefaultParamValuesFromTemplateAndHost(template, template.DefaultName, out IList<string> defaultParamsWithInvalidValues);
            TemplateCreator.ResolveUserParameters(template, allParams, userParameters, out IList<string> userParamsWithInvalidValues);

            string additionalInfo = null;
            if (userParamsWithInvalidValues.Any())
            {
                // Lookup the input param formats - userParamsWithInvalidValues has canonical.
                IList<string> inputParamFormats = new List<string>();
                foreach(string canonical in userParamsWithInvalidValues)
                {
                    string inputFormat = app.TemplateParamInputFormat(canonical);
                    inputParamFormats.Add(inputFormat);
                }
                string badParams = string.Join(", ", inputParamFormats);

                additionalInfo = string.Format(LocalizableStrings.InvalidParameterValues, badParams, template.Name);
            }

            ParameterHelp(allParams, app, additionalInfo);

            return 0;
        }

        private static void ParameterHelp(IParameterSet allParams, ExtendedCommandParser app, string additionalInfo = null)
        {
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                Reporter.Output.WriteLine(additionalInfo);
                Reporter.Output.WriteLine();
            }

            IEnumerable<ITemplateParameter> filteredParams = allParams.ParameterDefinitions.Where(x => x.Priority != TemplateParameterPriority.Implicit);

            if (filteredParams.Any())
            {
                HelpFormatter<ITemplateParameter> formatter = new HelpFormatter<ITemplateParameter>(filteredParams, 2, null, true);

                formatter.DefineColumn(
                    param =>
                    {
                        // the key is guaranteed to exist
                        IList<string> variants = app.CanonicalToVariantsTemplateParamMap[param.Name];
                        string options = string.Join("|", variants.Reverse());
                        return "  " + options;
                    },
                    LocalizableStrings.Options
                );

                formatter.DefineColumn(delegate (ITemplateParameter param)
                {
                    StringBuilder displayValue = new StringBuilder(255);
                    displayValue.AppendLine(param.Documentation);

                    if (string.Equals(param.DataType, "choice", StringComparison.OrdinalIgnoreCase))
                    {
                        displayValue.AppendLine(string.Join(", ", param.Choices));
                    }
                    else
                    {
                        displayValue.Append(param.DataType ?? "string");
                        displayValue.AppendLine(" - " + param.Priority.ToString());
                    }

                    // display the configured value if there is one
                    string configuredValue = null;
                    if (allParams.ResolvedValues.TryGetValue(param, out object resolvedValueObject))
                    {
                        string resolvedValue = resolvedValueObject as string;

                        if (!string.IsNullOrEmpty(resolvedValue)
                            && !string.IsNullOrEmpty(param.DefaultValue)
                            && !string.Equals(param.DefaultValue, resolvedValue))
                        {
                            configuredValue = resolvedValue;
                        }
                    }

                    if (string.IsNullOrEmpty(configuredValue))
                    {
                        // this will catch when the user inputs the default value. The above deliberately skips it on the resolved values.
                        if (string.Equals(param.DataType, "bool", StringComparison.OrdinalIgnoreCase)
                            && app.TemplateParamHasValue(param.Name)
                            && string.IsNullOrEmpty(app.TemplateParamValue(param.Name)))
                        {
                            configuredValue = "true";
                        }
                        else
                        {
                            app.AllTemplateParams.TryGetValue(param.Name, out configuredValue);
                        }
                    }

                    if (! string.IsNullOrEmpty(configuredValue))
                    {
                        displayValue.AppendLine(string.Format(LocalizableStrings.ConfiguredValue, configuredValue));
                    }

                    // display the default value if there is one
                    if (!string.IsNullOrEmpty(param.DefaultValue))
                    {
                        displayValue.AppendLine(string.Format(LocalizableStrings.DefaultValue, param.DefaultValue));
                    }

                    return displayValue.ToString();
                },
                    string.Empty
                );

                Reporter.Output.WriteLine(formatter.Layout());
            }
            else
            {
                Reporter.Output.WriteLine(LocalizableStrings.NoParameters);
            }
        }
    }
}
