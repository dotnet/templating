using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateSearch;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
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
            IReadOnlyDictionary<string, Guid> sourceNameToIdMap = MockTemplateSearchSource.SetupMultipleSources(EngineEnvironmentSettings, GetMockNameSearchResults());
            MockNewCommandInput fooCommandInput = new MockNewCommandInput()
            {
                TemplateName = "foo"
            };

            TemplateSearcher searcher = new TemplateSearcher(EngineEnvironmentSettings, fooCommandInput, "C#");
            IReadOnlyList<TemplateSourceSearchResult> searchResults = searcher.SearchForTemplatesAsync().Result;
            Assert.Equal(1, searchResults.Count);
            Assert.Equal("source one", searchResults[0].SourceDisplayName);
            Assert.Equal(1, searchResults[0].PacksWithMatches.Count);
            Assert.True(searchResults[0].PacksWithMatches.ContainsKey(_fooPackInfo));

            Assert.Single(searchResults[0].PacksWithMatches[_fooPackInfo].TemplateMatches.Where(x => string.Equals(x.Info.Name, "MockFooTemplateOne")));
            Assert.Single(searchResults[0].PacksWithMatches[_fooPackInfo].TemplateMatches.Where(x => string.Equals(x.Info.Name, "MockFooTemplateTwo")));
        }

        [Fact(DisplayName = nameof(SearcherCorrectlyFiltersSpecifiedPack))]
        public void SearcherCorrectlyFiltersSpecifiedPack()
        {
            IReadOnlyDictionary<string, Guid> sourceNameToIdMap = MockTemplateSearchSource.SetupMultipleSources(EngineEnvironmentSettings, GetMockNameSearchResults());
            MockNewCommandInput fooCommandInput = new MockNewCommandInput()
            {
                TemplateName = "foo"
            };

            TemplateSearcher searcher = new TemplateSearcher(EngineEnvironmentSettings, fooCommandInput, "C#");
            HashSet<string> packsToIgnore = new HashSet<string>()
            {
                _fooPackInfo.Name
            };
            IReadOnlyList<TemplateSourceSearchResult> searchResults = searcher.SearchForTemplatesAsync(packsToIgnore).Result;
            Assert.Equal(0, searchResults.Count);
        }

        private static readonly PackAndVersion _fooPackInfo = new PackAndVersion("fooPack", "1.0.0");
        private static readonly PackAndVersion _barPackInfo = new PackAndVersion("barPack", "2.0.0");
        private static readonly PackAndVersion _redPackInfo = new PackAndVersion("redPack", "1.1");
        private static readonly PackAndVersion _bluePackInfo = new PackAndVersion("bluePack", "2.1");
        private static readonly PackAndVersion _greenPackInfo = new PackAndVersion("greenPack", "3.0.0");

        private static IReadOnlyDictionary<string, IReadOnlyList<ITemplateNameSearchResult>> GetMockNameSearchResults()
        {
            Dictionary<string, IReadOnlyList<ITemplateNameSearchResult>> dataForSources = new Dictionary<string, IReadOnlyList<ITemplateNameSearchResult>>();

            List<TemplateNameSearchResult> sourceOneResults = new List<TemplateNameSearchResult>();


            ITemplateInfo sourceOneTemplateOne = new MockTemplate()
            {
                Identity = "Mock.Foo.1",
                Description = "Mock Foo template one",
                Name = "MockFooTemplateOne",
                ShortName = "foo1",
            };
            TemplateNameSearchResult sourceOneResultOne = new TemplateNameSearchResult(sourceOneTemplateOne, _fooPackInfo);
            sourceOneResults.Add(sourceOneResultOne);

            ITemplateInfo sourceOneTemplateTwo = new MockTemplate()
            {
                Identity = "Mock.Foo.2",
                Description = "Mock Foo template two",
                Name = "MockFooTemplateTwo",
                ShortName = "foo2"
            };
            TemplateNameSearchResult sourceOneResultTwo = new TemplateNameSearchResult(sourceOneTemplateTwo, _fooPackInfo);
            sourceOneResults.Add(sourceOneResultTwo);

            ITemplateInfo sourceOneTemplateThree = new MockTemplate()
            {
                Identity = "Mock.Bar.1",
                Description = "Mock Bar template one",
                Name = "MockBarTemplateOne",
                ShortName = "bar1"
            };
            TemplateNameSearchResult sourceOneResultThree = new TemplateNameSearchResult(sourceOneTemplateThree, _barPackInfo);
            sourceOneResults.Add(sourceOneResultThree);

            dataForSources["source one"] = sourceOneResults;

            List<TemplateNameSearchResult> sourceTwoResults = new List<TemplateNameSearchResult>();

            ITemplateInfo sourceTwoTemplateOne = new MockTemplate()
            {
                Identity = "Mock.Red.1",
                Description = "Mock red template",
                Name = "MockRedTemplate",
                ShortName = "red",
            };
            TemplateNameSearchResult sourceTwoResultOne = new TemplateNameSearchResult(sourceTwoTemplateOne, _redPackInfo);
            sourceTwoResults.Add(sourceTwoResultOne);

            ITemplateInfo sourceTwoTemplateTwo = new MockTemplate()
            {
                Identity = "Mock.Blue.1",
                Description = "Mock blue template",
                Name = "MockBlueTemplate",
                ShortName = "blue"
            };
            TemplateNameSearchResult sourceTwoResultTwo = new TemplateNameSearchResult(sourceTwoTemplateTwo, _bluePackInfo);
            sourceTwoResults.Add(sourceTwoResultTwo);

            ITemplateInfo sourceTwoTemplateThree = new MockTemplate()
            {
                Identity = "Mock.Green.1",
                Description = "Mock green template",
                Name = "MockGreenTemplate",
                ShortName = "green"
            };
            TemplateNameSearchResult sourceTwoResultThree = new TemplateNameSearchResult(sourceTwoTemplateThree, _greenPackInfo);
            sourceTwoResults.Add(sourceTwoResultThree);

            dataForSources["source two"] = sourceTwoResults;

            return dataForSources;
        }
    }
}
