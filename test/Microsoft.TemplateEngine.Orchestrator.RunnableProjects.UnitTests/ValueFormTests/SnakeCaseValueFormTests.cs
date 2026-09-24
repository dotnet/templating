// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.ValueFormTests
{
    public class SnakeCaseValueFormTests
    {
        [Theory]
        [InlineData("I", "i")]
        [InlineData("IO", "io")]
        [InlineData("FileIO", "file_io")]
        [InlineData("SignalR", "signal_r")]
        [InlineData("IOStream", "io_stream")]
        [InlineData("COMObject", "com_object")]
        [InlineData("WebAPI", "web_api")]
        [InlineData("XProjectX", "x_project_x")]
        [InlineData("NextXXXProject", "next_xxx_project")]
        [InlineData("NoNewProject", "no_new_project")]
        [InlineData("NONewProject", "no_new_project")]
        [InlineData("NewProjectName", "new_project_name")]
        [InlineData("ABBREVIATIONAndSomeName", "abbreviation_and_some_name")]
        [InlineData("NoNoNoNoNoNoNoName", "no_no_no_no_no_no_no_name")]
        [InlineData("AnotherNewNewNewNewNewProjectName", "another_new_new_new_new_new_project_name")]
        [InlineData("Param1TestValue", "param_1_test_value")]
        [InlineData("Windows10", "windows_10")]
        [InlineData("WindowsServer2016R2", "windows_server_2016_r_2")]
        [InlineData("", "")]
        [InlineData(";MyWord;", "my_word")]
        [InlineData("My Word", "my_word")]
        [InlineData("My    Word", "my_word")]
        [InlineData(";;;;;", "")]
        [InlineData("       ", "")]
        [InlineData("Simple TEXT_here", "simple_text_here")]
        [InlineData("НоваяПеременная", "новая_переменная")]
        public void SnakeCaseWorksAsExpected(string input, string expected)
        {
            IValueForm? model = new SnakeCaseValueFormFactory().Create("test");
            string actual = model.Process(input, new Dictionary<string, IValueForm>());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanHandleNullValue()
        {
            IValueForm model = new SnakeCaseValueFormFactory().Create("test");
            Assert.Throws<ArgumentNullException>(() => model.Process(null!, new Dictionary<string, IValueForm>()));
        }
    }
}
