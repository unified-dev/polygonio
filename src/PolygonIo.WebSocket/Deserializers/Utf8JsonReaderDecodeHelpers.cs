using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Factory;
using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public static class Utf8JsonReaderDecodeHelpers
    {
        public static ITrade DecodeTrade(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var trade = factory.NewTrade();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                if (reader.ValueTextEquals(StreamFieldNames.Symbol))
                    trade.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                else if (reader.ValueTextEquals(StreamFieldNames.ExchangeId))
                    trade.ExchangeId = reader.ExpectInt(StreamFieldNames.ExchangeId);
                else if (reader.ValueTextEquals(StreamFieldNames.TradeId))
                    trade.TradeId = reader.ExpectString(StreamFieldNames.TradeId);
                else if (reader.ValueTextEquals(StreamFieldNames.Tape))
                    trade.Tape = reader.ExpectInt(StreamFieldNames.Tape);
                else if (reader.ValueTextEquals(StreamFieldNames.Price))
                    trade.Price = reader.ExpectDecimal(StreamFieldNames.Price);
                else if (reader.ValueTextEquals(StreamFieldNames.TradeSize))
                    trade.TradeSize = reader.ExpectDecimal(StreamFieldNames.TradeSize);
                else if (reader.ValueTextEquals(StreamFieldNames.TradeConditions))
                    trade.TradeConditions = reader.ExpectIntArray(StreamFieldNames.TradeConditions).Select(x => (TradeCondition)x).ToArray();
                else if (reader.ValueTextEquals(StreamFieldNames.Timestamp))
                    trade.Timestamp = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.Timestamp);
                else
                {
                    onError(GetUnexpectedPropertyMessage(trade.GetType().ToString(), reader.GetString()));
                    reader.Skip();
                }
            }

            return trade;
        }

        public static IStatus DecodeStatus(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var status = factory.NewStatus();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                if (reader.ValueTextEquals(StreamFieldNames.Status))
                    status.Result = reader.ExpectString(StreamFieldNames.Status);
                else if(reader.ValueTextEquals(StreamFieldNames.Message))
                    status.Message = reader.ExpectString(StreamFieldNames.Message);
                else
                {
                    onError(GetUnexpectedPropertyMessage(status.GetType().ToString(), reader.GetString()));
                    reader.Skip();
                }
            }

            return status;
        }

        public static ITimeAggregate DecodeAggregate(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var timeAggregate = factory.NewTimeAggregate();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                if (reader.ValueTextEquals(StreamFieldNames.Symbol))
                    timeAggregate.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                else if (reader.ValueTextEquals(StreamFieldNames.TickOpen))
                    timeAggregate.Open = reader.ExpectDecimal(StreamFieldNames.TickOpen);
                else if (reader.ValueTextEquals(StreamFieldNames.TickHigh))
                    timeAggregate.High = reader.ExpectDecimal(StreamFieldNames.TickHigh);
                else if (reader.ValueTextEquals(StreamFieldNames.TickLow))
                    timeAggregate.Low = reader.ExpectDecimal(StreamFieldNames.TickLow);
                else if (reader.ValueTextEquals(StreamFieldNames.TickClose))
                    timeAggregate.Close = reader.ExpectDecimal(StreamFieldNames.TickClose);
                else if (reader.ValueTextEquals(StreamFieldNames.TickVolume))
                    timeAggregate.Volume = reader.ExpectDecimal(StreamFieldNames.TickVolume);
                else if (reader.ValueTextEquals(StreamFieldNames.StartTimestamp))
                    timeAggregate.StartDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.StartTimestamp);
                else if (reader.ValueTextEquals(StreamFieldNames.EndTimestamp))
                    timeAggregate.EndDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.EndTimestamp);
                else if (reader.ValueTextEquals(StreamFieldNames.AverageTradeSize))
                    timeAggregate.AverageTradeSize = reader.ExpectDecimal(StreamFieldNames.AverageTradeSize);
                else if (reader.ValueTextEquals(StreamFieldNames.TicksVolumeWeightsAveragePrice))
                    timeAggregate.TicksVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TicksVolumeWeightsAveragePrice);
                else if (reader.ValueTextEquals(StreamFieldNames.TodaysVolumeWeightedAveragePrice))
                    timeAggregate.TodaysVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TodaysVolumeWeightedAveragePrice);
                else if (reader.ValueTextEquals(StreamFieldNames.TodaysAccumulatedVolume))
                    timeAggregate.TodaysVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TodaysAccumulatedVolume);
                else if (reader.ValueTextEquals(StreamFieldNames.TodaysOpeningPrice))
                    timeAggregate.TodaysOpeningPrice = reader.ExpectDecimal(StreamFieldNames.TodaysOpeningPrice);
                else
                {
                    onError(GetUnexpectedPropertyMessage(timeAggregate.GetType().ToString(), reader.GetString()));
                    reader.Skip();
                }
            }

            return timeAggregate;
        }

        public static IQuote DecodeQuote(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var quote = factory.NewQuote();
            quote.BidExchangeId = -1; // in case this is a quote without bid
            quote.AskExchangeId = -1; // in case this is a quote without ask

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                if (reader.ValueTextEquals(StreamFieldNames.Symbol))
                    quote.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                else if (reader.ValueTextEquals(StreamFieldNames.BidExchangeId))
                    quote.BidExchangeId = reader.ExpectInt(StreamFieldNames.BidExchangeId);
                else if (reader.ValueTextEquals(StreamFieldNames.BidPrice))
                    quote.BidPrice = reader.ExpectDecimal(StreamFieldNames.BidPrice);
                else if (reader.ValueTextEquals(StreamFieldNames.BidSize))
                    quote.BidSize = reader.ExpectDecimal(StreamFieldNames.BidSize);
                else if (reader.ValueTextEquals(StreamFieldNames.AskExchangeId))
                    quote.AskExchangeId = reader.ExpectInt(StreamFieldNames.AskExchangeId);
                else if (reader.ValueTextEquals(StreamFieldNames.AskPrice))
                    quote.AskPrice = reader.ExpectDecimal(StreamFieldNames.AskPrice);
                else if (reader.ValueTextEquals(StreamFieldNames.AskSize))
                    quote.AskSize = reader.ExpectDecimal(StreamFieldNames.AskSize);
                else if (reader.ValueTextEquals(StreamFieldNames.Tape))
                    quote.Tape = reader.ExpectInt(StreamFieldNames.Tape);
                else if (reader.ValueTextEquals(StreamFieldNames.QuoteCondition))
                    quote.QuoteCondition = (QuoteCondition)reader.ExpectInt(StreamFieldNames.QuoteCondition);
                else if (reader.ValueTextEquals(StreamFieldNames.Timestamp))
                    quote.Timestamp = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.Timestamp);
                else
                {
                    onError(GetUnexpectedPropertyMessage(quote.GetType().ToString(), reader.GetString()));
                    reader.Skip();
                }
            }

            return quote;
        }

        private static string GetUnexpectedPropertyMessage(string typeName, string propertyName)
        {
            return $"Decoding {typeName} found unexpected property {propertyName}, skipping.";
        }
    }
}
