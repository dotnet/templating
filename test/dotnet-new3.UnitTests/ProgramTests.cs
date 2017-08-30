using System;
using Xunit;

namespace DotnetNew3.UnitTests
{
    public class ProgramTests
    {
        private static string[] CommandToArgArrayHelper(string command)
        {
            return command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        [Fact]
        public void TestTest()
        {
            //string command = "";
        }
    }
}
