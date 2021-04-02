using PolygonIo.WebSocket.Contracts;

namespace PolygonIo.WebSocket.Factory
{
    public interface IEventFactory
    {
        IQuote NewQuote();
        ITrade NewTrade();
        ITimeAggregate NewTimeAggregate();
        IStatus NewStatus();
    }
}