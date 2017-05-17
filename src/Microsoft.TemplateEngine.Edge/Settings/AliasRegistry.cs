using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class AliasRegistry
    {
        private AliasModel _aliases;

        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly Paths _paths;

        public AliasRegistry(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> AllAliases
        {
            get
            {
                EnsureLoaded();
                return new Dictionary<string, IReadOnlyList<string>>(_aliases.CommandAliases, StringComparer.OrdinalIgnoreCase);
            }
        }

        private void EnsureLoaded()
        {
            if (_aliases != null)
            {
                return;
            }

            if (!_paths.Exists(_paths.User.AliasesFile))
            {
                _aliases = new AliasModel();
                return;
            }

            string sourcesText = _paths.ReadAllText(_paths.User.AliasesFile, "{}");
            JObject parsed = JObject.Parse(sourcesText);
            IReadOnlyDictionary<string, IReadOnlyList<string>> commandAliases = parsed.ToStringListDictionary(StringComparer.OrdinalIgnoreCase, "CommandAliases");

            _aliases = new AliasModel(commandAliases);
        }

        private void Save()
        {
            JObject serialized = JObject.FromObject(_aliases);
            _environmentSettings.Host.FileSystem.WriteAllText(_paths.User.AliasesFile, serialized.ToString());
        }

        public AliasManipulationResult TryCreateOrRemoveAlias(string aliasName, IReadOnlyList<string> aliasTokens)
        {
            EnsureLoaded();

            if (aliasName == null)
            {
                // the input was malformed. Alias flag without alias name
                return new AliasManipulationResult(AliasManipulationStatus.InvalidInput);
            }
            else if (aliasTokens.Count == 0)
            {   // the command was just "--alias <alias name>"
                // remove the alias
                if (_aliases.TryRemoveCommandAlias(aliasName, out IReadOnlyList<string> removedAliasTokens))
                {
                    Save();
                    return new AliasManipulationResult(AliasManipulationStatus.Removed, aliasName, removedAliasTokens);
                }
                else
                {
                    return new AliasManipulationResult(AliasManipulationStatus.RemoveNonExistentFailed, aliasName, null);
                }
            }

            Dictionary<string, IReadOnlyList<string>> aliasesWithCandidate = new Dictionary<string, IReadOnlyList<string>>(_aliases.CommandAliases);
            aliasesWithCandidate[aliasName] = aliasTokens;
            if (!TryExpandCommandAliases(aliasesWithCandidate, aliasTokens, out IReadOnlyList<string> expandedInputTokens))
            {
                return new AliasManipulationResult(AliasManipulationStatus.WouldCreateCycle, aliasName, aliasTokens);
            }

            _aliases.AddCommandAlias(aliasName, aliasTokens);
            Save();
            return new AliasManipulationResult(AliasManipulationStatus.Created, aliasName, aliasTokens);
        }

        // Attempts to expand aliases on the input string, using the aliases in _aliases
        public bool TryExpandCommandAliases(IReadOnlyList<string> inputTokens, out IReadOnlyList<string> expandedInputTokens)
        {
            EnsureLoaded();

            if (inputTokens.Count == 0)
            {
                expandedInputTokens = new List<string>(inputTokens);
                return true;
            }

            if (TryExpandCommandAliases(_aliases.CommandAliases, inputTokens, out expandedInputTokens))
            {
                return true;
            }

            // TryExpandCommandAliases() returned false because was an expansion error
            expandedInputTokens = new List<string>();
            return false;
        }

        private static bool TryExpandCommandAliases(IReadOnlyDictionary<string, IReadOnlyList<string>> aliases, IReadOnlyList<string> inputTokens, out IReadOnlyList<string> expandedTokens)
        {
            bool expansionOccurred = false;
            HashSet<string> seenAliases = new HashSet<string>();
            expandedTokens = new List<string>(inputTokens);

            do
            {
                string candidateAliasName = expandedTokens[0];

                if (aliases.TryGetValue(candidateAliasName, out IReadOnlyList<string> aliasExpansion))
                {
                    if (!seenAliases.Add(candidateAliasName))
                    {
                        // a cycle has occurred.... not allowed.
                        expandedTokens = null;
                        return false;
                    }

                    // The expansion is the combination of the aliasExpansion (expands the 0th token of the previously expandedTokens)
                    //  and the rest of the previously expandedTokens
                    expandedTokens = new CombinedList<string>(aliasExpansion, expandedTokens.ToList().GetRange(1, expandedTokens.Count - 1));
                    expansionOccurred = true;
                }
                else
                {
                    expansionOccurred = false;
                }
            } while (expansionOccurred);

            return true;
        }
    }
}
