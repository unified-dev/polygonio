using Microsoft.Extensions.Logging;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Factory;
using System.Buffers;
using System;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    public class Utf8JsonDeserializer : IPolygonDeserializer
    {
        private readonly ILogger<Utf8JsonDeserializer> logger;
        private readonly IEventFactory eventDataTypeFactory;

        public Utf8JsonDeserializer(ILogger<Utf8JsonDeserializer> logger, IEventFactory eventDataTypeFactory)
        {
            this.eventDataTypeFactory = eventDataTypeFactory ?? throw new System.ArgumentNullException(nameof(eventDataTypeFactory));
            this.logger = logger;
        }

        public void Deserialize(ReadOnlySequence<byte> data, Action<IQuote> onQuote, Action<ITrade> onTrade, Action<ITimeAggregate> onPerSecondAggregate, Action<ITimeAggregate> onPerMinuteAggregate, Action<IStatus> onStatus)
        {
            var reader = new Utf8JsonReader(data);

            if (reader.SkipUpTo(JsonTokenType.StartArray) == false)
                throw new JsonException($"skipped all data and no array found");
           
            while (reader.SkipTillExpected(JsonTokenType.StartObject, JsonTokenType.EndArray))
            {
                try
                {
                    reader.ExpectNamedProperty(StreamFieldNames.Event);
                    var ev = reader.ExpectString(StreamFieldNames.Event);

                    // Order by most expected sequence.
                    switch (ev)
                    {
                        case StreamFieldNames.Quote:
                            onQuote(reader.DecodeQuote(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.Trade:
                            onTrade(reader.DecodeTrade(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.AggregatePerSecond:
                            onPerSecondAggregate(reader.DecodeAggregate(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.AggregatePerMinute:
                            onPerMinuteAggregate(reader.DecodeAggregate(this.eventDataTypeFactory));
                            break;
                        case StreamFieldNames.Status:
                            onStatus(reader.DecodeStatus(this.eventDataTypeFactory));
                            break;
                    }
                }
                catch (JsonException e)
                {
                    this.logger.LogError(e, $"decoding stream but will skip past error, encountered {e}");
                }
            }
        }
    }
}
