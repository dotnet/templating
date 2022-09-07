// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.ValueFormTests
{
    public class DefaultLowerSafeNamespaceValueFormModelTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("Ⅻ〇˙–⿻𠀀𠀁𪛕𪛖", "ⅻ〇_______")]
        [InlineData("𒁊𒁫¶ĚΘঊਇ", "___ěθঊਇ")]
        [InlineData("9heLLo", "_9hello")]
        [InlineData("broken-clock32", "broken_clock32")]
        [InlineData(";MyWord;", "_myword_")]
        [InlineData("&&*", "___")]
        [InlineData("ÇağrışımÖrüntüsü", "çağrışımörüntüsü")]
        [InlineData("number of sockets", "number_of_sockets")]
        [InlineData("НоваяПеременная", "новаяпеременная")]
        [InlineData("Company.Product", "company.product")]
        public void LowerSafeNamespaceWorksAsExpected(string input, string expected)
        {
            IValueForm model = new DefaultLowerSafeNamespaceValueFormFactory().Create("test");
            string actual = model.Process(input, new Dictionary<string, IValueForm>());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanHandleNullValue()
        {
            IValueForm model = new DefaultLowerSafeNamespaceValueFormFactory().Create("test");
            Assert.Throws<ArgumentNullException>(() => model.Process(null!, new Dictionary<string, IValueForm>()));
        }
    }
}
