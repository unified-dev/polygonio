using System;

namespace PolygonIo.Model.Contracts
{
    public class TimeAggregate : ITimeAggregate
    {
        public decimal Close { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public string Symbol { get; set; }
        public decimal Volume { get; set; }
    }
}