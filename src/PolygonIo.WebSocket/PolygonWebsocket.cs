using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Websocket.Client;
using System.Net.WebSockets;
using Websocket.Client.Models;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket
{
    public class PolygonWebsocket
    {
        protected enum SubscriptionType { AggregatePerSecond, AggregatePerMinute, Trade, Quote };

        readonly WebsocketClient client;
        private readonly ILogger<PolygonWebsocket> logger;
        Task subscriptionTask = null;
        CancellationTokenSource subscriptionTaskCts = null;
        CancellationTokenSource linkedCts = null;
        private readonly string apiKey;
        private readonly IEventFactory eventFactory;
        private readonly ILoggerFactory loggerFactory;
        private readonly Uri apiUri;
        const int SubsciptionChunkSize = 1000;
        private int isRunning;
        PolygonDataFrameDecoder polygonDecoder;

        public PolygonWebsocket(string apiKey, string apiUrl, int reconnectTimeout, ILoggerFactory loggerFactory, IEventFactory eventFactory = null)
        {
            this.logger = loggerFactory.CreateLogger<PolygonWebsocket>();
            this.apiKey = apiKey;            
            this.loggerFactory = loggerFactory;
            this.apiUri = new Uri(apiUrl);

            // setup factory for which types from decoder are created with
            if (eventFactory != null)
                this.eventFactory = eventFactory;
            else
                this.eventFactory = new PolygonTypesEventFactory();

            this.client = new WebsocketClient(this.apiUri)
            {
                ReconnectTimeout = TimeSpan.FromSeconds(reconnectTimeout)
            };
        }

        protected void Authenticate()
        {
            this.client.Send("{\"action\":\"auth\",\"params\":\"" + this.apiKey + "\"}");
        }

        void SubscriptionTask(IEnumerable<string> tickers, CancellationToken cancellationToken)
        {
            Authenticate();
            Subscribe(tickers, SubscriptionType.AggregatePerSecond, cancellationToken);
            Subscribe(tickers, SubscriptionType.AggregatePerMinute, cancellationToken);
            Subscribe(tickers, SubscriptionType.Quote, cancellationToken);
            Subscribe(tickers, SubscriptionType.Trade, cancellationToken);
        }

        protected void Subscribe(IEnumerable<string> symbols, SubscriptionType subscriptionType, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Subscribing to {subscriptionType} for {symbols.Count()} symbols.");

            var chunks = symbols
                            .Select((s, i) => new { Value = s, Index = i })
                            .GroupBy(x => x.Index / SubsciptionChunkSize)
                            .Select(grp => grp.Select(x => x.Value));

            this.logger.LogDebug($"Subscribing to {symbols.Count()} symbols for {subscriptionType.ToString()} updates (in {chunks.Count()} batches).");

            foreach (var chunk in chunks)
            {
                if (cancellationToken.IsCancellationRequested == true)
                    break;

                var subscriptionString = GetSubscriptionString(chunk.ToArray(), subscriptionType);

                this.client.Send("{\"action\":\"subscribe\",\"params\":\"" + subscriptionString + "\"}");
            }
        }

        string GetSubscriptionString(string[] symbols, SubscriptionType subscriptionType)
        {
            var sb = new StringBuilder(GetParam(symbols[0], subscriptionType));

            if (symbols.Length == 1)
                return sb.ToString();

            for (int i = 1; i < symbols.Length; i++)
            {
                sb.Append(",");
                sb.Append(GetParam(symbols[i], subscriptionType));
            }

            return sb.ToString();
        }

        string GetParam(string symbol, SubscriptionType subscriptionType)
        {
            return subscriptionType switch
            {
                SubscriptionType.AggregatePerSecond => StreamFieldNames.AggregatePerSecond + "." + symbol,
                SubscriptionType.AggregatePerMinute => StreamFieldNames.AggregatePerMinute + "." + symbol,
                SubscriptionType.Quote => StreamFieldNames.Quote + "." + symbol,
                SubscriptionType.Trade => StreamFieldNames.Trade + "." + symbol,
                _ => throw new ArgumentException(nameof(subscriptionType)),
            };
        }

        void OnReconnection(ReconnectionInfo info, IEnumerable<string> tickers, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            this.logger.LogInformation($"Reconnection happened, type: {info}.");

            // cancel any previous running subscription task
            if (subscriptionTask != null && subscriptionTask.IsCompleted == false)
            {
                this.subscriptionTaskCts.Cancel();
                this.subscriptionTask.Wait(); // wait for previous thread to shutdown
                this.subscriptionTaskCts.Dispose(); // we must dispose to prevent resource leak
                this.linkedCts.Dispose();
            }

            this.subscriptionTaskCts = new CancellationTokenSource();
            this.linkedCts = CancellationTokenSource.CreateLinkedTokenSource(new[] { subscriptionTaskCts.Token, cancellationToken });
            this.subscriptionTask = Task.Factory.StartNew(() => SubscriptionTask(tickers, linkedCts.Token), linkedCts.Token);
        }

        async Task OnMessageReceived(ResponseMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // TODO: give memory direct, for now just read from string
            // var sequence = new ReadOnlySequence<byte>(message.Binary);
            // this is NOT ideal as involves a double copy but avoid buffer management for now

            var bytes = Encoding.UTF8.GetBytes(message.Text);
            var frame = new ReadOnlySequence<byte>(bytes);
            await this.polygonDecoder.SendAsync(frame);          
        }

        public async Task StartAsync(IEnumerable<string> tickers, Func<PolygonDataFrame,Task> receiveMessagesFunc, CancellationToken stoppingToken)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
                throw new InvalidOperationException();

            // setup decoder
            this.polygonDecoder = new PolygonDataFrameDecoder(
                                        new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), this.eventFactory),
                                        this.loggerFactory.CreateLogger<PolygonDataFrameDecoder>(),
                                        receiveMessagesFunc);

            await StartAsyncCore(tickers, stoppingToken);
        }

        public async Task StartAsync(IEnumerable<string> tickers, ITargetBlock<PolygonDataFrame> targetBlock, CancellationToken stoppingToken)
        {
            if (Interlocked.CompareExchange(ref this.isRunning, 1, 0) == 1)
                throw new InvalidOperationException();

            // setup decoder
            this.polygonDecoder = new PolygonDataFrameDecoder(
                                        new Utf8JsonDeserializer(loggerFactory.CreateLogger<Utf8JsonDeserializer>(), this.eventFactory),
                                        this.loggerFactory.CreateLogger<PolygonDataFrameDecoder>(),
                                        targetBlock);

            await StartAsyncCore(tickers, stoppingToken);
        }

        async Task StartAsyncCore(IEnumerable<string> tickers, CancellationToken stoppingToken)
        {
            // setup client
            this.client.ReconnectionHappened.Subscribe(info => OnReconnection(info, tickers, stoppingToken), stoppingToken);
            this.client.MessageReceived.Subscribe(async (message) => await OnMessageReceived(message, stoppingToken), stoppingToken);
            await this.client.Start();
        }

        public async Task StopAsync()
        {
            if (this.isRunning == 0)
                throw new InvalidOperationException();

            // stopping the web client will stop the processing of new frames
            await this.client.Stop(WebSocketCloseStatus.NormalClosure, "");
            await this.polygonDecoder.Stop();            
        }
    }
}