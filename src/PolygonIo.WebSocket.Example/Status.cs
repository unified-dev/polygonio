using PolygonIo.Model.Contracts;

namespace PolygonIo.WebSocket.Example
{
    class Status : IStatus
    {
        public string Message { get; set; }
        public string Result { get; set; }
    }
}