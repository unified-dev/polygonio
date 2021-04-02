using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket
{
    internal static class PolygonSubscriptionExtensions
    {
        enum SubscriptionType { AggregatePerSecond, AggregatePerMinute, Trade, Quote };
        const int SubsciptionChunkSize = 1000;

        static public async Task Authenticate(this ManagedWebSocket websocket, string apiKey, CancellationToken cancellationToken)
        {
            await websocket.SendAsync("{\"action\":\"auth\",\"params\":\"" + apiKey + "\"}");
        }

        static public async Task SubscribeToAggregatePerSecond(this ManagedWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.AggregatePerSecond, cancellationToken);
        }

        static public async Task SubscribeToAggregatePerMinute(this ManagedWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.AggregatePerMinute, cancellationToken);
        }

        static public async Task SubscribeToTrades(this ManagedWebSocket websocket, IEnumerable<string> symbols,  CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.Trade, cancellationToken);
        }

        static public async Task SubscribeToQuotes(this ManagedWebSocket websocket, IEnumerable<string> symbols, CancellationToken cancellationToken)
        {
            await Subscribe(websocket, symbols, SubscriptionType.Quote, cancellationToken);
        }

        static private async Task Subscribe(this ManagedWebSocket websocket, IEnumerable<string> symbols, SubscriptionType subscriptionType, CancellationToken cancellationToken)
        {          
            var chunks = symbols
                            .Select((s, i) => new { Value = s, Index = i })
                            .GroupBy(x => x.Index / SubsciptionChunkSize)
                            .Select(grp => grp.Select(x => x.Value));

            foreach (var chunk in chunks)
            {
                if (cancellationToken.IsCancellationRequested == true)
                    break;

                var subscriptionString = GetSubscriptionString(chunk.ToArray(), subscriptionType);

                await websocket.SendAsync("{\"action\":\"subscribe\",\"params\":\"" + subscriptionString + "\"}");
            }
        }

        static string GetSubscriptionString(string[] symbols, SubscriptionType subscriptionType)
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

        static string GetParam(string symbol, SubscriptionType subscriptionType)
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
    }
}