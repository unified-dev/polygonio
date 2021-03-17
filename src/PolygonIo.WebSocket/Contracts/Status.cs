namespace PolygonIo.WebSocket.Contracts
{
    public class Status : IStatus
    {
        public string Result { get; set; }
        public string Message { get; set; }
    }
}
