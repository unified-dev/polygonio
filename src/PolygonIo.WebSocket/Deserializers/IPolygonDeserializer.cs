namespace PolygonIo.WebSocket.Deserializers
{
    public interface IPolygonDeserializer
    {
        DeserializedData Deserialize(byte[] data);
    }
}
