// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Util;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Core.Expressions.Cpp
{
    public static class CppStyleEvaluatorDefinition
    {
        private const int ReservedTokenCount = 24;
        private const int ReservedTokenMaxIndex = ReservedTokenCount - 1;
        private static readonly IOperationProvider[] NoOperationProviders = Array.Empty<IOperationProvider>();
        private static readonly char[] SupportedQuotes = { '"', '\'' };

        public static bool EvaluateFromString(ILogger logger, string text, IVariableCollection variables)
        {
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(text)))
            using (MemoryStream res = new())
            {
                EngineConfig cfg = new(logger, variables);
                IProcessorState state = new ProcessorState(ms, res, (int)ms.Length, (int)ms.Length, cfg, NoOperationProviders);
                int len = (int)ms.Length;
                int pos = 0;
                return Evaluate(state, ref len, ref pos, out bool faulted);
            }
        }

        public static bool Evaluate(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition, out bool faulted)
        {
            ILogger? logger = processor.Config.Logger;
            logger.LogTrace("{0}.{1} started: {2}:{3}, {4}:{5}.", nameof(CppStyleEvaluatorDefinition), nameof(Evaluate), nameof(bufferLength), bufferLength, nameof(currentBufferPosition), currentBufferPosition);
            faulted = false;
            TokenTrie trie = new();

            //Logic
            trie.AddToken(processor.Encoding.GetBytes("&&"), 0);
            trie.AddToken(processor.Encoding.GetBytes("||"), 1);
            trie.AddToken(processor.Encoding.GetBytes("^"), 2);
            trie.AddToken(processor.Encoding.GetBytes("!"), 3);
            trie.AddToken(processor.Encoding.GetBytes(">"), 4);
            trie.AddToken(processor.Encoding.GetBytes(">="), 5);
            trie.AddToken(processor.Encoding.GetBytes("<"), 6);
            trie.AddToken(processor.Encoding.GetBytes("<="), 7);
            trie.AddToken(processor.Encoding.GetBytes("=="), 8);
            trie.AddToken(processor.Encoding.GetBytes("="), 9);
            trie.AddToken(processor.Encoding.GetBytes("!="), 10);

            //Bitwise
            trie.AddToken(processor.Encoding.GetBytes("&"), 11);
            trie.AddToken(processor.Encoding.GetBytes("|"), 12);
            trie.AddToken(processor.Encoding.GetBytes("<<"), 13);
            trie.AddToken(processor.Encoding.GetBytes(">>"), 14);

            //Braces
            trie.AddToken(processor.Encoding.GetBytes("("), 15);
            trie.AddToken(processor.Encoding.GetBytes(")"), 16);

            //Whitespace
            trie.AddToken(processor.Encoding.GetBytes(" "), 17);
            trie.AddToken(processor.Encoding.GetBytes("\t"), 18);

            //EOLs
            trie.AddToken(processor.Encoding.GetBytes("\r\n"), 19);
            trie.AddToken(processor.Encoding.GetBytes("\n"), 20);
            trie.AddToken(processor.Encoding.GetBytes("\r"), 21);

            // quotes
            trie.AddToken(processor.Encoding.GetBytes("\""), 22);
            trie.AddToken(processor.Encoding.GetBytes("'"), 23);

            //Tokens
            trie.Append(processor.EncodingConfig.Variables);

            logger.LogTrace("Adding variables:");
            foreach (IToken? t in processor.EncodingConfig.Variables.Tokens)
            {
                logger.LogTrace("{0}", processor.Encoding.GetString(t.Value));
            }

            //Run forward to EOL and collect args
            TokenFamily currentTokenFamily;
            List<byte> currentTokenBytes = new();
            List<TokenRef> tokens = new();
            logger.LogTrace("Getting operation #1");
            if (!trie.GetOperation(processor.CurrentBuffer, bufferLength, ref currentBufferPosition, out int token))
            {
                logger.LogTrace("Getting operation failed, position: {0}", currentBufferPosition);
                currentTokenFamily = TokenFamily.Literal;
                currentTokenBytes.Add(processor.CurrentBuffer[currentBufferPosition++]);
                logger.LogTrace("Consider token as: {0}", TokenFamily.Literal);
                logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
            }
            else if (token > ReservedTokenMaxIndex)
            {
                logger.LogTrace("Token over max, position: {0}", currentBufferPosition);
                currentTokenFamily = TokenFamily.Reference | (TokenFamily)token;
                logger.LogTrace("Token: {0}({1},{2})", token, TokenFamily.Reference, (TokenFamily)token);
                tokens.Add(new TokenRef
                {
                    Family = currentTokenFamily
                });
            }
            else
            {
                logger.LogTrace("Getting succeeded failed, position: {0}", currentBufferPosition);
                logger.LogTrace("Token: {0}({1})", token, (TokenFamily)token);
                currentTokenFamily = (TokenFamily)token;

                if (currentTokenFamily != TokenFamily.WindowsEOL && currentTokenFamily != TokenFamily.LegacyMacEOL && currentTokenFamily != TokenFamily.UnixEOL)
                {
                    tokens.Add(new TokenRef
                    {
                        Family = currentTokenFamily
                    });
                }
                else
                {
                    logger.LogTrace("Eol, evaluating");
                    LogEvaluatedTokens(logger, tokens);
                    return EvaluateCondition(tokens, processor.EncodingConfig.VariableValues);
                }
            }

            int braceDepth = 0;
            if (tokens.Count > 0 && tokens[0].Family == TokenFamily.OpenBrace)
            {
                ++braceDepth;
            }

            bool first = true;
            QuotedRegionKind inQuoteType = QuotedRegionKind.None;

            while ((first || braceDepth > 0) && bufferLength > 0)
            {
                int targetLen = Math.Min(bufferLength, trie.MaxLength);
                logger.LogTrace("Position: {0}", currentBufferPosition);
                logger.LogTrace("bufferLength: {0}", bufferLength);
                logger.LogTrace("targetLen: {0}", targetLen);
                for (; currentBufferPosition < bufferLength - targetLen + 1;)
                {
                    int oldBufferPos = currentBufferPosition;
                    logger.LogTrace("Position: {0}", currentBufferPosition);
                    logger.LogTrace("Getting operation #2");
                    if (trie.GetOperation(processor.CurrentBuffer, bufferLength, ref currentBufferPosition, out token))
                    {
                        logger.LogTrace("Getting operation succeeded");
                        logger.LogTrace("Position: {0}", currentBufferPosition);
                        logger.LogTrace("Token: {0} ({1})", token, (TokenFamily)token);
                        if (braceDepth == 0)
                        {
                            switch (tokens[tokens.Count - 1].Family)
                            {
                                case TokenFamily.Whitespace:
                                case TokenFamily.Tab:
                                case TokenFamily.CloseBrace:
                                case TokenFamily.WindowsEOL:
                                case TokenFamily.UnixEOL:
                                case TokenFamily.LegacyMacEOL:
                                    TokenFamily thisFamily = (TokenFamily)token;
                                    if (thisFamily == TokenFamily.WindowsEOL || thisFamily == TokenFamily.UnixEOL || thisFamily == TokenFamily.LegacyMacEOL)
                                    {
                                        currentBufferPosition = oldBufferPos;
                                        logger.LogTrace("Changing position to: {0}", currentBufferPosition);
                                    }

                                    break;
                                default:
                                    currentBufferPosition = oldBufferPos;
                                    logger.LogTrace("Default, Changing position to: {0}", currentBufferPosition);
                                    first = false;
                                    break;
                            }

                            if (!first)
                            {
                                logger.LogTrace("Not first, breaking.");
                                break;
                            }
                        }

                        // We matched an item, so whatever this is, it's not a literal.
                        // if the current token is a literal, end it.
                        if (currentTokenFamily == TokenFamily.Literal)
                        {
                            string literal = processor.Encoding.GetString(currentTokenBytes.ToArray());
                            tokens.Add(new TokenRef
                            {
                                Family = TokenFamily.Literal,
                                Literal = literal
                            });
                            currentTokenBytes.Clear();
                            logger.LogDebug("Matched literal: {0}", literal);
                        }

                        TokenFamily foundTokenFamily = (TokenFamily)token;

                        if (foundTokenFamily == TokenFamily.QuotedLiteral || foundTokenFamily == TokenFamily.SingleQuotedLiteral)
                        {
                            QuotedRegionKind incomingQuoteKind = foundTokenFamily switch
                            {
                                TokenFamily.QuotedLiteral => QuotedRegionKind.DoubleQuoteRegion,
                                TokenFamily.SingleQuotedLiteral => QuotedRegionKind.SingleQuoteRegion,
                                _ => QuotedRegionKind.None,
                            };
                            if (inQuoteType == QuotedRegionKind.None)
                            {
                                // starting quote found
                                currentTokenBytes.AddRange(trie.Tokens[token].Value);
                                logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
                                inQuoteType = incomingQuoteKind;
                            }
                            else if (incomingQuoteKind == inQuoteType)
                            {
                                // end quote found
                                currentTokenBytes.AddRange(trie.Tokens[token].Value);
                                string? literal = processor.Encoding.GetString(currentTokenBytes.ToArray());
                                tokens.Add(new TokenRef
                                {
                                    Family = TokenFamily.Literal,
                                    Literal = literal
                                });
                                currentTokenBytes.Clear();
                                inQuoteType = QuotedRegionKind.None;
                                logger.LogTrace("Matched literal: {0}", literal);
                            }
                            else
                            {
                                // this is a different quote type. Treat it like a non-match, just add the token to the currentTokenBytes
                                currentTokenBytes.AddRange(trie.Tokens[token].Value);
                                logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
                            }
                        }
                        else if (inQuoteType != QuotedRegionKind.None)
                        {
                            // we're inside a quoted literal, the token found by the trie should not be processed, just included with the literal
                            currentTokenBytes.AddRange(trie.Tokens[token].Value);
                            logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
                        }
                        else if (token > ReservedTokenMaxIndex)
                        {
                            currentTokenFamily = TokenFamily.Reference | (TokenFamily)token;
                            tokens.Add(new TokenRef
                            {
                                Family = currentTokenFamily
                            });
                            logger.LogTrace("Token over max, position: {0}", currentBufferPosition);
                            logger.LogTrace("Token: {0}({1},{2})", token, TokenFamily.Reference, (TokenFamily)token);
                        }
                        else
                        {
                            //If we have a normal token...
                            currentTokenFamily = (TokenFamily)token;
                            logger.LogTrace("Token: {0}({1})", token, (TokenFamily)token);
                            if (currentTokenFamily != TokenFamily.WindowsEOL && currentTokenFamily != TokenFamily.LegacyMacEOL && currentTokenFamily != TokenFamily.UnixEOL)
                            {
                                switch (currentTokenFamily)
                                {
                                    case TokenFamily.OpenBrace:
                                        ++braceDepth;
                                        break;
                                    case TokenFamily.CloseBrace:
                                        --braceDepth;
                                        break;
                                }

                                tokens.Add(new TokenRef
                                {
                                    Family = currentTokenFamily
                                });
                            }
                            else
                            {
                                logger.LogTrace("EOL #2, evaluating");
                                LogEvaluatedTokens(logger, tokens);
                                return EvaluateCondition(tokens, processor.EncodingConfig.VariableValues);
                            }
                        }
                    }
                    else if (inQuoteType != QuotedRegionKind.None)
                    {
                        logger.LogTrace("Not a token, quotes");
                        logger.LogTrace("Position: {0}, increasing", currentBufferPosition);
                        // we're in a quoted literal but did not match a token at the current position.
                        // so just add the current byte to the currentTokenBytes
                        currentTokenBytes.Add(processor.CurrentBuffer[currentBufferPosition++]);
                        logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
                    }
                    else if (braceDepth > 0)
                    {
                        logger.LogTrace("Not a token, inside braces");
                        logger.LogTrace("Position: {0}, increasing", currentBufferPosition);
                        currentTokenFamily = TokenFamily.Literal;
                        currentTokenBytes.Add(processor.CurrentBuffer[currentBufferPosition++]);
                        logger.LogTrace("currentTokenBytes: {0}", processor.Encoding.GetString(currentTokenBytes.ToArray()));
                    }
                    else
                    {
                        logger.LogTrace("Not a token, evaluating");
                        logger.LogTrace("Position: {0}", currentBufferPosition);
                        LogEvaluatedTokens(logger, tokens);
                        return EvaluateCondition(tokens, processor.EncodingConfig.VariableValues);
                    }
                }
                logger.LogTrace("Advancing buffer position: {0}", currentBufferPosition);
                processor.AdvanceBuffer(currentBufferPosition);
                currentBufferPosition = processor.CurrentBufferPosition;
                bufferLength = processor.CurrentBufferLength;
                logger.LogTrace("Current position: {0}", currentBufferPosition);
                logger.LogTrace("Current buffer length: {0}", bufferLength);
            }

#if DEBUG
            Debug.Assert(
                inQuoteType == QuotedRegionKind.None,
                $"Malformed predicate due to unmatched quotes. InitialBuffer = {processor.Encoding.GetString(processor.CurrentBuffer)} currentTokenFamily = {currentTokenFamily} | TokenFamily.QuotedLiteral = {TokenFamily.QuotedLiteral} | TokenFamily.SingleQuotedLiteral = {TokenFamily.SingleQuotedLiteral}");
#endif
            logger.LogTrace("Finished, evaluating");
            LogEvaluatedTokens(logger, tokens);
            return EvaluateCondition(tokens, processor.EncodingConfig.VariableValues);
        }

        private static void LogEvaluatedTokens(ILogger logger, List<TokenRef> tokens)
        {
            logger.LogTrace("Discovered tokens:");
            foreach (TokenRef? t in tokens)
            {
                logger.LogTrace("{0}:{1}", t.Literal, t.Family.ToString());
            }
        }

        private static bool IsLogicalOperator(Operator op)
        {
            return op == Operator.And || op == Operator.Or || op == Operator.Xor || op == Operator.Not;
        }

        private static void CombineExpressionOperator(ref Scope current, Stack<Scope> parents)
        {
            if (current.Operator == Operator.None)
            {
                if (current.TargetPlacement != Scope.NextPlacement.Right
                    || current.Left is not Scope leftScope
                    || !IsLogicalOperator(leftScope.Operator))
                {
                    return;
                }

                Scope tmp2 = new()
                {
                    Value = leftScope.Right
                };

                leftScope.Right = tmp2;
                parents.Push(leftScope);
                current = tmp2;
                return;
            }

            Scope tmp = new();

            if (!IsLogicalOperator(current.Operator))
            {
                tmp.Value = current;
            }
            else
            {
                tmp.Value = current.Right;
                current.Right = tmp;
                parents.Push(current);
            }

            current = tmp;
        }

        private static bool EvaluateCondition(IReadOnlyList<TokenRef> tokens, IReadOnlyList<Func<object>> values)
        {
            //Skip over all leading whitespace
            int i = 0;
            for (; i < tokens.Count && (tokens[i].Family == TokenFamily.Whitespace || tokens[i].Family == TokenFamily.Tab); ++i)
            {
            }

            //Scan through all remaining tokens and put them in a form the standard evaluator can understand
            List<TokenRef> outputTokens = new();
            for (; i < tokens.Count; ++i)
            {
                if (tokens[i].Family == TokenFamily.Whitespace || tokens[i].Family == TokenFamily.Tab)
                {
                    //Ignore whitespace
                }
                else
                {
                    if (tokens[i].Family.HasFlag(TokenFamily.Reference))
                    {
                        outputTokens.Add(tokens[i]);
                    }
                    else if (tokens[i].Family == TokenFamily.Literal)
                    {
                        //Combine literals
                        string literalValue = tokens[i].Literal;
                        string followingWhitespace = "";
                        int reach = i;

                        for (int j = i + 1; j < tokens.Count; ++j)
                        {
                            switch (tokens[j].Family)
                            {
                                case TokenFamily.Literal:
                                    literalValue += followingWhitespace + tokens[j].Literal;
                                    followingWhitespace = string.Empty;
                                    reach = j;
                                    break;
                                case TokenFamily.Tab:
                                    followingWhitespace += '\t';
                                    break;
                                case TokenFamily.Whitespace:
                                    followingWhitespace += ' ';
                                    break;
                                default:
                                    j = tokens.Count;
                                    break;
                            }
                        }

                        i = reach;
                        outputTokens.Add(new TokenRef
                        {
                            Family = TokenFamily.Literal,
                            Literal = literalValue
                        });
                    }
                    else
                    {
                        outputTokens.Add(tokens[i]);
                    }
                }
            }

            Scope root = new();
            Scope current = root;
            Stack<Scope> parents = new();

            for (i = 0; i < outputTokens.Count; ++i)
            {
                switch (outputTokens[i].Family)
                {
                    case TokenFamily.Not:
                        {
                            Scope nextScope = new()
                            {
                                TargetPlacement = Scope.NextPlacement.Right
                            };

                            current.Value = nextScope;
                            parents.Push(current);
                            current = nextScope;
                            current.Operator = Operator.Not;
                            break;
                        }
                    case TokenFamily.Literal:
                        current.Value = InferTypeAndConvertLiteral(outputTokens[i].Literal);

                        while (parents.Count > 0 && current.TargetPlacement == Scope.NextPlacement.None)
                        {
                            current = parents.Pop();
                        }
                        break;
                    case TokenFamily.And:
                        if (current.Operator != Operator.None)
                        {
                            Scope s = new()
                            {
                                Value = current
                            };

                            current = s;
                        }

                        current.Operator = Operator.And;
                        current.TargetPlacement = Scope.NextPlacement.Right;
                        break;
                    case TokenFamily.BitwiseAnd:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.BitwiseAnd;
                        break;
                    case TokenFamily.BitwiseOr:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.BitwiseOr;
                        break;
                    case TokenFamily.CloseBrace:
                        //If the grouping is valid, the grouping has already been closed
                        //  due to argument fulfillment, do nothing.
                        // -- OR --
                        //This is a grouping around a unary operator
                        if (parents.Count > 0 && current.TargetPlacement == Scope.NextPlacement.Right)
                        {
                            current = parents.Pop();
                        }
                        break;
                    case TokenFamily.EqualTo:
                    case TokenFamily.EqualToShort:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.EqualTo;
                        break;
                    case TokenFamily.GreaterThan:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.GreaterThan;
                        break;
                    case TokenFamily.GreaterThanOrEqualTo:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.GreaterThanOrEqualTo;
                        break;
                    case TokenFamily.LeftShift:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.LeftShift;
                        break;
                    case TokenFamily.LessThan:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.LessThan;
                        break;
                    case TokenFamily.LessThanOrEqualTo:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.LessThanOrEqualTo;
                        break;
                    case TokenFamily.NotEqualTo:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.NotEqualTo;
                        break;
                    case TokenFamily.OpenBrace:
                        {
                            Scope nextScope = new();
                            current.Value = nextScope;
                            parents.Push(current);
                            current = nextScope;
                            break;
                        }
                    case TokenFamily.Or:
                        if (current.Operator != Operator.None)
                        {
                            Scope s = new()
                            {
                                Value = current
                            };

                            current = s;
                        }

                        current.Operator = Operator.Or;
                        break;
                    case TokenFamily.RightShift:
                        CombineExpressionOperator(ref current, parents);
                        current.Operator = Operator.RightShift;
                        break;
                    case TokenFamily.Xor:
                        if (current.Operator != Operator.None)
                        {
                            Scope s = new()
                            {
                                Value = current
                            };

                            current = s;
                        }

                        current.Operator = Operator.Xor;
                        break;
                    default:
                        current.Value = ResolveToken(outputTokens[i], values);

                        while (parents.Count > 0 && current.TargetPlacement == Scope.NextPlacement.None)
                        {
                            current = parents.Pop();
                        }

                        break;
                }
            }

            Debug.Assert(parents.Count == 0, "Unbalanced condition");
            return (bool)Convert.ChangeType(current.Evaluate() ?? "false", typeof(bool));
        }

        private static object? InferTypeAndConvertLiteral(string literal)
        {
            //A propertly quoted string must be...
            //  At least two characters long
            //  Start and end with the same character
            //  The character that the string starts with must be one of the supported quote kinds
            if (literal.Length < 2 || literal[0] != literal[literal.Length - 1] || !SupportedQuotes.Contains(literal[0]))
            {
                if (string.Equals(literal, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(literal, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.Equals(literal, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if ((literal.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    || literal.Contains(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
                    && ParserExtensions.DoubleTryParseСurrentOrInvariant(literal, out double literalDouble))
                {
                    return literalDouble;
                }

                if (long.TryParse(literal, out long literalLong))
                {
                    return literalLong;
                }

                if (literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(literal.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out literalLong))
                {
                    return literalLong;
                }

                return null;
            }

            return literal.Substring(1, literal.Length - 2);
        }

        private static object ResolveToken(TokenRef tokenRef, IReadOnlyList<Func<object>> values)
        {
            return values[(int)(tokenRef.Family & ~TokenFamily.Reference) - ReservedTokenCount]();
        }
    }
}
