// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Utils.UnitTests
{
    public class InMemoryFileSystemTests
    {
        [Fact(DisplayName = nameof(VerifyMultipleVirtualizationsAreHandled))]
        public void VerifyMultipleVirtualizationsAreHandled()
        {
            IPhysicalFileSystem mockFileSystem = new MockFileSystem();
            IPhysicalFileSystem virtualized1 = new InMemoryFileSystem(Directory.GetCurrentDirectory().CombinePaths("test1"), mockFileSystem);
            IPhysicalFileSystem virtualized2 = new InMemoryFileSystem(Directory.GetCurrentDirectory().CombinePaths("test2"), virtualized1);

            string testFilePath = Directory.GetCurrentDirectory().CombinePaths("test1", "test.txt");
            virtualized2.CreateFile(testFilePath).Dispose();
            Assert.False(mockFileSystem.FileExists(testFilePath));
            Assert.True(virtualized1.FileExists(testFilePath));
            Assert.True(virtualized2.FileExists(testFilePath));
        }

        [Fact]
        public void VerifyRootCanBeVirtualized()
        {
            IPhysicalFileSystem mockFileSystem = new MockFileSystem();
            IPhysicalFileSystem virtualized = new InMemoryFileSystem(Path.GetPathRoot(Directory.GetCurrentDirectory())!, mockFileSystem);

            string testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "test.txt");
            virtualized.WriteAllText(testFilePath, "test");

            var entries = virtualized.EnumerateFileSystemEntries(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly).ToList();
            Assert.Single(entries);

            Assert.Equal(testFilePath, entries[0]);
        }
    }
}
