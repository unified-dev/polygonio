using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PolygonIo.WebSocket.Deserializers;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";

            var configuration = new ConfigurationBuilder()
                                        .AddUserSecrets<Program>()
                                        .Build();

            var polygonWebSocket = new PolygonWebsocket(
                                            configuration.GetSection("PolygonIo").GetValue<string>("ApiKey"),
                                            "wss://socket.polygon.io/stocks",
                                            60,
                                            loggerFactory);

            var cts = new CancellationTokenSource();

            var tickers = new[] { "SPY", "VXX", "MSFT", "TSLA" };

            /*
            await polygonWebSocket.StartAsync(
                                        tickers,
                                        cts.Token,
                                        (messages) =>
                                        {
                                           Console.WriteLine(JsonConvert.SerializeObject(messages, Formatting.Indented));
                                           return Task.CompletedTask;
                                        });
            */


            await polygonWebSocket.StartAsync(
                                        tickers,
                                        new ActionBlock<PolygonDataFrame>((data) =>
                                        {
                                            Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                                        }),
                                        cts.Token);

            Console.ReadKey();
            polygonWebSocket.StopAsync().Wait();

        }
    }
}
