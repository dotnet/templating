using System;
using System.IO;

namespace Microsoft.TemplateEngine.Utils
{
    /// <summary>
    /// For usage, create a class that derives from this type and implements IBufferWriter&lt;byte&gt;
    /// </summary>
    public abstract class StreamBufferWriter : Stream
    {
        private readonly Stream _underlyingStream;
        private long _bytesWritten;
        private byte[] _currentBuffer;
        private int _position;

        public StreamBufferWriter(Stream underlyingStream, int bufferSize)
        {
            _underlyingStream = underlyingStream;
            _currentBuffer = new byte[bufferSize];
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _bytesWritten;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (_position > 0)
            {
                _underlyingStream.Write(_currentBuffer, 0, _position);
                _bytesWritten += _position;
                _position = 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Flush();
            _underlyingStream.Write(buffer, offset, count);
            _bytesWritten += count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
            }
        }

        public void Advance(int count)
        {
            //While the number of remaining bytes to advance exceeds the amount of free space in the buffer
            //  skip over and flush that number of bytes
            while (count >= _currentBuffer.Length - _position)
            {
                //Decrement count by the number of items that were able to be consumed by the current buffer
                count -= _currentBuffer.Length - _position;
                //Since this is specifically the "too many items" case (in this loop), we've filled the whole buffer
                _position = _currentBuffer.Length;
                //Flush it (which resets position, updates bytes written, etc)
                Flush();
            }

            //Add the remaining advance to the current position - which is now guaranteed to be less than the number
            //  of free elements in the buffer
            _position += count;
        }

        protected (byte[], int, int) EnsureCapacity(int sizeHint)
        {
            if (sizeHint > _currentBuffer.Length - _position)
            {
                Flush();
            }

            return (_currentBuffer, _position, _currentBuffer.Length - _position);
        }
    }
}
