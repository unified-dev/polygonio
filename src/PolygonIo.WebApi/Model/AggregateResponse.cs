using System.Collections.Generic;

namespace PolygonIo.WebApi.Model
{
    public class AggregateResponse
    {
        public string Url { get; set; }
        public string Ticker { get; set; }
        public string Status { get; set; }
        public bool Adjusted { get; set; }
        public int QueryCount { get; set; }
        public int ResultsCount { get; set; }
        public List<AggV2> Results { get; set; }
    }
}
