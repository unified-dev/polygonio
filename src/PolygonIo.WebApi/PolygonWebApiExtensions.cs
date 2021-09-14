using Flurl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.WebApi
{
    public static class PolygonWebApiExtensions
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string ApiUrl = "https://api.polygon.io/";
        private const string ApiVersionV2 = "v2";
        private const string ApiVersionV3 = "v3";
        private const int MaxResultsPerPage = 1000;
        private const string AggsString = "aggs";
        private const string TickerString = "ticker";
        private const string RangeString = "range";
        private const string GroupedString = "grouped";
        private const string LocaleString = "locale";
        private const string ApiKeyString = "apiKey";
        private const string ReferenceString = "reference";
        private const string TickersString = "tickers";
        private const string MarketString = "market";

        public static async Task<TickersV3Response> GetPolygonTickersV3(this HttpClient client,
            CancellationToken cancellationToken, string apiKey, int limit = 1000, SortTickersBy? sort = null,
            TickerType? type = null, Market? market = null, Locale? locale = null, bool? active = null,
            string search = null)
        {
            if (limit <= 0 || limit > MaxResultsPerPage)
                throw new ArgumentException($"Invalid {limit} value - must be greater or equal to 1 and equal to or less than {MaxResultsPerPage}.", nameof(limit));

            var url = ApiUrl
                        .AppendPathSegments(ApiVersionV3, ReferenceString, TickersString)
                        .SetQueryParamIfNotNull(nameof(sort), sort)
                        .SetQueryParamIfNotNull(nameof(type), type)
                        .SetQueryParamIfNotNull(nameof(market), market)
                        .SetQueryParamIfNotNull(nameof(locale), locale)
                        .SetQueryParamIfNotNull(nameof(active), active)
                        .SetQueryParamIfNotNull(nameof(search), search)
                        .SetQueryParamIfNotNull(nameof(limit), limit);

            return await GetResponse<TickersV3Response>(client, apiKey, url, cancellationToken);
        }

        public static async Task<IEnumerable<TickerV3>> GetAllPolygonTickersV3(this HttpClient client,
            CancellationToken cancellationToken, string apiKey, int limit = 1000, SortTickersBy? sort = null,
            TickerType? type = null, Market? market = null, Locale? locale = null, bool? active = null,
            string search = null)
        {
            var list = new List<TickerV3>();

            // Add the first fetch.
            var response = await GetPolygonTickersV3(client, cancellationToken, apiKey, limit, sort, type, market, locale, active, search);

            if (response.Results == null)
                return list;

            list.AddRange(response.Results);

            // Get additional pages.
            while (response.NextUrl != null && cancellationToken.IsCancellationRequested == false)
            {
                response = await GetResponse<TickersV3Response>(client, apiKey, response.NextUrl, cancellationToken);

                if (response.Results == null)
                    break;
                
                list.AddRange(response.Results);
            } 

            return list;
        }

        public static async Task<AggregateResponse> GetPolygonAggregatesBarsV2(this HttpClient client, string apiKey, string symbol, int multiplier, AggregateTimespan timespan,
            LocalDate from, LocalDate to, CancellationToken cancellationToken = default, bool? unadjusted = null, Sort? sort = null, int? limit = null)
        {
            if (to < from)
                throw new ArgumentException($"To '{to}' must not be earlier than from '{from}'.", nameof(to));

            var url = ApiUrl
                        .AppendPathSegments(ApiVersionV2, AggsString, TickerString ,symbol, RangeString, multiplier, timespan.ToString().ToLower(), from.ToString(DateFormat, CultureInfo.InvariantCulture), to.ToString(DateFormat, CultureInfo.InvariantCulture))
                        .SetQueryParamIfNotNull(nameof(unadjusted), unadjusted)
                        .SetQueryParamIfNotNull(nameof(sort), sort)
                        .SetQueryParamIfNotNull(nameof(limit), limit);

            return await GetResponse<AggregateResponse>(client, apiKey, url, cancellationToken);
        }

        public static async Task<GroupedDailyBarsResponse> GetPolygonGroupedDailyBarsV2(this HttpClient client,
            string apiKey, Locale locale, Market market, LocalDate date, CancellationToken cancellationToken)
        {
            return await GetPolygonGroupedDailyBarsV2(client, cancellationToken, apiKey, locale, market, date);
        }

        public static async Task<GroupedDailyBarsResponse> GetPolygonGroupedDailyBarsV2(this HttpClient client,
            CancellationToken cancellationToken, string apiKey, Locale locale, Market market, LocalDate date,
            bool? unadjusted = null)
        {
            var url = ApiUrl
                .AppendPathSegments(ApiVersionV2, AggsString, GroupedString, LocaleString, locale, MarketString, market, date.ToString(DateFormat, CultureInfo.InvariantCulture))
                .SetQueryParamIfNotNull(nameof(unadjusted), unadjusted);                             
            
            return await GetResponse<GroupedDailyBarsResponse>(client, apiKey, url, cancellationToken);
        }

        private static async Task<T> GetResponse<T>(HttpClient client, string apiKey, Url url, CancellationToken cancellationToken) where T: class
        {
            try
            {
                url = url.SetQueryParam(ApiKeyString, apiKey, false);

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
