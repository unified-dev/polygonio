using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using PolygonIo.WebSocket.Contracts;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket : IDisposable
    {
        private readonly int stackAllocLength;
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private ActionBlock<IEnumerable<object>> dispatchBlock;
        private readonly PolygonConnection polygonConnection;


        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, int stackAllocLength = 32768)
        {
            this.stackAllocLength = stackAllocLength;
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = new Utf8JsonDeserializer();
            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory, true);            
        }

        unsafe IEnumerable<object> Decode(ReadOnlySequence<byte> data)
        {
            Span<byte> localBuffer = data.Length < this.stackAllocLength ? stackalloc byte[(int)data.Length] : new byte[(int)data.Length];
            data.CopyTo(localBuffer); //  This will do a multi-segment copy of the sequence for us via dotnet framework.

            // We are using PolygonConnection with constructor parameter `isUsingArrayPool = true` as created in our constructor.
            // The data passed to us is from allocated buffers from the shared pool - so here we must return buffers of the sequence.
            foreach (var chunk in data)
            {
                 if (MemoryMarshal.TryGetArray(chunk, out var segment))
                     ArrayPool<byte>.Shared.Return(segment.Array);
            }
           
            var list = new List<object>();

            try
            {
                deserializer.Deserialize(localBuffer,
                            (quote) => list.Add(quote),
                            (trade) => list.Add(trade),
                            (aggregate) => list.Add(aggregate),
                            (aggregate) => list.Add(aggregate),
                            (status) => list.Add(status),
                            (ex) => this.logger.LogError(ex, ex.Message));
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, $"Error deserializing '{Encoding.UTF8.GetString(localBuffer.ToArray())}' ({ex.Message}).");
            }

            // Return data.
            return list.AsEnumerable();
        }

        ActionBlock<IEnumerable<object>> NewDispatchBlock(Func<Trade, Task> onTrade, Func<Quote, Task> onQuote, Func<TimeAggregate, Task> onAggregate, Func<Status, Task> onStatus)
        {
            return new ActionBlock<IEnumerable<object>>(async (items) =>
            {
                foreach (var item in items)
                {
                    if (item is Quote quote)
                        await onQuote(quote);
                    else if (item is Trade trade)
                        await onTrade(trade);
                    else if (item is TimeAggregate aggregate)
                        await onAggregate(aggregate);
                    else if (item is Status status)
                        await onStatus(status);
                }
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 1 });
        }

        public void Start(IEnumerable<string> tickers, Func<Trade, Task> onTrade, Func<Quote, Task> onQuote, Func<TimeAggregate, Task> onAggregate, Func<Status, Task> onStatus)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is already running.");
                return;
            }

            this.dispatchBlock = NewDispatchBlock(onTrade, onQuote, onAggregate, onStatus);
            this.polygonConnection.Start(tickers, async (data) =>
            {
                var decoded = Decode(data);
                await this.dispatchBlock.SendAsync(decoded);
            });
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 0, 1) == 0)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is not running.");
                return;
            }
            
            this.polygonConnection.Stop();            
            this.dispatchBlock.Complete();
            this.dispatchBlock.Completion.Wait();
        }

        public async Task StopAsync()
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 0, 1) == 0)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is not running.");
                return;
            }

            await this.polygonConnection.StopAsync();
            this.dispatchBlock.Complete();
            await this.dispatchBlock.Completion;
        }

        public void Dispose()
        {
            Dispose(true); // Dispose of unmanaged resources.
            GC.SuppressFinalize(this); // Suppress finalization.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this.polygonConnection.Dispose();
        }
    }
}