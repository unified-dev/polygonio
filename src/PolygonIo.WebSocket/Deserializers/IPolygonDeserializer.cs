using System.Buffers;

namespace PolygonIo.WebSocket.Deserializers
{
    public interface IPolygonDeserializer
    {
        DeserializedData Deserialize(ReadOnlySequence<byte> jsonData);
    }
}
