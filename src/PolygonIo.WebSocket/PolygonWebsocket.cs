using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;
using PolygonIo.WebSocket.Factory;

namespace PolygonIo.WebSocket
{

    public class PolygonWebsocket : IDisposable
    {        
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IEventFactory eventFactory;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private readonly TransformBlock<byte[], DeserializedData> decodeBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, loggerFactory, new PolygonTypesEventFactory())
        {
        }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory)
        {
            this.eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), this.eventFactory);

            this.decodeBlock = new TransformBlock<byte[], DeserializedData>((data) =>
            {
                try
                {
                    return this.deserializer.Deserialize(data);
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Error deserializing '{Encoding.UTF8.GetString(data)}' ({e}).");
                    return null;
                }
            });

            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, decodeBlock, loggerFactory);            
        }

        public void Start(IEnumerable<string> tickers, ITargetBlock<DeserializedData> targetBlock)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
                throw new InvalidOperationException();

            decodeBlock.LinkTo(targetBlock);
            this.polygonConnection.Start(tickers);
        }

        public void Dispose()
        {
            this.polygonConnection.Dispose();
        }
    }
}