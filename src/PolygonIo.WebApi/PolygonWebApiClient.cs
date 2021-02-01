using PolygonIo.WebApi.Model;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.WebApi
{
    public class PolygonWebApiClient
    {
        readonly string apiKey;
        readonly HttpClient client;

        public PolygonWebApiClient(HttpClient client, string apiKey)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }
        public Task<TickersResponse> GetTickersAsync(int perPage, int page, CancellationToken cancellationToken, SortTickersBy? sort = null, TickerType? type = null, Market? market = null, Locale? locale = null, bool? active = null, string search = null)
        {
            return PolygonWebApi.GetTickersAsync(this.client, this.apiKey, perPage, page, cancellationToken, sort, type, market, locale, active, search);
        }

        public Task<AggregateResponse> GetAggregatesBarsAsync(string symbol, int multiplier, Timespan timespan, DateTimeOffset from, DateTimeOffset to, bool? unadjusted, Sort? sort, int? limit, CancellationToken cancellationToken)
        {
            return PolygonWebApi.GetAggregatesBarsAsync(this.client, this.apiKey, symbol, multiplier, timespan, from, to, unadjusted, sort, limit, cancellationToken);
        }
    }
}
