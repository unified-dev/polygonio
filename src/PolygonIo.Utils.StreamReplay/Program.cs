using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Deserializers;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Utils.StreamReplay
{
    internal class Program
    {
        private static long countQuote = 0;
        private static long countTrade= 0;
        private static long countAggregates = 0;
        private static long countStatus = 0;
        private static long errors = 0;
        private static ILogger<Program> logger;
        private static Utf8JsonDeserializer deserializer;

        public static async Task Main(string[] args)
        {
            await using var fileStream = File.Open(args[0], FileMode.Open);
            var reader = PipeReader.Create(fileStream);

            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            logger = loggerFactory.CreateLogger<Program>();
            
            deserializer = new Utf8JsonDeserializer();

            var sw = new Stopwatch();
            sw.Start();

            var lineLengths = new List<long>();

            /*
            var bytes = File.ReadAllBytes(args[0]);
            var start = 0;
            for(var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == '\n')
                {
                    var line = new ReadOnlySpan<byte>(bytes, start, i - start);
                    var str = Encoding.UTF8.GetString(line.ToArray());
                    start = i + 1;
                    Dispatch(Convert(line));
                    lineLengths.Add(line.Length);
                }
            }
            */

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    Dispatch(Convert(line.ToArray()));
                    lineLengths.Add(line.Length);
                }
               
                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data.
                if (result.IsCompleted)
                   break;
            }

            sw.Stop();

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();

            var total = countQuote + countTrade + countAggregates + countStatus;
            logger.LogInformation($"Read {lineLengths.Count:n0} lines. Average {lineLengths.Average():n2} bytes per line (max: {lineLengths.Max():n0} / min: {lineLengths.Min():n0}).");
            logger.LogInformation($"Replayed {total:n0} objects with {errors:n0} errors in {sw.Elapsed.TotalSeconds:n2} seconds ({total/sw.Elapsed.TotalSeconds:n2} objects/second)");            
            logger.LogInformation($"Quotes: {countQuote:n0} ({100*countQuote/(float)total:n2}%). Trades: {countTrade:n0} ({100*countTrade/(float)total:n2}%). Aggregates: {countAggregates:n0} ({100*countAggregates/(float)total:n2}%). Statuses: {countStatus:n0} ({100*countStatus/(float)total:n2}%).");
        }

        private static void Dispatch(IEnumerable<object> data)
        {
            if (data == null)
                return;

            foreach(var item in data)
            {
                switch (item)
                {
                    case Quote quote:
                        countQuote++;
                        break;
                    case Trade trade:
                        countTrade++;
                        break;
                    case TimeAggregate timeAggregate:
                        countAggregates++;
                        break;
                    case Status status:
                        countStatus++;
                        break;
                }
            }
        }

        private static IEnumerable<object> Convert(ReadOnlySpan<byte> data)
        {
            var list = new List<object>();
            try
            {
                deserializer.Deserialize(data,
                    (quote) => list.Add(quote),
                    (trade) => list.Add(trade),
                    (aggregate) => list.Add(aggregate),
                    (aggregate) => list.Add(aggregate),
                    (status) => list.Add(status),
                    (ex) => logger.LogError(ex, ex.Message));                
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref errors);
                logger.LogError(ex, ex.Message);
            }

            return list;
        }

        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            // Look for a EOL in the buffer.
            var position = buffer.PositionOf((byte)'\n');

            if (position == null)
            {
                line = default;
                return false;
            }

            // Skip the line + the \n.
            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }
    }
}
