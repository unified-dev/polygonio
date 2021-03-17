namespace PolygonIo.WebSocket.Contracts
{
    public interface IStatus
    {
        string Message { get; set; }
        string Result { get; set; }
    }
}