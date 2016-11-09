﻿using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ITemplateInfo
    {
        string Author { get; }

        string Description { get; }

        IReadOnlyList<string> Classifications { get; }

        string DefaultName { get; }

        string Identity { get; }

        Guid GeneratorId { get; }

        string GroupIdentity { get; }

        string Name { get; }

        string ShortName { get; }

        IReadOnlyDictionary<string, string> Tags { get; }

        Guid ConfigMountPointId { get; }

        string ConfigPlace { get; }

        Guid LocaleConfigMountPointId { get; }

        string LocaleConfigPlace { get; }
    }

    public interface ITemplate : ITemplateInfo
    {
        IGenerator Generator { get; }

        IFileSystemInfo Configuration { get; }

        IFileSystemInfo LocaleConfiguration { get; }
    }
}