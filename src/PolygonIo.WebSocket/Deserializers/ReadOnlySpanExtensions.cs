using PolygonIo.WebSocket.Contracts;
using System;
using System.Text;

namespace PolygonIo.WebSocket.Deserializers
{
    internal static class ReadOnlySpanExtensions
    {
        const int EventTypePosition = 7;
        static readonly byte[] StatusAsUtf8 = Encoding.UTF8.GetBytes("status");

        public static bool ContainsTrade(this ReadOnlySpan<byte> data)
        {
            return data[EventTypePosition] == MessageTypes.Trade;
        }

        public static bool ContainsQuote(this ReadOnlySpan<byte> data)
        {
            return data[EventTypePosition] == MessageTypes.Quote;
        }

        public static bool ContainsAggregatePerSecond(this ReadOnlySpan<byte> data)
        {
            return data[EventTypePosition] == MessageTypes.AggregatePerSecond;
        }

        public static bool ContainsAggregatePerMinute(this ReadOnlySpan<byte> data)
        {
            return data[EventTypePosition] == MessageTypes.AggregatePerMinute[0]
                && data[EventTypePosition + 1] == MessageTypes.AggregatePerMinute[1];
        }
        public static bool ContainsStatus(this ReadOnlySpan<byte> data)
        {
            return data.IndexOf(StatusAsUtf8) != -1;
        }     
    }
}
