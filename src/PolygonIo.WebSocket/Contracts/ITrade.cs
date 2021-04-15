using System;

namespace PolygonIo.WebSocket.Contracts
{
    public interface ITrade : ISymbol
    {
        int ExchangeId { get; set; }
        decimal Price { get; set; }
        int Tape { get; set; }
        DateTimeOffset Timestamp { get; set; }
        TradeCondition[] TradeConditions { get; set; }
        string TradeId { get; set; }
        decimal TradeSize { get; set; }
    }
}