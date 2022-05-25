// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Cli
{
    /// <summary>
    /// Exit codes based on
    ///  * https://tldp.org/LDP/abs/html/exitcodes.html
    ///  * https://github.com/openbsd/src/blob/master/include/sysexits.h.
    /// Further documentation: https://aka.ms/templating-exit-codes.
    /// Future exit codes should be allocated in a range of 107 - 113. If not sufficient, a range of 79 - 99 may be used as well.
    /// </summary>
    internal enum NewCommandStatus
    {
        /// <summary>
        /// Unexpected internal software issue. The result received from template engine core is not expected.
        /// </summary>
        Unexpected = 70,

        /// <summary>
        /// Can't create output file. The operation was cancelled due to detection of an attempt to perform destructive changes to existing files.
        /// </summary>
        DestructiveChangesDetected = 73,

        /// <summary>
        /// The template was instantiated successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Instantiation Failed - Processing issues.
        /// </summary>
        CreateFailed = 100,

        /// <summary>
        /// Instantiation Failed - Missing mandatory parameter(s) for template.
        /// </summary>
        MissingMandatoryParam = 101,

        /// <summary>
        /// Instantiation/Search Failed - parameter(s) value(s) invalid.
        /// </summary>
        InvalidParamValues = 102,

        /// <summary>
        /// The template was not found.
        /// </summary>
        NotFound = 103,

        /// <summary>
        /// The operation was cancelled.
        /// </summary>
        Cancelled = 104,

        /// <summary>
        /// Instantiation Failed - Post action failed.
        /// </summary>
        PostActionFailed = 105,

        /// <summary>
        /// Installation/Uninstallation Failed - Processing issues.
        /// </summary>
        InstallFailed = 106,
    }
}
