using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket.Example
{
    class Program
    {
        static ILogger<Program> logger;
        
        static void Main(string[] args)
        {
            try
            {
                using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
                var loggerFactory = new LoggerFactory().AddSerilog(log);
                logger = loggerFactory.CreateLogger<Program>();

                var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

                var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                    devEnvironmentVariable.ToLower() == "development";

                var configuration = new ConfigurationBuilder()
                                            .AddUserSecrets<Program>()
                                            .Build();

                using var polygonWebSocket = new PolygonWebsocket(
                                                configuration.GetSection("PolygonIo").GetValue<string>("ApiKey"),
                                                "wss://socket.polygon.io/stocks",
                                                60,
                                                loggerFactory);

                var cts = new CancellationTokenSource();

                var tickers = new[] { "SPY", "QQQ", "IWM", "VXX", "MSFT", "TSLA", "RBLX", "GME", "AMC", "AMZN", "AMD", "INTC", "AAL", "UAL" };

                polygonWebSocket.Start(
                                    tickers,
                                    (trade) => PrintAndReturnTask(trade),
                                    (quote) => PrintAndReturnTask(quote),
                                    (aggregate) => PrintAndReturnTask(aggregate),
                                    (status) => PrintAndReturnTask(status));

                Console.ReadKey();

                polygonWebSocket.Stop();
            }
            catch(Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        static Task PrintAndReturnTask(object obj)
        {
            logger.LogInformation($"{obj.GetType()}\n{JsonConvert.SerializeObject(obj, Formatting.Indented)}");
            return Task.CompletedTask;
        }
    }
}
