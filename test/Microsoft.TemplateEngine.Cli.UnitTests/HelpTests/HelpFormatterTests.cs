// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Cli.TabularOutput;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.HelpTests
{
    public class HelpFormatterTests
    {
        [Fact(DisplayName = nameof(CanShrinkOneColumn))]
        public void CanShrinkOneColumn()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                new MockEnvironment()
                {
                    ConsoleBufferWidth = 6 + 2 + 12 + 1
                });

            IEnumerable<Tuple<string, string>> data = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("My test data", "My test data"),
                new Tuple<string, string>("My test data", "My test data")
            };

            string expectedOutput = $"Col...  Column 2    {Environment.NewLine}------  ------------{Environment.NewLine}My ...  My test data{Environment.NewLine}My ...  My test data{Environment.NewLine}";

            TabularOutput<Tuple<string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", shrinkIfNeeded: true, minWidth: 2)
                 .DefineColumn(t => t.Item2, "Column 2");

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CanShrinkMultipleColumnsAndBalanceShrinking))]
        public void CanShrinkMultipleColumnsAndBalanceShrinking()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                new MockEnvironment()
                {
                    ConsoleBufferWidth = 6 + 2 + 6 + 1,
                });

            IEnumerable<Tuple<string, string>> data = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("My test data", "My test data"),
                new Tuple<string, string>("My test data", "My test data")
            };

            string expectedOutput = $"Col...  Col...{Environment.NewLine}------  ------{Environment.NewLine}My ...  My ...{Environment.NewLine}My ...  My ...{Environment.NewLine}";

            TabularOutput<Tuple<string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", shrinkIfNeeded: true, minWidth: 2)
                 .DefineColumn(t => t.Item2, "Column 2", shrinkIfNeeded: true, minWidth: 2);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CannotShrinkOverMinimumWidth))]
        public void CannotShrinkOverMinimumWidth()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                 new MockEnvironment()
                 {
                     ConsoleBufferWidth = 10, //less than need for data below
                 });

            IEnumerable<Tuple<string, string>> data = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("My test data", "My test data"),
                new Tuple<string, string>("My test data", "My test data")
            };

            string expectedOutput = $"Column 1      Column 2   {Environment.NewLine}------------  -----------{Environment.NewLine}My test data  My test ...{Environment.NewLine}My test data  My test ...{Environment.NewLine}";

            TabularOutput<Tuple<string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", shrinkIfNeeded: true, minWidth: 15)
                 .DefineColumn(t => t.Item2, "Column 2", shrinkIfNeeded: true, minWidth: 8);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CanShowDefaultColumns))]
        public void CanShowDefaultColumns()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                       new MockEnvironment()
                       {
                           ConsoleBufferWidth = 100
                 });

            IEnumerable<Tuple<string, string, string>> data = new List<Tuple<string, string, string>>()
            {
                new Tuple<string, string, string>("My test data", "My test data", "Column 3 data"),
                new Tuple<string, string, string>("My test data", "My test data", "Column 3 data")
            };

            string expectedOutput = $"Column 1      Column 2    {Environment.NewLine}------------  ------------{Environment.NewLine}My test data  My test data{Environment.NewLine}My test data  My test data{Environment.NewLine}";

            TabularOutput<Tuple<string, string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", showAlways: true)
                 .DefineColumn(t => t.Item2, "Column 2", columnName: "column2") //defaultColumn: true by default
                 .DefineColumn(t => t.Item3, "Column 3", columnName: "column3", defaultColumn: false);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CanShowUserSelectedColumns))]
        public void CanShowUserSelectedColumns()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                        new MockEnvironment()
                        {
                            ConsoleBufferWidth = 100
                        },
                        columnsToDisplay: new[] { "column3" });

            IEnumerable<Tuple<string, string, string>> data = new List<Tuple<string, string, string>>()
            {
                new Tuple<string, string, string>("My test data", "My test data", "Column 3 data"),
                new Tuple<string, string, string>("My test data", "My test data", "Column 3 data")
            };

            string expectedOutput = $"Column 1      Column 3     {Environment.NewLine}------------  -------------{Environment.NewLine}My test data  Column 3 data{Environment.NewLine}My test data  Column 3 data{Environment.NewLine}";

            TabularOutput<Tuple<string, string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", showAlways: true)
                 .DefineColumn(t => t.Item2, "Column 2", columnName: "column2") //defaultColumn: true by default
                 .DefineColumn(t => t.Item3, "Column 3", columnName: "column3", defaultColumn: false);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CanShowAllColumns))]
        public void CanShowAllColumns()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                        new MockEnvironment()
                        {
                            ConsoleBufferWidth = 100
                        },
                        displayAllColumns: true);

            IEnumerable<Tuple<string, string, string>> data = new List<Tuple<string, string, string>>()
            {
                new Tuple<string, string, string>("Column 1 data", "Column 2 data", "Column 3 data"),
                new Tuple<string, string, string>("Column 1 data", "Column 2 data", "Column 3 data")
            };

            string expectedOutput = $"Column 1       Column 2       Column 3     {Environment.NewLine}-------------  -------------  -------------{Environment.NewLine}Column 1 data  Column 2 data  Column 3 data{Environment.NewLine}Column 1 data  Column 2 data  Column 3 data{Environment.NewLine}";

            TabularOutput<Tuple<string, string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1", showAlways: true)
                 .DefineColumn(t => t.Item2, "Column 2", columnName: "column2") //defaultColumn: true by default
                 .DefineColumn(t => t.Item3, "Column 3", columnName: "column3", defaultColumn: false);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }

        [Fact(DisplayName = nameof(CanRightAlign))]
        public void CanRightAlign()
        {
            ITabularOutputSettings outputSettings = new CliTabularOutputSettings(
                            new MockEnvironment()
                            {
                                ConsoleBufferWidth = 10
                            });

            IEnumerable<Tuple<string, string>> data = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Monday", "Wednesday"),
                new Tuple<string, string>("Tuesday", "Sunday")
            };

            string expectedOutput = $"Column 1   Column 2{Environment.NewLine}--------  ---------{Environment.NewLine}Monday    Wednesday{Environment.NewLine}Tuesday      Sunday{Environment.NewLine}";

            TabularOutput<Tuple<string, string>> formatter =
             TabularOutput.TabularOutput
                 .For(outputSettings, data)
                 .DefineColumn(t => t.Item1, "Column 1")
                 .DefineColumn(t => t.Item2, "Column 2", rightAlign: true);

            string result = formatter.Layout();
            Assert.Equal(expectedOutput, result);
        }
    }
}

