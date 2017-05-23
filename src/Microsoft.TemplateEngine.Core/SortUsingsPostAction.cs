using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Util;

namespace Microsoft.TemplateEngine.Core
{
    public class SortUsingsPostAction : ICommonPostActionHandler
    {
        public static readonly Guid ActionProcessorId = new Guid("8A0FF3C4-CE1F-4958-B0AB-C94D40E36EED");

        public Guid Id => ActionProcessorId;

        public bool Process(IEngineEnvironmentSettings settings, IPostAction actionConfig, ICreationResult templateCreationResult, string outputBasePath)
        {
            foreach(string file in Directory.EnumerateFiles(outputBasePath, "*.cs", SearchOption.AllDirectories))
            {
                byte[] fileData = File.ReadAllBytes(file);
                Encoding fileEncoding = EncodingUtil.Detect(fileData, fileData.Length, out byte[] bom);
                string allText = fileEncoding.GetString(fileData.Skip(bom.Length).ToArray());
                TokenTrie trie = new TokenTrie();
                HashSet<int> seenUsings = new HashSet<int>();
                int currentBufferPosition = 0;

                if(!actionConfig.Args.TryGetValue("SystemFirst", out string systemFirstString) || !bool.TryParse(systemFirstString, out bool systemFirst))
                {
                    systemFirst = true;
                }

                IComparer<string> usingDirectiveComparer = new UsingDirectiveComparer(systemFirst);

                IReadOnlyList<string> usingDirectives = allText.Split(new[]{ '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(IsUsingDirective).OrderBy(x => x, usingDirectiveComparer).ToList();

                foreach (string usingDirective in usingDirectives)
                {
                    trie.AddToken(fileEncoding.GetBytes(usingDirective + "\n"));
                }

                bool matched = false;
                using (Stream outputFile = File.Create(file))
                {
                    while (currentBufferPosition < fileData.Length)
                    {
                        if (!trie.GetOperation(fileData, fileData.Length, ref currentBufferPosition, out int token))
                        {
                            outputFile.WriteByte(fileData[currentBufferPosition++]);
                        }
                        else if (!matched)
                        {
                            matched = true;
                            foreach (string directive in usingDirectives)
                            {
                                byte[] tokenBytes = fileEncoding.GetBytes(directive + "\n");
                                outputFile.Write(tokenBytes, 0, tokenBytes.Length);
                            }
                        }
                    }

                    outputFile.Flush();
                }
            }

            return true;
        }

        private static bool IsUsingDirective(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("using ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal);
        }

        private class UsingDirectiveComparer : IComparer<string>
        {
            private bool _placeSystemDirectivesFirst;

            public UsingDirectiveComparer(bool placeSystemDirectivesFirst)
            {
                _placeSystemDirectivesFirst = placeSystemDirectivesFirst;
            }

            public int Compare(string x, string y)
            {
                if (!_placeSystemDirectivesFirst)
                {
                    return StringComparer.Ordinal.Compare(x.Trim().TrimEnd(';'), y.Trim().TrimEnd(';'));
                }

                bool xIsSystem = x.IndexOf(" System.", StringComparison.Ordinal) > -1 || x.IndexOf(" System;", StringComparison.Ordinal) > -1;
                bool yIsSystem = y.IndexOf(" System.", StringComparison.Ordinal) > -1 || y.IndexOf(" System;", StringComparison.Ordinal) > -1;

                if (xIsSystem && !yIsSystem)
                {
                    return -1;
                }
                else if (!xIsSystem && yIsSystem)
                {
                    return 1;
                }

                return StringComparer.Ordinal.Compare(x.Trim().TrimEnd(';'), y.Trim().TrimEnd(';'));
            }
        }
    }
}
