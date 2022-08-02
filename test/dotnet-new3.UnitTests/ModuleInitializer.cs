// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Dotnet_new3.IntegrationTests
{
    public class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            // Customization of storage of .verified comparison files (see https://github.com/VerifyTests/Verify/blob/main/docs/naming.md#derivepathinfo).
            VerifierSettings.DerivePathInfo(
                (_, _, type, method) => new(
                    directory: "Approvals",
                    typeName: type.Name,
                    methodName: method.Name));

            // Customize diff output of verifier
            VerifyDiffPlex.Initialize(OutputType.Compact);
        }
    }
}
