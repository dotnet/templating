﻿using System;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.TestHelper
{
    public abstract class TestBase
    {
        protected IEngineEnvironmentSettings EngineEnvironmentSettings { get; }

        protected TestBase()
        {
            ITemplateEngineHost host = new TestHost
            {
                HostIdentifier = "TestRunner",
                Version = "1.0.0.0",
                Locale = "en-US"
            };

            EngineEnvironmentSettings = new EngineEnvironmentSettings(host, x => null);
            host.VirtualizeDirectory(GetTemplateEngineDirectory());
        }
        
        private static string GetTemplateEngineDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), ".templateengine");
        }

        protected static void RunAndVerify(string originalValue, string expectedValue, IProcessor processor, int bufferSize, bool? changeOverride = null)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(originalValue);
            MemoryStream input = new MemoryStream(valueBytes);
            MemoryStream output = new MemoryStream();
            bool changed = processor.Run(input, output, bufferSize);
            Verify(Encoding.UTF8, output, changed, originalValue, expectedValue, changeOverride);
        }

        protected static void Verify(Encoding encoding, Stream output, bool changed, string source, string expected, bool? changeOverride = null)
        {
            output.Position = 0;
            byte[] resultBytes = new byte[output.Length];
            output.Read(resultBytes, 0, resultBytes.Length);
            string actual = encoding.GetString(resultBytes);
            Assert.Equal(expected, actual);

            bool expectedChange = changeOverride ?? !string.Equals(expected, source, StringComparison.Ordinal);
            string modifier = expectedChange ? "" : "not ";
            if (expectedChange ^ changed)
            {
                Assert.False(true, $"Expected value to {modifier} be changed");
            }
        }
    }
}
