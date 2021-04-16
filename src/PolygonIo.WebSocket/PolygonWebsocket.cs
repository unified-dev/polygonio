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
        private TransformBlock<ReadOnlySequence<byte>, IEnumerable<object>> decodeBlock;
        private ActionBlock<IEnumerable<object>> dispatchBlock;
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

        TransformBlock<ReadOnlySequence<byte>, IEnumerable<object>> NewDecodeBlock()
        {
            return new TransformBlock<ReadOnlySequence<byte>, IEnumerable<object>>((data) =>
            {
                var list = new List<object>();

                try
                {
                    try
                    {
                        deserializer.Deserialize(data,
                                    (quote) => list.Add(quote),
                                    (trade) => list.Add(trade),
                                    (aggregate) => list.Add(aggregate),
                                    (aggregate) => list.Add(aggregate),
                                    (status) => list.Add(status),
                                    (error) => this.logger.LogError(error));
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }

                    // We are using PolygonConnection with constructor parameter isUsingArrayPool = true, it has
                    // allocated buffers for us from the shared pool - so here we must return buffers.
                    foreach (var chunk in data)
                    {
                        if (MemoryMarshal.TryGetArray(chunk, out var segment))
                            ArrayPool<byte>.Shared.Return(segment.Array);
                    }

                    // Return data.
                    return list.AsEnumerable();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Error deserializing '{Encoding.UTF8.GetString(data.ToArray())}' ({ex}).");
                    return null;
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, BoundedCapacity = Environment.ProcessorCount*2 });
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

            this.decodeBlock = NewDecodeBlock();
            this.dispatchBlock = NewDispatchBlock(onTrade, onQuote, onAggregate, onStatus);
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

        public async Task StopAsync()
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 0, 1) == 0)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is not running.");
                return;
            }

            await this.polygonConnection.StopAsync();
            this.decodeBlock.Complete();
            await this.decodeBlock.Completion;
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