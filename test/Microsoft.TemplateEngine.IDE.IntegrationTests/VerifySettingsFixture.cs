﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using VerifyTests;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests
{
    public class VerifySettingsFixture : IDisposable
    {
        public VerifySettingsFixture()
        {
            VerifierSettings.DerivePathInfo(
                (_, _, type, method) => new(
                    directory: "Approvals",
                    typeName: type.Name,
                    methodName: method.Name));

            // Customize diff output of verifier
            VerifyDiffPlex.Initialize(OutputType.Compact);
        }

        public void Dispose() { }
    }
}
