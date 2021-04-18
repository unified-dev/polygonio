using System;
using System.IO;

namespace PolygonIo.Utils.StreamReplay
{
    public static class BinaryReaderExtensions
    {
        public static ReadOnlySpan<byte> ReadLine(this BinaryReader reader)
        {
            if (reader.IsEndOfStream())
                return null;

            var buffer = new byte[16384];
            var i = 0;

            while (!reader.IsEndOfStream() && i < buffer.Length)
            {
                if((buffer[i] = reader.ReadByte()) == '\n')
                    return new ReadOnlySpan<byte>(buffer, 0, i + 1);

                i++;           
            }
            return null;
        }

        public static bool IsEndOfStream(this BinaryReader reader)
        {
            return reader.BaseStream.Position == reader.BaseStream.Length;
        }
    }
}
