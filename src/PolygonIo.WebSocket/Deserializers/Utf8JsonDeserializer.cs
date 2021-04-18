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
        private readonly IEventFactory eventDataTypeFactory;

        public Utf8JsonDeserializer(IEventFactory eventDataTypeFactory)
        {
            this.eventDataTypeFactory = eventDataTypeFactory ?? throw new System.ArgumentNullException(nameof(eventDataTypeFactory));
        }

        public void Deserialize(ReadOnlySpan<byte> data, Action<IQuote> onQuote, Action<ITrade> onTrade, Action<ITimeAggregate> onPerSecondAggregate, Action<ITimeAggregate> onPerMinuteAggregate, Action<IStatus> onStatus, Action<string> onError)
        {
            // Important: we force the use of span, so we can access the ValueSpan property (and avoiding transcoding to string whilst looping through chunks)
            var reader = new Utf8JsonReader(data, true, new JsonReaderState());

            if (reader.SkipUpTo(JsonTokenType.StartArray) == false)
                onError($"Skipped all data and no array found.");
           
            while (reader.SkipTillExpected(JsonTokenType.StartObject, JsonTokenType.EndArray))
            {
                try
                {
                    reader.ExpectNamedProperty(StreamFieldNames.Event);
                    var ev = reader.ExpectString(StreamFieldNames.Event);

                    if (ev[0] == StreamFieldNames.Quote)
                    {
                        onQuote(reader.DecodeQuote(this.eventDataTypeFactory, onError));
                    }
                    else if (ev[0] == StreamFieldNames.Trade)
                    {
                        onTrade(reader.DecodeTrade(this.eventDataTypeFactory, onError));
                    }
                    else if (ev[0] == StreamFieldNames.AggregatePerSecond)
                    {
                        onPerSecondAggregate(reader.DecodeAggregate(this.eventDataTypeFactory, onError));
                    }
                    else if (ev == StreamFieldNames.AggregatePerMinute)
                    {
                        onPerMinuteAggregate(reader.DecodeAggregate(this.eventDataTypeFactory, onError));
                    }
                    else if (ev == StreamFieldNames.Status)
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
