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
    class SequenceParser
    {
        static void Parse(string[] args)
        {
            long countQuote = 0;
            long countTrade = 0;
            long countAggregates = 0;
            long countStatus = 0;
            long errors = 0;

            var filename = @"test.txt";

            var bytes = File.ReadAllBytes(filename);

            var sw = Stopwatch.StartNew();

            var buffer = new ReadOnlySequence<byte>(bytes);

            while (TryReadLine(ref buffer, out var line))
            {
                while (GetRecord(ref line, out var record))
                {
                    if (GetString(ref record, out var ev))
                    {
                        GetString(ref record, out var type);

                        if (type.FirstSpan[0] == (byte)'Q')
                        {
                            GetQuote(ref record, out var quote);
                            countQuote++;
                        }
                        else if (type.FirstSpan[0] == (byte)'T')
                        {
                            GetTrade(ref record, out var trade);
                            countTrade++;
                        }
                        else if (type.FirstSpan[0] == (byte)'A')
                        {
                            GetAggregate(ref record, out var timeAggregate);
                            countAggregates++;
                        }
                        else if (type.FirstSpan[0] == (byte)'A' && ev.FirstSpan[1] == (byte)'M')
                        {
                            GetAggregate(ref record, out var timeAggregate);
                            countAggregates++;
                        }
                        else if (System.Text.Encoding.UTF8.GetString(type.ToArray()) == "status")
                        {
                            countStatus++;
                        }
                    }
                }
            }

            sw.Stop();
            var total = countQuote + countTrade + countAggregates + countStatus;
            Console.WriteLine($"Replayed {total:n0} objects with {errors:n0} errors in {sw.Elapsed.TotalSeconds:n2} seconds ({total / sw.Elapsed.TotalSeconds:n2} objects/second)");
            Console.WriteLine($"Quotes: {countQuote:n0} ({100 * countQuote / (float)total:n2}%). Trades: {countTrade:n0} ({100 * countTrade / (float)total:n2}%). Aggregates: {countAggregates:n0} ({100 * countAggregates / (float)total:n2}%). Statuses: {countStatus:n0} ({100 * countStatus / (float)total:n2}%).");
            //Debug.WriteLine($"Number of Quotes: {this.QuoteCount:N0}");
            //Debug.WriteLine($"Number of Trades: {this.TradeCount:N0}");
        }

        private static bool GetQuote(ref ReadOnlySequence<byte> record, out Quote quote)
        {
            quote = new Quote();
            // quote.BidExchangeId = -1; // in case this is a quote without bid
            //quote.AskExchangeId = -1; // in case this is a quote without ask

            GetString(ref record, out var _);
            _ = GetString(ref record, out var symbol);
            quote.Symbol = Encoding.UTF8.GetString(symbol);

            bool bxfound = false;
            bool axfound = false;


            while (GetString(ref record, out var propertyName))
            {
                var span = propertyName.FirstSpan;
                if (propertyName.Length == 2 && span[0] == (byte)'a' && span[1] == (byte)'x')
                {
                    axfound = true;
                }
                else if (propertyName.Length == 2 && span[0] == (byte)'b' && span[1] == (byte)'x')
                {
                    bxfound = true;
                }
            }

            if (axfound == false || bxfound == false)
                Console.WriteLine("missing ax or bx");

            return true;
        }

        private static bool GetTrade(ref ReadOnlySequence<byte> record, out Trade trade)
        {
            trade = new Trade();

            // get the symbol field first
            GetString(ref record, out var _);
            _ = GetString(ref record, out var symbol);
            trade.Symbol = Encoding.UTF8.GetString(symbol);

            while (GetString(ref record, out var propertyName))
            {
                var span = propertyName.FirstSpan;
                if (propertyName.Length == 2 && span[0] == (byte)'b' && span[1] == (byte)'x')
                {

                }
            }

            return true;
        }

        private static bool GetAggregate(ref ReadOnlySequence<byte> record, out TimeAggregate timeAggregate)
        {
            timeAggregate = new TimeAggregate();

            // get the symbol field first
            GetString(ref record, out var _);
            _ = GetString(ref record, out var symbol);
            timeAggregate.Symbol = Encoding.UTF8.GetString(symbol);

            while (GetString(ref record, out var propertyName))
            {
                var span = propertyName.FirstSpan;
                if (propertyName.Length == 2 && span[0] == (byte)'b' && span[1] == (byte)'x')
                {
                    //Utf8Parser.TryParse(first.Span, out value, out consumed);
                }
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

        /*
        public static bool GetIntProperty(ref ReadOnlySequence<byte> record, string propertyName, out int value, bool validatePropertyName = false)
        {
            if(GetString(ref record, out var foundPropertyName))
            {
                if(validatePropertyName)
                {
                    if (propertyName.Length != foundPropertyName.Length)
                    {
                        value = default;
                        return false;
                    }
                }

                _ = GetString(ref record, out var bytes);
                Int32.Parse(bytes)

            }
            
        }*/

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

    class Chunk<T> : ReadOnlySequenceSegment<T>
    {
        public Chunk(ReadOnlyMemory<T> memory) => Memory = memory;
        public Chunk<T> Add(ReadOnlyMemory<T> mem)
        {
            var segment = new Chunk<T>(mem) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }

}
