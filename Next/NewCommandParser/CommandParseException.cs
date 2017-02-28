using System;

namespace Microsoft.CoreConsole
{
    public class CommandParseException : Exception
    {
        public CommandParseException(string message)
            : base(message)
        {
        }

        public CommandParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
