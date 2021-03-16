using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
                                            new EventFactory<Quote, Trade, TimeAggregate, Status>(),
                                            NullLoggerFactory.Instance);

            var cts = new CancellationTokenSource();

            await polygonWebSocket.StartAsync(
                                new[] { "TSLA" },
                                cts.Token,
                                (messages) =>
                                {
                                    Console.WriteLine(JsonConvert.SerializeObject(messages, Formatting.Indented));
                                    return Task.CompletedTask;
                                });

            Console.ReadKey();
            polygonWebSocket.StopAsync().Wait();
        }
    }
}
