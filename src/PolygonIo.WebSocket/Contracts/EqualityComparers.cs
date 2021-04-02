using PolygonIo.WebSocket.Contracts;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PolygonIo.WebSocket.Contracts
{
    internal class TradeComparer : EqualityComparer<ITrade>
    {
        public override bool Equals([AllowNull] ITrade x, [AllowNull] ITrade y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] ITrade obj) => obj.GetHashCode();
    }

    internal class QuoteComparer : EqualityComparer<IQuote>
    {
        public override bool Equals([AllowNull] IQuote x, [AllowNull] IQuote y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] IQuote obj) => obj.GetHashCode();
    }

    internal class AggregateComparer : EqualityComparer<ITimeAggregate>
    {
        public override bool Equals([AllowNull] ITimeAggregate x, [AllowNull] ITimeAggregate y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] ITimeAggregate obj) => obj.GetHashCode();
    }
}
