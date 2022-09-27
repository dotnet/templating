// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Operations;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.OperationConfig
{
    internal class OperationConfigDefault
    {
        internal OperationConfigDefault(string glob, string flagPrefix, string evaluatorName, ConditionalType type)
        {
            EvaluatorName = evaluatorName;
            Glob = glob;
            FlagPrefix = flagPrefix;
            ConditionalStyle = type;
        }

        internal static OperationConfigDefault Default { get; } = new(glob: string.Empty, flagPrefix: string.Empty, evaluatorName: "C++", type: ConditionalType.None);

        internal static OperationConfigDefault DefaultGlobalConfig { get; } = new(glob: string.Empty, flagPrefix: "//", evaluatorName: "C++", type: ConditionalType.CLineComments);

        internal static IReadOnlyList<OperationConfigDefault> DefaultSpecialConfig { get; } = new[]
                    {
                        new OperationConfigDefault("**/*.js", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.es", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.es6", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.ts", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.json", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.jsonld", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.hjson", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.json5", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.geojson", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.topojson", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.bowerrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.npmrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.job", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.postcssrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.babelrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.csslintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.eslintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.jade-lintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.pug-lintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.jshintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.stylelintrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.yarnrc", "//", "C++", ConditionalType.CLineComments),
                        new OperationConfigDefault("**/*.css.min", "/*", "C++", ConditionalType.CBlockComments),
                        new OperationConfigDefault("**/*.css", "/*", "C++", ConditionalType.CBlockComments),
                        new OperationConfigDefault("**/*.cshtml", "@*", "C++", ConditionalType.Razor),
                        new OperationConfigDefault("**/*.razor", "@*", "C++", ConditionalType.Razor),
                        new OperationConfigDefault("**/*.vbhtml", "@*", "VB", ConditionalType.Razor),
                        new OperationConfigDefault("**/*.cs", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.fs", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.c", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.cpp", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.cxx", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.h", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.hpp", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.hxx", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.cake", "//", "C++", ConditionalType.CNoComments),
                        new OperationConfigDefault("**/*.*proj", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.*proj.user", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.pubxml", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.pubxml.user", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.msbuild", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.targets", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.props", "<!--/", "MSBUILD", ConditionalType.MSBuild),
                        new OperationConfigDefault("**/*.svg", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.*htm", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.*html", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.md", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.jsp", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.asp", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.aspx", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/app.config", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/web.config", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/web.*.config", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/packages.config", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/nuget.config", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.nuspec", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.xslt", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.xsd", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.vsixmanifest", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.vsct", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.storyboard", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.axml", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.plist", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.xib", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.strings", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.bat", "rem --:", "C++", ConditionalType.RemLineComment),
                        new OperationConfigDefault("**/*.cmd", "rem --:", "C++", ConditionalType.RemLineComment),
                        new OperationConfigDefault("**/nginx.conf", "#--", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/robots.txt", "#--", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/*.sh", "#--", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/*.haml", "-#", "C++", ConditionalType.HamlLineComment),
                        new OperationConfigDefault("**/*.jsx", "{/*", "C++", ConditionalType.JsxBlockComment),
                        new OperationConfigDefault("**/*.tsx", "{/*", "C++", ConditionalType.JsxBlockComment),
                        new OperationConfigDefault("**/*.xml", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.resx", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.bas", "'", "VB", ConditionalType.VB),
                        new OperationConfigDefault("**/*.vb", "'", "VB", ConditionalType.VB),
                        new OperationConfigDefault("**/*.xaml", "<!--", "C++", ConditionalType.Xml),
                        new OperationConfigDefault("**/*.sln", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/*.yaml", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/*.yml", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/Dockerfile", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/.editorconfig", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/.gitattributes", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/.gitignore", "#-", "C++", ConditionalType.HashSignLineComment),
                        new OperationConfigDefault("**/.dockerignore", "#-", "C++", ConditionalType.HashSignLineComment)
                    };

        internal string Glob { get; }

        internal string EvaluatorName { get; }

        internal string FlagPrefix { get; }

        internal ConditionalType ConditionalStyle { get; }
    }
}
