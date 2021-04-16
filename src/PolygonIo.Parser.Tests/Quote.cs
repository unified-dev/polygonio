using System;

namespace PolygonIo.Parser.Tests
{
    public struct Quote
    {
        public int AskExchangeId { get; set; }
        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
        public int BidExchangeId { get; set; }
        public decimal BidSize { get; set; }
        //public QuoteCondition QuoteCondition { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public string Symbol;
        public decimal BidPrice;

        public static Quote Parse(ReadOnlySpan<char> line)
        {
            if (line[0] == '{' || line[^1] == '}')
                throw new InvalidOperationException();

            Quote q = new Quote();

            var data = line;
            while (data.Length > 0)
            {
                var lenKey = data.IndexOf(':') - 2;
                var key = data.Slice(1, lenKey);

                var ixValue = key.Length + 3; // two quotes and a colon
                var lenValue = 1;
                switch (data[ixValue])
                {
                    case '"':
                        ixValue += 1;
                        lenValue = data.Slice(ixValue).IndexOf('"');
                        break;
                    case '[':
                        ixValue += 1;
                        lenValue = data.Slice(ixValue).IndexOf(']');
                        break;
                    default:
                        var x = data.Slice(ixValue);
                        var ix = x.IndexOf(',');
                        lenValue = (ix == -1) ? x.Length : ix;
                        break;
                }
                var val = data.Slice(ixValue, lenValue);

                Assign(ref q, key, val);

                var ixNextSlice = ixValue + lenValue + 2;
                // ",
                // ],
                // EOL

                if (ixNextSlice < data.Length)
                {
                    data = data.Slice(ixNextSlice);
                }
                else
                {
                    break;
                }
            }

            return q;
        }

        private static void Assign(ref Quote q, ReadOnlySpan<char> k, ReadOnlySpan<char> v)
        {
            if (k.Length == 2 && k[0] == 'b' && k[1] == 'p')
            {
                q.BidPrice = decimal.Parse(v);
                return;
            }

            if (k.Length == 3 && k[0] == 's' && k[1] == 'y' && k[2] == 'm')
            {
                q.Symbol = new string(v);
                return;
            }
        }
    }
}