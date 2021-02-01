using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Channels;
using System.Reactive.Linq;
using System.IO;
using System.Text;

namespace PolygonIo.WebSocket
{
    public class DeserializerQueue
    {
        private readonly Channel<ReadOnlySequence<byte>> queue;
        private readonly ILogger<DeserializerQueue> logger;
        private readonly IPolygonDeserializer deserializer;
        Task backgroundThread = null;
        readonly CancellationTokenSource cts = new CancellationTokenSource();

        public DeserializerQueue(ILogger<DeserializerQueue> logger, IPolygonDeserializer deserializer)
        {
            this.queue = Channel.CreateUnbounded<ReadOnlySequence<byte>>();
            this.logger = logger;
            this.deserializer = deserializer;
        }

        string GetFileName()
        {
            return "Polygon_" + DateTime.UtcNow.ToString()
                                    .Replace(" ", "_")
                                    .Replace(":", "-")
                                    .Replace("/", "-") + ".log";
        }

        // no async code, just start without task
        public void Start(Func<DeserializedData,Task> receiveMessagesAsync, bool logToFile = false)
        {
            if (backgroundThread != null)
                throw new Exception("worker already running");

            var fileName = GetFileName();

            if (logToFile == true && File.Exists(fileName))
            {
                throw new Exception("Log file exists " + fileName);
            }

            backgroundThread = Task.Run(async () =>
            {
                if (logToFile == true)
                {
                    using var fs = new StreamWriter(fileName, false);

                    while (cts.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            var item = await this.queue.Reader.ReadAsync(cts.Token);

                            try
                            {
                                var text = Encoding.UTF8.GetString(item.ToArray());
                                await fs.WriteAsync(text);

                                var data = this.deserializer.Deserialize(item);

                                await receiveMessagesAsync(data);
                            }
                            catch (Exception e)
                            {
                                this.logger.LogError($"error deserializing {System.Text.Encoding.UTF8.GetString(item.ToArray())} {e}");
                            }
                        }
                        catch (OperationCanceledException) { break; }  // task cancelled, break from loop
                        catch (Exception e) { this.logger.LogError(e.ToString()); }
                    }
                }
                else
                {
                    while (cts.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            var item = await this.queue.Reader.ReadAsync(cts.Token);

                            try
                            {
                                var data = this.deserializer.Deserialize(item);
                                await receiveMessagesAsync(data);
                            }
                            catch (Exception e)
                            {
                                this.logger.LogError($"error deserializing {System.Text.Encoding.UTF8.GetString(item.ToArray())} {e}");
                            }
                        }
                        catch (OperationCanceledException) { break; }  // task cancelled, break from loop
                        catch (Exception e) { this.logger.LogError(e.ToString()); }
                    }
                }
                this.queue.Writer.Complete();
                await this.queue.Reader.Completion;
            });
        }

        public async Task StopAsync()
        {
            this.cts.Cancel();
            // could take a while to shutdown, await the backgroundThread to complete
            await this.backgroundThread;
        }

        public async Task AddFrame(ReadOnlySequence<byte> data)
        {
            await this.queue.Writer.WriteAsync(data);
        }
    }
}