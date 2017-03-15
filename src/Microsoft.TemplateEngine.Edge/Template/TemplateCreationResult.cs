﻿using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public class TemplateCreationResult
    {
        public TemplateCreationResult(string message, CreationResultStatus status, string templateFullName)
            :this(message, status, templateFullName, null, null)
        { }

        public TemplateCreationResult(string message, CreationResultStatus status, string templateFullName, ICreationResult creationOutputs, string outputBaseDir)
        {
            Message = message;
            Status = status;
            TemplateFullName = templateFullName;
            ResultInfo = creationOutputs;
            OutputBaseDirectory = outputBaseDir;
        }

        public string Message { get; }

        public CreationResultStatus Status { get; }

        public string TemplateFullName { get; }

        public ICreationResult ResultInfo { get; }

        public string OutputBaseDirectory { get; }
    }
}
