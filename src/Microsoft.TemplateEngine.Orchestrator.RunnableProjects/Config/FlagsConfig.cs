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
    internal class FlagsConfig : IOperationConfig
    {
        private const string NoEmitSuffix = ":noEmit";
        private const string FlagConditionalSuffix = ":cnd";
        private const string FlagReplacementSuffix = ":replacements";
        private const string FlagExpandVariablesSuffix = ":vars";
        private const string FlagIncludeSuffix = ":include";
        private const string FlagFlagsSuffix = ":flags";

        public string Key => SetFlag.OperationName;

        public Guid Id => new Guid("A1E27A4B-9608-47F1-B3B8-F70DF62DC521");

        public IEnumerable<IOperationProvider> ConfigureFromJson(string rawConfiguration, IDirectory templateRoot)
        {
            JObject json = JObject.Parse(rawConfiguration);
            string flag = json.ToString("name");
            string on = json.ToString("on") ?? string.Empty;
            string off = json.ToString("off") ?? string.Empty;
            string onNoEmit = json.ToString("onNoEmit") ?? string.Empty;
            string offNoEmit = json.ToString("offNoEmit") ?? string.Empty;
            string defaultStr = json.ToString("default");
            string id = json.ToString("id");
            bool onByDefault = json.ToBool("onByDefault");
            bool? @default = null;

            if (defaultStr != null)
            {
                @default = bool.Parse(defaultStr);
            }

            yield return new SetFlag(flag, on.TokenConfig(), off.TokenConfig(), onNoEmit.TokenConfig(), offNoEmit.TokenConfig(), id, onByDefault, @default);
        }

        // Returns a default flags operations setup for the given switchPrefix
        internal static IReadOnlyList<IOperationProvider> FlagsDefaultSetup(string switchPrefix)
        {
            List<IOperationProvider> flagOperations = new List<IOperationProvider>();
            string on = string.Format("{0}+{1}", switchPrefix, FlagConditionalSuffix);
            string off = string.Format("{0}-{1}", switchPrefix, FlagConditionalSuffix);
            flagOperations.Add(new SetFlag(Conditional.OperationName, on.TokenConfig(), off.TokenConfig(), (on + NoEmitSuffix).TokenConfig(), (off + NoEmitSuffix).TokenConfig(), null, true));

            on = string.Format("{0}+{1}", switchPrefix, FlagReplacementSuffix);
            off = string.Format("{0}-{1}", switchPrefix, FlagReplacementSuffix);
            flagOperations.Add(new SetFlag(Replacement.OperationName, on.TokenConfig(), off.TokenConfig(), (on + NoEmitSuffix).TokenConfig(), (off + NoEmitSuffix).TokenConfig(), null, true));

            on = string.Format("{0}+{1}", switchPrefix, FlagExpandVariablesSuffix);
            off = string.Format("{0}-{1}", switchPrefix, FlagExpandVariablesSuffix);
            flagOperations.Add(new SetFlag(ExpandVariables.OperationName, on.TokenConfig(), off.TokenConfig(), (on + NoEmitSuffix).TokenConfig(), (off + NoEmitSuffix).TokenConfig(), null, true));

            on = string.Format("{0}+{1}", switchPrefix, FlagIncludeSuffix);
            off = string.Format("{0}-{1}", switchPrefix, FlagIncludeSuffix);
            flagOperations.Add(new SetFlag(Include.OperationName, on.TokenConfig(), off.TokenConfig(), (on + NoEmitSuffix).TokenConfig(), (off + NoEmitSuffix).TokenConfig(), null, true));

            // no off for the flag-flag
            on = string.Format("{0}+{1}", switchPrefix, FlagFlagsSuffix);
            flagOperations.Add(new SetFlag(SetFlag.OperationName, on.TokenConfig(), string.Empty.TokenConfig(), (on + NoEmitSuffix).TokenConfig(), string.Empty.TokenConfig(), null, true));

            return flagOperations;
        }
    }
}
