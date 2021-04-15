using System;

namespace PolygonIo.WebSocket.Contracts
{
    public interface ITimeAggregate : ISymbol
    {
        decimal Close { get; set; }
        DateTimeOffset EndDateTime { get; set; }
        decimal High { get; set; }
        decimal Low { get; set; }
        decimal Open { get; set; }
        DateTimeOffset StartDateTime { get; set; }
        decimal Volume { get; set; }
    }
}