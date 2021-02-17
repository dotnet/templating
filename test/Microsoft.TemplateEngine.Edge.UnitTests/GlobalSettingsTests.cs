// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class GlobalSettingsTests : IDisposable
    {
        EnvironmentSettingsHelper _helper = new EnvironmentSettingsHelper();

        [Fact]
        public async Task TestLocking()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            var lock1 = await globalSettings1.LockAsync(default);
            bool exceptionThrown = false;
            try
            {
                await globalSettings2.LockAsync(new CancellationTokenSource(50).Token);
            }
            catch (TaskCanceledException)
            {
                exceptionThrown = true;
            }
            Assert.True(exceptionThrown, nameof(globalSettings2) + " was able to get lock on when it shouldn't");
            lock1.Dispose();
            var lock2 = await globalSettings2.LockAsync(new CancellationTokenSource(100).Token);
            Assert.NotNull(lock2);
            lock2.Dispose();
        }


        [Fact]
        public async Task TestFilwatcher()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            var taskSource = new TaskCompletionSource<TemplatesSourceData>();
            globalSettings2.SettingsChanged += () => taskSource.SetResult(globalSettings2.UserInstalledTemplatesSources.Single());
            var lock1 = await globalSettings1.LockAsync(default);
            var newData = new TemplatesSourceData()
            {
                InstallerId = Guid.NewGuid(),
                MountPointUri = "Hi",
                Details = new Dictionary<string, string>() { { "a", "b" } },
                LastChangeTime = DateTime.UtcNow
            };
            globalSettings1.Add(newData);
            lock1.Dispose();
            var timeoutTask = Task.Delay(100);
            var firstFinishedTask = await Task.WhenAny(timeoutTask, taskSource.Task);
            Assert.Equal(firstFinishedTask, taskSource.Task);

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
            var lock1 = await globalSettings1.LockAsync(default);
            var newData = new TemplatesSourceData()
            {
                InstallerId = Guid.NewGuid(),
                MountPointUri = "Hi",
                Details = new Dictionary<string, string>() { { "a", "b" } },
                LastChangeTime = DateTime.UtcNow
            };
            globalSettings1.Add(newData);
            lock1.Dispose();
            #endregion

            #region Open2LoadAndLock
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            Assert.Equal(globalSettings1.UserInstalledTemplatesSources[0].InstallerId, globalSettings2.UserInstalledTemplatesSources[0].InstallerId);
            var lock2 = await globalSettings2.LockAsync(default);
            #endregion

            #region Open3Load
            using var globalSettings3 = new GlobalSettings(envSettings, settingsFile);
            Assert.Equal(globalSettings1.UserInstalledTemplatesSources[0].InstallerId, globalSettings3.UserInstalledTemplatesSources[0].InstallerId);
            #endregion

            lock2.Dispose();
        }

        public void Dispose()
        {
            _helper.Dispose();
        }
    }
}
