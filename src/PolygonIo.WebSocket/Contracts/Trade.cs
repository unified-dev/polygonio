using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PolygonIo.WebSocket.Contracts
{
    public struct Trade
    {
        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("i")]
        public string TradeId { get; set; }

        [JsonPropertyName("x")]
        public uint ExchangeId { get; set; }

        [JsonPropertyName("p")]
        public float Price { get; set; }

        [JsonPropertyName("s")]
        public uint Size { get; set; }

        [JsonPropertyName("t")]
        public ulong UnixTimestamp { get; set; }

        [JsonPropertyName("z")]
        public uint Tape { get; set; }

        [JsonPropertyName("c")]
        public IEnumerable<TradeCondition> Conditions { get; set; }
    }
}