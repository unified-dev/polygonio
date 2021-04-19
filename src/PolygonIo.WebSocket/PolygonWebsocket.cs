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
using PolygonIo.WebSocket.Contracts;
using System.Linq;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket : IDisposable
    {        
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private ActionBlock<IEnumerable<object>> dispatchBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, loggerFactory, new PolygonTypesEventFactory())
        {
        }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = new Utf8JsonDeserializer(eventFactory);
            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory, true);            
        }

        unsafe IEnumerable<object> Decode(ReadOnlySequence<byte> data)
        {
            Span<byte> localBuffer = data.Length < 8_192 ? stackalloc byte[(int)data.Length] : new byte[(int)data.Length];

            data.ToArray().CopyTo(localBuffer);
            /*data.FirstSpan.CopyTo(localBuffer);

            if (data.IsSingleSegment == false)
            {
                // TODO: Allow arbitary number of frames.
                var otherStart = localBuffer.Slice(data.FirstSpan.Length, localBuffer.Length - data.FirstSpan.Length);                    
                var enumerator = data.GetEnumerator();
                enumerator.MoveNext();
                enumerator.Current.Span.CopyTo(otherStart);                
            }*/

            // We are using PolygonConnection with constructor parameter isUsingArrayPool = true, it has
            // allocated buffers for us from the shared pool - so here we must return buffers.
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
                            (error) => this.logger.LogError(error));
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, $"Error deserializing '{Encoding.UTF8.GetString(localBuffer.ToArray())}' ({ex.Message}).");
            }

            // Return data.
            return list.AsEnumerable();
        }

        ActionBlock<IEnumerable<object>> NewDispatchBlock(Func<ITrade, Task> onTrade, Func<IQuote, Task> onQuote, Func<ITimeAggregate, Task> onAggregate, Func<IStatus, Task> onStatus)
        {
            return new ActionBlock<IEnumerable<object>>(async (items) =>
            {
                foreach (var item in items)
                {
                    if (item is IQuote quote)
                        await onQuote(quote);
                    else if (item is ITrade trade)
                        await onTrade(trade);
                    else if (item is ITimeAggregate aggregate)
                        await onAggregate(aggregate);
                    else if (item is IStatus status)
                        await onStatus(status);
                }
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 1 });
        }

        public void Start(IEnumerable<string> tickers, Func<ITrade, Task> onTrade, Func<IQuote, Task> onQuote, Func<ITimeAggregate, Task> onAggregate, Func<IStatus, Task> onStatus)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is already running.");
                return;
            }

            this.dispatchBlock = NewDispatchBlock(onTrade, onQuote, onAggregate, onStatus);
            this.polygonConnection.Start(tickers, async (data) => await this.dispatchBlock.SendAsync(Decode(data)));
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