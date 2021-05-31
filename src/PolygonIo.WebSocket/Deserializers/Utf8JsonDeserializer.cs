using PolygonIo.WebSocket.Contracts;
using System;
using System.Text.Json;

namespace PolygonIo.WebSocket.Deserializers
{
    sealed public class Utf8JsonDeserializer : IPolygonDeserializer
    {
        bool GetJsonObject(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> found)
        {          
            var start = data.IndexOf((byte)'{');
            var end = data.IndexOf((byte)'}');

            if (start == -1 || end == -1)
            {
                found = null;
                return false;
            }

            found = data.Slice(start, end - start + 1);
            return true;
        }

        public void Deserialize(ReadOnlySpan<byte> data, Action<Quote> onQuote, Action<Trade> onTrade, Action<TimeAggregate> onPerSecondAggregate, Action<TimeAggregate> onPerMinuteAggregate, Action<Status> onStatus, Action<Exception> onError)
        {
            while(GetJsonObject(data, out var jsonObject))
            {
                try
                {
                    if (jsonObject.ContainsQuote())
                    {
                        onQuote(JsonSerializer.Deserialize<Quote>(jsonObject));
                    }
                    else if (jsonObject.ContainsTrade())
                    {      
                        onTrade(JsonSerializer.Deserialize<Trade>(jsonObject));
                    }
                    else if (jsonObject.ContainsAggregatePerSecond())
                    {
                        onPerSecondAggregate(JsonSerializer.Deserialize<TimeAggregate>(jsonObject));
                    }
                    else if (jsonObject.ContainsAggregatePerMinute())
                    {
                        onPerMinuteAggregate(JsonSerializer.Deserialize<TimeAggregate>(jsonObject));
                    }
                    else if (jsonObject.ContainsStatus())
                    {
                        onStatus(JsonSerializer.Deserialize<Status>(jsonObject));
                    }
                    else
                    {
                        // Unknown event type.
                    }
                    
                    data = data.Slice(jsonObject.Length + 1); // Skip forward over the processed json.
                }
                catch (Exception e)
                {
                    onError(e);
                }
            }
        }
    }
}
