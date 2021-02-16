// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.IDE
{
    public class Bootstrapper
    {
        private readonly ITemplateEngineHost _host;
        private readonly Action<IEngineEnvironmentSettings> _onFirstRun;
        private readonly Paths _paths;
        private readonly TemplateCreator _templateCreator;

        private EngineEnvironmentSettings EnvironmentSettings { get; }

        public Bootstrapper(ITemplateEngineHost host, Action<IEngineEnvironmentSettings> onFirstRun, bool virtualizeConfiguration)
        {
            _host = host;
            EnvironmentSettings = new EngineEnvironmentSettings(host, x => new SettingsLoader(x));
            //Installer = new Installer(EnvironmentSettings);
            _onFirstRun = onFirstRun;
            _paths = new Paths(EnvironmentSettings);
            _templateCreator = new TemplateCreator(EnvironmentSettings);

            if (virtualizeConfiguration)
            {
                EnvironmentSettings.Host.VirtualizeDirectory(_paths.User.BaseDir);
            }
        }

        private void EnsureInitialized()
        {
            if (!_paths.Exists(_paths.User.BaseDir) || !_paths.Exists(_paths.User.FirstRunCookie))
            {
                _onFirstRun?.Invoke(EnvironmentSettings);
                _paths.WriteAllText(_paths.User.FirstRunCookie, "");
            }
        }

        public void Register(Type type)
        {
            EnvironmentSettings.SettingsLoader.Components.Register(type);
        }

        public void Register(Assembly assembly)
        {
            EnvironmentSettings.SettingsLoader.Components.RegisterMany(assembly.GetTypes());
        }

        public void Install(string path)
        {
            EnsureInitialized();
            //TODO: Handle this...
            //Installer.InstallPackages(new[] { path });
        }

        public void Install(params string[] paths)
        {
            EnsureInitialized();
            //TODO: Handle this...
            //Installer.InstallPackages(paths);
        }

        public void Install(IEnumerable<string> paths)
        {
            EnsureInitialized();
            //TODO: Handle this...
            //Installer.InstallPackages(paths);
        }

        public async Task<IReadOnlyCollection<IFilteredTemplateInfo>> ListTemplates(bool exactMatchesOnly, params Func<ITemplateInfo, MatchInfo?>[] filters)
        {
            EnsureInitialized();
            return TemplateListFilter.FilterTemplates(await EnvironmentSettings.SettingsLoader.GetTemplatesAsync(default).ConfigureAwait(false), exactMatchesOnly, filters);
        }

        public async Task<ICreationResult> CreateAsync(ITemplateInfo info, string name, string outputPath, IReadOnlyDictionary<string, string> parameters, bool skipUpdateCheck, string baselineName)
        {
            TemplateCreationResult instantiateResult = await _templateCreator.InstantiateAsync(info, name, name, outputPath, parameters, skipUpdateCheck, false, baselineName).ConfigureAwait(false);
            return instantiateResult.ResultInfo;
        }

        public async Task<ICreationEffects> GetCreationEffectsAsync(ITemplateInfo info, string name, string outputPath, IReadOnlyDictionary<string, string> parameters, string baselineName)
        {
            TemplateCreationResult instantiateResult = await _templateCreator.InstantiateAsync(info, name, name, outputPath, parameters, true, false, baselineName, true).ConfigureAwait(false);
            return instantiateResult.CreationEffects;
        }

        public IEnumerable<string> Uninstall(string path)
        {
            EnsureInitialized();

            //TODO: Handle this...
            //return Installer.Uninstall(new[] { path });
            throw new NotImplementedException();
        }

        public IEnumerable<string> Uninstall(params string[] paths)
        {
            EnsureInitialized();

            //TODO: Handle this...
            //return Installer.Uninstall(paths);
            throw new NotImplementedException();
        }

        public IEnumerable<string> Uninstall(IEnumerable<string> paths)
        {
            EnsureInitialized();

            //TODO: Handle this...
            //return Installer.Uninstall(paths);
            throw new NotImplementedException();
        }
    }
}
