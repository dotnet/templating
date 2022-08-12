﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel
{
    /// <summary>
    /// Defines operation for specific files.
    /// </summary>
    public sealed class CustomFileGlobModel : ConditionedConfigurationElement
    {
        internal CustomFileGlobModel(string glob, IReadOnlyList<CustomOperationModel> operations)
        {
            Glob = glob;
            Operations = operations;
        }

        /// <summary>
        /// Gets the glob which defines the files that operations should be applied to.
        /// </summary>
        public string Glob { get; }

        /// <summary>
        /// Gets the collection of operations to apply.
        /// </summary>
        public IReadOnlyList<CustomOperationModel> Operations { get; }

        /// <summary>
        /// Gets the variable configuration format.
        /// </summary>
        public IVariableConfig VariableFormat { get; } = VariableConfig.DefaultVariableSetup();

        /// <summary>
        /// Gets the prefix that is used in flags.
        /// </summary>
        public string? FlagPrefix { get; internal init; }

        internal static CustomFileGlobModel FromJObject(JObject globData, string globName)
        {
            // setup the custom operations
            List<CustomOperationModel> customOpsForGlob = new List<CustomOperationModel>();
            if (globData.TryGetValue(nameof(Operations), StringComparison.OrdinalIgnoreCase, out JToken? operationData))
            {
                foreach (JObject operationConfig in (JArray)operationData)
                {
                    customOpsForGlob.Add(CustomOperationModel.FromJObject(operationConfig));
                }
            }

            CustomFileGlobModel globModel = new CustomFileGlobModel(globName, customOpsForGlob)
            {
                FlagPrefix = globData.ToString(nameof(FlagPrefix)),
                Condition = globData.ToString(nameof(Condition))
            };

            return globModel;
        }
    }
}
