using Microsoft.Extensions.Logging;
using PolygonIo.Model.Contracts;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public partial class Utf8JsonDeserializer : IPolygonDeserializer
    {
        private readonly ILogger<Utf8JsonDeserializer> logger;
        private readonly IEventDataTypeFactory eventDataTypeFactory;

        public Utf8JsonDeserializer(ILogger<Utf8JsonDeserializer> logger, IEventDataTypeFactory eventDataTypeFactory)
        {
            this.eventDataTypeFactory = eventDataTypeFactory;
            this.logger = logger;
        }

        public DeserializedData Deserialize(ReadOnlySequence<byte> jsonData)
        {
            var perSecondAggregates = new List<ITimeAggregate>();
            var perMinuteAggregates = new List<ITimeAggregate>();
            var quotes = new List<IQuote>();
            var trades = new List<ITrade>();
            var status = new List<IStatus>();

            var reader = new Utf8JsonReader(jsonData);

            if (SkipUpTo(ref reader, JsonTokenType.StartArray) == false)
                throw new JsonException($"skipped all data and no array found");
           
            while (SkipTillExpected(ref reader, JsonTokenType.StartObject, JsonTokenType.EndArray))
            {
                try
                {
                    ExpectNamedProperty(ref reader, StreamFieldNames.Event);
                    var ev = ExpectString(ref reader, StreamFieldNames.Event);

                    switch (ev)
                    {
                        case StreamFieldNames.Quote:
                            quotes.Add(DecodeQuote(ref reader));
                            break;
                        case StreamFieldNames.Trade:
                            trades.Add(DecodeTrade(ref reader));
                            break;
                        case StreamFieldNames.AggregatePerSecond:
                            perSecondAggregates.Add(DecodeAggregate(ref reader));
                            break;
                        case StreamFieldNames.AggregatePerMinute:
                            perMinuteAggregates.Add(DecodeAggregate(ref reader));
                            break;
                        case StreamFieldNames.Status:
                            status.Add(DecodeStatus(ref reader));
                            break;
                    }
                }
                catch (JsonException e)
                {
                    this.logger.LogError(e, $"decoding stream but will skip past error, encountered {e}");
                }

                

                /*
                 * 
                 *         private readonly TradeComparer tradeComparer = new TradeComparer();
        private readonly QuoteComparer quoteComparer = new QuoteComparer();
        private readonly AggregateComparer aggregateComparer = new AggregateComparer();
                 *    AggregatesPerSecond = perSecondAggregates.Distinct(this.aggregateComparer).ToList(),
                AggregatesPerMinute = perMinuteAggregates.Distinct(this.aggregateComparer).ToList(),
                Quotes = quotes.Distinct(this.quoteComparer).ToList(),
                Trades = trades.Distinct(this.tradeComparer).ToList(),
                Statuses = statuses
                */

            }

            return new DeserializedData(trades, quotes, status, perSecondAggregates, perMinuteAggregates);
        }

        private bool SkipUpTo(ref Utf8JsonReader reader, JsonTokenType a)
        {
            while(reader.Read())
            {
                if(reader.TokenType == a)
                    return true;
            }
            return false;
        }

        private bool SkipTillExpected(ref Utf8JsonReader reader, JsonTokenType a, JsonTokenType b)
        {
            while (reader.Read())
            {
                if (reader.TokenType == a)
                    return true;
            }

            if (reader.TokenType == b)
                return false;

            throw new JsonException($"expected {a} found {b} at position {reader.Position.GetInteger()}");
        }
    }
}
