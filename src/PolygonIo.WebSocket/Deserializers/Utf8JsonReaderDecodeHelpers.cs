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

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            trade.Symbol = reader.ExpectString(StreamFieldNames.Symbol);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            trade.TradeId = reader.ExpectString(StreamFieldNames.TradeId);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            trade.ExchangeId = reader.ExpectInt(StreamFieldNames.ExchangeId);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            trade.Price = reader.ExpectDecimal(StreamFieldNames.Price);

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var propertyName = reader.GetString();

                if (propertyName[0] == StreamFieldNames.TradeSize)
                    trade.TradeSize = reader.ExpectDecimal(StreamFieldNames.TradeSize);
                if (propertyName[0] == StreamFieldNames.TradeConditions)
                    trade.TradeConditions = reader.ExpectIntArray(StreamFieldNames.TradeConditions).Select(x => (TradeCondition)x).ToArray();
                else if (propertyName[0] == StreamFieldNames.Timestamp)
                    trade.Timestamp = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.Timestamp);
                else if (propertyName[0] == StreamFieldNames.Tape)
                    trade.Tape = reader.ExpectInt(StreamFieldNames.Tape);
            }
            //onError(GetUnexpectedPropertyMessage(trade.GetType().ToString(), reader.GetString()));
            //reader.Skip();

            return trade;
        }

        public static IStatus DecodeStatus(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var status = factory.NewStatus();

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            status.Result = reader.ExpectString(StreamFieldNames.Status);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            status.Message = reader.ExpectString(StreamFieldNames.Message);

            //onError(GetUnexpectedPropertyMessage(status.GetType().ToString(), reader.GetString()));
            //reader.Skip();

            return status;
        }

        public static ITimeAggregate DecodeAggregate(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var timeAggregate = factory.NewTimeAggregate();

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.Symbol = reader.ExpectString(StreamFieldNames.Symbol);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.Volume = reader.ExpectDecimal(StreamFieldNames.TickVolume);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.TodaysVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TodaysAccumulatedVolume);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.TodaysOpeningPrice = reader.ExpectDecimal(StreamFieldNames.TodaysOpeningPrice);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.TodaysVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TodaysVolumeWeightedAveragePrice);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.Open = reader.ExpectDecimal(StreamFieldNames.TickOpen);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.Close = reader.ExpectDecimal(StreamFieldNames.TickClose);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.High = reader.ExpectDecimal(StreamFieldNames.TickHigh);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.Low = reader.ExpectDecimal(StreamFieldNames.TickLow);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.TicksVolumeWeightedAveragePrice = reader.ExpectDecimal(StreamFieldNames.TicksVolumeWeightsAveragePrice);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.AverageTradeSize = reader.ExpectDecimal(StreamFieldNames.AverageTradeSize);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.StartDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.StartTimestamp);

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            timeAggregate.EndDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.EndTimestamp);

            //onError(GetUnexpectedPropertyMessage(timeAggregate.GetType().ToString(), reader.GetString()));
            //reader.Skip();                

            return timeAggregate;
        }

        public static IQuote DecodeQuote(this ref Utf8JsonReader reader, IEventFactory factory, Action<string> onError)
        {
            var quote = factory.NewQuote();
            quote.BidExchangeId = -1; // in case this is a quote without bid
            quote.AskExchangeId = -1; // in case this is a quote without ask

            reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject);
            _ = reader.GetString();
            quote.Symbol = reader.ExpectString(StreamFieldNames.Symbol);

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.BidExchangeId[0], StreamFieldNames.BidExchangeId[1] }))) 
                    quote.BidExchangeId = reader.ExpectInt(StreamFieldNames.BidExchangeId);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.BidPrice[0], StreamFieldNames.BidPrice[1] })))
                    quote.BidPrice = reader.ExpectDecimal(StreamFieldNames.BidPrice);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.BidSize[0], StreamFieldNames.BidSize[1] })))
                    quote.BidSize = reader.ExpectDecimal(StreamFieldNames.BidSize);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.AskExchangeId[0], StreamFieldNames.AskExchangeId[1] })))
                    quote.AskExchangeId = reader.ExpectInt(StreamFieldNames.AskExchangeId);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.AskPrice[0], StreamFieldNames.AskPrice[1] })))
                    quote.AskPrice = reader.ExpectDecimal(StreamFieldNames.AskPrice);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.AskSize[0], StreamFieldNames.AskSize[1] })))
                    quote.AskSize = reader.ExpectDecimal(StreamFieldNames.AskSize);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] {  StreamFieldNames.Tape })))
                    quote.Tape = reader.ExpectInt(StreamFieldNames.Tape);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.QuoteCondition })))
                    quote.QuoteCondition = (QuoteCondition)reader.ExpectInt(StreamFieldNames.QuoteCondition);
                else if (reader.ValueTextEquals(new ReadOnlySpan<char>(new[] { StreamFieldNames.Timestamp })))
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
