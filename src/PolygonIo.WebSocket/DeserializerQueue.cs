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
    // provides a queue of input bytes from an external source, to be buffered and processed by the deserializer
    internal class DeserializerQueue
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

        public void Start(Func<DeserializedData,Task> receiveMessagesFuncAsync)
        {
            AssertNotRunning();

            this.backgroundThread = Task.Run(async () =>
            {               
                while (cts.Token.IsCancellationRequested == false)
                {
                    try
                    {
                        // read data from queue until it arrives, or we are cancelled
                        var item = await this.queue.Reader.ReadAsync(cts.Token);

                        try
                        {                            
                            var data = this.deserializer.Deserialize(item); // deserialize
                            await receiveMessagesFuncAsync(data); // dispatch
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError($"Error deserializing '{Encoding.UTF8.GetString(item.ToArray())}' ({e}).");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;   // task cancelled, break from loop
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e.ToString());
                    }
                }

                this.queue.Writer.Complete(); // mark writer as complete (will not accept anymore writes)
            });
        }

        void AssertNotRunning()
        {
            if (this.backgroundThread != null)
                throw new Exception("worker already running");
        }

        public async Task StopAsync()
        {
            this.cts.Cancel(); // initiate shutdown           
            await this.backgroundThread; // await the backgroundThread to complete
        }

        // add data to the queue from an external source
        public async Task AddFrame(ReadOnlySequence<byte> data)
        {
            await this.queue.Writer.WriteAsync(data);
        }
    }
}