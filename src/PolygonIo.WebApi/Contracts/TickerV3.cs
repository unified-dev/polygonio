using System;
using Newtonsoft.Json;

namespace PolygonIo.WebApi.Contracts
{
    public class TickerV3
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public Market Market { get; set; }
        public Locale Locale { get; set; }
        public bool Active { get; set; }
        [JsonProperty("primary_exchange")]
        public string PrimaryExchange { get; set; }
        [JsonProperty("currency_name")]
        public CurrencyCode CurrencyName { get; set; }
        [JsonProperty("cik")]
        public string CIK { get; set; }
        public TickerType Type { get; set; }
        [JsonProperty("composite_figi")]
        public string CompositeFigi { get; set; }
        [JsonProperty("share_class_figi")]
        public string ShareClassFigi { get; set; }
        [JsonProperty("last_updated_utc")]
        public DateTimeOffset LastUpdatedUtc { get; set; }
    }
}
