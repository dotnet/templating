// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.TemplateEngine.Abstractions
{
    [Obsolete("Use " + nameof(IFileChange2) + " instead")]
    public interface IFileChange
    {
        string TargetRelativePath { get; }

        ChangeKind ChangeKind { get; }

        byte[] Contents { get; }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public interface IFileChange2 : IFileChange
#pragma warning restore CS0618 // Type or member is obsolete
    {
        string SourceRelativePath { get; }
    }
}
