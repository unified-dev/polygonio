using System;

namespace PolygonIo.WebApi.Model
{
    public class TickerV2
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public Market Market { get; set; }
        public Locale Locale { get; set; }
        public bool Active { get; set; }
        public PrimaryExch PrimaryExch { get; set; }
        public DateTime Updated { get; set; }
        public CurrencyCode Currency { get; set; }
    }

    class Stock : TickerV2
    {
        public TickerType Type { get; set; }
        public string CIK { get; set; }
        public string FIGIUID { get; set; }
        public string SCFIGI { get; set; }
        public string CFIGI { get; set; }
        public string FIGI { get; set; }
    }

    class ForeignExchange : TickerV2
    {
        public CurrencyCode BaseCurrency { get; set; }
    }

}
