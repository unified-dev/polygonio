using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Contracts;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{

    public class Utf8JsonDeserializer : IPolygonDeserializer
    {
        private readonly ILogger<Utf8JsonDeserializer> logger;
        private readonly IEventFactory eventDataTypeFactory;

        public Utf8JsonDeserializer(ILogger<Utf8JsonDeserializer> logger, IEventFactory eventDataTypeFactory)
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

            if (reader.SkipUpTo(JsonTokenType.StartArray) == false)
                throw new JsonException($"skipped all data and no array found");
           
            while (reader.SkipTillExpected(JsonTokenType.StartObject, JsonTokenType.EndArray))
            {
                try
                {
                    reader.ExpectNamedProperty(StreamFieldNames.Event);
                    var ev = reader.ExpectString(StreamFieldNames.Event);

                    switch (ev)
                    {
                        case StreamFieldNames.Quote:
                            quotes.Add(reader.DecodeQuote(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.Trade:
                            trades.Add(reader.DecodeTrade(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.AggregatePerSecond:
                            perSecondAggregates.Add(reader.DecodeAggregate(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.AggregatePerMinute:
                            perMinuteAggregates.Add(reader.DecodeAggregate(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.Status:
                            status.Add(reader.DecodeStatus(this.eventDataTypeFactory));
                            break;
                    }
                }
                catch (JsonException e)
                {
                    this.logger.LogError(e, $"decoding stream but will skip past error, encountered {e}");
                }
            }

            return new DeserializedData(trades, quotes, status, perSecondAggregates, perMinuteAggregates);
        } 
    }
}
