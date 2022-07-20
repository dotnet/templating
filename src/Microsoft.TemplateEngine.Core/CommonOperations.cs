// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core
{
    internal static class CommonOperations
    {
        internal static void WhitespaceHandler(
            this IProcessorState processor,
            ref int bufferLength,
            ref int currentBufferPosition,
            bool wholeLine = false,
            bool trim = false,
            bool trimForward = false,
            bool trimBackward = false)
        {
            if (wholeLine)
            {
                processor.ConsumeWholeLine(ref bufferLength, ref currentBufferPosition);
                return;
            }

            if (trim)
            {
                trimForward = true;
                trimBackward = true;
            }

            processor.TrimWhitespace(trimForward, trimBackward, ref bufferLength, ref currentBufferPosition);
        }

        internal static void ConsumeWholeLine(this IProcessorState processor, ref int bufferLength, ref int currentBufferPosition)
        {
            processor.SeekTargetBackWhile(processor.EncodingConfig.Whitespace);
            processor.SeekBufferForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
        }

        internal static void TrimWhitespace(this IProcessorState processor, bool forward, bool backward, ref int bufferLength, ref int currentBufferPosition)
        {
            if (backward)
            {
                processor.SeekTargetBackWhile(processor.EncodingConfig.Whitespace);
            }

            if (forward)
            {
                processor.SeekBufferForwardWhile(processor.EncodingConfig.Whitespace, ref bufferLength, ref currentBufferPosition);
                //Consume the trailing line end if possible
                processor.EncodingConfig.LineEndings.GetOperation(processor.CurrentBuffer, bufferLength, ref currentBufferPosition, out _);
            }
        }
    }
}
