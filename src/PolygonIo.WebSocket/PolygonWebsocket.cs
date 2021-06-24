using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Serializers;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket : IDisposable
    {
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IPolygonDeserializer deserializer;
        private bool isRunning;
        private ActionBlock<IEnumerable<object>> dispatchBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, new SystemTextJsonDeserializer(), loggerFactory)
        { }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, IPolygonDeserializer deserializer, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = deserializer;
            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory, true);            
        }

        private IEnumerable<object> Decode(ReadOnlySequence<byte> data)
        {
            var list = new List<object>();
            Span<byte> localBuffer = new byte[(int)data.Length];
            data.CopyTo(localBuffer); //  This will do a multi-segment copy of the sequence for us via dotnet framework.

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

            // Return data as a list so we can dispatch to queue with one method call, as opposed to individual dispatches.
            return list;
        }

        private void ReturnRentedBuffer(ReadOnlySequence<byte> data)
        {
            // We are using PolygonConnection with constructor parameter `isUsingArrayPool = true` as created in our constructor.
            // The data passed to us is from allocated buffers from the shared pool - so here we must return buffers of the sequence.
            foreach (var chunk in data)
            {
                if (MemoryMarshal.TryGetArray(chunk, out var segment))
                    ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }

        public void Start(IEnumerable<string> tickers, Func<Trade, Task> onTradeAsync, Func<Quote, Task> onQuoteAsync,
            Func<TimeAggregate, Task> onAggregateAsync, Func<StatusMessage, Task> onStatusAsync,
            Func<ReadOnlySequence<byte>, Task> onTraceRawFrameAsync = null)
        {
            if (this.isRunning)
                return;
            this.isRunning = true;

            this.dispatchBlock = new ActionBlock<IEnumerable<object>>(async (items) =>
            {
                foreach (var item in items)
                {
                    if (item is Quote quote)
                        await onQuoteAsync(quote);
                    else if (item is Trade trade)
                        await onTradeAsync(trade);
                    else if (item is TimeAggregate aggregate)
                        await onAggregateAsync(aggregate);
                    else if (item is StatusMessage status)
                        await onStatusAsync(status);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 1 });

            this.polygonConnection.Start(tickers, async (data) =>
            {
                if (onTraceRawFrameAsync != null)
                    await onTraceRawFrameAsync(data);

                var decodedData = Decode(data);
                ReturnRentedBuffer(data);
                await this.dispatchBlock.SendAsync(decodedData);
            });
        }

        public void Stop()
        {
            if(this.isRunning == false)
                return;

            this.polygonConnection.Stop();            
            this.dispatchBlock.Complete();
            this.dispatchBlock.Completion.Wait();
            this.isRunning = false;
        }

        public async Task StopAsync()
        {
            if (this.isRunning == false)
                return;

            await this.polygonConnection.StopAsync();
            this.dispatchBlock.Complete();
            await this.dispatchBlock.Completion;
            this.isRunning = false;
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