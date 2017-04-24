using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Core.UnitTests
{
    internal class StubSettingsLoader : ISettingsLoader
    {
        public IComponentManager Components => throw new NotImplementedException();

        public IEngineEnvironmentSettings EnvironmentSettings => throw new NotImplementedException();

        public IEnumerable<MountPointInfo> MountPoints => throw new NotImplementedException();

        public void AddMountPoint(IMountPoint mountPoint)
        {
            throw new NotImplementedException();
        }

        public void AddProbingPath(string probeIn)
        {
            throw new NotImplementedException();
        }

        public IFile FindBestHostTemplateConfigFile(IFileSystemInfo config)
        {
            throw new NotImplementedException();
        }

        public void GetTemplates(HashSet<ITemplateInfo> templates)
        {
            throw new NotImplementedException();
        }

        public ITemplate LoadTemplate(ITemplateInfo info)
        {
            throw new NotImplementedException();
        }

        public void ReleaseMountPoint(IMountPoint mountPoint)
        {
            throw new NotImplementedException();
        }

        public void RemoveMountPoint(IMountPoint mountPoint)
        {
            throw new NotImplementedException();
        }

        public void RemoveMountPoints(IEnumerable<Guid> mountPoints)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {

        }

        public bool TryGetFileFromIdAndPath(Guid mountPointId, string place, out IFile file, out IMountPoint mountPoint)
        {
            throw new NotImplementedException();
        }

        public bool TryGetMountPointFromPlace(string mountPointPlace, out IMountPoint mountPoint)
        {
            throw new NotImplementedException();
        }

        public bool TryGetMountPointInfo(Guid mountPointId, out MountPointInfo info)
        {
            throw new NotImplementedException();
        }

        public void WriteTemplateCache(IList<ITemplateInfo> templates, string locale)
        {
            throw new NotImplementedException();
        }
    }
}
