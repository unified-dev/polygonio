using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PolygonIo.WebSocket.Deserializers;
using Serilog;
using Serilog.Debugging;
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
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            var logger = loggerFactory.CreateLogger<Program>();

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

            polygonWebSocket.Start(
                                tickers,
                                new ActionBlock<DeserializedData>((data) =>
                                {
                                    logger.LogInformation(JsonConvert.SerializeObject(data, Formatting.Indented));
                                }));

            Console.ReadKey();
            polygonWebSocket.Dispose();
            Console.ReadKey();
        }
    }
}
