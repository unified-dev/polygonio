using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CanProcessData()
        {
            var utf8JsonDeserializer = new Utf8JsonDeserializer(
                                                NullLogger<Utf8JsonDeserializer>.Instance,
                                                new EventFactory<Quote, Trade, TimeAggregate, Status>());

            var str = "[{ \"ev\":\"status\",\"status\":\"connected\",\"message\":\"Connected Successfully\"}]";

            IStatus s = null;
            utf8JsonDeserializer.Deserialize(
                                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(str)),
                                    (quote) => { },
                                    (trade) => { },
                                    (aggregate) => { },
                                    (aggregate) => { },
                                    (status) => { s = status; });


            Assert.IsNotEmpty(s.Message);
        }
    }
}