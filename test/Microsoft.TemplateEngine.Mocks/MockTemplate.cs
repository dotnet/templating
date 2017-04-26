using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockTemplate : ITemplate
    {
        public IGenerator Generator => throw new NotImplementedException();

        public IFileSystemInfo Configuration => throw new NotImplementedException();

        public IFileSystemInfo LocaleConfiguration => throw new NotImplementedException();

        public IDirectory TemplateSourceRoot => throw new NotImplementedException();

        public bool IsNameAgreementWithFolderPreferred => throw new NotImplementedException();

        public string Author => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public IReadOnlyList<string> Classifications => throw new NotImplementedException();

        public string DefaultName => throw new NotImplementedException();

        public string Identity => throw new NotImplementedException();

        public Guid GeneratorId => throw new NotImplementedException();

        public string GroupIdentity => throw new NotImplementedException();

        public int Precedence => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string ShortName => throw new NotImplementedException();

        public IReadOnlyDictionary<string, ICacheTag> Tags => throw new NotImplementedException();

        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters => throw new NotImplementedException();

        public IReadOnlyList<ITemplateParameter> Parameters => throw new NotImplementedException();

        public Guid ConfigMountPointId => throw new NotImplementedException();

        public string ConfigPlace => throw new NotImplementedException();

        public Guid LocaleConfigMountPointId => throw new NotImplementedException();

        public string LocaleConfigPlace => throw new NotImplementedException();

        public Guid HostConfigMountPointId => throw new NotImplementedException();

        public string HostConfigPlace => throw new NotImplementedException();

        public string ThirdPartyNotices => throw new NotImplementedException();

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo => throw new NotImplementedException();
    }
}
