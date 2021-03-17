using PolygonIo.WebSocket.Contracts;
using System;
using System.Collections.Generic;

namespace PolygonIo.WebSocket.Deserializers
{
    public class DeserializedData
    {
        public DeserializedData(IEnumerable<ITrade> trades, IEnumerable<IQuote> quotes, IEnumerable<IStatus> status, IEnumerable<ITimeAggregate> perSecondAggregates, IEnumerable<ITimeAggregate> perMinuteAggregates)
        {
            Trades = trades ?? throw new ArgumentNullException(nameof(trades));
            Quotes = quotes ?? throw new ArgumentNullException(nameof(quotes));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            PerSecondAggregates = perSecondAggregates ?? throw new ArgumentNullException(nameof(perSecondAggregates));
            PerMinuteAggregates = perMinuteAggregates ?? throw new ArgumentNullException(nameof(perMinuteAggregates));
        }

        public IEnumerable<ITrade> Trades { get; }
        public IEnumerable<IQuote> Quotes { get; }
        public IEnumerable<IStatus> Status { get; }
        public IEnumerable<ITimeAggregate> PerSecondAggregates { get; }
        public IEnumerable<ITimeAggregate> PerMinuteAggregates { get; }
    }
}
