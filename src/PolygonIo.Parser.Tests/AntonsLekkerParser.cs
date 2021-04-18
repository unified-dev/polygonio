using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonIo.Parser.Tests
{
    class AntonsLekkerParser
    {
        static void Parse(byte[] bytes)
        {
            long countQuote = 0;
            long countTrade = 0;
            long countAggregates = 0;
            long countStatus = 0;
            long errors = 0;

            var buffer = new ReadOnlySequence<byte>(bytes);

            while (TryReadLine(ref buffer, out var line))
            {
                while (GetRecord(ref line, out var record))
                {
                    if (GetString(ref record, out var ev))
                    {
                        if (ev.Is("ev") == false)
                            ; // Error - deal with it.

                        if (GetString(ref record, out var type))
                        {
                            if (type.Is('Q'))
                            {
                                GetQuote(ref record, out var quote);
                                countQuote++;
                            }
                            else if (type.Is('T'))
                            {
                                GetTrade(ref record, out var trade);
                                countTrade++;
                            }
                            else if (type.Is('A'))
                            {
                                GetAggregate(ref record, out var timeAggregate);
                                countAggregates++;
                            }
                            else if (type.Is("AM"))
                            {
                                GetAggregate(ref record, out var timeAggregate);
                                countAggregates++;
                            }
                            else if (type.Is("status"))
                            {
                                // TODO: parse
                                countStatus++;
                            }
                        }
                    }
                }
            }
        }

        private static bool GetQuote(ref ReadOnlySequence<byte> record, out Quote quote)
        {
            quote = new Quote();
            quote.BidExchangeId = -1; // in case this is a quote without bid
            quote.AskExchangeId = -1; // in case this is a quote without ask

            while (GetString(ref record, out var propertyValue))
            {
                if (propertyValue.Is(StreamFieldNames.Symbol))
                    quote.Symbol = ReadString(ref record);
                else if (propertyValue.Is(StreamFieldNames.AskExchangeId))
                    quote.AskExchangeId = ReadInt(ref record);
                else if (propertyValue.Is(StreamFieldNames.BidExchangeId))
                    quote.BidExchangeId = ReadInt(ref record);
            }

            return true;
        }

        private static bool GetTrade(ref ReadOnlySequence<byte> record, out Trade trade)
        {
            trade = new Trade();

            while (GetString(ref record, out var propertyValue))
            {
                if (propertyValue.Is(StreamFieldNames.Symbol))
                    trade.Symbol = ReadString(ref record);
            }

            return true;
        }

        private static bool GetAggregate(ref ReadOnlySequence<byte> record, out TimeAggregate timeAggregate)
        {
            timeAggregate = new TimeAggregate();

            while (GetString(ref record, out var propertyValue))
            {
                if (propertyValue.Is(StreamFieldNames.Symbol))
                    timeAggregate.Symbol = ReadString(ref record);
            }

            return true;
        }

        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            var position = buffer.PositionOf((byte)'\n'); // Look for a EOL in the buffer.

            if (position == null)
            {
                line = default;
                return false;
            }

            line = buffer.Slice(0, position.Value); // Skip the line + the \n.
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }

        public static int ReadInt(ref ReadOnlySequence<byte> record, bool validatePropertyName = false)
        {
            _ = GetValue(ref record, out var value);
            if (Utf8Parser.TryParse(value.FirstSpan, out int parsed, out var consumed))
            {
                return parsed;
            }
            else
                throw new Exception("look into this");
        }

        public static string ReadString(ref ReadOnlySequence<byte> line)
        {
            if (GetString(ref line, out var stringValue))
                return Encoding.Default.GetString(stringValue);
            else
                return null;
        }

        public static bool GetString(ref ReadOnlySequence<byte> line, out ReadOnlySequence<byte> value)
        {
            return Get(ref line, (byte)'"', (byte)'"', out value);
        }

        public static bool GetValue(ref ReadOnlySequence<byte> line, out ReadOnlySequence<byte> value)
        {
            return Get(ref line, (byte)':', (byte)',', out value);
        }

        public static bool GetRecord(ref ReadOnlySequence<byte> line, out ReadOnlySequence<byte> record)
        {
            return Get(ref line, (byte)'{', (byte)'}', out record);
        }

        public static bool GetArray<T>(ref ReadOnlySequence<T> line, T arrayStart, T arrayDelimiter, T arrayEnd, out IEnumerable<ReadOnlySequence<T>> value) where T : IEquatable<T>
        {
            var list = new List<ReadOnlySequence<T>>();

            var start = line.PositionOf(arrayStart);

            if (start == null)
            {
                value = default;
                return false;
            }

            // Array is open, move past the array start.
            line = line.Slice(line.GetPosition(1, start.Value));

            while (true)
            {
                SequencePosition? elementEnd;

                if ((elementEnd = line.PositionOf(arrayDelimiter)) != null)
                {
                    // We found a regular value.
                    list.Add(line.Slice(0, elementEnd.Value));
                    line = line.Slice(line.GetPosition(1, elementEnd.Value));
                }
                else if ((elementEnd = line.PositionOf(arrayEnd)) != null)
                {
                    // Array is closed, move past the array end
                    line = line.Slice(line.GetPosition(1, elementEnd.Value));
                    break;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            value = list;
            return true;
        }

        public static bool Get<T>(ref ReadOnlySequence<T> line, T startToken, T stopToken, out ReadOnlySequence<T> value) where T : IEquatable<T>
        {
            var start = line.PositionOf(startToken);
            if (start == null)
            {
                value = default;
                return false;
            }

            value = line.Slice(line.GetPosition(1, start.Value)); // Start slice one past start token.

            var end = value.PositionOf(stopToken);
            if (end == null)
            {
                value = default;
                return false;
            }

            value = value.Slice(0, end.Value); // Slice upto the end position.
            line = line.Slice(line.GetPosition(1, end.Value)); // Move the line forward one past the end.
            return true;
        }
    }
}
