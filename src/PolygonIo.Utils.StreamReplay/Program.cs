using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using Serilog;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.Utils.StreamReplay
{
    class Program
    {
        static long count = 0;
        static long errors = 0;
        static ILogger<Program> logger;
        static Utf8JsonDeserializer deserializer;

        static async Task Main(string[] args)
        {
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            logger = loggerFactory.CreateLogger<Program>();

            deserializer = new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), new PolygonTypesEventFactory());

            using var stream = File.Open(args[0], FileMode.Open);
            var reader = PipeReader.Create(stream);

            var queue = new TransformBlock<byte[], DeserializedData>(
                                Process,
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4, BoundedCapacity = 16 });

            var counter = new ActionBlock<DeserializedData>(
                                Dispatch,
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 1 });
            
            queue.LinkTo(
                    counter,
                    new DataflowLinkOptions { PropagateCompletion = true });

            var sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    await queue.SendAsync(line.ToArray());
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
            queue.Complete();
            await queue.Completion;
            await counter.Completion;

            logger.LogInformation($"Replayed {count:n0} objects with {errors:n0} errors in {sw.Elapsed.TotalSeconds:n2} seconds ({count/sw.Elapsed.TotalSeconds:n2} objects/second)");
        }

        static Task<DeserializedData> Dispatch(DeserializedData data)
        {
            if (data != null)
            {
                count = count + data.Quotes.Count();
                count = count + data.Trades.Count();
                count = count + data.Status.Count();
                count = count + data.PerSecondAggregates.Count();
                count = count + data.PerMinuteAggregates.Count();
            }
            return Task.FromResult(data);
        }

        static Task<DeserializedData> Process(byte[] data)
        {
            try
            {
                return Task.FromResult(deserializer.Deserialize(new ReadOnlySequence<byte>(data)));
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref errors);
                logger.LogError(ex, ex.Message);
            }

            return Task.FromResult<DeserializedData>(null);
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
