using PolygonIo.Model.Contracts;
using System.Linq;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public partial class Utf8JsonDeserializer : IPolygonDeserializer
    {
        private ITrade DecodeTrade(ref Utf8JsonReader reader)
        {
            var trade = this.eventDataTypeFactory.NewTrade();

            while (Expect(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                /*switch (property)
                {
                    default:
                        this.logger.LogError($"decoding {trade.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        trade.Symbol = ExpectString(ref reader, StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.ExchangeId:
                        trade.ExchangeId = ExpectInt(ref reader, StreamFieldNames.ExchangeId);
                        break;
                    case StreamFieldNames.TradeId:
                        trade.TradeId = ExpectString(ref reader, StreamFieldNames.TradeId);
                        break;
                    case StreamFieldNames.Tape:
                        trade.Tape = ExpectInt(ref reader, StreamFieldNames.Tape);
                        break;
                    case StreamFieldNames.Price:
                        trade.Price = ExpectDecimal(ref reader, StreamFieldNames.Price);
                        break;
                    case StreamFieldNames.TradeSize:
                        trade.TradeSize = ExpectDecimal(ref reader, StreamFieldNames.TradeSize);
                        break;
                    case StreamFieldNames.TradeConditions:
                        var array = ExpectIntArray(ref reader, StreamFieldNames.TradeConditions);
                        foreach (var condition in array)
                            trade.TradeConditions.Add((TradeCondition)condition);
                        break;
                    case StreamFieldNames.Timestamp:
                        trade.Timestamp = Timestamp.FromDateTimeOffset(ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.Timestamp));
                        break;
                }
                */

                switch (property)
                {
                    default:
                        //this.logger.LogError($"decoding {property.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        trade.Symbol = ExpectString(ref reader, StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.ExchangeId:
                        trade.ExchangeId = ExpectInt(ref reader, StreamFieldNames.ExchangeId);
                        break;
                    case StreamFieldNames.TradeId:
                        trade.TradeId = ExpectString(ref reader, StreamFieldNames.TradeId);
                        break;
                    case StreamFieldNames.Tape:
                        trade.Tape = ExpectInt(ref reader, StreamFieldNames.Tape);
                        break;
                    case StreamFieldNames.Price:
                        trade.Price = ExpectDecimal(ref reader, StreamFieldNames.Price);
                        break;
                    case StreamFieldNames.TradeSize:
                        trade.TradeSize = ExpectDecimal(ref reader, StreamFieldNames.TradeSize);
                        break;
                    case StreamFieldNames.TradeConditions:
                        trade.TradeConditions = ExpectIntArray(ref reader, StreamFieldNames.TradeConditions).Select( x => (TradeCondition)x).ToArray();
                        break;
                    case StreamFieldNames.Timestamp:
                        trade.Timestamp = ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.Timestamp);
                        break;
                }
            }

            return trade;
        }

        private IStatus DecodeStatus(ref Utf8JsonReader reader)
        {
            var status = this.eventDataTypeFactory.NewStatus();

            while (Expect(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        //this.logger.LogError($"decoding {property.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Status:
                        status.Result = ExpectString(ref reader, StreamFieldNames.Status);
                        break;
                    case StreamFieldNames.Message:
                        status.Message = ExpectString(ref reader, StreamFieldNames.Message);
                        break;
                }
            }

            return status;
        }

        private ITimeAggregate DecodeAggregate(ref Utf8JsonReader reader)
        {
            /*var aggregate = new TimeAggregate
            {
                Ohlc = new Ohlc()
            };

            while (Expect(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        // this.logger.LogError($"decoding {aggregate.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        aggregate.Symbol = ExpectString(ref reader, StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.TickOpen:
                        aggregate.Ohlc.Open = ExpectDecimal(ref reader, StreamFieldNames.TickOpen);
                        break;
                    case StreamFieldNames.TickHigh:
                        aggregate.Ohlc.High = ExpectDecimal(ref reader, StreamFieldNames.TickHigh);
                        break;
                    case StreamFieldNames.TickLow:
                        aggregate.Ohlc.Low = ExpectDecimal(ref reader, StreamFieldNames.TickLow);
                        break;
                    case StreamFieldNames.TickClose:
                        aggregate.Ohlc.Close = ExpectDecimal(ref reader, StreamFieldNames.TickClose);
                        break;
                    case StreamFieldNames.TickVolume:
                        aggregate.Ohlc.Volume = ExpectDecimal(ref reader, StreamFieldNames.TickVolume);
                        break;
                    case StreamFieldNames.StartTimestamp:
                        aggregate.StartDateTime = Timestamp.FromDateTimeOffset(ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.StartTimestamp));
                        break;
                    case StreamFieldNames.EndTimestamp:
                        aggregate.EndDateTime = Timestamp.FromDateTimeOffset(ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.EndTimestamp));
                        break;
                }
            }
            */

            var timeAggregate = this.eventDataTypeFactory.NewTimeAggregate();

            while (Expect(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        // this.logger.LogError($"decoding {aggregate.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        timeAggregate.Symbol = ExpectString(ref reader, StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.TickOpen:
                        timeAggregate.Open = ExpectDecimal(ref reader, StreamFieldNames.TickOpen);
                        break;
                    case StreamFieldNames.TickHigh:
                        timeAggregate.High = ExpectDecimal(ref reader, StreamFieldNames.TickHigh);
                        break;
                    case StreamFieldNames.TickLow:
                        timeAggregate.Low = ExpectDecimal(ref reader, StreamFieldNames.TickLow);
                        break;
                    case StreamFieldNames.TickClose:
                        timeAggregate.Close = ExpectDecimal(ref reader, StreamFieldNames.TickClose);
                        break;
                    case StreamFieldNames.TickVolume:
                        timeAggregate.Volume = ExpectDecimal(ref reader, StreamFieldNames.TickVolume);
                        break;
                    case StreamFieldNames.StartTimestamp:
                        timeAggregate.StartDateTime = ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.StartTimestamp);
                        break;
                    case StreamFieldNames.EndTimestamp:
                        timeAggregate.EndDateTime = ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.EndTimestamp);
                        break;
                }
            }

            return timeAggregate;
        }

        private IQuote DecodeQuote(ref Utf8JsonReader reader)
        {
            var quote = this.eventDataTypeFactory.NewQuote();            
            quote.BidExchangeId = -1; // in case this is a quote without bid
            quote.AskExchangeId = -1; // in case this is a quote without ask

            while (Expect(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject))
            {
                var property = reader.GetString();

                switch (property)
                {
                    default:
                        // this.logger.LogError($"decoding {quote.GetType().Name} found unexpected property {property}, skipping");
                        reader.Skip();
                        break;
                    case StreamFieldNames.Symbol:
                        quote.Symbol = ExpectString(ref reader, StreamFieldNames.Symbol);
                        break;
                    case StreamFieldNames.BidExchangeId:
                        quote.BidExchangeId = ExpectInt(ref reader, StreamFieldNames.BidExchangeId);
                        break;
                    case StreamFieldNames.BidPrice:
                        quote.BidPrice = ExpectDecimal(ref reader, StreamFieldNames.BidPrice);
                        break;
                    case StreamFieldNames.BidSize:
                        quote.BidSize = ExpectDecimal(ref reader, StreamFieldNames.BidSize);
                        break;
                    case StreamFieldNames.AskExchangeId:
                        quote.AskExchangeId = ExpectInt(ref reader, StreamFieldNames.AskExchangeId);
                        break;
                    case StreamFieldNames.AskPrice:
                        quote.AskPrice = ExpectDecimal(ref reader, StreamFieldNames.AskPrice);
                        break;
                    case StreamFieldNames.AskSize:
                        quote.AskSize = ExpectDecimal(ref reader, StreamFieldNames.AskSize);
                        break;
                    case StreamFieldNames.QuoteCondition:
                        quote.QuoteCondition = (QuoteCondition)ExpectInt(ref reader, StreamFieldNames.QuoteCondition);
                        break;
                    case StreamFieldNames.Timestamp:
                        quote.Timestamp = ExpectUnixTimeMilliseconds(ref reader, StreamFieldNames.Timestamp);
                        break;
                }
            }
            return quote;
        }
    }
}
