using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using Serilog;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace PolygonIo.Utils.StreamReplay
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            var logger = loggerFactory.CreateLogger<Program>();

            var deserializer = new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), new PolygonTypesEventFactory());

            using var stream = File.Open(/*args[0]*/ "polygonio_dump_20210409T213817.dmp", FileMode.Open);
            var reader = PipeReader.Create(stream);

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    var str = System.Text.Encoding.UTF8.GetString(line.ToArray());                                        
                    Console.WriteLine(str);

                    // var obj = deserializer.Deserialize(line);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();     
        }

        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            // Look for a EOL in the buffer.
            SequencePosition? position = buffer.PositionOf((byte)'\n');

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
