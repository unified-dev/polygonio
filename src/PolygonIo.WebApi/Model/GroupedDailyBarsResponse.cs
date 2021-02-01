using PolygonIo.WebApi.Model;
using System.Collections.Generic;

namespace PolygonIo.WebApi.Model
{
    public class GroupedDailyBarsResponse
    {
        public string Url { get; set; }
        public string Status { get; set; }
        public bool Adjusted { get; set; }
        public int QueryCount { get; set; }
        public int ResultsCount { get; set; }
        public List<GroupedDailyBars> Results { get; set; }
    }
}
