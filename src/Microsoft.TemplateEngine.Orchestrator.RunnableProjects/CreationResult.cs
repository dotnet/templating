using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class CreationResult : ICreationResult, IFinalizedOutputValueContainer
    {
        private IReadOnlyDictionary<string, IReadOnlyList<IOutputValue>> _valuesByInputFile;
        private IReadOnlyDictionary<string, IReadOnlyList<IOutputValue>> _valuesByOutputFile;
        private IReadOnlyDictionary<string, IReadOnlyList<IOutputValue>> _valuesByName;

        public IReadOnlyList<IPostAction> PostActions { get; set; }

        public IReadOnlyList<ICreationPath> PrimaryOutputs { get; set; }

        public void SetOutputValues(IEnumerable<IOutputValue> values)
        {
            Dictionary<string, IReadOnlyList<IOutputValue>> valuesByInputFile = new Dictionary<string, IReadOnlyList<IOutputValue>>(StringComparer.Ordinal);
            Dictionary<string, IReadOnlyList<IOutputValue>> valuesByOutputFile = new Dictionary<string, IReadOnlyList<IOutputValue>>(StringComparer.Ordinal);
            Dictionary<string, IReadOnlyList<IOutputValue>> valuesByName = new Dictionary<string, IReadOnlyList<IOutputValue>>(StringComparer.Ordinal);

            foreach (IOutputValue value in values)
            {
                List<IOutputValue> inputFileBag, outputFileBag, nameBag;
                if (!valuesByInputFile.TryGetValue(value.InputPath, out IReadOnlyList<IOutputValue> bag))
                {
                    valuesByInputFile[value.InputPath] = inputFileBag = new List<IOutputValue>();
                }
                else
                {
                    inputFileBag = (List<IOutputValue>) bag;
                }

                if (!valuesByOutputFile.TryGetValue(value.OutputPath, out bag))
                {
                    valuesByOutputFile[value.OutputPath] = outputFileBag = new List<IOutputValue>();
                }
                else
                {
                    outputFileBag = (List<IOutputValue>) bag;
                }

                if (!valuesByName.TryGetValue(value.Name, out bag))
                {
                    valuesByName[value.Name] = nameBag = new List<IOutputValue>();
                }
                else
                {
                    nameBag = (List<IOutputValue>) bag;
                }

                inputFileBag.Add(value);
                outputFileBag.Add(value);
                nameBag.Add(value);
            }

            _valuesByInputFile = valuesByInputFile;
            _valuesByOutputFile = valuesByOutputFile;
            _valuesByName = valuesByName;
        }

        public IEnumerable<IOutputValue> GetValuesByOutputFile(string outputFilePath)
        {
            return _valuesByOutputFile == null
                   || !_valuesByOutputFile.TryGetValue(outputFilePath, out IReadOnlyList<IOutputValue> result)
                ? Empty<IOutputValue>.List.Value
                : result;
        }

        public IEnumerable<IOutputValue> GetValuesByInputFile(string inputFilePath)
        {
            return _valuesByInputFile == null
                   || !_valuesByInputFile.TryGetValue(inputFilePath, out IReadOnlyList<IOutputValue> result)
                ? Empty<IOutputValue>.List.Value
                : result;
        }

        public IEnumerable<IOutputValue> GetValuesByName(string name)
        {
            return _valuesByName == null
                   || !_valuesByName.TryGetValue(name, out IReadOnlyList<IOutputValue> result)
                ? Empty<IOutputValue>.List.Value
                : result;
        }
    }
}
