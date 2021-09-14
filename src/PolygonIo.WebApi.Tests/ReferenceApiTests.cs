using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PolygonIo.WebApi.Contracts;

namespace PolygonIo.WebApi.Tests
{
    public class ReferenceApiTests
    {
        private string apiKey;

        [OneTimeSetUp]
        public void Setup()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ReferenceApiTests>()
                .Build();

            this.apiKey = configuration.GetSection("ApiKeys")["PolygonIoApiKey"];
        }

        [Test]
        public async Task GetAllPolygonTickersV3Async()
        {
            var cts = new CancellationTokenSource();

            var client = new HttpClient();
            var data = await client.GetAllPolygonTickersV3(cancellationToken: cts.Token, apiKey: this.apiKey, market: Market.Stocks);
        }
    }
}