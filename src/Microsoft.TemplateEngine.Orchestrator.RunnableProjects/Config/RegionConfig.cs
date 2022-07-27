// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.OperationConfig
{
    internal class RegionConfig : IOperationConfig
    {
        public string Key => Region.OperationName;

        public Guid Id => new Guid("3D33B3BF-F40E-43EB-A14D-F40516F880CD");

        public IEnumerable<IOperationProvider> ConfigureFromJson(string rawConfiguration, IDirectory templateRoot)
        {
            JObject json = JObject.Parse(rawConfiguration);
            string id = json.ToString("id");
            string start = json.ToString("start");
            string end = json.ToString("end");
            bool include = json.ToBool("include");
            bool regionTrim = json.ToBool("trim");
            bool regionWholeLine = json.ToBool("wholeLine");
            bool onByDefault = json.ToBool("onByDefault");

            yield return new Region(start.TokenConfig(), end.TokenConfig(), include, regionWholeLine, regionTrim, id, onByDefault);
        }
    }
}
