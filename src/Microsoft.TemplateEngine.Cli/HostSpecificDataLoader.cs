using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli
{
    public class HostSpecificDataLoader : IHostSpecificDataLoader
    {
        public HostSpecificDataLoader(ISettingsLoader settingsLoader, IJsonDocumentObjectModelFactory jsonDomFactory)
        {
            _settingsLoader = settingsLoader;
            _jsonDomFactory = jsonDomFactory;
        }

        private readonly ISettingsLoader _settingsLoader;
        private readonly IJsonDocumentObjectModelFactory _jsonDomFactory;

        public HostSpecificTemplateData ReadHostSpecificTemplateData(ITemplateInfo templateInfo)
        {
            IMountPoint mountPoint = null;

            try
            {
                HostSpecificTemplateData hostData = new HostSpecificTemplateData();
                
                if (_settingsLoader.TryGetFileFromIdAndPath(templateInfo.HostConfigMountPointId, templateInfo.HostConfigPlace, out IFile file, out mountPoint))
                {
                    if (_jsonDomFactory.TryLoadFromIFile(file, out IJsonToken root) && root is IJsonObject rootObject)
                    {
                        (string, Action<IJsonToken>)[] extractors = new (string, Action<IJsonToken>)[]
                        {
                            JsonHelpers.CreateArrayValueExtractor<string>("usageExamples", hostData.UsageExamples),
                            SetupSymbolInfoExtractor(hostData),
                            SetupIsHiddenExtractor(hostData)
                        };

                        rootObject.ExtractValues(extractors);
                    }
                }

                return hostData;
            }
            catch
            {
                // ignore malformed host files
            }
            finally
            {
                if (mountPoint != null)
                {
                    _settingsLoader.ReleaseMountPoint(mountPoint);
                }
            }

            return HostSpecificTemplateData.Default;
        }

        private static (string, Action<IJsonToken>) SetupIsHiddenExtractor(HostSpecificTemplateData hostData)
        {
            return ("isHidden",
                (token) =>
                {
                    if (token is IJsonValue tokenValue)
                    {
                        hostData.IsHidden = (bool)tokenValue.Value;
                    }
                }
            );
        }

        private static (string, Action<IJsonToken>) SetupSymbolInfoExtractor(HostSpecificTemplateData hostData)
        {
            return ("symbolInfo",
                (token) =>
                {
                    if (token is IJsonObject tokenObject)
                    {
                        // get the symbol objects
                        Dictionary<string, IJsonObject> symbolObjectMap = new Dictionary<string, IJsonObject>();

                        List<(string, Action<IJsonToken>)> symbolExtractorMap = new List<(string, Action<IJsonToken>)>();

                        foreach (string symbolName in tokenObject.PropertyNames)
                        {
                            (string, Action<IJsonToken>) symbolExtractor = (symbolName,
                                (symbolToken) =>
                                {
                                    if (symbolToken is IJsonObject symbolTokenObject)
                                    {
                                        symbolObjectMap[symbolName] = symbolTokenObject;
                                    }
                                }
                            );

                            symbolExtractorMap.Add(symbolExtractor);
                        }

                        tokenObject.ExtractValues(symbolExtractorMap.ToArray());

                        // get the symbol details
                        foreach (KeyValuePair<string, IJsonObject> symbolNameObjectPair in symbolObjectMap)
                        {
                            string symbolName = symbolNameObjectPair.Key;
                            IJsonObject symbolObject = symbolNameObjectPair.Value;
                            //
                            Dictionary<string, string> infoForSymbol = new Dictionary<string, string>();
                            (string, Action<IJsonToken>)[] symbolInfoExtractors = JsonHelpers.CreateStringKeyDictionaryExtractor<string>(symbolObject, infoForSymbol);

                            symbolObject.ExtractValues(symbolInfoExtractors);

                            hostData.SymbolInfo[symbolName] = infoForSymbol;
                        }
                    }
                }
            );
        }
    }
}
