
namespace PolygonIo.WebSocket.Contracts
{
    public static class MessageTypes
    {
        public const string Event = "ev";
        public const string AggregatePerMinute = "AM";
        public const char AggregatePerSecond = 'A';
        public const char Quote = 'Q';
        public const char Trade = 'T';
        public const string Status = "status";
    }
}
