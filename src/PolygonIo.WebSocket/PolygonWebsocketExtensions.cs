using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PolygonIo.WebSocket
{
    // recommended to use default buffered approach given volume of messages, however helper below should you wish to call each method for each message
    public static class PolygonWebsocketExtensions
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