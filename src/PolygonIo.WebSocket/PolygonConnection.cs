using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Linq;
using System.Net.WebSockets;
using System.Buffers;

namespace PolygonIo.WebSocket
{
    public class PolygonConnection : IDisposable
    {
        const int ReceiveChunkSize = 2048;
        
        private readonly ILogger<PolygonConnection> logger;
        private readonly ITargetBlock<ReadOnlySequence<byte>> targetBlock;
        private readonly TimeSpan keepAliveInterval;        
        private readonly Uri uri;
        private readonly string apiKey;

        private Task loopTask;
        private CancellationTokenSource cts = null;

        public PolygonConnection(string apiKey, string apiUrl, ITargetBlock<ReadOnlySequence<byte>> targetBlock, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory)
        {
            this.uri = string.IsNullOrEmpty(apiUrl) ? throw new ArgumentException($"'{nameof(apiUrl)}' cannot be null or empty.", nameof(apiUrl)) : new Uri(apiUrl);
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.targetBlock = targetBlock ?? throw new ArgumentNullException(nameof(targetBlock));
            this.keepAliveInterval = keepAliveInterval;            
            this.logger = loggerFactory.CreateLogger<PolygonConnection>();
        }

        async Task Loop(IEnumerable<string> tickers, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Entering {nameof(PolygonConnection.Loop)}.");

            while (cancellationToken.IsCancellationRequested == false)
            {
                var webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = this.keepAliveInterval;
                var buffer = new ArraySegment<byte>(new byte[ReceiveChunkSize]);
                var resultProcessor = new WebSocketReceiveResultProcessor();

                try
                {
                    this.logger.LogInformation($"Connecting.");
                    await webSocket.ConnectAsync(uri, cancellationToken);
                    
                    this.logger.LogInformation($"Authenticating.");
                    await webSocket.SendAuthentication(this.apiKey, cancellationToken);
                    
                    this.logger.LogInformation($"Subscribing to {tickers.Count()} symbols.");
                    await webSocket.SubscribeToAggregatePerSecond(tickers, cancellationToken);
                    await webSocket.SubscribeToAggregatePerMinute(tickers, cancellationToken);
                    await webSocket.SubscribeToQuotes(tickers, cancellationToken);
                    await webSocket.SubscribeToTrades(tickers, cancellationToken);

                    while (webSocket.State == WebSocketState.Open && cancellationToken.IsCancellationRequested == false)
                    {
                        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                        var isEndOfMessage = resultProcessor.Receive(result, buffer, out var frame);

                        if (isEndOfMessage)
                        {
                            if (frame.IsEmpty == true)
                                break; // end of mesage with no data means socket closed - break so we can reconnect
                            else
                                await this.targetBlock.SendAsync(frame);
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    this.logger.LogError(ex.Message, ex);
                }
                catch (Exception ex)
                {
                    this.logger.LogCritical(ex.Message, ex);
                    return;
                }
                finally
                {
                    webSocket.Dispose();
                }
            }
        }

        public void Start(IEnumerable<string> tickers)
        {
            this.cts = new CancellationTokenSource();
            this.loopTask = Task.Factory.StartNew(async () => await Loop(tickers, this.cts.Token));
        }

        public void Stop()
        {
            cts?.Cancel();
            loopTask?.Wait();
            cts?.Dispose();
            loopTask?.Dispose();

            this.cts = null;
            this.loopTask = null;
        }

        private async Task StopAsync()
        {
            cts?.Cancel();

            if (loopTask != null)
                await loopTask;

            cts?.Dispose();
            loopTask?.Dispose();

            this.cts = null;
            this.loopTask = null;
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
                Stop();
            }
        }
    }
}