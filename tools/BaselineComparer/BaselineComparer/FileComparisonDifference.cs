using System.Collections.Generic;
using System.Linq;

namespace BaselineComparer
{
    public class FileComparisonDifference
    {
        public FileComparisonDifference(string filename)
        {
            Filename = filename;
            _positionallyMatchedDifferences = new List<PositionalComparisonDifference>();
            _baselineOnlyDifferences = new List<PositionalDifference>();
            _checkOnlyDifferences = new List<PositionalDifference>();
        }

        public string Filename { get; }

        private List<PositionalComparisonDifference> _positionallyMatchedDifferences;
        private List<PositionalDifference> _baselineOnlyDifferences;
        private List<PositionalDifference> _checkOnlyDifferences;
        private bool _missingBaselineComparison;
        private bool _missingCheckComparison;

        // Stores the differences that positionally match up between two sets of comparisons.
        // The key is the baseline difference, the value is the difference for the current comparison.
        public IReadOnlyList<PositionalComparisonDifference> PositionallyMatchedDifferences => _positionallyMatchedDifferences;

        // Differences from the baseline comparison that couldn't be matched to differences from the check comparison.
        public IReadOnlyList<PositionalDifference> BaselineOnlyDifferences => _baselineOnlyDifferences;

        // Differences from the check comparison that couldn't be matched to differences from the baseline comparison.
        public IReadOnlyList<PositionalDifference> CheckOnlyDifferences => _checkOnlyDifferences;

        public void AddPositionallyMatchedDifference(PositionalDifference baselineDifference, PositionalDifference checkDifference, PositionalComparisonDisposition disposition)
        {
            _positionallyMatchedDifferences.Add(new PositionalComparisonDifference(baselineDifference, checkDifference, disposition));
        }

        public void AddBaselineOnlyDifference(PositionalDifference baselineDifference)
        {
            if (MissingBaselineComparison || MissingCheckComparison)
            {
                throw new System.Exception("Cant have differences if a comparison is missing.");
            }

            _baselineOnlyDifferences.Add(baselineDifference);
        }

        public void AddCheckOnlyDifference(PositionalDifference checkDifference)
        {
            if (MissingBaselineComparison || MissingCheckComparison)
            {
                throw new System.Exception("Cant have differences if a comparison is missing.");
            }

            _checkOnlyDifferences.Add(checkDifference);
        }

        public bool AnyInvalidDifferences
        {
            get
            {
                return BaselineOnlyDifferences.Count > 0
                    || CheckOnlyDifferences.Count > 0
                    || PositionallyMatchedDifferences.Any(d => d.Disposition != PositionalComparisonDisposition.Match);
            }
        }

        public bool MissingBaselineComparison
        {
            get
            {
                return _missingBaselineComparison;
            }
            set
            {
                if (_positionallyMatchedDifferences.Count > 0
                    || _baselineOnlyDifferences.Count > 0
                    || _checkOnlyDifferences.Count > 0)
                {
                    throw new System.Exception("Cant label comparison as missing - there are registered differences.");
                }

                _missingBaselineComparison = value;
            }
        }

        public bool MissingCheckComparison
        {
            get
            {
                return _missingCheckComparison;
            }
            set
            {
                if (_positionallyMatchedDifferences.Count > 0
                    || _baselineOnlyDifferences.Count > 0
                    || _checkOnlyDifferences.Count > 0)
                {
                    throw new System.Exception("Cant label comparison as missing - there are registered differences.");
                }

                _missingCheckComparison = value;
            }
        }
    }
}
