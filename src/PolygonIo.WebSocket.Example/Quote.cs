using PolygonIo.Model.Contracts;
using System;

namespace PolygonIo.WebSocket.Example
{
    class Quote : IQuote
    {
        public int AskExchangeId { get; set; }
        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
        public int BidExchangeId { get; set; }
        public decimal BidPrice { get; set; }
        public decimal BidSize { get; set; }
        public QuoteCondition QuoteCondition { get; set; }
        public string Symbol { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}