using System.Buffers;

namespace PolygonIo.WebSocket.Deserializers
{
    public interface IPolygonDeserializer
    {
        PolygonDataFrame Deserialize(ReadOnlySequence<byte> jsonData);
    }
}
