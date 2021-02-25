// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
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
            await globalSettings1.LockAsync(default);
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
            await globalSettings1.UnlockAsync(default);
            //Check that we don't time out
            await globalSettings2.LockAsync(new CancellationTokenSource(1000).Token);
            await globalSettings2.UnlockAsync(default);
        }


        [Fact]
        public async Task TestFilwatcher()
        {
            var envSettings = _helper.CreateEnvironment();
            var settingsFile = Path.Combine(_helper.CreateTemporaryFolder(), "settings.json");
            using var globalSettings1 = new GlobalSettings(envSettings, settingsFile);
            await globalSettings1.ReloadSettings(false, default);
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            await globalSettings2.ReloadSettings(false, default);
            var taskSource = new TaskCompletionSource<TemplatesSourceData>();
            globalSettings2.SettingsChanged += () => taskSource.SetResult(globalSettings2.UserInstalledTemplatesSources.Single());
            await globalSettings1.LockAsync(default);
            var newData = new TemplatesSourceData()
            {
                InstallerId = Guid.NewGuid(),
                MountPointUri = "Hi",
                Details = new Dictionary<string, string>() { { "a", "b" } },
                LastChangeTime = DateTime.UtcNow
            };
            globalSettings1.Add(newData);
            await globalSettings1.UnlockAsync(default);
            var timeoutTask = Task.Delay(1000);
            var firstFinishedTask = await Task.WhenAny(timeoutTask, taskSource.Task);
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
            await globalSettings1.LockAsync(default);
            var newData = new TemplatesSourceData()
            {
                InstallerId = Guid.NewGuid(),
                MountPointUri = "Hi",
                Details = new Dictionary<string, string>() { { "a", "b" } },
                LastChangeTime = DateTime.UtcNow
            };
            globalSettings1.Add(newData);
            await globalSettings1.UnlockAsync(default);
            #endregion

            #region Open2LoadAndLock
            using var globalSettings2 = new GlobalSettings(envSettings, settingsFile);
            await globalSettings2.ReloadSettings(false, default);
            Assert.Equal(globalSettings1.UserInstalledTemplatesSources[0].InstallerId, globalSettings2.UserInstalledTemplatesSources[0].InstallerId);
            await globalSettings2.LockAsync(default);
            #endregion

            #region Open3Load
            using var globalSettings3 = new GlobalSettings(envSettings, settingsFile);
            await globalSettings3.ReloadSettings(false, default);
            Assert.Equal(globalSettings1.UserInstalledTemplatesSources[0].InstallerId, globalSettings3.UserInstalledTemplatesSources[0].InstallerId);
            #endregion

            await globalSettings2.UnlockAsync(default);
        }

        public void Dispose()
        {
            _helper.Dispose();
        }
    }
}
