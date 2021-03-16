using PolygonIo.Model.Contracts;

namespace PolygonIo.WebSocket
{
    public class EventFactory<TQuote, TTrade, TTimeAggregate, TStatus> : IEventFactory
        where TQuote : IQuote, new()
        where TTrade : ITrade, new()
        where TTimeAggregate : ITimeAggregate, new()
        where TStatus : IStatus, new()
    {
        public IQuote NewQuote() => new TQuote();
        public IStatus NewStatus() => new TStatus();
        public ITimeAggregate NewTimeAggregate() => new TTimeAggregate();
        public ITrade NewTrade() => new TTrade();
    }
}