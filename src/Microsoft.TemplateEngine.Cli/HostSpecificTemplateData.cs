// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Cli
{
    public class HostSpecificTemplateData
    {
        private const string IsHiddenKey = "isHidden";
        private const string LongNameKey = "longName";
        private const string ShortNameKey = "shortName";
        private const string AlwaysShowKey = "alwaysShow";
        private const string UsageExamplesKey = "usageExamples";
        private const string ValueKey = "Value";
        private const string SymbolInfoKey = "symbolInfo";
        private const string SymbolInfoEntryKey = "symbolInfoEntry";

        internal static readonly DeserializationPlan<HostSpecificTemplateData> DeserializationPlan = Deserializer.CreateDeserializerBuilder()
            .IfObject()
                .Property(UsageExamplesKey)
                    .IfArray(UsageExamplesKey)
                        .IfString(ValueKey)
                    .Pop()
                .Pop()
                .Property(IsHiddenKey)
                    .IfBool(IsHiddenKey)
                .Pop()
                .Property(SymbolInfoKey)
                    .IfObject()
                        .StoreAsDictionary(SymbolInfoKey)
                            .IfObject()
                                .StoreAsDictionary(SymbolInfoEntryKey)
                                    .IfString(ValueKey)
                                .Pop()
                            .Pop()
                        .Pop()
                    .Pop()
                .Pop()
            .Pop()
            .ToPlan(CreateHostSpecificTemplateDataFromDeserializationContext);

        private static HostSpecificTemplateData CreateHostSpecificTemplateDataFromDeserializationContext(DeserializationContext context)
        {
            HostSpecificTemplateData data = new HostSpecificTemplateData();
            data.UsageExamples = context.GetValue(UsageExamplesKey, Empty<DeserializationContext>.List.Value).Select(x => x.GetValue<string>(ValueKey)).ToList();
            data.IsHidden = context.GetValue<bool>(IsHiddenKey);

            Dictionary<string, DeserializationContext> symbolInfoData = context.GetValue<Dictionary<string, DeserializationContext>>(SymbolInfoKey);

            if (!(symbolInfoData is null))
            {
                foreach (KeyValuePair<string, DeserializationContext> entry in symbolInfoData)
                {
                    data._symbolInfo[entry.Key] = entry.Value.GetValue<Dictionary<string, DeserializationContext>>(SymbolInfoEntryKey).ToDictionary(x => x.Key, x => x.Value.GetValue<string>(ValueKey));
                }
            }

            return data;
        }

        public static HostSpecificTemplateData Default { get; } = new HostSpecificTemplateData();

        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _symbolInfo;

        public HostSpecificTemplateData()
        {
            _symbolInfo = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        }

        [JsonProperty]
        public List<string> UsageExamples { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SymbolInfo => _symbolInfo;

        [JsonProperty]
        public bool IsHidden { get; set; }

        public HashSet<string> HiddenParameterNames
        {
            get
            {
                HashSet<string> hiddenNames = new HashSet<string>();
                foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> paramInfo in SymbolInfo)
                {
                    if (paramInfo.Value.TryGetValue(IsHiddenKey, out string hiddenStringValue)
                        && bool.TryParse(hiddenStringValue, out bool hiddenBoolValue)
                        && hiddenBoolValue)
                    {
                        hiddenNames.Add(paramInfo.Key);
                    }
                }

                return hiddenNames;
            }
        }

        public HashSet<string> ParametersToAlwaysShow
        {
            get
            {
                HashSet<string> parametersToAlwaysShow = new HashSet<string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> paramInfo in SymbolInfo)
                {
                    if(paramInfo.Value.TryGetValue(AlwaysShowKey, out string alwaysShowValue)
                        && bool.TryParse(alwaysShowValue, out bool alwaysShowBoolValue)
                        && alwaysShowBoolValue)
                    {
                        parametersToAlwaysShow.Add(paramInfo.Key);
                    }
                }

                return parametersToAlwaysShow;
            }
        }

        public Dictionary<string, string> LongNameOverrides
        {
            get
            {
                Dictionary<string, string> map = new Dictionary<string, string>();

                foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> paramInfo in SymbolInfo)
                {
                    if (paramInfo.Value.TryGetValue(LongNameKey, out string longNameOverride))
                    {
                        map.Add(paramInfo.Key, longNameOverride);
                    }
                }

                return map;
            }
        }

        public Dictionary<string, string> ShortNameOverrides
        {
            get
            {
                Dictionary<string, string> map = new Dictionary<string, string>();

                foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> paramInfo in SymbolInfo)
                {
                    if (paramInfo.Value.TryGetValue(ShortNameKey, out string shortNameOverride))
                    {
                        map.Add(paramInfo.Key, shortNameOverride);
                    }
                }

                return map;
            }
        }

        public string DisplayNameForParameter(string parameterName)
        {
            if (SymbolInfo.TryGetValue(parameterName, out IReadOnlyDictionary<string, string> configForParam)
                && configForParam.TryGetValue(LongNameKey, out string longName))
            {
                return longName;
            }

            return parameterName;
        }
    }
}
