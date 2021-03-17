using PolygonIo.Model.Contracts;

namespace PolygonIo.WebSocket
{
    // to be used where client wants to take a dependency on this packages types
    public class PolygonTypesEventFactory : IEventFactory
    {
        public IQuote NewQuote() => new Quote();
        public IStatus NewStatus() => new Status();
        public ITimeAggregate NewTimeAggregate() => new TimeAggregate();
        public ITrade NewTrade() => new Trade();
    }
}