using System;
using PolygonIo.WebSocket.Contracts;

namespace PolygonIo.WebSocket
{
    public interface IPolygonDeserializer
    {
        void Deserialize(ReadOnlySpan<byte> data, Action<Quote> onQuote, Action<Trade> onTrade, Action<TimeAggregate> onPerSecondAggregate, Action<TimeAggregate> onPerMinuteAggregate, Action<StatusMessage> onStatus, Action<Exception,byte[]> onError, Action<string> onUnknown = null);
    }
}
