using System;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    static class Utf8JsonReaderExtensions
    {
        public static bool ValueSpanIs(this ref Utf8JsonReader reader, string s)
        {
            if (s.Length > reader.ValueSpan.Length)
                return false;

            var sSpan = s.AsSpan();

            for (var i = 0; i < s.Length; i++)
            {
                if (reader.ValueSpan[i] != sSpan[i])
                    return false;
            }

            return true;
        }

        public static bool ValueSpanIs(this ref Utf8JsonReader reader, char c)
        {
            if (reader.ValueSpan.Length > 1)
                return false;
            else
                return reader.ValueSpan[0] == c;
        }
    }
}
