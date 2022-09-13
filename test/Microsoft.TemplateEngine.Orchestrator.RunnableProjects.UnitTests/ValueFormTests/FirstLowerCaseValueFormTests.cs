// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.ValueFormTests
{
    public class FirstLowerCaseValueFormTests
    {
        [Theory]
        [InlineData("A", "a", null)]
        [InlineData("NO", "nO", null)]
        [InlineData("NEW", "nEW", null)]
        [InlineData("", "", null)]
        [InlineData("İndigo", "indigo", "tr-TR")]
        [InlineData("Indigo", "ındigo", "tr-TR")]
        public void FirstLowerCaseWorksAsExpected(string input, string expected, string? culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                if (culture == "invariant")
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                }
                else
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);
                }
            }

            IValueForm model = new FirstLowerCaseValueFormFactory().Create("test");
            string actual = model.Process(input, new Dictionary<string, IValueForm>());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanHandleNullValue()
        {
            IValueForm model = new FirstLowerCaseValueFormFactory().Create("test");
            Assert.Throws<ArgumentNullException>(() => model.Process(null!, new Dictionary<string, IValueForm>()));
        }
    }
}
