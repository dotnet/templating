// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;

namespace Microsoft.TemplateEngine.Edge
{
    /// <summary>
    /// Manages evaluation of constraints for the templates.
    /// </summary>
    public class TemplateConstraintManager : IDisposable
    {
        private readonly ILogger<TemplateConstraintManager> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<string, ITemplateConstraint> _templateConstraints = new();
        private readonly Dictionary<(string Type, string? Args), TemplateConstraintResult> _evaluatedConstraints = new();

        public TemplateConstraintManager(IEngineEnvironmentSettings engineEnvironmentSettings)
        {
            _logger = engineEnvironmentSettings.Host.LoggerFactory.CreateLogger<TemplateConstraintManager>();
            InitializeTemplateConstraints(engineEnvironmentSettings).GetAwaiter().GetResult();
        }

#pragma warning disable CS1998
        /// <summary>
        /// Returns the list of initialized <see cref="ITemplateConstraint"/>s.
        /// Only returns the list of <see cref="ITemplateConstraint"/> that were initialized successfully.
        /// The constraints which failed to be initialized are skipped and warning is logged.
        /// </summary>
        /// <param name="templates">if given, only returns the list of constraints defined in the templates.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The list of successfully initialized <see cref="ITemplateConstraint"/>s.</returns>
        public async Task<IReadOnlyList<ITemplateConstraint>> GetConstraintsAsync(IEnumerable<ITemplateInfo>? templates = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var uniqueConstraints = templates?.SelectMany(ti => ti.Constraints.Select(c => c.Type)).Distinct() ?? _templateConstraints.Keys;
            List<ITemplateConstraint> templateConstraints = [];
            foreach (var constraint in uniqueConstraints)
            {
                if (_templateConstraints.TryGetValue(constraint, out var result))
                {
                    templateConstraints.Add(result);
                }
            }

            return templateConstraints;
        }

        /// <summary>
        /// Evaluates the constraints with given <paramref name="type"/> for given args <paramref name="args"/>.
        /// </summary>
        /// <param name="type">constraint type to evaluate.</param>
        /// <param name="args">arguments to use for evaluation.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="TemplateConstraintResult"/> indicating if constraint is met, or details why the constraint is not met.</returns>
        public async Task<TemplateConstraintResult> EvaluateConstraintAsync(string type, string? args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_evaluatedConstraints.TryGetValue((type, args), out TemplateConstraintResult result))
            {
                return result;
            }

            if (!_templateConstraints.TryGetValue(type, out ITemplateConstraint constraint))
            {
                if (_evaluatedConstraints.TryGetValue((type, null), out result))
                {
                    return result;
                }

                _logger.LogDebug($"The constraint '{type}' is unknown.");
                return TemplateConstraintResult.CreateInitializationFailure(type, string.Format(LocalizableStrings.TemplateConstraintManager_Error_UnknownType, type));
            }

            try
            {
                result = constraint.Evaluate(args);
                _evaluatedConstraints.Add((type, args), result);
                return result;
            }
            catch (Exception e)
            {
                _logger.LogDebug($"The constraint '{type}' failed to be evaluated for the args '{args}', details: {e}.");
                return TemplateConstraintResult.CreateEvaluationFailure(constraint, string.Format(LocalizableStrings.TemplateConstraintManager_Error_FailedToEvaluate, type, args, e.Message));
            }

        }
#pragma warning restore CS1998

        /// <summary>
        /// Evaluates the constraints with given <paramref name="templates"/>.
        /// The method doesn't throw when the constraint is failed to be evaluated, returns <see cref="TemplateConstraintResult"/> with status <see cref="TemplateConstraintResult.Status.NotEvaluated"/> instead.
        /// </summary>
        /// <param name="templates">the list of templates to evaluate constraints for given templates.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="TemplateConstraintResult"/> indicating if constraint is met, or details why the constraint is not met.</returns>
        public async Task<IReadOnlyList<(ITemplateInfo Template, IReadOnlyList<TemplateConstraintResult> Result)>> EvaluateConstraintsAsync(IEnumerable<ITemplateInfo> templates, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<(ITemplateInfo Template, IReadOnlyList<TemplateConstraintResult> Result)> list = [];
            foreach (var template in templates)
            {
                List<TemplateConstraintResult> results = [];
                foreach (var constraint in template.Constraints)
                {
                    results.Add(await EvaluateConstraintAsync(constraint.Type, constraint.Args, cancellationToken).ConfigureAwait(false));
                }

                list.Add((template, results));
            }

            return list;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task CancellableWhenAll(IEnumerable<Task> tasks, CancellationToken cancellationToken)
        {
            await Task.WhenAny(
                Task.WhenAll(tasks),
                Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            //throws exceptions
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task InitializeTemplateConstraints(IEngineEnvironmentSettings engineEnvironmentSettings)
        {
            var constraintFactories = engineEnvironmentSettings.Components.OfType<ITemplateConstraintFactory>();
            _logger.LogDebug($"Found {constraintFactories.Count()} constraints factories, initializing.");
            foreach (var constraintFactory in constraintFactories)
            {
                ITemplateConstraint? constraint = null;
                Exception? exception = null;

                try
                {
                    constraint = await constraintFactory.CreateTemplateConstraintAsync(engineEnvironmentSettings, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    //handled below
                    exception = e;
                }

                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (constraint is null)
                {
                    exception = exception is not null ? exception.InnerException ?? exception : exception;
                    _logger.LogDebug($"The constraint '{constraintFactory.Type}' failed to be initialized, details: {exception}.");
                    _evaluatedConstraints.Add((constraintFactory.Type, null), TemplateConstraintResult.CreateInitializationFailure(constraintFactory.Type, string.Format(LocalizableStrings.TemplateConstraintManager_Error_FailedToInitialize, constraintFactory.Type, exception?.Message)));
                }
                else
                {
                    _templateConstraints[constraintFactory.Type] = constraint;
                }
            }
        }
    }
}
