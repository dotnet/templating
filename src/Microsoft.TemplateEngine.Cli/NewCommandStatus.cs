// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Cli
{
    // * MS HResults overview: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/705fb797-2175-4a90-b5a3-3918024b10b8
    // * Most common HResults: https://docs.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values
    // * shell (bash) system exit codes: https://tldp.org/LDP/abs/html/exitcodes.html
    // * sysexit.h: https://www.freebsd.org/cgi/man.cgi?query=sysexits&apropos=0&sektion=0&manpath=FreeBSD+4.3-RELEASE&format=html

    internal enum NewCommandStatus
    {
        /// <summary>
        /// The template was instantiated successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The template instantiation failed.
        /// Unix:     126    Command invoked cannot execute    |    Permission problem or command is not an executable
        /// HResult:  0x80040206 EVENT_E_INTERNALERROR | An unexpected internal error was detected.
        /// HResult:  0x80020009 DISP_E_EXCEPTION | Exception occurred.
        /// </summary>
        CreateFailed = unchecked((int)0x80020009),

        /// <summary>
        /// The mandatory parameters for template are missing.
        /// Unix:     EX_USAGE (64)    The command was used incorrectly, e.g., with the wrong number of arguments, a bad flag, a bad syntax in a parameter, or whatever.
        /// HResult:  0x8002000F DISP_E_PARAMNOTOPTIONAL | Parameter not optional.
        /// </summary>
        MissingMandatoryParam = unchecked((int)0x8002000F),

        /// <summary>
        /// The values passed for template parameters are invalid.
        /// Unix:     EX_USAGE (64)    The command was used incorrectly, e.g., with the wrong number of arguments, a bad flag, a bad syntax in a parameter, or whatever.
        /// HResult:  0x80070057 E_INVALIDARG | One or more arguments are not valid
        /// HResult:  0x80020005 DISP_E_TYPEMISMATCH | Type mismatch.
        ///  // RPC - probably not good choice here
        /// HResult:  0x80010010 RPC_E_INVALID_PARAMETER | A particular parameter is invalid and cannot be (un)marshaled.
        /// </summary>
        InvalidParamValues = unchecked((int)0x80020005),

        /// <summary>
        /// The subcommand to run is not specified.
        /// Unix:     EX_USAGE (64)    The command was used incorrectly, e.g., with the wrong number of arguments, a bad flag, a bad syntax in a parameter, or whatever.
        /// HResult:  0x80020003 DISP_E_MEMBERNOTFOUND | Member not found.
        /// HResult:  0x8002000E DISP_E_BADPARAMCOUNT | Invalid number of parameters.
        /// </summary>
        OperationNotSpecified = unchecked((int)0x8002000E),

        /// <summary>
        /// The template is not found.
        /// Unix:     127    "command not found"    |    Possible problem with $PATH or a typo
        /// HResult:  0x80020003 DISP_E_MEMBERNOTFOUND | Member not found.
        /// HResult:  0x80020006 DISP_E_UNKNOWNNAME | Unknown name.
        /// </summary>
        NotFound = unchecked((int)0x80020006),

        /// <summary>
        /// The operation is cancelled.
        /// Unix: ? - this is actually not an abort, but rather a wrong usage (so EX_USAGE?)
        /// HResult:    0x80004004 E_ABORT | Operation aborted.
        /// </summary>
        Cancelled = unchecked((int)0x80004004),

        /// <summary>
        /// The result received from template engine core is not expected.
        /// Unix:     EX_SOFTWARE (70)    An internal software error has been detected.  This should be limited to non-operating system related errors as possible.
        ///  // RPC - probably not good choice here
        /// HResult:  0x8000FFFF E_UNEXPECTED | Unexpected failure
        /// HResult:  0x80040206 EVENT_E_INTERNALERROR | An unexpected internal error was detected.
        /// HRESULT:  0x80010001 RPC_E_CALL_REJECTED | Call was rejected by callee.
        /// </summary>
        UnexpectedResult = unchecked((int)0x80010001),

        /// <summary>
        /// The manipulation with alias has failed.
        /// Not used - questionable intention
        /// HRESULT:  0x80010002 RPC_E_CALL_CANCELED | Call was canceled by the message filter.
        /// </summary>
        AliasFailed = unchecked((int)0x80010002),

        /// <summary>
        /// The operation is cancelled due to destructive changes to existing files are detected.
        /// Unix:     EX_CANTCREAT (73)    A (user specified) output file cannot be created.
        /// HResult:  0x80004004 E_ABORT | Operation aborted.
        ///   // looks off
        /// HResult:    0x8002000D DISP_E_ARRAYISLOCKED | Memory is locked.
        /// </summary>
        DestructiveChangesDetected = unchecked((int)0x8002000D),

        /// <summary>
        /// Post action failed.
        /// Unix:     EX_SOFTWARE (70)    An internal software error has been detected.  This should be limited to non-operating system related errors as possible.
        /// HResult:  0x8000FFFF E_UNEXPECTED | Unexpected failure
        /// HResult:  0x80040206 EVENT_E_INTERNALERROR | An unexpected internal error was detected.
        /// HResult:  0x80010003 RPC_E_CANTPOST_INSENDCALL | The caller is dispatching an intertask SendMessage call and cannot call out via PostMessage.
        /// </summary>
        PostActionFailed = unchecked((int)0x80010003),

        /// <summary>
        /// Generic error when displaying help.
        /// Not used - questionable intent
        /// </summary>
        DisplayHelpFailed = unchecked((int)0x80010004)
    }
}
