using System;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Utils
{
    public static class BytePositionUtil
    {
        public static Tuple<int, int> ToLineAndColumn(IPhysicalFileSystem fileSystem, string path, int bytePosition)
        {
            using (Stream fileData = fileSystem.OpenRead(path))
            {
                byte[] buffer = new byte[bytePosition];
                int nRead = fileData.Read(buffer, 0, buffer.Length);
                Encoding encoding = EncodingUtil.Detect(buffer, nRead, out byte[] bom);
                string data = encoding.GetString(buffer);

                int lineEnding = data.IndexOfAny(new[]
                {
                    '\r',
                    '\n'
                }, bom.Length);

                if (lineEnding == -1)
                {
                    return new Tuple<int, int>(1, data.Length);
                }

                if (lineEnding == data.Length - 1)
                {
                    return new Tuple<int, int>(2, 1);
                }

                char lineEndingLastChar = data[lineEnding];
                if (data[lineEnding + 1] != lineEndingLastChar && (data[lineEnding + 1] == '\r' || data[lineEnding + 1] == '\n'))
                {
                    lineEndingLastChar = data[lineEnding + 1];
                    ++lineEnding;
                }

                //We know that we're on line 2 at the very least since we found a line ending at all
                //  so initialize linesFound to 1 & bump it to 2 in the first iteration of the loop below
                int linesFound = 1;
                int lastLineEnding;

                do
                {
                    ++linesFound;
                    lastLineEnding = lineEnding;
                    lineEnding = data.IndexOf(lineEndingLastChar, lineEnding + 1);
                }
                while (lineEnding > -1);

                int column = data.Length - lastLineEnding;
                return Tuple.Create(linesFound, column);
            }
        }
    }
}
