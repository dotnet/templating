// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using VerifyTests;

namespace Dotnet_new3.IntegrationTests
{
    public class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) => new(
                    directory: Path.Combine(projectDirectory, "Approvals"),
                    typeName: type.Name,
                    methodName: method.Name));
        }
    }
}
