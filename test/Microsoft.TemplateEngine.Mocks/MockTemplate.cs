// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.Parameters;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockTemplate : ITemplate
    {
        private readonly IMountPoint _mountPoint;

        public MockTemplate(IGenerator generator, IMountPoint mountPoint)
        {
            Generator = generator;
            _mountPoint = mountPoint;

            MountPointUriToIdentityMapMock.TryGetValue(mountPoint.MountPointUri, out string mockedIdentity);
            Identity = string.IsNullOrEmpty(mockedIdentity) ? "Static.Test.Template" : mockedIdentity;

            DefaultName = mountPoint.MountPointUri;
        }

        public IDictionary<string, string> MountPointUriToIdentityMapMock { get; set; } = new Dictionary<string, string>();

        public IGenerator Generator { get; }

        public IFileSystemInfo Configuration => _mountPoint.Root;

        public IFileSystemInfo? LocaleConfiguration => null;

        public IDirectory TemplateSourceRoot => _mountPoint.Root;

        public bool IsNameAgreementWithFolderPreferred => false;

        public string? Author => "Microsoft";

        public string? Description => "This is the description";

        public IReadOnlyList<string> Classifications => Array.Empty<string>();

        public string? DefaultName { get; set; }

        public bool PreferDefaultName => false;

        public string Identity { get; set; }

        public Guid GeneratorId => Generator.Id;

        public string? GroupIdentity => null;

        public int Precedence => 0;

        public string Name => "Test template";

        public string ShortName => "test-template";

        [Obsolete("Mock class")]
        public IReadOnlyDictionary<string, ICacheTag> Tags => throw new NotImplementedException();

        public IReadOnlyDictionary<string, string> TagsCollection { get; } = new Dictionary<string, string>();

        [Obsolete("Mock class")]
        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters => throw new NotImplementedException();

        public IParameterDefinitionSet ParameterDefinitions => ParameterDefinitionSet.Empty;

        public IReadOnlyList<ITemplateParameter> Parameters => ParameterDefinitions;

        public string MountPointUri => _mountPoint.MountPointUri;

        public string ConfigPlace => ".";

        public string? LocaleConfigPlace => null;

        public string? HostConfigPlace => null;

        public string? ThirdPartyNotices => null;

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; } = new Dictionary<string, IBaselineInfo>();

        public bool HasScriptRunningPostActions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IReadOnlyList<string> ShortNameList => new[] { ShortName };

        public IReadOnlyList<Guid> PostActions => Array.Empty<Guid>();

        public IReadOnlyList<TemplateConstraintInfo> Constraints => Array.Empty<TemplateConstraintInfo>();
    }
}
