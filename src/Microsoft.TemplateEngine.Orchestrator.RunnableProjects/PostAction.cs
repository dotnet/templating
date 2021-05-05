// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class PostAction : IPostAction
    {
        Guid IPostAction.ActionId => ActionId;

        bool IPostAction.ContinueOnError => ContinueOnError;

        IReadOnlyDictionary<string, string> IPostAction.Args => Args;

        string IPostAction.ManualInstructions => ManualInstructions;

        string IPostAction.ConfigFile => ConfigFile;

        string IPostAction.Description => Description;

        internal string Description { get; private set; }

        internal Guid ActionId { get; private set; }

        internal bool ContinueOnError { get; private set; }

        internal IReadOnlyDictionary<string, string> Args { get; private set; }

        internal string ManualInstructions { get; private set; }

        internal string ConfigFile { get; private set; }

        internal static List<IPostAction> ListFromModel(IEngineEnvironmentSettings environmentSettings, IReadOnlyList<IPostActionModel> modelList, IVariableCollection rootVariableCollection)
        {
            List<IPostAction> actionList = new List<IPostAction>();

            if (rootVariableCollection == null)
            {
                rootVariableCollection = new VariableCollection();
            }

            foreach (IPostActionModel model in modelList)
            {
                model.EvaluateCondition(environmentSettings, rootVariableCollection);

                if (!model.ConditionResult)
                {
                    // Condition on the post action is blank, or not true. Don't include this post action.
                    continue;
                }

                string chosenInstruction = string.Empty;

                if (model.ManualInstructionInfo != null)
                {
                    foreach (ManualInstructionModel modelInstruction in model.ManualInstructionInfo)
                    {
                        if (string.IsNullOrEmpty(modelInstruction.Condition))
                        {
                            // no condition
                            if (string.IsNullOrEmpty(chosenInstruction))
                            {
                                // No condition, and no instruction previously chosen. Take this one.
                                // We don't want a default instruction to override a conditional one.
                                chosenInstruction = modelInstruction.Text;
                            }
                        }
                        else if (modelInstruction.EvaluateCondition(environmentSettings, rootVariableCollection))
                        {
                            // condition is not blank and true, take this one. This results in a last-in-wins behaviour for conditions that are true.
                            chosenInstruction = modelInstruction.Text;
                        }
                    }
                }

                IPostAction postAction = new PostAction()
                {
                    Description = model.Description,
                    ActionId = model.ActionId,
                    ContinueOnError = model.ContinueOnError,
                    Args = model.Args,
                    ManualInstructions = chosenInstruction,
                    ConfigFile = model.ConfigFile,
                };

                actionList.Add(postAction);
            }

            return actionList;
        }
    }
}
