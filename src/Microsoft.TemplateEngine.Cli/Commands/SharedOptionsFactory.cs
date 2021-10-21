// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal static class SharedOptionsFactory
    {
        internal static Option<bool> GetInteractiveOption()
        {
            return new Option<bool>("--interactive")
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionInteractive
            };
        }

        internal static Option<IReadOnlyList<string>> GetAddSourceOption()
        {
            return new(new[] { "--add-source", "--nuget-source" })
            {
                Arity = new ArgumentArity(0, 99),
                Description = LocalizableStrings.OptionDescriptionNuGetSource,
                AllowMultipleArgumentsPerToken = true,
            };
        }

        internal static Option<string> GetAuthorOption()
        {
            return new(new[] { "--author" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionAuthorFilter
            };
        }

        internal static Option<string> GetBaselineOption()
        {
            return new(new[] { "--baseline" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionBaseline,
                IsHidden = true
            };
        }

        internal static Option<string> GetLanguageOption()
        {
            return new(new[] { "--language", "-lang" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionLanguageFilter
            };
        }

        internal static Option<string> GetTypeOption()
        {
            return new(new[] { "--type" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionTypeFilter
            };
        }

        internal static Option<string> GetTagOption()
        {
            return new(new[] { "--tag" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionTagFilter
            };
        }

        internal static Option<string> GetPackageOption()
        {
            return new(new[] { "--package" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionPackageFilter
            };
        }

        internal static Option<bool> GetColumnsAllOption()
        {
            return new(new[] { "--columns-all" })
            {
                Arity = new ArgumentArity(0, 1),
                Description = LocalizableStrings.OptionDescriptionColumnsAll
            };
        }

        internal static Option<IReadOnlyList<string>> GetColumnsOption()
        {
            Option<IReadOnlyList<string>> option = new(new[] { "--columns" }, ParseCommaSeparatedValues)
            {
                Arity = new ArgumentArity(0, 4),
                Description = LocalizableStrings.OptionDescriptionColumns,
                AllowMultipleArgumentsPerToken = true,
            };
            option.FromAmong("tags", "language", "type", "author");
            return option;
        }

        internal static IReadOnlyList<string> ParseCommaSeparatedValues(ArgumentResult result)
        {
            List<string> values = new List<string>();
            foreach (var value in result.Tokens.Select(t => t.Value))
            {
                values.AddRange(value.Split(",", StringSplitOptions.TrimEntries).Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            return values;
        }

        internal static Option AsHidden(this Option o)
        {
            o.IsHidden = true;
            return o;
        }

        internal static Option<T> AsHidden<T>(this Option<T> o)
        {
            o.IsHidden = true;
            return o;
        }

        internal static Option<T> DisableAllowMultipleArgumentsPerToken<T>(this Option<T> o)
        {
            o.AllowMultipleArgumentsPerToken = false;
            return o;
        }
    }
}
