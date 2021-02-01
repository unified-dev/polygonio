using Newtonsoft.Json;
using PolygonIo.WebApi.Json;
using System;

namespace PolygonIo.WebApi.Model
{
    public class AggV2
    {
        [JsonProperty("v")]
        public decimal Volume { get; set; }
        [JsonProperty("o")]
        public decimal Open { get; set; }
        [JsonProperty("c")]
        public decimal Close { get; set; }
        [JsonProperty("h")]
        public decimal High { get; set; }
        [JsonProperty("l")]
        public decimal Low { get; set; }
        [JsonProperty("t")]
        [JsonConverter(typeof(UnixMillisecondsConverter))]
        public DateTimeOffset Timestamp { get; set; }
        [JsonProperty("n")]
        public int Samples { get; set; }
    }
}
