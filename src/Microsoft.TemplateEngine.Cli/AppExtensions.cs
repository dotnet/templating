// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Cli
{
    internal static class AppExtensions
    {
        public static IReadOnlyList<string> CreateArgListFromAdditionalFiles(IList<string> extraArgFileNames, IJsonDocumentObjectModelFactory jsonDomFactory)
        {
            IReadOnlyDictionary<string, IReadOnlyList<string>> argsDict = ParseArgsFromFile(extraArgFileNames, jsonDomFactory);

            List<string> argsFlattened = new List<string>();
            foreach (KeyValuePair<string, IReadOnlyList<string>> oneArg in argsDict)
            {
                argsFlattened.Add(oneArg.Key);
                if (oneArg.Value.Count > 0)
                {
                    argsFlattened.AddRange(oneArg.Value);
                }
            }

            return argsFlattened;
        }

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseArgsFromFile(IList<string> extraArgFileNames, IJsonDocumentObjectModelFactory jsonDomFactory)
        {
            Dictionary<string, IReadOnlyList<string>> parameters = new Dictionary<string, IReadOnlyList<string>>();

            if (extraArgFileNames.Count > 0)
            {
                foreach (string argFile in extraArgFileNames)
                {
                    if (!File.Exists(argFile))
                    {
                        throw new CommandParserException(string.Format(LocalizableStrings.ArgsFileNotFound, argFile), argFile);
                    }

                    try
                    {
                        using (Stream s = File.OpenRead(argFile))
                        using (TextReader r = new StreamReader(s, Encoding.UTF8, true, 4096, true))
                        {
                            string text = r.ReadToEnd();

                            if (jsonDomFactory.TryParse(text, out IJsonToken jsonToken) && jsonToken is IJsonObject jsonObj)
                            {
                                List<(string, Action<IJsonToken>)> keyValueExtractorMap = new List<(string, Action<IJsonToken>)>();

                                foreach (string key in jsonObj.PropertyNames)
                                {
                                    keyValueExtractorMap.Add((key,
                                            (token) =>
                                            {
                                                if (token is IJsonValue tokenValue)
                                                {
                                                    // adding 2 dashes to the file-based params
                                                    // won't work right if there's a param that should have 1 dash
                                                    //
                                                    // TOOD: come up with a better way to deal with this
                                                    parameters["--" + key] = new List<string>() { tokenValue.Value.ToString() };
                                                }
                                            }
                                        ));
                                }

                                jsonObj.ExtractValues(keyValueExtractorMap.ToArray());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new CommandParserException(string.Format(LocalizableStrings.ArgsFileWrongFormat, argFile), argFile, ex);
                    }
                }
            }

            return parameters;
        }
    }
}
