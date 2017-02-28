namespace Microsoft.CoreConsole
{
    public class Argument
    {
        internal Argument(string name, ArgumentType type)
        {
            Name = name;
            Type = type;
        }

        public ArgumentType Type { get; }

        public string Name { get; set; }

        public string Documentation { get; set; }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
