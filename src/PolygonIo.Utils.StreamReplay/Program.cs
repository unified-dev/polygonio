using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.Utils.StreamReplay
{

    class Program
    {
        static long countQuote = 0;
        static long countTrade= 0;
        static long countAggregates = 0;
        static long countStatus = 0;
        static long errors = 0;

        static ILogger<Program> logger;
        static Utf8JsonDeserializer deserializer;       
        
        static async Task Main(string[] args)
        {
            // using var fileStream = File.Open(args[0], FileMode.Open);
            
            var bytes = File.ReadAllBytes(args[0]);
            var buffer = new ReadOnlySequence<byte>(bytes);

            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            logger = loggerFactory.CreateLogger<Program>();

            deserializer = new Utf8JsonDeserializer(new PolygonTypesEventFactory());

            // var reader = PipeReader.Create(fileStream);

            var sw = new Stopwatch();
            sw.Start();

            //while (true)
            //{
                //var result = await reader.ReadAsync();
                //var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    Dispatch(Convert(line));
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                //reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data.
                //if (result.IsCompleted)
                //   break;
            //}

            sw.Stop();

            // Mark the PipeReader as complete.
            //await reader.CompleteAsync();

            var total = countQuote + countTrade + countAggregates + countStatus;
            logger.LogInformation($"Replayed {total:n0} objects with {errors:n0} errors in {sw.Elapsed.TotalSeconds:n2} seconds ({total/sw.Elapsed.TotalSeconds:n2} objects/second)");
            logger.LogInformation($"Quotes: {countQuote:n0} ({100*countQuote/(float)total:n2}%). Trades: {countTrade:n0} ({100*countTrade/(float)total:n2}%). Aggregates: {countAggregates:n0} ({100*countAggregates/(float)total:n2}%). Statuses: {countStatus:n0} ({100*countStatus/(float)total:n2}%).");
        }

        static void Dispatch(IEnumerable<object> data)
        {
            if (data != null)
            {
                foreach(var item in data)
                {
                    if(item is IQuote)
                    {
                        countQuote++;
                    }
                    else if(item is ITrade)
                    {
                        countTrade++;
                    }
                    else if(item is ITimeAggregate)
                    {
                        countAggregates++;
                    }
                    else if(item is IStatus)
                    {
                        countStatus++;
                    }
                }
            }
        }

        static IEnumerable<object> Convert(ReadOnlySequence<byte> data)
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
                    (error) => logger.LogError(error));                
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
