using PolygonIo.WebSocket.Contracts;
using System;
using System.Buffers;

namespace PolygonIo.WebSocket.Deserializers
{
    public interface IPolygonDeserializer
    {
        void Deserialize(ReadOnlySpan<byte> data, Action<IQuote> onQuote, Action<ITrade> onTrade, Action<ITimeAggregate> onPerSecondAggregate, Action<ITimeAggregate> onPerMinuteAggregate, Action<IStatus> onStatus, Action<string> onError);
    }
}
