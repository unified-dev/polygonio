using System;

namespace PolygonIo.Model.Contracts
{
    public interface ITrade
    {
        int ExchangeId { get; set; }
        decimal Price { get; set; }
        string Symbol { get; set; }
        int Tape { get; set; }
        DateTimeOffset Timestamp { get; set; }
        TradeCondition[] TradeConditions { get; set; }
        string TradeId { get; set; }
        decimal TradeSize { get; set; }
    }
}