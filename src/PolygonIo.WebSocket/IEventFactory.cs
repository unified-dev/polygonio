using PolygonIo.WebSocket.Contracts;

namespace PolygonIo.WebSocket
{
    public interface IEventFactory
    {
        IQuote NewQuote();
        ITrade NewTrade();
        ITimeAggregate NewTimeAggregate();
        IStatus NewStatus();
    }
}