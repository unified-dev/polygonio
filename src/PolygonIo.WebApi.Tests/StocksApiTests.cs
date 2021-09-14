using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NUnit.Framework;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.WebApi.Tests
{
    public class StocksApiTests
    {
        private string apiKey;

        [OneTimeSetUp]
        public void Setup()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<StocksApiTests>()
                .Build();

            this.apiKey = configuration.GetSection("ApiKeys")["PolygonIoApiKey"];
        }

        [Test]
        public async Task GetAllPolygonTickersV3Async()
        {
            var client = new HttpClient();
            var from = new LocalDate(2021, 01, 02);
            var to = new LocalDate(2021, 02, 02);
            var data = await client.GetPolygonAggregatesBarsV2(symbol: "VXX",  apiKey: this.apiKey, multiplier: 1, timespan: AggregateTimespan.Minute, from: from, to: to);
        }
    }
}