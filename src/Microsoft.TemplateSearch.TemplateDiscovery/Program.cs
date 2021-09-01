// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.TemplateSearch.TemplateDiscovery.NuGet;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;
using Microsoft.TemplateSearch.TemplateDiscovery.Results;
using Microsoft.TemplateSearch.TemplateDiscovery.Test;

namespace Microsoft.TemplateSearch.TemplateDiscovery
{
    internal class Program
    {
        private const int _defaultPageSize = 100;

        private static async Task Main(string[] args)
        {
            RootCommand rootCommand = CreateCommand();
            await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private static async Task<int> ExecuteAsync(
            DirectoryInfo basePath,
            bool allowPreviewPacks,
            int pageSize,
            bool onePage,
            bool savePacks,
            bool noTemplateJsonFilter,
            bool verbose,
            bool test,
            IEnumerable<SupportedQueries>? queries,
            DirectoryInfo? packagesPath,
            bool diffMode,
            FileInfo? overridePreviousCachePath,
            FileInfo? overrideNonPackagesListPath)
        {
            Verbose.IsEnabled = verbose;
            CommandArgs config = new CommandArgs(
                basePath,
                allowPreviewPacks,
                pageSize,
                onePage,
                savePacks,
                noTemplateJsonFilter,
                queries,
                packagesPath,
                diffMode,
                overridePreviousCachePath,
                overrideNonPackagesListPath);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                IPackCheckerFactory factory = packagesPath == null ? new NuGetPackSourceCheckerFactory() : new TestPackCheckerFactory();
                PackSourceChecker packSourceChecker = await factory.CreatePackSourceCheckerAsync(config, cts.Token).ConfigureAwait(false);
                PackSourceCheckResult checkResults = await packSourceChecker.CheckPackagesAsync(cts.Token).ConfigureAwait(false);
                (string metadataPath, string legacyMetadataPath) = PackCheckResultReportWriter.WriteResults(config.OutputPath, checkResults);
                if (test)
                {
                    CacheFileTestsBefore60.RunTests(legacyMetadataPath);
                    CacheFileTests60.RunTests(legacyMetadataPath);
                    CacheFileTests60.RunTests(metadataPath);
                }
                return 0;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Operation was cancelled.");
                return 2;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occured: {e}");
                return 1;
            }

        }

        private static RootCommand CreateCommand()
        {
            Option<DirectoryInfo> basePathOption = new Option<DirectoryInfo>("--basePath")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "The root dir for output for this run.",
                IsRequired = true
            }.LegalFilePathsOnly();

            Option<bool> allowPreviewPacksOption = new Option<bool>("--allowPreviewPacks")
            {
                Description = "Include preview packs in the results (by default, preview packs are ignored and the latest stable pack is used.",
            };

            Option<int> pageSizeOption = new Option<int>("--pageSize", getDefaultValue: () => _defaultPageSize)
            {
                Description = "(debugging) The chunk size for interactions with the source.",
            };

            Option<bool> onePageOption = new Option<bool>("--onePage")
            {
                Description = "(debugging) Only process one page of template packs.",
            };

            Option<bool> savePacksOption = new Option<bool>("--savePacks")
            {
                Description = "Don't delete downloaded candidate packs (by default, they're deleted at the end of a run).",
            };

            Option<bool> noTemplateJsonFilterOption = new Option<bool>("--noTemplateJsonFilter")
            {
                Description = "Don't prefilter packs that don't contain any template.json files (this filter is applied by default).",
            };

            Option<bool> verboseOption = new Option<bool>(new[] { "-v", "--verbose" })
            {
                Description = "Verbose output for template processing.",
            };

            Option<bool> testOption = new Option<bool>(new[] { "-t", "--test" })
            {
                Description = "Run tests on generated metadata files.",
            };

            Option<SupportedQueries[]> queriesOption = new Option<SupportedQueries[]>("--queries")
            {
                Arity = ArgumentArity.OneOrMore,
                Description = $"The list of providers to run. Supported providers: {string.Join(",", Enum.GetValues<SupportedQueries>())}.",
                AllowMultipleArgumentsPerToken = true,
            };
            queriesOption.FromAmong(Enum.GetValues<SupportedQueries>().Select(e => e.ToString()).ToArray());

            Option<DirectoryInfo> packagesPathOption = (new Option<DirectoryInfo>("--packagesPath")
            {
                Description = $"Path to pre-downloaded packages. If specified, the packages won't be downloaded from NuGet.org.",
            }.ExistingOnly());

            Option<bool> diffOption = new Option<bool>("--diff", getDefaultValue: () => true)
            {
                Description = $"The list of packages will be compared with previous run, and if package version is not changed, the package won't be rescanned.",
            };

            Option<FileInfo> diffOverrideCacheOption = new Option<FileInfo>("--diff-override-cache")
            {
                Description = $"Location of current search cache (local path only).",
            }.ExistingOnly();

            Option<FileInfo> diffOverrideNonPackagesOption = new Option<FileInfo>("--diff-override-non-packages")
            {
                Description = $"Location of the list of packages known not to be a valid package (local path only).",
            }.ExistingOnly();

            RootCommand rootCommand = new RootCommand("Generates the template package search cache file based on the packages available on NuGet.org.")
            {
                basePathOption,
                allowPreviewPacksOption,
                pageSizeOption,
                onePageOption,
                savePacksOption,
                noTemplateJsonFilterOption,
                verboseOption,
                testOption,
                queriesOption,
                packagesPathOption,
                diffOption,
                diffOverrideCacheOption,
                diffOverrideNonPackagesOption
            };

            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.Handler = CommandHandler.Create(
                basePathOption,
                allowPreviewPacksOption,
                pageSizeOption,
                onePageOption,
                savePacksOption,
                noTemplateJsonFilterOption,
                verboseOption,
                testOption,
                queriesOption,
                packagesPathOption,
                diffOption,
                diffOverrideCacheOption,
                diffOverrideNonPackagesOption,
                ExecuteAsync);

            return rootCommand;
        }
    }
}
