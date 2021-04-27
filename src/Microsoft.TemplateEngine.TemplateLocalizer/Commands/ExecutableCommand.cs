﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Commands
{
    /// <summary>
    /// Represents a <see cref="System.CommandLine.Command"/> together with its handler.
    /// </summary>
    internal abstract class ExecutableCommand : ICommandHandler
    {
        /// <summary>
        /// Creates a <see cref="System.CommandLine.Command"/> containing the details
        /// of this command such as name, description, arguments and options.
        /// </summary>
        /// <returns>The created command.</returns>
        public abstract Command CreateCommand();

        /// <summary>
        /// Executes the command with the given arguments specified in the invocation context.
        /// </summary>
        /// <returns>A task that tracks the asynchronous operation.
        /// 0 result from the completed task means that the command execution was successful.
        /// where any other value indicates a failure.</returns>
        public abstract Task<int> InvokeAsync(InvocationContext context);
    }

    /// <summary>
    /// Represents an <see cref="ExecutableCommand"/> where the command line arguments
    /// are mapped to a model class.
    /// </summary>
    /// <typeparam name="TModel">Type of the model class whose properties represent the arguments.</typeparam>
    internal abstract class ModelBoundExecutableCommand<TModel> : ExecutableCommand where TModel : class
    {
        // @inheritdoc
        public override Task<int> InvokeAsync(InvocationContext context)
        {
            return CommandHandler.Create<TModel>(Execute).InvokeAsync(context);
        }

        /// <summary>
        /// Executes the command with the given input.
        /// </summary>
        /// <param name="args">Arguments for the command.</param>
        protected abstract void Execute(TModel args);
    }
}
