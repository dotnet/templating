using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Util;

namespace Microsoft.TemplateEngine.Core
{
    public class RemoveDuplicateUsingsPostAction : ICommonPostActionHandler
    {
        public static readonly Guid ActionProcessorId = new Guid("C405DDB2-4F64-4A2D-8005-1243D5C55EAD");

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

                foreach (string usingDirective in allText.Split(new[]{ '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(IsUsingDirective))
                {
                    trie.AddToken(fileEncoding.GetBytes(usingDirective + "\n"));
                }

                using (Stream outputFile = File.Create(file))
                {
                    while (currentBufferPosition < fileData.Length)
                    {
                        if (!trie.GetOperation(fileData, fileData.Length, ref currentBufferPosition, out int token))
                        {
                            outputFile.WriteByte(fileData[currentBufferPosition++]);
                        }
                        else if(seenUsings.Add(token))
                        {
                            byte[] tokenBytes = trie.Tokens[token].Value;
                            outputFile.Write(tokenBytes, 0, tokenBytes.Length);
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
    }
}
