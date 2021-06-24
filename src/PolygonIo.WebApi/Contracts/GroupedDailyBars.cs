
using Newtonsoft.Json;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.WebApi.Contracts
{
    public class GroupedDailyBars : AggV2
    {
        [JsonProperty("T")]
        public string Ticker { get; set; }
    }
}
