using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;
using PolygonIo.WebSocket.Factory;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket : IDisposable
    {        
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private TransformBlock<ReadOnlySequence<byte>, DeserializedData> decodeBlock;
        private ActionBlock<DeserializedData> dispatchBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, loggerFactory, new PolygonTypesEventFactory())
        {
        }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), eventFactory);
            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory, true);            
        }

        TransformBlock<ReadOnlySequence<byte>, DeserializedData> NewDecodeBlock()
        {
            return new TransformBlock<ReadOnlySequence<byte>, DeserializedData>((data) =>
            {
                try
                {
                    var deserializedData = this.deserializer.Deserialize(data);

                    // We are using PolygonConnection with constructor parameter isUsingArrayPool = true, it has
                    // allocated buffers for us from the shared pool - so here we must return buffers.
                    foreach (var chunk in data)
                    {
                        if (MemoryMarshal.TryGetArray(chunk, out var segment))
                            ArrayPool<byte>.Shared.Return(segment.Array);
                    }

                    // Return data.
                    return deserializedData;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Error deserializing '{Encoding.UTF8.GetString(data.ToArray())}' ({ex}).");
                    return null;
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = Environment.ProcessorCount*2 });
        }

        ActionBlock<DeserializedData> NewDispatchBlock(Func<DeserializedData, Task> target)
        {
            return new ActionBlock<DeserializedData>(async (deserializedData) => await target(deserializedData),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 1 });
        }

        public void Start(IEnumerable<string> tickers, Func<DeserializedData,Task> target)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is already running.");
                return;
            }

            this.decodeBlock = NewDecodeBlock();
            this.dispatchBlock = NewDispatchBlock(target);
            this.decodeBlock.LinkTo(this.dispatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            this.polygonConnection.Start(tickers, async (data) => await this.decodeBlock.SendAsync(data));
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 0, 1) == 0)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is not running.");
                return;
            }
            
            this.polygonConnection.Stop();            
            this.decodeBlock.Complete();
            this.decodeBlock.Completion.Wait();
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.polygonConnection.Dispose();
            }
        }
    }
}