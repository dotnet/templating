// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    /// <summary>
    /// Represents match information for the filter applied to template.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MatchInfo
    {
        public MatchInfo(string parameterName, string? parameterValue, MatchKind kind, string? inputFormat = null)
        {
            ParameterName = parameterName;
#pragma warning disable CS0618 // Type or member is obsolete - setters should be private after removing obsolete
            Kind = kind;
            ParameterValue = parameterValue;
            InputParameterFormat = inputFormat;
            Location = null;
            InputParameterName = null;
            AdditionalInformation = null;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Obsolete("Use ParameterName instead")]
        public MatchLocation? Location { get; set; }

        /// <summary>
        /// Defines the match status.
        /// </summary>
        public MatchKind Kind
        {
            get;
            [Obsolete("Use constructor instead")]
            set;
        }

        [Obsolete("Use ParameterName instead")]
        /// <summary>
        /// stores canonical parameter name
        /// </summary>
        public string? InputParameterName { get; set; }

        /// <summary>
        /// stores canonical parameter name or default filter name (see <see cref="DefaultParameter"/>).
        /// </summary>
        public string ParameterName
        {
            get;
            set;
        }

        /// <summary>
        /// stores parameter value.
        /// </summary>
        public string? ParameterValue
        {
            get;
            [Obsolete("Use constructor instead")]
            set;
        }

        [Obsolete("Deprecated feature")]
        /// <summary>
        /// stores the exception message if there is an args parse error.
        /// </summary>
        public string? AdditionalInformation { get; set; }

        /// <summary>
        /// stores the option for parameter as used in the host
        /// for example dotnet CLI offers two options for Framework parameter: -f and --framework
        /// if the user uses -f when executing command, <see cref="InputParameterFormat"/> contains -f.
        /// </summary>
        public string? InputParameterFormat
        {
            get;
            [Obsolete("Use constructor instead")]
            set;
        }

        /// <summary>
        /// Contains the names of the template properties supported for filtering in addition to <see cref="ITemplateInfo.CacheParameters"/> and <see cref="ITemplateInfo.Tags"/> defined in templates.
        /// </summary>
        public struct DefaultParameter
        {
            /// <summary>
            /// Template name <see cref="ITemplateInfo.Name"/>.
            /// </summary>
            public const string Name = "Name";

            /// <summary>
            /// Template short names <see cref="ITemplateInfo.ShortNameList"/>.
            /// </summary>
            public const string ShortName = "ShortName";

            /// <summary>
            /// Template classifications <see cref="ITemplateInfo.Classifications"/>.
            /// </summary>
            public const string Classification = "Classification";

            /// <summary>
            /// Template language (<see cref="ITemplateInfo.Tags"/> named "language").
            /// </summary>
            public const string Language = "Language";

            /// <summary>
            /// Template type (<see cref="ITemplateInfo.Tags"/> named "type").
            /// </summary>
            public const string Type = "Type";

            /// <summary>
            /// Template baseline names <see cref="ITemplateInfo.BaselineInfo"/>.
            /// </summary>
            public const string Baseline = "Baseline";

            /// <summary>
            /// Template author <see cref="ITemplateInfo.Author"/>.
            /// </summary>
            public const string Author = "Author";
        }
    }
}
