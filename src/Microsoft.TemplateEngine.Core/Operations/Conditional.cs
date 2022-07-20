// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Util;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public class Conditional : IOperationProvider
    {
        public static readonly string OperationName = "conditional";

        // the unusual order of these is historical, no special meaning
        // if actual_token_index % 10 == baseTokenIndex
        // then actual_token_index is of the baseTokenIndex type
        // these are now "Base indexes"
        private const int IfTokenBaseIndex = 0;

        private const int EndTokenBaseIndex = 1;
        private const int ElseIfTokenBaseIndex = 2;
        private const int ElseTokenBaseIndex = 3;
        private const int IfTokenActionableBaseIndex = 4;
        private const int ElseIfTokenActionableBaseIndex = 5;
        private const int ElseTokenActionableBaseIndex = 6;

        // must be > the highest token type index
        private const int TokenTypeModulus = 10;
        private readonly bool _initialState;

        public Conditional(ConditionalTokens tokenVariants, bool wholeLine, bool trimWhitespace, ConditionEvaluator evaluator, string id, bool initialState)
        {
            TrimWhitespace = trimWhitespace;
            WholeLine = wholeLine;
            Evaluator = evaluator;
            Tokens = tokenVariants;
            Id = id;
            _initialState = initialState;
        }

        public string Id { get; private set; }

        public bool WholeLine { get; private set; }

        public bool TrimWhitespace { get; private set; }

        public ConditionEvaluator Evaluator { get; private set; }

        public ConditionalTokens Tokens { get; private set; }

        /// <summary>
        /// Returns the numner of elements in the longest of the token variant lists.
        /// </summary>
        private int LongestTokenVariantListSize
        {
            get
            {
                int maxListSize = Math.Max(Tokens.IfTokens.Count, Tokens.ElseTokens.Count);
                maxListSize = Math.Max(maxListSize, Tokens.ElseIfTokens.Count);
                maxListSize = Math.Max(maxListSize, Tokens.EndIfTokens.Count);
                maxListSize = Math.Max(maxListSize, Tokens.ActionableIfTokens.Count);
                maxListSize = Math.Max(maxListSize, Tokens.ActionableElseTokens.Count);
                maxListSize = Math.Max(maxListSize, Tokens.ActionableElseIfTokens.Count);

                return maxListSize;
            }
        }

        public IOperation GetOperation(Encoding encoding, IProcessorState processorState)
        {
            TokenTrie trie = new();

            List<IToken?> tokens = new(TokenTypeModulus * LongestTokenVariantListSize);
            for (int i = 0; i < tokens.Capacity; i++)
            {
                tokens.Add(null);
            }

            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.IfTokens, IfTokenBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.ElseTokens, ElseTokenBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.ElseIfTokens, ElseIfTokenBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.EndIfTokens, EndTokenBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.ActionableIfTokens, IfTokenActionableBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.ActionableElseTokens, ElseTokenActionableBaseIndex, encoding);
            Conditional.AddTokensOfTypeToTokenListAndTrie(trie, tokens, Tokens.ActionableElseIfTokens, ElseIfTokenActionableBaseIndex, encoding);

            return new Impl(this, tokens, trie, Id, _initialState);
        }

        /// <summary>
        /// Returns true if the tokenIndex indicates the token is a variant of its base type,
        /// false otherwise.
        /// </summary>
        /// <param name="tokenIndex"></param>
        /// <param name="baseTypeIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTokenIndexOfType(int tokenIndex, int baseTypeIndex)
        {
            return (tokenIndex % TokenTypeModulus) == baseTypeIndex;
        }

        /// <summary>
        /// Puts the tokensOfType into the tokenMasterList at indexes which are congruent to typeRemainder mod TokenTypeModulus.
        /// </summary>
        /// <param name="trie"></param>
        /// <param name="tokenMasterList"></param>
        /// <param name="tokensOfType"></param>
        /// <param name="typeRemainder"></param>
        /// <param name="encoding"></param>
        private static void AddTokensOfTypeToTokenListAndTrie(ITokenTrie trie, List<IToken?> tokenMasterList, IReadOnlyList<ITokenConfig> tokensOfType, int typeRemainder, Encoding encoding)
        {
            int tokenIndex = typeRemainder;

            for (int i = 0; i < tokensOfType.Count; i++)
            {
                tokenMasterList[tokenIndex] = tokensOfType[i].ToToken(encoding);
                trie.AddToken(tokenMasterList[tokenIndex], typeRemainder);
                tokenIndex += TokenTypeModulus;
            }
        }

        private class Impl : IOperation
        {
            private readonly Conditional _definition;
            private readonly Stack<EvaluationState> _pendingCompletion = new();
            private readonly ITokenTrie _trie;
            private EvaluationState? _current;

            public Impl(Conditional definition, IReadOnlyList<IToken?> tokens, ITokenTrie trie, string id, bool initialState)
            {
                _trie = trie;
                _definition = definition;
                Tokens = tokens;
                Id = id;
                IsInitialStateOn = string.IsNullOrEmpty(id) || initialState;
            }

            public string Id { get; private set; }

            public IReadOnlyList<IToken?> Tokens { get; }

            public bool IsInitialStateOn { get; }

            public int HandleMatch(IProcessorState processor, int bufferLength, ref int currentBufferPosition, int tokenIndex)
            {
                ILogger logger = processor.Config.Logger;
                logger.LogTrace("{0}.{1}, {2}: {3}, {4}: {5}, {6}: {7}", nameof(Conditional), nameof(HandleMatch), nameof(bufferLength), bufferLength, nameof(currentBufferPosition), currentBufferPosition, nameof(tokenIndex), tokenIndex);

                if (processor.Config.Flags.TryGetValue(OperationName, out bool flag) && !flag)
                {
                    IToken? currentToken = Tokens[tokenIndex];
                    if (currentToken == null)
                    {
                        throw new InvalidOperationException($"Token with index {tokenIndex} is null.");
                    }

                    logger.LogTrace("Flag for operation name {0} is unset", OperationName);
                    logger.LogTrace("Writing token {0} and stop processing", processor.Encoding.GetString(currentToken.Value));
                    processor.Write(currentToken.Value, currentToken.Start, currentToken.Length);
                    return currentToken.Length;
                }

                // conditional has not started, or this is the "if"
                if (_current != null || IsTokenIndexOfType(tokenIndex, IfTokenBaseIndex) || IsTokenIndexOfType(tokenIndex, IfTokenActionableBaseIndex))
                {
                    logger.LogDebug("Starting condition");
                    if (_definition.WholeLine)
                    {
                        logger.LogTrace("Seeking back target until line end");
                        processor.SeekTargetBackUntil(processor.EncodingConfig.LineEndings);
                    }
                    else if (_definition.TrimWhitespace)
                    {
                        logger.LogTrace("Triming source whitespace");
                        processor.TrimWhitespace(false, true, ref bufferLength, ref currentBufferPosition);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                    }
                }

            BEGIN:
                //Got the "if" token...
                if (IsTokenIndexOfType(tokenIndex, IfTokenBaseIndex) || IsTokenIndexOfType(tokenIndex, IfTokenActionableBaseIndex))
                {
                    logger.LogTrace("Got 'if' token");
                    if (_current == null)
                    {
                        _current = new EvaluationState(this);
                    }
                    else
                    {
                        _pendingCompletion.Push(_current);
                        _current = new EvaluationState(this);
                    }

                    //If the "if" branch is taken, all else and elseif blocks will be omitted, return
                    //  control to the processor so nested "if"s/mutations can be processed. Note that
                    //  this block will not be terminated until the corresponding endif is found
                    if (_current.Evaluate(processor, ref bufferLength, ref currentBufferPosition))
                    {
                        logger.LogTrace("'if' condition evaluated to 'true'");
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                        if (_definition.WholeLine)
                        {
                            logger.LogTrace("Seeking forward until line end");
                            processor.SeekBufferForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                            logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                            logger.LogTrace("Current buffer length: {0}", bufferLength);
                        }

                        if (IsTokenIndexOfType(tokenIndex, IfTokenActionableBaseIndex))
                        {
                            // "Actionable" if token, so enable the flag operation(s)
                            _current.ToggleActionableOperations(true, processor);
                        }

                        // if (true_condition) was found.
                        return 0;
                    }
                    else
                    {
                        logger.LogTrace("'if' condition evaluated to 'false', skipping to next token at same level.");
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                        // if (false_condition) was found. Skip to the next token of the if-elseif-elseif-...elseif-else-endif
                        SeekToNextTokenAtSameLevel(processor, ref bufferLength, ref currentBufferPosition, out tokenIndex);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                        goto BEGIN;
                    }
                }

                // If we've got an unbalanced statement, emit the token
                if (_current == null)
                {
                    IToken? currentToken = Tokens[tokenIndex];
                    if (currentToken == null)
                    {
                        throw new InvalidOperationException($"Token with index {tokenIndex} is null.");
                    }

                    logger.LogTrace("unbalanced statement");
                    logger.LogTrace("Writing token {0} and stop processing", processor.Encoding.GetString(currentToken.Value));
                    processor.Write(currentToken.Value, currentToken.Start, currentToken.Length);
                    return currentToken.Length;
                }

                //Got the endif token, exit to the parent "if" scope if it exists
                if (IsTokenIndexOfType(tokenIndex, EndTokenBaseIndex))
                {
                    logger.LogTrace("Got 'endif' token");
                    if (_pendingCompletion.Count > 0)
                    {
                        _current = _pendingCompletion.Pop();
                        _current.ToggleActionableOperations(_current.ActionableOperationsEnabled, processor);
                    }
                    else
                    {
                        // disable the special case operations (note: they may already be disabled, but cheaper to do than check)
                        _current.ToggleActionableOperations(false, processor);
                        _current = null;
                    }

                    if (_definition.WholeLine)
                    {
                        logger.LogTrace("Seeking forward until line end");
                        processor.SeekBufferForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                    }
                    else if (_definition.TrimWhitespace)
                    {
                        logger.LogTrace("Trimming whitespace");
                        processor.TrimWhitespace(true, false, ref bufferLength, ref currentBufferPosition);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                    }

                    return 0;
                }

                if (_current.BranchTaken)
                {
                    logger.LogTrace("Branch taken, skiiping target back until line end");
                    processor.SeekTargetBackUntil(processor.EncodingConfig.LineEndings, true);
                    //A previous branch was taken. Skip to the endif token.
                    // NOTE: this can probably use the new method SeekToNextTokenAtSameLevel() - they do almost the same thing.
                    logger.LogTrace("Branch taken, skipping until 'endif'");
                    SkipToMatchingEndif(processor, ref bufferLength, ref currentBufferPosition, ref tokenIndex);
                    logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                    logger.LogTrace("Current buffer length: {0}", bufferLength);

                    if (_pendingCompletion.Count > 0)
                    {
                        _current = _pendingCompletion.Pop();
                        _current.ToggleActionableOperations(_current.ActionableOperationsEnabled, processor);
                    }
                    else
                    {
                        // disable the special case operation (note: it may already be disabled, but cheaper to do than check)
                        _current.ToggleActionableOperations(false, processor);
                        _current = null;
                    }

                    if (_definition.WholeLine)
                    {
                        logger.LogTrace("Seeking forward until line end");
                        processor.SeekBufferForwardUntil(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                    }
                    else if (_definition.TrimWhitespace)
                    {
                        logger.LogTrace("Trimming whitespace");
                        processor.TrimWhitespace(true, false, ref bufferLength, ref currentBufferPosition);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                    }
                    return 0;
                }

                //We have an "elseif" and haven't taken a previous branch
                if (IsTokenIndexOfType(tokenIndex, ElseIfTokenBaseIndex) || IsTokenIndexOfType(tokenIndex, ElseIfTokenActionableBaseIndex))
                {
                    logger.LogDebug("Got 'elseif' token");
                    // 8-19 attempt to make the same as if() handling
                    //
                    if (_current.Evaluate(processor, ref bufferLength, ref currentBufferPosition))
                    {
                        logger.LogTrace("'elseif' condition evaluated to 'true'");
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                        if (_definition.WholeLine)
                        {
                            logger.LogTrace("Seeking forward until line end");
                            processor.SeekBufferForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                            logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                            logger.LogTrace("Current buffer length: {0}", bufferLength);
                        }

                        if (IsTokenIndexOfType(tokenIndex, ElseIfTokenActionableBaseIndex))
                        {
                            // the elseif branch is taken.
                            _current.ToggleActionableOperations(true, processor);
                        }

                        return 0;
                    }
                    else
                    {
                        logger.LogTrace("'if' condition evaluated to 'false', skipping to next token at same level.");
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);
                        SeekToNextTokenAtSameLevel(processor, ref bufferLength, ref currentBufferPosition, out tokenIndex);
                        logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                        logger.LogTrace("Current buffer length: {0}", bufferLength);

                        // In the original version this was conditional on SeekToToken() succeeding.
                        // Not sure if it should be conditional. It should never fail, unless the template is malformed.
                        goto BEGIN;
                    }
                }

                //We have an "else" token and haven't taken any other branches, return control
                //  after setting that a branch has been taken
                if (IsTokenIndexOfType(tokenIndex, ElseTokenBaseIndex) || IsTokenIndexOfType(tokenIndex, ElseTokenActionableBaseIndex))
                {
                    logger.LogTrace("Got 'else' token");
                    if (IsTokenIndexOfType(tokenIndex, ElseTokenActionableBaseIndex))
                    {
                        _current.ToggleActionableOperations(true, processor);
                    }

                    _current.BranchTaken = true;
                    logger.LogTrace("handling whitespaces");
                    processor.WhitespaceHandler(ref bufferLength, ref currentBufferPosition, wholeLine: _definition.WholeLine, trim: _definition.TrimWhitespace);
                    logger.LogTrace("Current buffer position: {0}", currentBufferPosition);
                    logger.LogTrace("Current buffer length: {0}", bufferLength);
                    return 0;
                }
                else
                {
                    Debug.Assert(true, "Unknown token index: " + tokenIndex);
                    return 0;   // TODO: revisit. Not sure what's best here.
                }
            }

            // moves the buffer to the next token at the same level.
            // Returns false if no end token can be found at the same level.
            //      this is probably indicative of a template authoring problem, or possibly a buffer problem.
            private bool SkipToMatchingEndif(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition, ref int token)
            {
                while (!IsTokenIndexOfType(token, EndTokenBaseIndex))
                {
                    bool seekSucceeded = SeekToNextTokenAtSameLevel(processor, ref bufferLength, ref currentBufferPosition, out token);

                    if (!seekSucceeded)
                    {
                        return false;
                    }
                }

                return true;
            }

            // Moves the buffer to the next token at the same level of nesting as the current token.
            // Should never be called if we're on an end token!!!
            // Returns false if no next token can be found at the same level.
            //      this is probably indicative of a template authoring problem, or possibly a buffer problem.
            private bool SeekToNextTokenAtSameLevel(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition, out int token)
            {
                if (_definition.WholeLine)
                {
                    processor.SeekBufferForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                }

                bool seekSucceeded = SeekToToken(processor, ref bufferLength, ref currentBufferPosition, out token);

                //Keep on scanning until we've hit a balancing token that belongs to us
                // each "if" found opens a new level of nesting
                while (IsTokenIndexOfType(token, IfTokenBaseIndex) || IsTokenIndexOfType(token, IfTokenActionableBaseIndex))
                {
                    int open = 1;
                    while (open != 0)
                    {
                        seekSucceeded &= SeekToToken(processor, ref bufferLength, ref currentBufferPosition, out token);

                        if (IsTokenIndexOfType(token, IfTokenBaseIndex) || IsTokenIndexOfType(token, IfTokenActionableBaseIndex))
                        {
                            ++open;
                        }
                        else if (IsTokenIndexOfType(token, EndTokenBaseIndex))
                        {
                            --open;
                        }
                    }

                    seekSucceeded &= SeekToToken(processor, ref bufferLength, ref currentBufferPosition, out token);
                }

                // this may be irrelevant. If it happens, the template is malformed (i think)
                return seekSucceeded;
            }

            // moves to the next token
            // returns false if the end of the buffer was reached without finding a token.
            private bool SeekToToken(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition, out int token)
            {
                bool bufferAdvanceFailed = false;
                ITokenTrieEvaluator evaluator = _trie.CreateEvaluator();

                while (true)
                {
                    for (; currentBufferPosition < bufferLength; ++currentBufferPosition)
                    {
                        if (evaluator.Accept(processor.CurrentBuffer[currentBufferPosition], ref currentBufferPosition, out token))
                        {
                            if (bufferAdvanceFailed || (currentBufferPosition != bufferLength))
                            {
                                return true;
                            }
                        }
                    }

                    if (bufferAdvanceFailed)
                    {
                        if (evaluator.TryFinalizeMatchesInProgress(ref currentBufferPosition, out token))
                        {
                            return true;
                        }

                        break;
                    }

                    bufferAdvanceFailed = !processor.AdvanceBuffer(bufferLength - evaluator.BytesToKeepInBuffer);
                    currentBufferPosition = evaluator.BytesToKeepInBuffer;
                    bufferLength = processor.CurrentBufferLength;
                }

                //If we run out of places to look, assert that the end of the buffer is the end
                token = EndTokenBaseIndex;
                currentBufferPosition = bufferLength;
                return false;   // no terminator found
            }

            private class EvaluationState
            {
                private readonly Impl _impl;
                private bool _branchTaken;

                public EvaluationState(Impl impl)
                {
                    _impl = impl;
                    ActionableOperationsEnabled = false;
                }

                public bool BranchTaken
                {
                    get { return _branchTaken; }
                    set { _branchTaken |= value; }
                }

                public bool ActionableOperationsEnabled { get; private set; }

                public void ToggleActionableOperations(bool enabled, IProcessorState processor)
                {
                    ActionableOperationsEnabled = enabled;

                    foreach (string otherOptionDisableFlag in _impl._definition.Tokens.ActionableOperations)
                    {
                        processor.Config.Flags[otherOptionDisableFlag] = enabled;
                    }
                }

                internal bool Evaluate(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition)
                {
                    BranchTaken = _impl._definition.Evaluator(processor, ref bufferLength, ref currentBufferPosition, out bool faulted);
                    return BranchTaken;
                }
            }
        }
    }
}
