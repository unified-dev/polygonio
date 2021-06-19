using System.Collections.Generic;
using Newtonsoft.Json;

namespace PolygonIo.WebApi.Model
{
    public class TickersV3Response
    {
        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("next_url")]
        public string NextUrl { get; set; }
        public int Count { get; set; }
        public string Status { get; set; }
        public IEnumerable<TickerV3> Results { get; set; }
    }
}
