using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Linq;
using PolygonIo.WebSocket.Socket;

namespace PolygonIo.WebSocket
{
    public class PolygonConnection
    {
        private readonly ManagedWebSocket managedWebSocket;
        private readonly ILogger<PolygonConnection> logger;        
        private readonly string apiKey;
        private readonly ITargetBlock<byte[]> targetBlock;
        private Task subscriptionTask = null;
        private CancellationTokenSource subscriptionCancelationTokenSource = null;
        private readonly CancellationTokenSource mainCancellationTokenSource = null;
        private CancellationTokenSource linkedCts = null;
        private IEnumerable<string> tickers;

        public PolygonConnection(string apiKey, string apiUrl, int reconnectTimeout, ITargetBlock<byte[]> targetBlock, ILoggerFactory loggerFactory)
        {
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.targetBlock = targetBlock ?? throw new ArgumentNullException(nameof(targetBlock));

            if (string.IsNullOrEmpty(apiUrl))
                throw new ArgumentException($"'{nameof(apiUrl)}' cannot be null or empty.", nameof(apiUrl));

            this.logger = loggerFactory.CreateLogger<PolygonConnection>();                        
            this.mainCancellationTokenSource = new CancellationTokenSource();
            this.managedWebSocket = new ManagedWebSocket(apiUrl, this.targetBlock);

            this.managedWebSocket.OnConnectedAsync = new Func<Task>( async () =>
            {
                await WaitForSubscriptionTaskAsync();

                // start a new subscription
                this.subscriptionCancelationTokenSource = new CancellationTokenSource();
                this.linkedCts = CancellationTokenSource.CreateLinkedTokenSource(new[] { subscriptionCancelationTokenSource.Token, this.mainCancellationTokenSource.Token });
                this.subscriptionTask = Task.Factory.StartNew(() => SubscriptionTask(tickers, linkedCts.Token), linkedCts.Token);
            });      
        }

        async Task WaitForSubscriptionTaskAsync()
        {
            // cancel any previous running subscription task
            if (subscriptionTask != null && subscriptionTask.IsCompleted == false)
            {
                // wait for previous thread to shutdown
                this.subscriptionCancelationTokenSource.Cancel();
                await this.subscriptionTask;

                // dispose to prevent resource leak
                this.subscriptionCancelationTokenSource.Dispose();
                this.linkedCts.Dispose();
            }
        }

        async Task SubscriptionTask(IEnumerable<string> tickers, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Subscribing to {tickers.Count()} symbols.");
            await this.managedWebSocket.Authenticate(this.apiKey, cancellationToken);
            await this.managedWebSocket.SubscribeToAggregatePerSecond(tickers, cancellationToken);
            await this.managedWebSocket.SubscribeToAggregatePerMinute(tickers, cancellationToken);
            await this.managedWebSocket.SubscribeToQuotes(tickers, cancellationToken);
            await this.managedWebSocket.SubscribeToTrades(tickers, cancellationToken);            
        }

        public async Task StopAsync()
        {
            this.targetBlock.Complete();
            this.mainCancellationTokenSource.Cancel();
            await WaitForSubscriptionTaskAsync();
            // stopping the web client will stop the processing of new frames


            // FIIIIIIIIIIIIIIIIIIIIIIIIIIXXX

           // await this.client.Stop(WebSocketCloseStatus.NormalClosure, "");            
        }
        public void Start(IEnumerable<string> tickers)
        {
            this.tickers = tickers;
            this.managedWebSocket.Start();
        }
    }
}