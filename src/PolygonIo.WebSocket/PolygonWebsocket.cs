using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Websocket.Client;
using System.Net.WebSockets;
using Websocket.Client.Models;
using PolygonIo.WebSocket.Deserializers;

namespace PolygonIo.WebSocket
{

    public class PolygonWebsocket
    {
        protected enum SubscriptionType { AggregatePerSecond, AggregatePerMinute, Trade, Quote };

        readonly WebsocketClient client;
        private readonly ILogger<PolygonWebsocket> logger;
        private readonly DeserializerQueue deserializerQueue;
        Task subscriptionTask = null;
        CancellationTokenSource subscriptionTaskCts = null;
        CancellationTokenSource linkedCts = null;
        private readonly string apiKey;
        private readonly Uri apiUri;
        const int SubsciptionChunkSize = 1000;

        public PolygonWebsocket(IConfiguration configuration, DeserializerQueue deserializerQueue, ILogger<PolygonWebsocket> logger)
        {
            this.logger = logger;
            this.deserializerQueue = deserializerQueue;

            // config
            var section = configuration.GetSection("Polygon");
            this.apiKey = section.GetValue<string>("ApiKey");
            this.apiUri = new Uri(section.GetValue<string>("StocksStreamUri"));

            this.client = new WebsocketClient(this.apiUri)
            {
                ReconnectTimeout = TimeSpan.FromSeconds(section.GetValue<int>("ReconnectTimeout", 60))
            };

            /*System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += (sender, onTimedEvent) =>
            {
                for(var i=0;i<10000;i++)
                    // this.client.StreamFakeMessage(ResponseMessage.TextMessage(@"[{""ev"":""T"",""sym"":""JDST"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3}]"));
                    this.client.StreamFakeMessage(ResponseMessage.TextMessage(@"[{""ev"":""T"",""sym"":""JDST"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""JNUG"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""MSFT"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""TVIX"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""SHOP"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""TVIX"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{""ev"":""T"",""sym"":""DAL"",""i"":""31154"",""x"":11,""p"":215.37,""s"":500,""c"":[14,12,41],""t"":1588630929640,""z"":3},{ ""ev"": ""AM"", ""sym"": ""MSFT"", ""v"": 10204, ""av"": 200304, ""op"": 114.04, ""vw"": 114.4040, ""o"": 114.11, ""c"": 114.14, ""h"": 114.19, ""l"": 114.09, ""a"": 114.1314, ""s"": 1536036818784, ""e"": 1536036818784 }]"));
            };
            aTimer.Interval = 1000;
            aTimer.Enabled = true;
            */
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
            logger.LogInformation($"subscribing to {subscriptionType} for {symbols.Count()} symbols");

            var chunks = symbols
                            .Select((s, i) => new { Value = s, Index = i })
                            .GroupBy(x => x.Index / SubsciptionChunkSize)
                            .Select(grp => grp.Select(x => x.Value));

            this.logger.LogInformation($"subscribing to {symbols.Count()} symbols for {subscriptionType.ToString()} updates (in {chunks.Count()} batches)");

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

            this.logger.LogInformation($"Reconnection happened, type: {info}");

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

            try
            {
                // TODO: give memory direct, for now just read from string
                // var sequence = new ReadOnlySequence<byte>(message.Binary);
                // this is NOT ideal as involves a double copy but avoid buffer management for now

                var bytes = Encoding.UTF8.GetBytes(message.Text);
                await this.deserializerQueue.AddFrame(new ReadOnlySequence<byte>(bytes));                
            }
            catch (JsonException e)
            {
                this.logger.LogError(e.ToString() + Environment.NewLine + e.InnerException?.ToString());
            }
        }

        public async Task StartAsync(IEnumerable<string> tickers, CancellationToken stoppingToken, Func<DeserializedData,Task> receiveMessages, bool logToFile = false)
        {
            this.client.ReconnectionHappened.Subscribe(info => OnReconnection(info, tickers, stoppingToken), stoppingToken);
            this.client.MessageReceived.Subscribe(async (message) => await OnMessageReceived(message, stoppingToken), stoppingToken);

            this.deserializerQueue.Start(receiveMessages, logToFile);
            await this.client.Start();           
        }

        public async Task StopAsync()
        {
            await this.deserializerQueue.StopAsync();
            await this.client.Stop(WebSocketCloseStatus.NormalClosure, "");
        }
    }

    // recommended to use default buffered approach given volume of messages, however helper below should you wish to call each method for each message
    public static class PolygonWebsocketHelpers
    {
        public static Task StartAsyncUsingCallbacks(this PolygonWebsocket socket, IEnumerable<string> tickers, CancellationToken stoppingToken, IReceivePolygonMessages callbacks)
        {
            return socket.StartAsync(tickers, stoppingToken, async (data) => {
                foreach (var quote in data.Quotes)
                    await callbacks.OnQuoteReceivedAsync(quote);

                foreach (var trade in data.Trades)
                    await callbacks.OnTradeReceivedAsync(trade);

                foreach (var aggregatePerSecond in data.PerSecondAggregates)
                    await callbacks.OnTimeAggregatePerSecondReceivedAsync(aggregatePerSecond);

                foreach (var aggregatePerMinute in data.PerMinuteAggregates)
                    await callbacks.OnTimeAggregatePerMinuteReceivedAsync(aggregatePerMinute);

                foreach (var status in data.Status)
                    await callbacks.OnStatusReceivedAsync(status);
            });
        }
    }
}