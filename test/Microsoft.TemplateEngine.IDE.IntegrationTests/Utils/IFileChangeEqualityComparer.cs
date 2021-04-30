// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests.Utils
{
    internal class IFileChangeComparer : IEqualityComparer<IFileChange2>, IComparer<IFileChange2>
    {
        public int Compare(IFileChange2 x, IFileChange2 y)
        {
            if (Equals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }
            if (x.ChangeKind > y.ChangeKind)
            {
                return 1;
            }
            if (x.ChangeKind < y.ChangeKind)
            {
                return -1;
            }

            int compareResult = ComparePaths(x.TargetRelativePath, y.TargetRelativePath);
            if (compareResult != 0)
            {
                return compareResult;
            }
            return ComparePaths(x.SourceRelativePath, y.SourceRelativePath);
        }

        public bool Equals(IFileChange2 x, IFileChange2 y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }

            return ComparePaths(x.TargetRelativePath, y.TargetRelativePath) == 0
                    && x.ChangeKind == y.ChangeKind
                    && ComparePaths(x.SourceRelativePath, y.SourceRelativePath) == 0;
        }

        public int GetHashCode(IFileChange2 obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (GetHashValue(obj.TargetRelativePath), obj.ChangeKind, obj.SourceRelativePath).GetHashCode();
        }

        private static int ComparePaths(string x, string y)
        {
            if (string.IsNullOrWhiteSpace(x))
            {
                return string.IsNullOrWhiteSpace(y) ? 0 : -1;
            }
            if (string.IsNullOrWhiteSpace(y))
            {
                return 1;
            }
            return string.Compare(Path.GetFullPath(x), Path.GetFullPath(y), StringComparison.OrdinalIgnoreCase);
        }

        private string GetHashValue(string x)
        {
            if (string.IsNullOrWhiteSpace(x))
            {
                return string.Empty;
            }
            return Path.GetFullPath(x).ToLowerInvariant();
        }
    }
}
