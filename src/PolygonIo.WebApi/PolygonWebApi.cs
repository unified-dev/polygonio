using Flurl;
using Newtonsoft.Json;
using PolygonIo.WebApi.Model;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.WebApi
{
    public class PolygonWebApi
    {
        const string DateFormat = "yyyy-MM-dd";        

        const string apiUrl = "https://api.polygon.io/";
        const string apiVersion = "v2";
        const int MaxResultsPerPage = 50;
        private const string RedactedKeyString = "***removed***";
        private const string AggsString = "aggs";
        private const string TickerString = "ticker";
        private const string RangeString = "range";
        private const string GroupedString = "grouped";
        private const string LocaleString = "locale";
        private const string ApiKeyString = "apiKey";
        private const string ReferenceString = "reference";
        private const string TickersString = "tickers";
        private const string MarketString = "market";

        public static async Task<TickersResponse> GetTickersAsync(HttpClient client, string apiKey, int perPage, int page, CancellationToken cancellationToken, SortTickersBy? sort = null, TickerType? type = null, Market? market = null, Locale? locale = null, bool? active = null, string search = null)
        {
            if (perPage > MaxResultsPerPage)
                throw new ArgumentException($"max {perPage} is {MaxResultsPerPage}", nameof(perPage));

            var url = apiUrl
                        .AppendPathSegments(
                                apiVersion,
                                ReferenceString,
                                TickersString)                      
            .SetQueryParamIfNotNull(nameof(sort), sort)
            .SetQueryParamIfNotNull(nameof(type), type)
            .SetQueryParamIfNotNull(nameof(market), market)
            .SetQueryParamIfNotNull(nameof(locale), locale)
            .SetQueryParamIfNotNull(nameof(active), active)
            .SetQueryParamIfNotNull(nameof(search), search)
            .SetQueryParam(nameof(perPage), perPage)
            .SetQueryParam(nameof(page), page)
            .SetQueryParam(nameof(apiKey), apiKey);

            return await GetResponse<TickersResponse>(client, url, cancellationToken);
        }

        public static async Task<AggregateResponse> GetAggregatesBarsAsync(HttpClient client, string apiKey, string symbol, int multiplier, Timespan timespan, DateTimeOffset from, DateTimeOffset to, bool? unadjusted, Sort? sort, int? limit, CancellationToken cancellationToken)
        {
            var url = apiUrl
                        .AppendPathSegments(
                            apiVersion,
                            AggsString,
                            TickerString,
                            symbol,
                            RangeString,
                            multiplier,
                            timespan.ToString("g").ToLower(),
                            from.ToString(DateFormat),
                            to.ToString(DateFormat))
                        .SetQueryParamIfNotNull(nameof(unadjusted), unadjusted)
                        .SetQueryParamIfNotNull(nameof(sort), sort)
                        .SetQueryParamIfNotNull(nameof(limit), limit)
                        .SetQueryParam(ApiKeyString, apiKey);

            var result = await GetResponse<AggregateResponse>(client, url, cancellationToken);
            result.Url = url.SetQueryParam(ApiKeyString, RedactedKeyString);
            return result;
        }

        public static async Task<GroupedDailyBarsResponse> GetGroupedDailyBarsAsync(HttpClient client, string apiKey, Locale locale, Market market, DateTimeOffset date, bool? unadjusted, CancellationToken cancellationToken)
        {
            var url = apiUrl
                        .AppendPathSegments(
                            apiVersion,
                            AggsString,
                            GroupedString,
                            LocaleString,
                            locale,
                            MarketString,
                            market,
                            date.ToString(DateFormat))
                        .SetQueryParamIfNotNull(nameof(unadjusted), unadjusted)                               
                        .SetQueryParam(ApiKeyString, apiKey);

            var result = await GetResponse<GroupedDailyBarsResponse>(client, url, cancellationToken);
            result.Url = url.SetQueryParam(ApiKeyString, RedactedKeyString);
            return result;
        }

        async static Task<T> GetResponse<T>(HttpClient client, string url, CancellationToken cancellationToken) where T: class
        {
            try
            {
                var response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode == false)
                    return null;

                var data = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(data);
            }
            catch(OperationCanceledException)
            {
                return default;
            }
        }
    }
}
