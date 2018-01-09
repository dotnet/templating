using System;
using System.Collections.Generic;

namespace BaselineComparer
{
    public class DirectoryComparisonDifference
    {
        public DirectoryComparisonDifference()
        {
            _fileResults = new List<FileComparisonDifference>();
        }

        private List<FileComparisonDifference> _fileResults;
        private bool _invalidCheckData;
        private bool _invalidBaselineData;

        public IReadOnlyList<FileComparisonDifference> FileResults => _fileResults;

        public void AddFileResult(FileComparisonDifference fileComparison)
        {
            _fileResults.Add(fileComparison);
        }

        public bool InvalidBaselineData
        {
            get
            {
                return _invalidBaselineData;
            }
            set
            {
                if (_fileResults.Count > 0)
                {
                    throw new Exception("Cant have comparisons if the baseline is invalid.");
                }

                _invalidBaselineData = value;
            }
        }

        public bool InvalidCheckData
        {
            get
            {
                return _invalidCheckData;
            }
            set
            {
                if (_fileResults.Count > 0)
                {
                    throw new Exception("Cant have comparisons if the check data is invalid.");
                }

                _invalidCheckData = value;
            }
        }
    }
}
