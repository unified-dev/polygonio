using System.Collections;
using System.Collections.Generic;

namespace PolygonIo.WebApi.Model
{
    public class TickersV3Response
    {
        public string RequestId { get; set; }
        public string NextUrl { get; set; }
        public int Count { get; set; }
        public string Status { get; set; }
        public IEnumerable<TickerV3> Results { get; set; }
    }
}
