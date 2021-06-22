using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.WebApi.Tests
{
    public class TickerTests
    {
        private string apiKey;

        [OneTimeSetUp]
        public async Task Setup()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<TickerTests>()
                .Build();

            this.apiKey = configuration.GetSection("ApiKeys")["PolygonIoApiKey"];
        }

        [Test]
        public async Task LoadAllData()
        {
            var cts = new CancellationTokenSource();

            var client = new HttpClient();
            var data = await client.GetAllPolygonTickersV3Async(cancellationToken: cts.Token, apiKey: this.apiKey, market: Market.Stocks);
        }
    }
}