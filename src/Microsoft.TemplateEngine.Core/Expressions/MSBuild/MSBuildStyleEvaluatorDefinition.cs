﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Util;

namespace Microsoft.TemplateEngine.Core.Expressions.MSBuild
{
    public class MSBuildStyleEvaluatorDefinition
    {
        private static readonly Dictionary<Encoding, ITokenTrie> TokenCache = new Dictionary<Encoding, ITokenTrie>();

        private static ITokenTrie GetSymbols(IProcessorState processor)
        {
            ITokenTrie tokens;
            if (!TokenCache.TryGetValue(processor.Encoding, out tokens))
            {
                TokenTrie trie = new TokenTrie();

                //Logic
                trie.AddToken(processor.Encoding.GetBytes("AND"));
                trie.AddToken(processor.Encoding.GetBytes("OR"));
                trie.AddToken(processor.Encoding.GetBytes("!"));
                trie.AddToken(processor.Encoding.GetBytes("&gt;"));
                trie.AddToken(processor.Encoding.GetBytes("&gt;="));
                trie.AddToken(processor.Encoding.GetBytes("&lt;"));
                trie.AddToken(processor.Encoding.GetBytes("&lt;="));
                trie.AddToken(processor.Encoding.GetBytes("=="));
                trie.AddToken(processor.Encoding.GetBytes("!="));

                //Braces
                trie.AddToken(processor.Encoding.GetBytes("("));
                trie.AddToken(processor.Encoding.GetBytes(")"));

                //Whitespace
                trie.AddToken(processor.Encoding.GetBytes(" "));
                trie.AddToken(processor.Encoding.GetBytes("\t"));

                //EOLs
                trie.AddToken(processor.Encoding.GetBytes("\r\n"));
                trie.AddToken(processor.Encoding.GetBytes("\n"));
                trie.AddToken(processor.Encoding.GetBytes("\r"));

                // quotes
                trie.AddToken(processor.Encoding.GetBytes("'"));
                TokenCache[processor.Encoding] = tokens = trie;
            }

            return tokens;
        }

        private enum Tokens
        {
            And = 0,
            Or = 1,
            Not = 2,
            GreaterThan = 3,
            GreaterThanOrEqualTo = 4,
            LessThan = 5,
            LessThanOrEqualTo = 6,
            EqualTo = 7,
            NotEqualTo = 8,
            OpenBrace = 9,
            CloseBrace = 10,
            Space = 11,
            Tab = 12,
            WindowsEOL = 13,
            UnixEOL = 14,
            LegacyMacEOL = 15,
            Quote = 16,
            Literal = 17,
        }

        private static readonly IReadOnlyDictionary<Tokens, Operators> TokensToOperatorsMap = new Dictionary<Tokens, Operators>
        {
            {Tokens.And, Operators.And},
            {Tokens.Or, Operators.Or},
            {Tokens.Not, Operators.Not},
            {Tokens.GreaterThan, Operators.GreaterThan},
            {Tokens.GreaterThanOrEqualTo, Operators.GreaterThanOrEqualTo},
            {Tokens.LessThan, Operators.LessThan},
            {Tokens.LessThanOrEqualTo, Operators.LessThanOrEqualTo},
            {Tokens.EqualTo, Operators.EqualTo},
            {Tokens.NotEqualTo, Operators.NotEqualTo},
        };

        public static bool MSBuildStyleEvaluator(IProcessorState processor, ref int bufferLength, ref int currentBufferPosition, out bool faulted)
        {
            ITokenTrie tokens = GetSymbols(processor);
            ScopeBuilder<Operators, Tokens> builder = new ScopeBuilder<Operators, Tokens>(processor, tokens, processor.Config.Variables,
                TokensToOperatorsMap, CommonOperators.OperatorScopeFactoryLookup, true,
                Tokens.OpenBrace, Tokens.CloseBrace,
                Tokens.Literal, Operators.Identity,
                new HashSet<Tokens> {Tokens.Quote},
                new HashSet<Tokens> {Tokens.Space, Tokens.Tab},
                new HashSet<Tokens> {Tokens.WindowsEOL, Tokens.UnixEOL, Tokens.LegacyMacEOL});
            bool isFaulted = false;
            IEvaluable result = builder.Build(ref bufferLength, ref currentBufferPosition, x => isFaulted = true);

            if (isFaulted)
            {
                faulted = true;
                return false;
            }

            try
            {
                object evalResult = result.Evaluate();
                bool r = (bool) Convert.ChangeType(evalResult, typeof(bool));
                faulted = false;
                return r;
            }
            catch
            {
                faulted = true;
                return false;
            }
        }
    }
}
