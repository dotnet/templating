using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CoreConsole
{
    public class CommandParser
    {
        private static readonly IReadOnlyList<Argument> NoArguments;
        private static readonly IReadOnlyList<Switch> NoSwitches;

        private IReadOnlyList<Argument> _arguments;
        private IReadOnlyList<Switch> _switches;

        static CommandParser()
        {
            NoArguments = new Argument[0];
            NoSwitches = new Switch[0];
        }

        public CommandParser()
        {
            _arguments = NoArguments;
            _switches = NoSwitches;
        }

        private CommandParser(CommandParser existing, Argument arg)
        {
            Argument[] args = new Argument[existing._arguments.Count + 1];
            
            for(int i = 0; i < existing._arguments.Count; ++i)
            {
                args[i] = existing._arguments[i];
            }

            args[existing._arguments.Count] = arg;
            _arguments = args;
            _switches = existing._switches;
        }

        private CommandParser(CommandParser existing, Switch @switch)
        {
            Switch[] switches = new Switch[existing._switches.Count + 1];

            for (int i = 0; i < existing._switches.Count; ++i)
            {
                switches[i] = existing._switches[i];
            }

            switches[existing._switches.Count] = @switch;
            _switches = switches;
            _arguments = existing._arguments;
        }

        public CommandParser Use(Argument argument)
        {
            if (_arguments.Contains(argument))
            {
                return this;
            }

            return new CommandParser(this, argument);
        }

        public CommandParser Use(Switch @switch)
        {
            if (_switches.Contains(@switch))
            {
                return this;
            }

            return new CommandParser(this, @switch);
        }

        public CommandParser Define(out Argument argument, ArgumentType type, string name)
        {
            argument = new Argument(name, type);
            return new CommandParser(this, argument);
        }

        public CommandParser Define(out Switch @switch, SwitchType type, int valueCount, string canonicalForm, params string[] alternateForms)
        {
            @switch = new Switch(canonicalForm, alternateForms, type, valueCount);
            return new CommandParser(this, @switch);
        }

        public CommandParseResult Parse(IReadOnlyList<string> args)
        {
            HashSet<string> switchForms = new HashSet<string>(StringComparer.Ordinal);

            foreach (Switch sw in _switches)
            {
                switchForms.UnionWith(sw.Forms);
            }

            Dictionary<Argument, IReadOnlyList<string>> argValues = new Dictionary<Argument, IReadOnlyList<string>>();
            Dictionary<Switch, List<IReadOnlyList<string>>> switchValues = new Dictionary<Switch, List<IReadOnlyList<string>>>();
            List<string> remainingArgs = new List<string>();
            int i = 0;

            if (_arguments.Count > 0)
            {
                Queue<Argument> toParse = new Queue<Argument>(_arguments);
                Argument currentArg = toParse.Dequeue();
                List<string> currentArgValues = new List<string>();

                for (; i < args.Count && !switchForms.Contains(args[i]); ++i)
                {
                    if (currentArg == null)
                    {
                        remainingArgs.Add(args[i]);
                        continue;
                    }

                    currentArgValues.Add(args[i]);

                    if (currentArg.Type == ArgumentType.SingleValue)
                    {
                        argValues[currentArg] = currentArgValues;

                        if (toParse.Count == 0)
                        {
                            currentArg = null;
                        }
                        else
                        {
                            currentArgValues = new List<string>();
                            currentArg = toParse.Dequeue();
                        }
                    }
                }

                if (currentArg != null && currentArgValues.Count > 0)
                {
                    argValues[currentArg] = currentArgValues;
                    currentArg = null;
                }

                if (toParse.Count > 0 || currentArg != null)
                {
                    throw new CommandParseException("Not all arguments were parsed");
                }
            }

            for (; i < args.Count; ++i)
            {
                bool handled = false;
                for (int j = 0; j < _switches.Count; ++j)
                {
                    //Too many values required, can't be this switch
                    if (i + 1 + _switches[j].ValueCount >= args.Count)
                    {
                        continue;
                    }

                    bool isMatch = false;
                    for (int k = 0; !isMatch && k < _switches[j].Forms.Count; ++k)
                    {
                        isMatch = string.Equals(args[i], _switches[j].Forms[k], StringComparison.Ordinal);
                    }

                    if (isMatch && (_switches[j].Type == SwitchType.MultiUse || !switchValues.ContainsKey(_switches[j])))
                    {
                        string[] consumed = new string[_switches[j].ValueCount];
                        for (int k = 0; k < consumed.Length; ++k)
                        {
                            consumed[k] = args[i + 1 + k];
                        }

                        i += consumed.Length;

                        if (!switchValues.TryGetValue(_switches[j], out List<IReadOnlyList<string>> values))
                        {
                            switchValues[_switches[j]] = values = new List<IReadOnlyList<string>>();
                        }

                        values.Add(consumed);
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                {
                    remainingArgs.Add(args[i]);
                }
            }

            Dictionary<Switch, IReadOnlyList<IReadOnlyList<string>>> processedSwitchValues = new Dictionary<Switch, IReadOnlyList<IReadOnlyList<string>>>();

            foreach (KeyValuePair<Switch, List<IReadOnlyList<string>>> pair in switchValues)
            {
                processedSwitchValues[pair.Key] = pair.Value;
            }

            return new CommandParseResult(argValues, processedSwitchValues, remainingArgs);
        }
    }
}
