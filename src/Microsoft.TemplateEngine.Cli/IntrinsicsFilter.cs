using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli
{
    internal static class IntrinsicsFilter
    {
        public static bool TryFilterSingularGroupWithIntrinsics(IEngineEnvironmentSettings environmentSettings, IReadOnlyList<ITemplateMatchInfo> unambiguousTemplateGroup, out IReadOnlyList<string> specificIdentities, out IReadOnlyList<string> matchedIntrinsics)
        {
            //If we can't get the intrinsics keys collection, there's nothing to be done
            if (!environmentSettings.Host.TryGetHostParamDefault("meta:intrinsics-keys", out string intrinsicsKeys))
            {
                matchedIntrinsics = null;
                specificIdentities = null;
                return false;
            }

            //Build up the intrinsics map
            string[] keys = intrinsicsKeys.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> intrinsics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string key in keys)
            {
                if (environmentSettings.Host.TryGetHostParamDefault(key, out string value))
                {
                    intrinsics[key] = value;
                }
            }

            //If we made it through all that & didn't actually find any intrinsics, quit
            if (intrinsics.Count == 0)
            {
                matchedIntrinsics = null;
                specificIdentities = null;
                return false;
            }

            HashSet<string> matchedTemplatesByIntrinsic = new HashSet<string>(StringComparer.Ordinal);
            HashSet<ITemplateMatchInfo> matchedMatches = new HashSet<ITemplateMatchInfo>();

            //Choice type parameters are represented as tags in the cache, see if any of the valid choices
            //  match any of the intrinsics
            foreach (ITemplateMatchInfo matchInfo in unambiguousTemplateGroup)
            {
                IReadOnlyDictionary<string, ICacheTag> tags = matchInfo.Info.Tags;

                foreach (KeyValuePair<string, string> entry in intrinsics)
                {
                    foreach (KeyValuePair<string, ICacheTag> choiceParmeter in tags)
                    {
                        if (choiceParmeter.Value.ChoicesAndDescriptions.ContainsKey(entry.Value))
                        {
                            matchedTemplatesByIntrinsic.Add(entry.Key);
                            matchedMatches.Add(matchInfo);
                        }
                    }
                }
            }

            //If we didn't turn up anything that matched one of the instrinsics or we matched everything, quit
            if (matchedMatches.Count == 0 || matchedMatches.Count == unambiguousTemplateGroup.Count)
            {
                matchedIntrinsics = null;
                specificIdentities = null;
                return false;
            }

            matchedIntrinsics = matchedTemplatesByIntrinsic.ToList();
            specificIdentities = matchedMatches.Select(x => x.Info.Identity).ToList();
            return true;
        }
    }
}
