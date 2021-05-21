// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Core
{
    public class FileChange : IFileChange2
    {
        public FileChange(string sourceRelativePath, string targetRelativePath, ChangeKind changeKind, byte[]? contents = null)
        {
            SourceRelativePath = sourceRelativePath;
            TargetRelativePath = targetRelativePath;
            ChangeKind = changeKind;
            Contents = contents ?? Array.Empty<byte>();
        }

        public string SourceRelativePath { get; }

        public string TargetRelativePath { get; }

        public ChangeKind ChangeKind { get; }

        //this property is always empty now - never set.
        public byte[] Contents { get; }
    }
}
