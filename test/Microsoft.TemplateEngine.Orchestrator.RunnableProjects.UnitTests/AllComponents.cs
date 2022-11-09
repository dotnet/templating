﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests
{
    public class AllComponents
    {
        [Fact]
        public void TestAllComponents()
        {
            var assemblyCatalog = new AssemblyComponentCatalog(new[] { typeof(Components).Assembly });

            IOrderedEnumerable<string> expectedTypeNames = assemblyCatalog
                .Where(pair => !pair.Item1.IsGenericType)
                //obsolete type kept for backward compatibiility.
#pragma warning disable CS0618 // Type or member is obsolete
                .Where(pair => pair.Item1 != typeof(IDeferredMacro))
#pragma warning restore CS0618 // Type or member is obsolete
                .Select(pair => pair.Item1.FullName + ";" + pair.Item2.GetType().FullName)
                .OrderBy(name => name);

            IOrderedEnumerable<string> actualTypeNames = Components.AllComponents.Select(t => t.Type.FullName + ";" + t.Instance.GetType().FullName).OrderBy(name => name);

            Assert.Equal(expectedTypeNames, actualTypeNames);
        }
    }
}
