using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Buffers;
using System.Runtime.InteropServices;
using PolygonIo.WebSocket.Socket;

namespace PolygonIo.WebSocket
{
    public class PolygonConnection : IDisposable
    {
        private readonly int receiveChunkSize;
        private readonly ILogger<PolygonConnection> logger;
        private readonly TimeSpan keepAliveInterval;
        private readonly Uri uri;
        private readonly string apiKey;

        private Task loopTask;
        private CancellationTokenSource cts = null;

        public PolygonConnection(string apiKey, string apiUrl, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory, int receiveChunkSize = 4096)
        {
            this.uri = string.IsNullOrEmpty(apiUrl) ? throw new ArgumentException($"'{nameof(apiUrl)}' cannot be null or empty.", nameof(apiUrl)) : new Uri(apiUrl);
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.receiveChunkSize = receiveChunkSize;
            this.keepAliveInterval = keepAliveInterval;
            this.logger = loggerFactory.CreateLogger<PolygonConnection>();
        }

        private ArraySegment<byte> RentBuffer()
        {
            return ArrayPool<byte>.Shared.Rent(this.receiveChunkSize); // Rent a buffer.
        }

        private void ReturnRentedBuffer(ReadOnlySequence<byte> data)
        {
            foreach (var chunk in data)
            {
                if (MemoryMarshal.TryGetArray(chunk, out var segment))
                    ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }

        private async Task Loop(IEnumerable<string> tickers, Func<ReadOnlySequence<byte>, Action, Task> dispatch, CancellationToken cancellationToken)
        {           
            this.logger.LogInformation($"Entering {nameof(PolygonConnection.Loop)}.");

            while (cancellationToken.IsCancellationRequested == false)
            {
                var webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = this.keepAliveInterval;
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
                        var buffer = RentBuffer();
                        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                        if (resultProcessor.Receive(result, buffer, out var frame))
                        {
                            if (frame.IsEmpty == true)
                                break; // End of message with no data means socket closed - break so we can reconnect.
                            
                            // Send the frame, and delegate consumer should call to release the buffer once done.
                            await dispatch(frame, () => ReturnRentedBuffer(frame));
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    this.logger.LogError(ex, ex.Message);
                }
                catch (Exception ex)
                {
                    this.logger.LogCritical(ex, ex.Message);
                    return;
                }
                finally
                {
                    webSocket.Dispose();
                    resultProcessor.Dispose();
                }
            }
        }

        public void Start(IEnumerable<string> tickers, Func<ReadOnlySequence<byte>, Action, Task> dispatch)
        {
            if (dispatch is null)
                throw new ArgumentNullException(nameof(dispatch));

            this.cts = new CancellationTokenSource();
            this.loopTask = Task.Factory.StartNew(async () => await Loop(tickers, dispatch, this.cts.Token));
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

        public async Task StopAsync()
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
            Dispose(true); // Dispose of unmanaged resources.
            GC.SuppressFinalize(this); // Suppress finalization.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Stop();
        }
    }
}