// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public partial class TemplateInfo
    {
        internal TemplateInfo(ITemplateInfo template, ILocalizationLocator? localizationInfo)
        {
            GeneratorId = template.GeneratorId;
            ConfigPlace = template.ConfigPlace;
            MountPointUri = template.MountPointUri;
            Name = localizationInfo?.Name ?? template.Name;
            Tags = LocalizeCacheTags(template, localizationInfo);
            CacheParameters = LocalizeCacheParameters(template, localizationInfo);
            Classifications = template.Classifications;
            Author = localizationInfo?.Author ?? template.Author;
            Description = localizationInfo?.Description ?? template.Description;
            GroupIdentity = template.GroupIdentity ?? string.Empty;
            Precedence = template.Precedence;
            Identity = template.Identity;
            DefaultName = template.DefaultName;
            LocaleConfigPlace = localizationInfo?.ConfigPlace;
            HostConfigPlace = template.HostConfigPlace;
            ThirdPartyNotices = template.ThirdPartyNotices;
            BaselineInfo = template.BaselineInfo;
            HasScriptRunningPostActions = template.HasScriptRunningPostActions;

            if (template is IShortNameList templateWithShortNameList)
            {
                ShortNameList = templateWithShortNameList.ShortNameList;
            }
            else
            {
                ShortNameList = new List<string>() { template.ShortName };
            }
        }

        private static IReadOnlyDictionary<string, ICacheTag> LocalizeCacheTags(ITemplateInfo template, ILocalizationLocator? localizationInfo)
        {
            if (localizationInfo == null || localizationInfo.ParameterSymbols == null)
            {
                return template.Tags;
            }

            IReadOnlyDictionary<string, ICacheTag> templateTags = template.Tags;
            IReadOnlyDictionary<string, IParameterSymbolLocalizationModel> localizedParameterSymbols = localizationInfo.ParameterSymbols;

            Dictionary<string, ICacheTag> localizedCacheTags = new Dictionary<string, ICacheTag>();

            foreach (KeyValuePair<string, ICacheTag> templateTag in templateTags)
            {
                if (!localizedParameterSymbols.TryGetValue(templateTag.Key, out IParameterSymbolLocalizationModel localizationForTag))
                {
                    // There is no localization for this symbol. Use the symbol as is.
                    localizedCacheTags.Add(templateTag.Key, templateTag.Value);
                    continue;
                }

                // There is localization. Create a localized instance, starting with the choices.
                var localizedChoices = new Dictionary<string, ParameterChoice>();

                foreach (KeyValuePair<string, ParameterChoice> templateChoice in templateTag.Value.Choices)
                {
                    ParameterChoice localizedChoice = new ParameterChoice(
                        templateChoice.Value.DisplayName,
                        templateChoice.Value.Description);

                    if (localizationForTag.Choices.TryGetValue(templateChoice.Key, out ParameterChoiceLocalizationModel locModel))
                    {
                        localizedChoice.Localize(locModel);
                    }

                    localizedChoices.Add(templateChoice.Key, localizedChoice);
                }

                ICacheTag localizedTag = new CacheTag(
                    localizationForTag.DisplayName ?? templateTag.Value.DisplayName,
                    localizationForTag.Description ?? templateTag.Value.Description,
                    localizedChoices,
                    templateTag.Value.DefaultValue,
                    (templateTag.Value as IAllowDefaultIfOptionWithoutValue)?.DefaultIfOptionWithoutValue);

                localizedCacheTags.Add(templateTag.Key, localizedTag);
            }

            return localizedCacheTags;
        }

        private static IReadOnlyDictionary<string, ICacheParameter> LocalizeCacheParameters(ITemplateInfo template, ILocalizationLocator? localizationInfo)
        {
            if (localizationInfo == null || localizationInfo.ParameterSymbols == null)
            {
                return template.CacheParameters;
            }

            IReadOnlyDictionary<string, ICacheParameter> templateCacheParameters = template.CacheParameters;
            IReadOnlyDictionary<string, IParameterSymbolLocalizationModel> localizedParameterSymbols = localizationInfo.ParameterSymbols;


            Dictionary<string, ICacheParameter> localizedCacheParams = new Dictionary<string, ICacheParameter>();

            foreach (KeyValuePair<string, ICacheParameter> templateParam in templateCacheParameters)
            {
                if (localizedParameterSymbols.TryGetValue(templateParam.Key, out IParameterSymbolLocalizationModel localizationForParam))
                {   // there is loc info for this symbol
                    ICacheParameter localizedParam = new CacheParameter
                    {
                        DataType = templateParam.Value.DataType,
                        DefaultValue = templateParam.Value.DefaultValue,
                        DisplayName = localizationForParam.DisplayName ?? templateParam.Value.DisplayName,
                        Description = localizationForParam.Description ?? templateParam.Value.Description
                    };

                    localizedCacheParams.Add(templateParam.Key, localizedParam);
                }
                else
                {
                    localizedCacheParams.Add(templateParam.Key, templateParam.Value);
                }
            }

            return localizedCacheParams;
        }
    }
}
