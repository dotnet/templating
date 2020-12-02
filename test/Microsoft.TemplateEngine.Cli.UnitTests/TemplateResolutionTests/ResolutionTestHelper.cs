using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    internal static class ResolutionTestHelper
    {
        public static ICacheTag CreateTestCacheTag(string choice, string choiceDescription = null, string defaultValue = null, string defaultIfOptionWithoutValue = null)
        {
            return new CacheTag(null,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { choice, choiceDescription }
                },
                defaultValue,
                defaultIfOptionWithoutValue);
        }

        public static ICacheTag CreateTestCacheTag(IReadOnlyList<string> choiceList, string tagDescription = null, string defaultValue = null, string defaultIfOptionWithoutValue = null)
        {
            Dictionary<string, string> choicesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string choice in choiceList)
            {
                choicesDict.Add(choice, null);
            };

            return new CacheTag(tagDescription, choicesDict, defaultValue, defaultIfOptionWithoutValue);
        }
    }
}
