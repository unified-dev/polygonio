using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;
using PolygonIo.WebSocket.Factory;
using System.Buffers;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket : IDisposable
    {        
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private IDisposable link;
        private readonly TransformBlock<ReadOnlySequence<byte>, DeserializedData> decodeBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, loggerFactory, new PolygonTypesEventFactory())
        {
        }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), eventFactory);

            this.decodeBlock = new TransformBlock<ReadOnlySequence<byte>, DeserializedData>((data) =>
            {
                try
                {
                    return this.deserializer.Deserialize(data);
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, $"Error deserializing '{Encoding.UTF8.GetString(data.ToArray())}' ({e}).");
                    return null;
                }
            });

            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, decodeBlock, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory);            
        }

        public void Start(IEnumerable<string> tickers, ITargetBlock<DeserializedData> targetBlock)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is already running.");
                return;
            }

            this.link = decodeBlock.LinkTo(targetBlock);
            this.polygonConnection.Start(tickers);
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 0, 1) == 0)
            {
                this.logger.LogDebug($"{nameof(PolygonWebsocket)} is not running.");
                return;
            }

            this.polygonConnection.Stop();
            this.link?.Dispose(); // unlink
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