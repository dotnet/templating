﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class AllComponents
    {
        [Fact]
        public void TestAllEdgeComponentsAdded()
        {
            var assemblyCatalog = new AssemblyComponentCatalog(new[] { typeof(Components).Assembly });

            var expectedTypeNames = assemblyCatalog.Select(pair => pair.Item1.FullName + ";" + pair.Item2.GetType().FullName).OrderBy(name => name);
            var actualTypeNames = Components.AllComponents.Select(t => t.Type.FullName + ";" + t.Instance.GetType().FullName).OrderBy(name => name);

            Assert.Equal(expectedTypeNames, actualTypeNames);
        }
    }
}
