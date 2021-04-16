namespace PolygonIo.WebSocket.Contracts
{
    internal static class StreamFieldNames
    {
        public const string Event = "ev";
        public const string AggregatePerMinute = "AM";
        public const string AggregatePerSecond = "A";
        public const string Quote = "Q";
        public const string Trade = "T";
        public const string Status = "status";
        public const string Message = "message";
        public const string Symbol = "sym";
        public const string ExchangeId = "x";
        public const string BidExchangeId = "bx";
        public const string BidPrice = "bp";
        public const string BidSize = "bs";
        public const string AskExchangeId = "ax";
        public const string AskPrice = "ap";
        public const string AskSize = "as";
        public const string TradeId = "i";
        public const string Tape = "z";
        public const string AverageTradeSize = "z";
        public const string Price = "p";
        public const string TradeSize = "s";
        public const string TradeConditions = "c";
        public const string QuoteCondition = "c";
        public const string TickVolume = "v";
        public const string TickOpen = "o";
        public const string TickClose = "c";
        public const string TickHigh = "h";
        public const string TickLow = "l";        
        public const string Timestamp = "t";
        public const string StartTimestamp = "s";
        public const string EndTimestamp = "e";
        public const string TicksVolumeWeightsAveragePrice = "a";
        public const string TodaysVolumeWeightedAveragePrice = "vw";
        public const string TodaysOpeningPrice = "op";
        public const string TodaysAccumulatedVolume = "av";
    }
}
