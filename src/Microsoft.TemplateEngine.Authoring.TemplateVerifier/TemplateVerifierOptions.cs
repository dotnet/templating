// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier
{
    public delegate void ScrubTemplateFileContent(string filename, StringBuilder content);

    public delegate Task VerifyFileContent(string filename, AsyncLazy<string> content, Task defaultVarifierAction);

    public class TemplateVerifierOptions : IOptions<TemplateVerifierOptions>
    {
        /// <summary>
        /// Gets the name of locally installed template.
        /// </summary>
        public string? TemplateName { get; init; }

        /// <summary>
        /// Gets the path to template.json file or containing directory.
        /// </summary>
        public string? TemplatePath { get; init; }

        /// <summary>
        /// Gets the path to custom assembly implementing the new command.
        /// </summary>
        public string? DotnetNewCommandAssemblyPath { get; init; }

        /// <summary>
        /// Gets the template specific arguments.
        /// </summary>
        public IEnumerable<string>? TemplateSpecificArgs { get; init; }

        /// <summary>
        /// Gets the directory with expectation files.
        /// </summary>
        public string? ExpectationsDirectory { get; init; }

        /// <summary>
        /// If set to true - 'dotnet new' command standard output and error contents will be verified along with the produced template files.
        /// </summary>
        public bool? VerifyCommandOutput { get; init; }

        /// <summary>
        /// If set to true - 'dotnet new' command is expected to return nonzero return code.
        /// Otherwise a zero error code and no error output is expected.
        /// </summary>
        public bool? IsCommandExpectedToFail { get; init; }

        /// <summary>
        /// If set to true - the diff tool won't be automatically started by the Verifier on verification failures.
        /// </summary>
        public bool? DisableDiffTool { get; init; }

        /// <summary>
        /// If set to true - all template output files will be verified, unless <see cref="VerificationExcludePatterns"/> are specified.
        /// Otherwise a default exclusions (to be documented - mostly binaries etc.).
        /// </summary>
        public bool? DisableDefaultVerificationExcludePatterns { get; init; }

        /// <summary>
        /// Set of patterns defining files to be excluded from verification.
        /// </summary>
        public IEnumerable<string>? VerificationExcludePatterns { get; init; }

        /// <summary>
        /// Gets the target directory to output the generated template.
        /// </summary>
        public string? OutputDirectory { get; init; }

        /// <summary>
        /// Gets the delegate that performs custom scrubbing of template output contents before verifications.
        /// </summary>
        public ScrubTemplateFileContent? CustomScrubber { get; init; }

        /// <summary>
        /// Gets the delegate that performs custom verification of template output contents.
        /// </summary>
        public VerifyFileContent? CustomVerifier { get; init; }

        TemplateVerifierOptions IOptions<TemplateVerifierOptions>.Value => this;
    }
}
