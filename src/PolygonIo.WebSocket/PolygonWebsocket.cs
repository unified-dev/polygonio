using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Collections.Generic;
using System.Buffers;
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
        // private ActionBlock<IEnumerable<object>> dispatchBlock;
        private readonly PolygonConnection polygonConnection;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory)
            : this(apiKey, apiUrl, reconnectTimeout, new SystemTextJsonDeserializer(), loggerFactory)
        { }

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, IPolygonDeserializer deserializer, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.deserializer = deserializer;

            // Use 16kB buffers to minimize copying
            this.polygonConnection = new PolygonConnection(apiKey, apiUrl, TimeSpan.FromSeconds(reconnectTimeout), loggerFactory, 16384);            
        }

        private ReadOnlySpan<byte> GetBuffer(ReadOnlySequence<byte> data)
        {
            // Optimized case, entire frame in the supplied sequence.
            if (data.IsSingleSegment)
                return data.FirstSpan;

            //  This will do a multi-segment copy of the sequence for us via dotnet framework.
            var localBuffer = new byte[(int)data.Length];
            data.CopyTo(localBuffer);
            return localBuffer;
        }

        private void Decode(ReadOnlySequence<byte> data, Action<Quote> onQuote, Action<Trade> onTrade,
            Action<TimeAggregate> onPerSecondAggregate, Action<TimeAggregate> onPerMinuteAggregate,
            Action<StatusMessage> onStatusMessage)
        {
            var list = new List<object>();
            var localBuffer = GetBuffer(data);

            deserializer.Deserialize(localBuffer, onQuote, onTrade, onPerSecondAggregate, onPerMinuteAggregate, onStatusMessage,
                    (ex, frame) => this.logger.LogError(ex, $"Error deserializing '{Encoding.UTF8.GetString(frame)}' ({ex.Message})."),
                    (eventName) => this.logger.LogWarning($"Unknown event type '{eventName}'."));
        }

        public void Start(IEnumerable<string> tickers, Action<Trade> onTrade, Action<Quote> onQuote,
            Action<TimeAggregate> onAggregate, Action<StatusMessage> onStatusMessage)
        {
            StartCore(tickers, onTrade, onQuote, onAggregate, onStatusMessage, null);
        }

        public void StartWithFrameTrace(IEnumerable<string> tickers, Action<Trade> onTrade, Action<Quote> onQuote,
            Action<TimeAggregate> onAggregate, Action<StatusMessage> onStatusMessage, Action<byte[]> withRawFrame)
        {
            if (withRawFrame == null) throw new ArgumentNullException(nameof(withRawFrame));
            StartCore(tickers, onTrade, onQuote, onAggregate, onStatusMessage, withRawFrame);
        }

        private void StartCore(IEnumerable<string> tickers, Action<Trade> onTrade, Action<Quote> onQuote,
            Action<TimeAggregate> onAggregate, Action<StatusMessage> onStatusMessage, Action<byte[]> withRawFrame)
        {
            if (this.isRunning)
                return;
            this.isRunning = true;

            this.polygonConnection.Start(tickers, (data, releaseBuffer) =>
            {
                Decode(data, onQuote, onTrade, onAggregate, onAggregate, onStatusMessage);
                // If caller has set the raw frame callback, then Copy the array to a new array and send.
                withRawFrame?.Invoke(data.ToArray());
                releaseBuffer.Dispose(); // Always dispose the rented buffer.
            });
        }

        public void Stop()
        {
            if(this.isRunning == false)
                return;

            this.polygonConnection.Stop();
            this.isRunning = false;
        }

        public async Task StopAsync()
        {
            if (this.isRunning == false)
                return;

            await this.polygonConnection.StopAsync();
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