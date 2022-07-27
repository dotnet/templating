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
    internal class BalancedNestingConfig : IOperationConfig
    {
        public string Key => BalancedNesting.OperationName;

        public Guid Id => new Guid("3147965A-08E5-4523-B869-02C8E9A8AAA1");

        public IEnumerable<IOperationProvider> ConfigureFromJson(string rawConfiguration, IDirectory templateRoot)
        {
            JObject json = JObject.Parse(rawConfiguration);
            string startToken = json.ToString("startToken");
            string realEndToken = json.ToString("realEndToken");
            string pseudoEndToken = json.ToString("pseudoEndToken");
            string id = json.ToString("id");
            string resetFlag = json.ToString("resetFlag");
            bool onByDefault = json.ToBool("onByDefault");

            yield return new BalancedNesting(startToken.TokenConfig(), realEndToken.TokenConfig(), pseudoEndToken.TokenConfig(), id, resetFlag, onByDefault);
        }

        internal static JObject CreateConfiguration(string startToken, string realEndToken, string pseudoEndToken, string id, string resetFlag)
        {
            JObject config = new JObject
            {
                ["startToken"] = startToken,
                ["realEndToken"] = realEndToken,
                ["pseudoEndToken"] = pseudoEndToken,
                ["id"] = id,
                ["resetFlag"] = resetFlag
            };

            return config;
        }
    }
}
