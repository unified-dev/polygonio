using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket
{

    public class PolygonWebsocket
    {        
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly IEventFactory eventFactory;
        private readonly IPolygonDeserializer deserializer;
        private int isRunning;
        private readonly TransformBlock<byte[], DeserializedData> decodeBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory = null)
        {
            // setup factory for which types from decoder are created with
            if (eventFactory != null)
                this.eventFactory = eventFactory;
            else
                this.eventFactory = new PolygonTypesEventFactory();

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

            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, reconnectTimeout, decodeBlock, loggerFactory);
        }

        public void Start(IEnumerable<string> tickers, ITargetBlock<DeserializedData> targetBlock)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
                throw new InvalidOperationException();

            decodeBlock.LinkTo(targetBlock);
            this.polygonConnection.Start(tickers);
        }

        public async Task StopAsync()
        {
            if (this.isRunning == 0)
                return;

            await this.polygonConnection.StopAsync();     

        }
    }
}