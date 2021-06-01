using PolygonIo.WebSocket.Contracts;
using System;

namespace PolygonIo.WebSocket.Deserializers
{
    public interface IPolygonDeserializer
    {
        void Deserialize(ReadOnlySpan<byte> data, Action<Quote> onQuote, Action<Trade> onTrade, Action<TimeAggregate> onPerSecondAggregate, Action<TimeAggregate> onPerMinuteAggregate, Action<Status> onStatus, Action<Exception> onError, Action<string> onUnknown = null);
    }
}
