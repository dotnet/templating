// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class PostActionModel : ConditionedConfigurationElementBase, IPostActionModel
    {
        public PostActionModel(
            string? id,
            string? description,
            Guid actionId,
            bool continueOnError,
            IReadOnlyDictionary<string, string> args,
            IReadOnlyList<ManualInstructionModel> manualInstructionInfo,
            string? condition)
        {
            Id = id;
            Description = description;
            ActionId = actionId;
            ContinueOnError = continueOnError;
            Args = args;
            ManualInstructionInfo = manualInstructionInfo;
            Condition = condition;
        }

        public string? Id { get; }

        public string? Description { get; }

        public Guid ActionId { get; }

        public bool ContinueOnError { get; }

        public IReadOnlyDictionary<string, string> Args { get; }

        public IReadOnlyList<ManualInstructionModel> ManualInstructionInfo { get; }

        internal static IReadOnlyList<IPostActionModel> ListFromJArray(JArray jObject, IReadOnlyDictionary<string, IPostActionLocalizationModel>? localizations, ITemplateEngineHost host)
        {
            // TODO Host is only here to allow logging. Once ILogger is available, remove host from required parameters.
            List<IPostActionModel> localizedPostActions = new List<IPostActionModel>();
            int localizedPostActionCount = 0;

            if (jObject == null)
            {
                return localizedPostActions;
            }

            foreach (JToken action in jObject)
            {
                string? postActionId = action.ToString(nameof(Id));

                Dictionary<string, string> args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (JProperty argInfo in action.PropertiesOf("Args"))
                {
                    args.Add(argInfo.Name, argInfo.Value.ToString());
                }

                IPostActionLocalizationModel? actionLocalizations = null;

                if (postActionId != null &&
                    localizations != null &&
                    localizations.TryGetValue(postActionId, out actionLocalizations) &&
                    actionLocalizations != null)
                {
                    localizedPostActionCount++;
                }

                List<ManualInstructionModel> localizedInstructions = new();
                JArray? manualInstructions = action.Get<JArray>("ManualInstructions");

                if (manualInstructions != null)
                {
                    int localizedInstructionCount = 0;
                    for (int i = 0; i < manualInstructions.Count; i++)
                    {
                        string? id = manualInstructions[i].ToString("id");
                        string? text = string.Empty;
                        string? localizationKey = id ?? (manualInstructions.Count == 0 ? "default" : null);

                        if (id != null && (actionLocalizations?.Instructions.TryGetValue(id, out text) ?? false) ||
                            manualInstructions.Count == 1 && (actionLocalizations?.Instructions.TryGetValue("default", out text) ?? false))
                        {
                            localizedInstructionCount++;
                        }

                        if (string.IsNullOrEmpty(text))
                        {
                            text = manualInstructions[i].ToString("text");
                        }

                        localizedInstructions.Add(new ManualInstructionModel(id, text ?? string.Empty, manualInstructions[i].ToString("condition")));
                    }

                    if (actionLocalizations?.Instructions.Count > localizedInstructionCount)
                    {
                        // Localizations provide more translations than the number of manual instructions we have.
                        string excessInstructionLocalizationIds = string.Join(
                            ", ",
                            actionLocalizations.Instructions.Keys.Where(k => !localizedInstructions.Any(i => i.Id == k)));
                        host.Logger.LogWarning(string.Format(LocalizableStrings.Authoring_InvalidManualInstructionLocalizationIndex, excessInstructionLocalizationIds, postActionId));
                    }
                }

                PostActionModel model = new PostActionModel(
                    postActionId,
                    actionLocalizations?.Description ?? action.ToString(nameof(Description)),
                    action.ToGuid(nameof(ActionId)),
                    action.ToBool(nameof(ContinueOnError)),
                    args,
                    localizedInstructions,
                    action.ToString(nameof(Condition))
                );

                localizedPostActions.Add(model);
            }

            if (localizations?.Count > localizedPostActionCount)
            {
                // Localizations provide more translations than the number of post actions we have.
                string excessPostActionLocalizationIds = string.Join(", ", localizations.Keys.Where(k => !localizedPostActions.Any(p => p.Id == k)).Select(k => k.ToString()));
                host.Logger.LogWarning(string.Format(LocalizableStrings.Authoring_InvalidPostActionLocalizationId, excessPostActionLocalizationId));
            }
            return localizedPostActions;
        }
    }
}
