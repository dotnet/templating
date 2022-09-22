// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace Microsoft.TemplateEngine.Authoring.CLI.Commands.Verify
{
    internal class VerifyCommand : ExecutableCommand<VerifyCommandArgs>
    {
        private const string CommandName = "verify";

        private readonly Argument<string> _templateNameArgument = new("-n")
        {
            Name = "template-short-name",
            //TODO: localize
            Description = "Name of already installed template to be verified.",
            // 0 for case where only path is specified
            Arity = new ArgumentArity(1, 1)
        };

        private readonly Option<string> _remainingArguments = new Option<string>("--template-args")
        {
            Description = "Template specific arguments - all joined into single enquoted string. Any needed quotations of actual arguments has to be escaped.",
            Arity = new ArgumentArity(0, 1)
        };

        private readonly Option<string> _templatePathOption = new("-p")
        {
            Name = "--template-path",
            //TODO: localize
            Description = "Specifies path to the directory with template to be verified.",
        };

        private readonly Option<string> _newCommandPathOption = new("--new-command-assembly")
        {
            //TODO: localize
            Description = "Specifies path to custom assembly implementing the new command.",
            //TODO: do we have better way of distinguishing options that might rarely be needed?
            // if not - we should probably add a link to more detailed help in the command description (mentioning that online help has additional options)
            IsHidden = true
        };

        private readonly Option<string> _templateOutputPathOption = new("-o")
        {
            Name = "--output",
            //TODO: localize
            Description = "Specifies path to target directory to output the generated template.",
        };

        private readonly Option<string> _expectationsDirectoryOption = new("-d")
        {
            Name = "--expectations-directory",
            //TODO: localize
            Description = "Specifies path to directory with expectation files.",
        };

        private readonly Option<bool> _disableDiffToolOption = new("--disable-diff-tool")
        {
            //TODO: localize
            Description = "If set to true - the diff tool won't be automatically started by the Verifier on verification failures.",
        };

        private readonly Option<bool> _disableDefaultExcludePatternsOption = new("--disable-default-exclude-patterns")
        {
            //TODO: localize
            Description = "If set to true - all template output files will be verified, unless --exclude-pattern option is used.",
        };

        private readonly Option<IEnumerable<string>> _excludePatternOption = new("--exclude-pattern")
        {
            //TODO: localize
            Description = "Specifies pattern(s) defining files to be excluded from verification.",
            Arity = new ArgumentArity(0, 999)
        };

        private readonly Option<bool> _verifyCommandOutputOption = new("--verify-std")
        {
            //TODO: localize
            Description = "If set to true - 'dotnet new' command standard output and error contents will be verified along with the produced template files.",
        };

        private readonly Option<bool> _isCommandExpectedToFailOption = new("--fail-expected")
        {
            //TODO: localize
            Description = "If set to true - 'dotnet new' command is expected to return nonzero return code.",
        };

        private readonly Option<IEnumerable<string>> _uniqueForOption = new("--unique-for")
        {
            //TODO: localize
            Description = "Sets the Verifier expectations directory naming convention - by indicating which scenarios should be differentiated.",
            Arity = new ArgumentArity(0, 999),
            AllowMultipleArgumentsPerToken = true,
        };

        public VerifyCommand(ILoggerFactory loggerFactory)
            : base(CommandName, "Runs the template with specified arguments and compares the result with expectations files (or creates those if yet don't exist).", loggerFactory)
        {
            AddArgument(_templateNameArgument);
            AddOption(_remainingArguments);
            AddOption(_templatePathOption);
            AddOption(_newCommandPathOption);
            AddOption(_templateOutputPathOption);
            AddOption(_expectationsDirectoryOption);
            AddOption(_disableDiffToolOption);
            AddOption(_disableDefaultExcludePatternsOption);
            AddOption(_excludePatternOption);
            AddOption(_verifyCommandOutputOption);
            AddOption(_isCommandExpectedToFailOption);
            FromAmongCaseInsensitive(
                _uniqueForOption,
                System.Enum.GetNames(typeof(UniqueForOption))
                    .Where(v => !v.Equals(UniqueForOption.None.ToString(), StringComparison.OrdinalIgnoreCase))
                    .ToArray());
            AddOption(_uniqueForOption);
        }

        protected override async Task<int> ExecuteAsync(VerifyCommandArgs args, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Running the verification of {templateName}.", args.TemplateName);

            try
            {
                VerificationEngine engine = new VerificationEngine(
                    new TemplateVerifierOptions()
                    {
                        TemplateName = args.TemplateName,
                        TemplatePath = args.TemplatePath,
                        TemplateSpecificArgs = args.TemplateSpecificArgs,
                        DisableDiffTool = args.DisableDiffTool,
                        DisableDefaultVerificationExcludePatterns = args.DisableDefaultVerificationExcludePatterns,
                        VerificationExcludePatterns = args.VerificationExcludePatterns,
                        DotnetNewCommandAssemblyPath = args.DotnetNewCommandAssemblyPath,
                        ExpectationsDirectory = args.ExpectationsDirectory,
                        OutputDirectory = args.OutputDirectory,
                        VerifyCommandOutput = args.VerifyCommandOutput,
                        IsCommandExpectedToFail = args.IsCommandExpectedToFail,
                        UniqueFor = args.UniqueFor,
                    },
                    Logger
                );
                await engine.Execute(cancellationToken).ConfigureAwait(false);
                return 0;
            }
            catch (Exception e)
            {
                Reporter.Error.WriteLine("Verification Failed.");
                Logger.LogError(e.Message);
                TemplateVerificationException? ex = e as TemplateVerificationException;
                return (int)(ex?.TemplateVerificationErrorCode ?? TemplateVerificationErrorCode.InternalError);
            }
        }

        protected override BinderBase<VerifyCommandArgs> GetModelBinder() => new VerifyModelBinder(this);

        /// <summary>
        /// Case insensitive version for <see cref="System.CommandLine.OptionExtensions.FromAmong{TOption}(TOption, string[])"/>.
        /// </summary>
        private static void FromAmongCaseInsensitive(Option<IEnumerable<string>> option, string[]? allowedValues = null, string? allowedHiddenValue = null)
        {
            allowedValues ??= Array.Empty<string>();
            option.AddValidator(optionResult => ValidateAllowedValues(optionResult, allowedValues, allowedHiddenValue));
            option.AddCompletions(allowedValues);
        }

        private static void ValidateAllowedValues(OptionResult optionResult, string[] allowedValues, string? allowedHiddenValue = null)
        {
            var invalidArguments = optionResult.Tokens.Where(token => !allowedValues.Append(allowedHiddenValue).Contains(token.Value, StringComparer.OrdinalIgnoreCase)).ToList();
            if (invalidArguments.Any())
            {
                //TODO: localize
                optionResult.ErrorMessage = string.Format(
                    "Argument(s) {0} are not recognized. Must be one of: {1}.",
                    string.Join(", ", invalidArguments.Select(arg => $"'{arg.Value}'")),
                    string.Join(", ", allowedValues.Select(allowedValue => $"'{allowedValue}'")));
            }
        }

        private class VerifyModelBinder : BinderBase<VerifyCommandArgs>
        {
            private readonly VerifyCommand _verifyCommand;

            internal VerifyModelBinder(VerifyCommand verifyCommand)
            {
                _verifyCommand = verifyCommand;
            }

            protected override VerifyCommandArgs GetBoundValue(BindingContext bindingContext)
            {
                return new VerifyCommandArgs(
                    templateName: bindingContext.ParseResult.GetValueForArgument(_verifyCommand._templateNameArgument),
                    templateSpecificArgs: bindingContext.ParseResult.GetValueForOption(_verifyCommand._remainingArguments),
                    templatePath: bindingContext.ParseResult.GetValueForOption(_verifyCommand._templatePathOption),
                    dotnetNewCommandAssemblyPath: bindingContext.ParseResult.GetValueForOption(_verifyCommand._newCommandPathOption),
                    expectationsDirectory: bindingContext.ParseResult.GetValueForOption(_verifyCommand._expectationsDirectoryOption),
                    outputDirectory: bindingContext.ParseResult.GetValueForOption(_verifyCommand._templateOutputPathOption),
                    disableDiffTool: bindingContext.ParseResult.GetValueForOption(_verifyCommand._disableDiffToolOption),
                    disableDefaultVerificationExcludePatterns: bindingContext.ParseResult.GetValueForOption(_verifyCommand._disableDefaultExcludePatternsOption),
                    verificationExcludePatterns: bindingContext.ParseResult.GetValueForOption(_verifyCommand._excludePatternOption),
                    verifyCommandOutput: bindingContext.ParseResult.GetValueForOption(_verifyCommand._verifyCommandOutputOption),
                    isCommandExpectedToFail: bindingContext.ParseResult.GetValueForOption(_verifyCommand._isCommandExpectedToFailOption),
                    uniqueForOptions: bindingContext.ParseResult.GetValueForOption(_verifyCommand._uniqueForOption));
            }
        }
    }
}
