namespace PolygonIo.WebSocket.Contracts
{
    public struct Status : IStatus
    {
        public string Result { get; set; }
        public string Message { get; set; }
    }
}
