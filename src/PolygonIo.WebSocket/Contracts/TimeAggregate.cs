using System.Text.Json.Serialization;

namespace PolygonIo.WebSocket.Contracts
{
    public class TimeAggregate
    {
        [JsonPropertyName("c")]
        public float Close { get; set; }

        [JsonPropertyName("e")]
        public ulong EndUnixTimestamp { get; set; }

        [JsonPropertyName("h")]
        public float High { get; set; }

        [JsonPropertyName("l")]
        public float Low { get; set; }

        [JsonPropertyName("o")]
        public float Open { get; set; }

        [JsonPropertyName("s")]
        public ulong StartUnixTimestamp { get; set; }

        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("v")]
        public float Volume { get; set; }

        [JsonPropertyName("z")]
        public float AverageTradeSize { get; set; }

        [JsonPropertyName("a")]
        public float TodayVolumeWeightedAveragePrice { get; set; }

        [JsonPropertyName("vw")]
        public float TicksVolumeWeightedAveragePrice { get; set; }

        [JsonPropertyName("op")]
        public float TodayOpeningPrice { get; set; }
    }
}