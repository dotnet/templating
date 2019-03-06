using System;
using System.Buffers;
using System.IO;
using Microsoft.TemplateEngine.Utils;

namespace dotnet_new3
{
    internal class StreamBufferWriterImpl : StreamBufferWriter, IBufferWriter<byte>
    {
        public StreamBufferWriterImpl(Stream underlyingStream, int bufferSize)
            : base(underlyingStream, bufferSize)
        {
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            (byte[] buffer, int start, int length) = EnsureCapacity(sizeHint);
            return buffer.AsMemory(start, length);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            (byte[] buffer, int start, int length) = EnsureCapacity(sizeHint);
            return buffer.AsSpan(start, length);
        }
    }
}
