using System.Collections.Generic;

namespace Microsoft.CoreConsole
{
    public class Switch
    {
        internal Switch(string canonicalForm, string[] alternateForms, SwitchType type, int valueCount)
        {
            string[] forms = new string[alternateForms.Length + 1];

            forms[0] = canonicalForm;
            for (int i = 0; i < alternateForms.Length; ++i)
            {
                forms[i + 1] = alternateForms[i];
            }

            CanonicalForm = canonicalForm;
            Forms = forms;
            Type = type;
            ValueCount = valueCount;
        }

        public string CanonicalForm { get; }

        public IReadOnlyList<string> Forms { get; }

        public SwitchType Type { get; }

        public string Documentation { get; set; }

        public object HelpFilter { get; set; }

        public int ValueCount { get; }

        public override string ToString()
        {
            return CanonicalForm;
        }
    }
}
