using System.Text.Json.Serialization;

namespace PolygonIo.WebSocket.Contracts
{
    public class TimeAggregate
    {
        [JsonPropertyName("c")]
        public decimal Close { get; set; }

        [JsonPropertyName("e")]
        public long EndUnixTimestamp { get; set; }

        [JsonPropertyName("h")]
        public decimal High { get; set; }

        [JsonPropertyName("l")]
        public decimal Low { get; set; }

        [JsonPropertyName("o")]
        public decimal Open { get; set; }

        [JsonPropertyName("s")]
        public long StartUnixTimestamp { get; set; }

        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("v")]
        public decimal Volume { get; set; }

        [JsonPropertyName("z")]
        public decimal AverageTradeSize { get; set; }

        [JsonPropertyName("a")]
        public decimal TodaysVolumeWeightedAveragePrice { get; set; }

        [JsonPropertyName("vw")]
        public decimal TicksVolumeWeightedAveragePrice { get; set; }

        [JsonPropertyName("op")]
        public decimal TodaysOpeningPrice { get; set; }
    }
}