using System;

namespace PolygonIo.WebApi.Model
{
    public class TickerV3
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public Market Market { get; set; }
        public Locale Locale { get; set; }
        public bool Active { get; set; }
        public PrimaryExch PrimaryExch { get; set; }
        public DateTime Updated { get; set; }
        public CurrencyCode CurrencyName { get; set; }
        public string CIK { get; set; }
        public TickerType Type { get; set; }
        public string CompositeFigi { get; set; }
        public string ShareClassFigi { get; set; }
        public DateTimeOffset LastUpdatedUtc { get; set; }
    }
}
