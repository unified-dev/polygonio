using System;
using System.Buffers;

namespace PolygonIo.Parser.Tests
{
    static class ReadOnlySequenceExtensions
    {
        public static bool Is(this ReadOnlySequence<byte> readOnlySequence, char c)
        {
            if(readOnlySequence.IsSingleSegment)
                return readOnlySequence.FirstSpan[0] == c;
            else
            {
                // TODO: check across multiple spans.
                return false;
            }
        }

        public static bool Is(this ReadOnlySequence<byte> readOnlySequence, string s)
        {
            if (readOnlySequence.Length < s.Length)
                return false;

            var sSpan = s.AsSpan();

            if (readOnlySequence.IsSingleSegment)
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (readOnlySequence.FirstSpan[i] != sSpan[i])
                        return false;
                }

                return true;
            }
            else
            {
                // TODO: check across multiple spans.
                return false;
            }
        }
    }
}
