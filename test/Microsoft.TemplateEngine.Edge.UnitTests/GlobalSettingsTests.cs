﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.BuiltInManagedProvider;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class GlobalSettingsTests : IDisposable
    {
        private EnvironmentSettingsHelper _helper = new EnvironmentSettingsHelper();

        [Fact]
        public async Task TestLocking()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            var disposable = await globalSettings1.LockAsync(default).ConfigureAwait(false);
            bool exceptionThrown = false;
            using var cts = new CancellationTokenSource(50);
            try
            {
                await globalSettings2.LockAsync(cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                exceptionThrown = true;
            }
            Assert.True(exceptionThrown, nameof(globalSettings2) + " was able to get lock on when it shouldn't");
            disposable.Dispose();
            //Check that we don't time out
            using var cts2 = new CancellationTokenSource(1000);
            using var settingsLock = await globalSettings2.LockAsync(cts2.Token).ConfigureAwait(false);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Randomly failing see https://github.com/dotnet/templating/issues/3336")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public async Task TestFileWatcher()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            var taskSource = new TaskCompletionSource<TemplatePackageData>();
            globalSettings2.SettingsChanged += async () => taskSource.TrySetResult((await globalSettings2.GetInstalledTemplatePackagesAsync(default).ConfigureAwait(false)).Single());
            var mutex = await globalSettings1.LockAsync(default).ConfigureAwait(false);
            var newData = new TemplatePackageData(
                Guid.NewGuid(),
                "Hi",
                DateTime.UtcNow,
                new Dictionary<string, string>() { { "a", "b" } });
            await globalSettings1.SetInstalledTemplatePackagesAsync(new[] { newData }, default).ConfigureAwait(false);
            mutex.Dispose();
            var timeoutTask = Task.Delay(1000);
            var firstFinishedTask = await Task.WhenAny(timeoutTask, taskSource.Task).ConfigureAwait(false);
            Assert.Equal(taskSource.Task, firstFinishedTask);

            var newData2 = taskSource.Task.Result;
            Assert.Equal(newData.InstallerId, newData2.InstallerId);
            Assert.Equal(newData.MountPointUri, newData2.MountPointUri);
            Assert.Equal(newData.Details["a"], newData2.Details["a"]);
            Assert.Equal(newData.LastChangeTime, newData2.LastChangeTime);
        }

        [Fact]
        public async Task TestReadWhileLocked()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);

            #region Open1AndPopulateAndSave

            using (await globalSettings1.LockAsync(default).ConfigureAwait(false))
            {
                var newData = new TemplatePackageData(
                Guid.NewGuid(),
                    "Hi",
                    DateTime.UtcNow,
                    new Dictionary<string, string>() { { "a", "b" } });
                await globalSettings1.SetInstalledTemplatePackagesAsync(new[] { newData }, default).ConfigureAwait(false);
            }

            #endregion Open1AndPopulateAndSave

            #region Open2LoadAndLock

            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            Assert.Equal((await globalSettings1.GetInstalledTemplatePackagesAsync(default).ConfigureAwait(false))[0].InstallerId, (await globalSettings2.GetInstalledTemplatePackagesAsync(default).ConfigureAwait(false))[0].InstallerId);
            var mutex2 = await globalSettings2.LockAsync(default).ConfigureAwait(false);

            #endregion Open2LoadAndLock

            #region Open3Load

            using var globalSettings3 = new GlobalSettings(envSettings, settingsFile);
            Assert.Equal((await globalSettings1.GetInstalledTemplatePackagesAsync(default).ConfigureAwait(false))[0].InstallerId, (await globalSettings3.GetInstalledTemplatePackagesAsync(default).ConfigureAwait(false))[0].InstallerId);

            #endregion Open3Load

            mutex2.Dispose();
        }

        [Fact]
        public void TestDisablingFilewatcher()
        {
            var envSettings = _helper.CreateEnvironment(environment: new MockEnvironment(new Dictionary<string, string> { { "TEMPLATE_ENGINE_DISABLE_FILEWATCHER", "1" } }));
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            Assert.Empty(((MonitoredFileSystem)envSettings.Host.FileSystem).FilesWatched);

            envSettings = _helper.CreateEnvironment();
            settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            Assert.Single(((MonitoredFileSystem)envSettings.Host.FileSystem).FilesWatched);
            Assert.Equal(settingsFile, ((MonitoredFileSystem)envSettings.Host.FileSystem).FilesWatched.Single());
        }

        public void Dispose()
        {
            _helper.Dispose();
        }
    }
}
