using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Mocks;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockSettingsLoader : ISettingsLoader
    {
        public IComponentManager Components => throw new NotImplementedException();

        public IEngineEnvironmentSettings EnvironmentSettings => throw new NotImplementedException();

        public IEnumerable<MountPointInfo> MountPoints => throw new NotImplementedException();

        public void AddMountPoint(IMountPoint mountPoint)
        {
        }

        public void AddProbingPath(string probeIn)
        {
        }

        public IFile FindBestHostTemplateConfigFile(IFileSystemInfo config)
        {
            return new MockFile(string.Empty, new MockMountPoint(this.EnvironmentSettings));
        }

        public void GetTemplates(HashSet<ITemplateInfo> templates)
        {
        }

        public ITemplate LoadTemplate(ITemplateInfo info)
        {
            return null;
        }

        public ITemplate LoadTemplate(ITemplateInfo info, string baselineName)
        {
            return null;
        }

        public void ReleaseMountPoint(IMountPoint mountPoint)
        {
        }

        public void RemoveMountPoint(IMountPoint mountPoint)
        {
        }

        public void RemoveMountPoints(IEnumerable<Guid> mountPoints)
        {
        }

        public void Save()
        {

        }

        public bool TryGetFileFromIdAndPath(Guid mountPointId, string place, out IFile file, out IMountPoint mountPoint)
        {
            file = new MockFile(string.Empty, new MockMountPoint(this.EnvironmentSettings));
            mountPoint = new MockMountPoint(this.EnvironmentSettings);
            return true;
        }

        public bool TryGetMountPointFromPlace(string mountPointPlace, out IMountPoint mountPoint)
        {
            mountPoint = new MockMountPoint(this.EnvironmentSettings);
            return true;
        }

        public bool TryGetMountPointInfo(Guid mountPointId, out MountPointInfo info)
        {
            info = new MountPointInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), string.Empty);
            return true;
        }

        public void WriteTemplateCache(IList<ITemplateInfo> templates, string locale)
        {
        }
    }
}
