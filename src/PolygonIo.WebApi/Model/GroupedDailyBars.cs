using Newtonsoft.Json;

namespace PolygonIo.WebApi.Model
{
    public class GroupedDailyBars : AggV2
    {
        [JsonProperty("T")]
        public string Ticker { get; set; }
    }
}
