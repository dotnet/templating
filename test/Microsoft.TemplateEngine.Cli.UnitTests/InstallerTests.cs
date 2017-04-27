using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class InstallerTests : TestBase
    {
        [Theory(DisplayName = nameof(GitTemplatePathCallsGitClone))]
        [InlineData("https://github.com/acme/templates.git/sub/folder")]
        [InlineData("https://github.com/acme/templates.git")]
        public void GitTemplatePathCallsGitClone(string request)
        {
            InstallerTestWrapper installer = new InstallerTestWrapper(this.EnvironmentSettings);

            string[] installationRequests = new[] { request };
            installer.InstallPackages(installationRequests);

            GitSource gitSource = null;
            GitSource.TryParseGitSource(request, out gitSource);

            Assert.Equal("git", installer.ExecuteProcessCommands[0][0]);
            Assert.Equal("clone", installer.ExecuteProcessCommands[0][1]);
            Assert.Equal(gitSource.GitUrl, installer.ExecuteProcessCommands[0][2]);
            Assert.Contains($"scratch/{gitSource.RepositoryName}", installer.ExecuteProcessCommands[0][3]);
        }
        [Theory(DisplayName = nameof(GitTemplatePathCallsInstallLocalPackageWithCloneDirectory))]
        [InlineData("https://github.com/acme/templates.git/sub/folder")]
        [InlineData("https://github.com/acme/templates.git")]
        public void GitTemplatePathCallsInstallLocalPackageWithCloneDirectory(string request)
        {
            FileSystemTestWrapper fileSystemTestWrapper = new FileSystemTestWrapper();
            (this.EnvironmentSettings.Host as TestHelper.TestHost).FileSystem = fileSystemTestWrapper;
            
            GitSource gitSource = null;
            GitSource.TryParseGitSource(request, out gitSource);

            bool cloneDirectoryFound = false;
            fileSystemTestWrapper.VerifyDirectoryExists = path =>
            {
                if (path.Contains($"scratch/{gitSource.RepositoryName}/{gitSource.SubFolder}")) {
                    cloneDirectoryFound = true;
                }
            };

            InstallerTestWrapper installer = new InstallerTestWrapper(this.EnvironmentSettings);
            string[] installationRequests = new[] { request };
            installer.InstallPackages(installationRequests);

            Assert.True(cloneDirectoryFound, "Clone directory was found.");
        }

        [Fact(DisplayName = nameof(GitCloneFailureStopsTemplateInstall))]
        public void GitCloneFailureStopsTemplateInstall()
        {
            FileSystemTestWrapper fileSystemTestWrapper = new FileSystemTestWrapper();
            (this.EnvironmentSettings.Host as TestHelper.TestHost).FileSystem = fileSystemTestWrapper;
            List<string> directoryExistChecks = new List<string>();
            fileSystemTestWrapper.VerifyDirectoryExists = path =>
            {
                directoryExistChecks.Add(path);
            };

            InstallerTestWrapper installer = new InstallerTestWrapper(this.EnvironmentSettings);
            installer.ExecuteProcessReturn = false;

            string[] installationRequests = new[] { "http://myurl.com/myrepo.git" };
            installer.InstallPackages(installationRequests);

            // Should only check directory exists twice
            // More calls indicate package was installed as local package
            Assert.Equal(2, directoryExistChecks.Count);
        }
    }

    internal class FileSystemTestWrapper : PhysicalFileSystem, IPhysicalFileSystem
    {
        public Action<string> VerifyDirectoryExists { get; set; }

        public new bool DirectoryExists(string directory)
        {
            VerifyDirectoryExists(directory);
            return base.DirectoryExists(directory);
        }
    }
    internal class InstallerTestWrapper : Installer
    {
        public InstallerTestWrapper(IEngineEnvironmentSettings environmentSettings) : base(environmentSettings)
        {
            ExecuteProcessCommands = new List<string[]>();
            ExecuteProcessReturn = true;
        }

        public List<string[]> ExecuteProcessCommands { get; set; }
        public bool ExecuteProcessReturn { get; set; }

        internal override bool ExecuteProcess(string command, params string[] args)
        {
            ExecuteProcessCommands.Add(new string[] { command }.Concat(args).ToArray());
            return ExecuteProcessReturn;
        }
    }
}
