using System;

namespace PolygonIo.WebSocket.Deserializers
{
    public class UnexpectedPropertyException : Exception
    {
        public UnexpectedPropertyException(Type type, string propertyName)
        : base($"Decoding {type.GetType()} found unexpected property {propertyName}.") { }
    }
}
