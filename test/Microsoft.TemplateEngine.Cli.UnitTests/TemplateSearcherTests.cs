using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateSearch.Common;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class TemplateSearcherTests : TestBase
    {
        [Fact(DisplayName = nameof(TwoSourcesAreBothSearched))]
        public void TwoSourcesAreBothSearched()
        {
            EngineEnvironmentSettings.SettingsLoader.Components.Register(typeof(MockTemplateSearchSource));
            EngineEnvironmentSettings.SettingsLoader.Components.Register(typeof(MockTemplateSearchSource));

            IList<ITemplateSearchSource> searchSources = EngineEnvironmentSettings.SettingsLoader.Components.OfType<ITemplateSearchSource>().ToList();

            Assert.Equal(2, searchSources.Count);
        }

        [Fact(DisplayName = nameof(SourcesCorrectlySearchOnName))]
        public void SourcesCorrectlySearchOnName()
        {
            MockTemplateSearchSource.ClearResultsForAllSources();
            IReadOnlyDictionary<string, Guid> sourceNameToIdMap = MockTemplateSearchSource.SetupMultipleSources(EngineEnvironmentSettings, GetMockNameSearchResults());

            const string templateName = "foo";

            TemplateSearcher searcher = new TemplateSearcher(EngineEnvironmentSettings, "C#", MockTemplateSearchHelpers.DefaultMatchFilter);
            List<IManagedTemplatesSource> existingInstalls = new List<IManagedTemplatesSource>();
            SearchResults searchResults = searcher.SearchForTemplatesAsync(existingInstalls, templateName).Result;
            Assert.True(searchResults.AnySources);
            Assert.Equal(1, searchResults.MatchesBySource.Count);
            Assert.Equal("source one", searchResults.MatchesBySource[0].SourceDisplayName);
            Assert.Equal(1, searchResults.MatchesBySource[0].PacksWithMatches.Count);
            Assert.True(searchResults.MatchesBySource[0].PacksWithMatches.ContainsKey(_fooPackInfo));

            Assert.Single(searchResults.MatchesBySource[0].PacksWithMatches[_fooPackInfo].TemplateMatches.Where(x => string.Equals(x.Info.Name, "MockFooTemplateOne")));
            Assert.Single(searchResults.MatchesBySource[0].PacksWithMatches[_fooPackInfo].TemplateMatches.Where(x => string.Equals(x.Info.Name, "MockFooTemplateTwo")));
        }

        [Fact(DisplayName = nameof(SearcherCorrectlyFiltersSpecifiedPack))]
        public void SearcherCorrectlyFiltersSpecifiedPack()
        {
            const string templateName = "foo";

            TemplateSearcher searcher = new TemplateSearcher(EngineEnvironmentSettings, "C#", MockTemplateSearchHelpers.DefaultMatchFilter);

            IReadOnlyList<IManagedTemplatesSource> packsToIgnore = new List<IManagedTemplatesSource>()
            {
                _fooPackInstallDescriptor
            };

            SearchResults searchResults = searcher.SearchForTemplatesAsync(packsToIgnore, templateName).Result;
            Assert.Equal(0, searchResults.MatchesBySource.Count);
        }

        private static readonly PackInfo _fooPackInfo = new PackInfo("fooPack", "1.0.0");
        private static readonly PackInfo _barPackInfo = new PackInfo("barPack", "2.0.0");
        private static readonly PackInfo _redPackInfo = new PackInfo("redPack", "1.1");
        private static readonly PackInfo _bluePackInfo = new PackInfo("bluePack", "2.1");
        private static readonly PackInfo _greenPackInfo = new PackInfo("greenPack", "3.0.0");

        private static readonly IManagedTemplatesSource _fooPackInstallDescriptor = new NuGetManagedTemplatesSource(null, string.Empty, null);

        private static IReadOnlyDictionary<string, IReadOnlyList<ITemplateNameSearchResult>> GetMockNameSearchResults()
        {
            Dictionary<string, IReadOnlyList<ITemplateNameSearchResult>> dataForSources = new Dictionary<string, IReadOnlyList<ITemplateNameSearchResult>>();

            List<TemplateNameSearchResult> sourceOneResults = new List<TemplateNameSearchResult>();

            ITemplateInfo sourceOneTemplateOne = new MockTemplateInfo("foo1", name: "MockFooTemplateOne", identity: "Mock.Foo.1").WithDescription("Mock Foo template one");
            TemplateNameSearchResult sourceOneResultOne = new TemplateNameSearchResult(sourceOneTemplateOne, _fooPackInfo);
            sourceOneResults.Add(sourceOneResultOne);

            ITemplateInfo sourceOneTemplateTwo = new MockTemplateInfo("foo2", name: "MockFooTemplateTwo", identity: "Mock.Foo.2").WithDescription("Mock Foo template two");
            TemplateNameSearchResult sourceOneResultTwo = new TemplateNameSearchResult(sourceOneTemplateTwo, _fooPackInfo);
            sourceOneResults.Add(sourceOneResultTwo);

            ITemplateInfo sourceOneTemplateThree = new MockTemplateInfo("bar1", name: "MockBarTemplateOne", identity: "Mock.Bar.1").WithDescription("Mock Bar template one");
            TemplateNameSearchResult sourceOneResultThree = new TemplateNameSearchResult(sourceOneTemplateThree, _barPackInfo);
            sourceOneResults.Add(sourceOneResultThree);

            dataForSources["source one"] = sourceOneResults;

            List<TemplateNameSearchResult> sourceTwoResults = new List<TemplateNameSearchResult>();

            ITemplateInfo sourceTwoTemplateOne = new MockTemplateInfo("red", name: "MockRedTemplate", identity: "Mock.Red.1").WithDescription("Mock red template");

            TemplateNameSearchResult sourceTwoResultOne = new TemplateNameSearchResult(sourceTwoTemplateOne, _redPackInfo);
            sourceTwoResults.Add(sourceTwoResultOne);

            ITemplateInfo sourceTwoTemplateTwo = new MockTemplateInfo("blue", name: "MockBlueTemplate", identity: "Mock.Blue.1").WithDescription("Mock blue template");
            TemplateNameSearchResult sourceTwoResultTwo = new TemplateNameSearchResult(sourceTwoTemplateTwo, _bluePackInfo);
            sourceTwoResults.Add(sourceTwoResultTwo);

            ITemplateInfo sourceTwoTemplateThree = new MockTemplateInfo("green", name: "MockGreenTemplate", identity: "Mock.Green.1").WithDescription("Mock green template");
            TemplateNameSearchResult sourceTwoResultThree = new TemplateNameSearchResult(sourceTwoTemplateThree, _greenPackInfo);
            sourceTwoResults.Add(sourceTwoResultThree);

            dataForSources["source two"] = sourceTwoResults;

            return dataForSources;
        }
    }
}
