﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.OperationConfig
{
    internal class ConditionalOperationOptions
    {
        private const string DefaultEvaluatorType = "C++";
        private const bool DefaultWholeLine = true;
        private const bool DefaultTrimWhitespace = true;
        private static readonly string DefaultId;

        internal ConditionalOperationOptions()
        {
            EvaluatorType = DefaultEvaluatorType;
            WholeLine = DefaultWholeLine;
            TrimWhitespace = DefaultTrimWhitespace;
            Id = DefaultId;
        }

        internal string EvaluatorType { get; set; }

        internal bool WholeLine { get; set; }

        internal bool TrimWhitespace { get; set; }

        internal string Id { get; set; }

        internal bool OnByDefault { get; set; }

        internal static ConditionalOperationOptions FromJObject(JObject rawConfiguration)
        {
            ConditionalOperationOptions options = new ConditionalOperationOptions();

            string evaluatorType = rawConfiguration.ToString("evaluator");
            if (!string.IsNullOrWhiteSpace(evaluatorType))
            {
                options.EvaluatorType = evaluatorType;
            }

            options.TrimWhitespace = rawConfiguration.ToBool("trim", true);
            options.WholeLine = rawConfiguration.ToBool("wholeLine", true);
            options.OnByDefault = rawConfiguration.ToBool("onByDefault");

            string id = rawConfiguration.ToString("id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                options.Id = id;
            }

            return options;
        }
    }
}
