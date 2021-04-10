using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace PolygonIo.Utils.StreamReplay
{
    class Program
    {
        static long position = 0;
        static byte[] previousBuffer;

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

                while (TryDeframe(ref buffer, out ReadOnlySequence<byte> line))
                {
                    var str = System.Text.Encoding.UTF8.GetString(line.ToArray());                    
                    var obj = deserializer.Deserialize(line.ToArray());
                    Console.WriteLine($"{position:n0}: {str}");
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();     
        }

        private static bool TryDeframe(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
        {
            int frameCount = 0;
            int start = -1;
            int end = -1;

            var bytes = buffer.ToArray();

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                position++;

                if (b == (byte)'[')
                {
                    if (start == -1)
                        start = i;

                    frameCount++;
                }
                else if (b == (byte)']')
                {
                    frameCount--;

                    if(frameCount < 0)
                    {
                        var str = System.Text.Encoding.UTF8.GetString(bytes);
                        throw new Exception($"Framing error at position {position:n0} in file (0x{position:X}). Tried to parse {str}.");                        
                    }

                    if (frameCount == 0)
                    {
                        end = i;
                        break;
                    }
                }
            }

            if (start == -1 || end == -1) // no frame found
            {
                frame = default;
                return false; 
            }

            previousBuffer = bytes;
            frame = buffer.Slice(start, end+1);
            buffer = buffer.Slice(frame.Length);
            return true;
        }
    }
}
