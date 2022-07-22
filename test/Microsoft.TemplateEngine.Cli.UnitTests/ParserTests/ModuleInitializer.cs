// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.TemplateEngine.Cli.UnitTests.ParserTests
{
    public class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.DerivePathInfo(
                (_, _, type, method) => new(
                    directory: "Approvals",
                    typeName: type.Name,
                    methodName: method.Name));
        }
    }
}
