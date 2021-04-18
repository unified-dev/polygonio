using System;

namespace PolygonIo.Parser.Tests
{
    public class TimeAggregate
    {
        public decimal Close { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public string Symbol { get; set; }
        public decimal Volume { get; set; }
        public decimal AverageTradeSize { get; set; }
        public decimal TodaysVolumeWeightedAveragePrice { get; set; }
        public decimal TicksVolumeWeightedAveragePrice { get; set; }
        public decimal TodaysOpeningPrice { get; set; }
    }
}