using System.Collections.Generic;

namespace Microsoft.CoreConsole
{
    public class CommandParseResult
    {
        private static readonly IReadOnlyList<string> NoValues = new string[0];
        private static readonly IReadOnlyList<IReadOnlyList<string>> NoValueSets = new string[0][];
        private readonly IReadOnlyDictionary<Argument, IReadOnlyList<string>> _arguments;
        private readonly IReadOnlyDictionary<Switch, IReadOnlyList<IReadOnlyList<string>>> _switches;

        public IReadOnlyList<string> RemainingArgs { get; }

        public CommandParseResult(IReadOnlyDictionary<Argument, IReadOnlyList<string>> arguments, IReadOnlyDictionary<Switch, IReadOnlyList<IReadOnlyList<string>>> switches, IReadOnlyList<string> remainingArgs)
        {
            _arguments = arguments;
            _switches = switches;
            RemainingArgs = remainingArgs;
        }

        public string Value(Argument arg)
        {
            if (!_arguments.TryGetValue(arg, out IReadOnlyList<string> values) || values.Count != 1)
            {
                return null;
            }

            return values[0];
        }

        public IReadOnlyList<string> Values(Argument arg)
        {
            if (!_arguments.TryGetValue(arg, out IReadOnlyList<string> values))
            {
                return NoValues;
            }

            return values;
        }

        public bool HasValue(Switch @switch)
        {
            return _switches.ContainsKey(@switch);
        }

        public IReadOnlyList<string> Value(Switch @switch)
        {
            if (!_switches.TryGetValue(@switch, out IReadOnlyList<IReadOnlyList<string>> values) || values.Count != 1)
            {
                return NoValues;
            }

            return values[0];
        }

        public IReadOnlyList<IReadOnlyList<string>> Values(Switch @switch)
        {
            if (!_switches.TryGetValue(@switch, out IReadOnlyList<IReadOnlyList<string>> values))
            {
                return NoValueSets;
            }

            return values;
        }
    }
}
