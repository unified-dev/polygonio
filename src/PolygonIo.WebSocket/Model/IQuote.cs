using System;

namespace PolygonIo.Model.Contracts
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
        string Symbol { get; set; }
        DateTimeOffset Timestamp { get; set; }
    }
}