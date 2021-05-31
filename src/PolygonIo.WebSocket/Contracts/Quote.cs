using System.Text.Json.Serialization;

namespace PolygonIo.WebSocket.Contracts
{
    public struct Quote
    {
        [JsonPropertyName("sym")]
        public string Symbol { get; set; }

        [JsonPropertyName("ax")]
        public uint? AskExchangeId { get; set; }

        [JsonPropertyName("as")]
        public uint AskSize { get; set; }

        [JsonPropertyName("ap")]
        public float AskPrice { get; set; }

        [JsonPropertyName("bx")]
        public uint? BidExchangeId { get; set; }

        [JsonPropertyName("bs")]
        public int BidSize { get; set; }

        [JsonPropertyName("bp")]
        public float BidPrice { get; set; }

        [JsonPropertyName("t")]
        public ulong UnixTimestamp { get; set; }

        [JsonPropertyName("c")]
        public QuoteCondition Condition { get; set; }

        [JsonPropertyName("z")]
        public uint Tape { get; set; }
    }
}