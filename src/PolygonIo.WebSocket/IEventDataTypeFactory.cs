using PolygonIo.Model.Contracts;

namespace PolygonIo.WebSocket
{
    public interface IEventDataTypeFactory
    {
        IQuote NewQuote();
        ITrade NewTrade();
        ITimeAggregate NewTimeAggregate();
        IStatus NewStatus();
    }

    public class EventDataTypeFactory<TQuote, TTrade, TTimeAggregate, TStatus> : IEventDataTypeFactory
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