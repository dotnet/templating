using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class RunSpecificEngineEnvironmentSettings : IEngineEnvironmentSettings, IOutputValuesContainer
    {
        private readonly IEngineEnvironmentSettings _basis;
        private readonly Dictionary<string, Dictionary<string, object>> _valueLookup;
        private readonly Dictionary<string, string> _outputToInputMap;

        public RunSpecificEngineEnvironmentSettings(IEngineEnvironmentSettings basis)
        {
            _basis = basis;
            _valueLookup = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            _outputToInputMap = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public ISettingsLoader SettingsLoader => _basis.SettingsLoader;

        public ITemplateEngineHost Host => _basis.Host;

        public IEnvironment Environment => _basis.Environment;

        public IPathInfo Paths => _basis.Paths;

        public IEnumerator<IOutputValue> GetEnumerator()
        {
            foreach (KeyValuePair<string, Dictionary<string, object>> bag in _valueLookup)
            {
                foreach (KeyValuePair<string, object> entry in bag.Value)
                {
                    yield return new OutputValue(_outputToInputMap[bag.Key], bag.Key, entry.Key, entry.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void MapFile(string inputFile, string outputFile)
        {
            if (!string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(outputFile))
            {
                _outputToInputMap[outputFile] = inputFile;
            }
        }

        public object this[string inputPath, string outputPath, string name]
        {
            get => !_valueLookup.TryGetValue(outputPath, out Dictionary<string, object> lookup)
                   || !lookup.TryGetValue(name, out object result)
                ? null
                : result;
            set
            {
                if (!_valueLookup.TryGetValue(outputPath, out Dictionary<string, object> lookup))
                {
                    _valueLookup[outputPath] = lookup = new Dictionary<string, object>(StringComparer.Ordinal);
                }

                lookup[name] = value;
            }
        }

        public int Count => _valueLookup.Sum(x => x.Value.Count);
    }
}
