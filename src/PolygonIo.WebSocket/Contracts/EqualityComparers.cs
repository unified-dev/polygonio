using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PolygonIo.WebSocket.Contracts
{
    internal class TradeComparer : EqualityComparer<Trade>
    {
        public override bool Equals([AllowNull] Trade x, [AllowNull] Trade y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] Trade obj) => obj.GetHashCode();
    }

    internal class QuoteComparer : EqualityComparer<Quote>
    {
        public override bool Equals([AllowNull] Quote x, [AllowNull] Quote y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] Quote obj) => obj.GetHashCode();
    }

    internal class AggregateComparer : EqualityComparer<TimeAggregate>
    {
        public override bool Equals([AllowNull] TimeAggregate x, [AllowNull] TimeAggregate y) => x.GetHashCode() == y.GetHashCode();

        public override int GetHashCode([DisallowNull] TimeAggregate obj) => obj.GetHashCode();
    }
}
