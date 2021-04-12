// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.TemplateSearch.Common
{
    internal class BlobTemplateInfo : ITemplateInfo, IShortNameList
    {
        public BlobTemplateInfo(JObject jObject)
        {
            Identity = jObject.Value<string>(nameof(Identity));
            Name = jObject.Value<string>(nameof(Name));
            ShortNameList = jObject.Value<JArray>("ShortNameList").ToObject<string[]>();
            Author = jObject.Value<string>(nameof(Author));
            GroupIdentity = jObject.Value<string>(nameof(GroupIdentity));
            //TODO: Check if this is actually needed for --search, or could it be just hard coded 0
            Precedence = jObject.Value<int>(nameof(Precedence));

            var tags = new Dictionary<string, ICacheTag>();
            foreach (var tag in jObject.Value<JObject>("Tags").Properties())
            {
                var choices = new Dictionary<string, ParameterChoice>();
                foreach (var choice in tag.Value.Value<JObject>("ChoicesAndDescriptions").Properties())
                {
                    choices.Add(choice.Name, new ParameterChoice(null, null));
                }
                tags.Add(tag.Name,
                    new CacheTag(
                        null,
                        null,
                        choices,
                        tag.Value.Value<JObject>().Value<string>("Default")));
            }
            Tags = tags;

            var classificationsArray = jObject.Value<JArray>(nameof(Classifications));
            List<string> classifications = new List<string>();
            Classifications = classifications;
            if (classificationsArray != null)
            {
                foreach (JToken item in classificationsArray)
                {
                    classifications.Add(item.ToString());
                }
            }
        }

        public string? Author { get; }

        public string? Description => null;

        public IReadOnlyList<string> Classifications { get; }

        public string? DefaultName => null;

        public string Identity { get; }

        public Guid GeneratorId => Guid.Empty;

        public string? GroupIdentity { get; }

        public int Precedence { get; }

        public string Name { get; }

        public string ShortName => ShortNameList[0];

        public IReadOnlyDictionary<string, ICacheTag> Tags { get; }

        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters => new Dictionary<string, ICacheParameter>();

        public IReadOnlyList<ITemplateParameter> Parameters => Empty<ITemplateParameter>.List.Value;

        public string MountPointUri => "";

        public string ConfigPlace => "";

        public string? LocaleConfigPlace => null;

        public string? HostConfigPlace => null;

        public string? ThirdPartyNotices => null;

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo => new Dictionary<string, IBaselineInfo>();

        public bool HasScriptRunningPostActions { get => false; }

        public IReadOnlyList<string> ShortNameList { get; }
    }
}
