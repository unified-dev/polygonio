using PolygonIo.WebSocket.Contracts;
using System.Buffers;
using System.Linq;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public static class Utf8JsonReaderDecodeHelpers
    {
        public static ITrade DecodeTrade(this ref Utf8JsonReader reader, IEventFactory factory)
        {
            var trade = factory.NewTrade();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        //this.logger.LogError($"decoding {property.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        trade.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.ExchangeId:
                        trade.ExchangeId = reader.ExpectInt(StreamFieldNames.ExchangeId);
                        break;
                    case StreamFieldNames.TradeId:
                        trade.TradeId = reader.ExpectString(StreamFieldNames.TradeId);
                        break;
                    case StreamFieldNames.Tape:
                        trade.Tape = reader.ExpectInt(StreamFieldNames.Tape);
                        break;
                    case StreamFieldNames.Price:
                        trade.Price = reader.ExpectDecimal(StreamFieldNames.Price);
                        break;
                    case StreamFieldNames.TradeSize:
                        trade.TradeSize = reader.ExpectDecimal(StreamFieldNames.TradeSize);
                        break;
                    case StreamFieldNames.TradeConditions:
                        trade.TradeConditions = reader.ExpectIntArray(StreamFieldNames.TradeConditions).Select(x => (TradeCondition)x).ToArray();
                        break;
                    case StreamFieldNames.Timestamp:
                        trade.Timestamp = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.Timestamp);
                        break;
                }
            }

            return trade;
        }

        public static IStatus DecodeStatus(this ref Utf8JsonReader reader, IEventFactory factory)
        {
            var status = factory.NewStatus();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        //this.logger.LogError($"decoding {property.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Status:
                        status.Result = reader.ExpectString(StreamFieldNames.Status);
                        break;
                    case StreamFieldNames.Message:
                        status.Message = reader.ExpectString(StreamFieldNames.Message);
                        break;
                }
            }

            return status;
        }

        public static ITimeAggregate DecodeAggregate(this ref Utf8JsonReader reader, IEventFactory factory)
        {
            var timeAggregate = factory.NewTimeAggregate();

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        // this.logger.LogError($"decoding {aggregate.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        timeAggregate.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.TickOpen:
                        timeAggregate.Open = reader.ExpectDecimal(StreamFieldNames.TickOpen);
                        break;
                    case StreamFieldNames.TickHigh:
                        timeAggregate.High = reader.ExpectDecimal(StreamFieldNames.TickHigh);
                        break;
                    case StreamFieldNames.TickLow:
                        timeAggregate.Low = reader.ExpectDecimal(StreamFieldNames.TickLow);
                        break;
                    case StreamFieldNames.TickClose:
                        timeAggregate.Close = reader.ExpectDecimal(StreamFieldNames.TickClose);
                        break;
                    case StreamFieldNames.TickVolume:
                        timeAggregate.Volume = reader.ExpectDecimal(StreamFieldNames.TickVolume);
                        break;
                    case StreamFieldNames.StartTimestamp:
                        timeAggregate.StartDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.StartTimestamp);
                        break;
                    case StreamFieldNames.EndTimestamp:
                        timeAggregate.EndDateTime = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.EndTimestamp);
                        break;
                }
            }

            return timeAggregate;
        }

        public static IQuote DecodeQuote(this ref Utf8JsonReader reader, IEventFactory factory)
        {
            var quote = factory.NewQuote();
            quote.BidExchangeId = -1; // in case this is a quote without bid
            quote.AskExchangeId = -1; // in case this is a quote without ask

            while (reader.Expect(JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        // this.logger.LogError($"decoding {quote.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        quote.Symbol = reader.ExpectString(StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.BidExchangeId:
                        quote.BidExchangeId = reader.ExpectInt(StreamFieldNames.BidExchangeId);
                        break;
                    case StreamFieldNames.BidPrice:
                        quote.BidPrice = reader.ExpectDecimal(StreamFieldNames.BidPrice);
                        break;
                    case StreamFieldNames.BidSize:
                        quote.BidSize = reader.ExpectDecimal(StreamFieldNames.BidSize);
                        break;
                    case StreamFieldNames.AskExchangeId:
                        quote.AskExchangeId = reader.ExpectInt(StreamFieldNames.AskExchangeId);
                        break;
                    case StreamFieldNames.AskPrice:
                        quote.AskPrice = reader.ExpectDecimal(StreamFieldNames.AskPrice);
                        break;
                    case StreamFieldNames.AskSize:
                        quote.AskSize = reader.ExpectDecimal(StreamFieldNames.AskSize);
                        break;
                    case StreamFieldNames.QuoteCondition:
                        quote.QuoteCondition = (QuoteCondition)reader.ExpectInt(StreamFieldNames.QuoteCondition);
                        break;
                    case StreamFieldNames.Timestamp:
                        quote.Timestamp = reader.ExpectUnixTimeMilliseconds(StreamFieldNames.Timestamp);
                        break;
                }
            }
            return quote;
        }
    }
}
