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

        public void Deserialize(ReadOnlySequence<byte> data, Action<IQuote> onQuote, Action<ITrade> onTrade, Action<ITimeAggregate> onPerSecondAggregate, Action<ITimeAggregate> onPerMinuteAggregate, Action<IStatus> onStatus, Action<string> onError)
        {
            var reader = new Utf8JsonReader(data);

            if (reader.SkipUpTo(JsonTokenType.StartArray) == false)
                onError($"skipped all data and no array found");
           
            while (reader.SkipTillExpected(JsonTokenType.StartObject, JsonTokenType.EndArray))
            {
                try
                {
                    reader.ExpectNamedProperty(StreamFieldNames.Event);
                    reader.Read();

                    if (reader.ValueTextEquals(StreamFieldNames.Quote))
                    {
                        onQuote(reader.DecodeQuote(this.eventDataTypeFactory, onError));
                    }
                    else if (reader.ValueTextEquals(StreamFieldNames.Trade))
                    {
                        onTrade(reader.DecodeTrade(this.eventDataTypeFactory, onError));
                    }
                    else if (reader.ValueTextEquals(StreamFieldNames.AggregatePerSecond))
                    {
                        onPerSecondAggregate(reader.DecodeAggregate(this.eventDataTypeFactory, onError));
                    }
                    else if (reader.ValueTextEquals(StreamFieldNames.AggregatePerMinute))
                    {
                        onPerMinuteAggregate(reader.DecodeAggregate(this.eventDataTypeFactory, onError));
                    }
                    else if (reader.ValueTextEquals(StreamFieldNames.Status))
                    {
                        onStatus(reader.DecodeStatus(this.eventDataTypeFactory, onError));
                    }
                }

                catch (JsonException ex)
                {
                    onError($"Decoding stream but will skip past error, encountered {ex.Message}.");
                }
            }
        }
    }
}
