using System;

namespace PolygonIo.WebSocket.Contracts
{
    public interface IQuote
    {
        int AskExchangeId { get; set; }
        decimal AskPrice { get; set; }
        decimal AskSize { get; set; }
        int BidExchangeId { get; set; }
        decimal BidPrice { get; set; }
        decimal BidSize { get; set; }
        QuoteCondition QuoteCondition { get; set; }
        DateTimeOffset Timestamp { get; set; }
        string Symbol { get; set; }
        int Tape { get; set; }
    }
}