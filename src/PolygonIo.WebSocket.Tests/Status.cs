using PolygonIo.Model.Contracts;

namespace PolygonIo.WebSocket.Tests
{
    class Status : IStatus
    {
        public string Message { get; set; } 
        public string Result { get; set; }
    }
}