namespace PolygonIo.WebApi.Model
{
    public class TickersResponse
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Count { get; set; }
        public string Status { get; set; }
        public TickerV2[] Tickers { get; set; }
    }
}
