using System;

namespace PolygonIo.Parser.Tests
{
    public struct Trade
    {
        public int ExchangeId { get; set; }
        public int Tape { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        //public TradeCondition[] TradeConditions { get; set; }
        public string TradeId { get; set; }
        public decimal TradeSize { get; set; }

        public string Symbol;
        public decimal Price;

        public static Trade Parse(ReadOnlySpan<char> line)
        {
            if (line[0] == '{' || line[^1] == '}')
                throw new InvalidOperationException();

            Trade t = new Trade();

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

                Assign(ref t, key, val);

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
            return t;
        }

        private static void Assign(ref Trade t, ReadOnlySpan<char> k, ReadOnlySpan<char> v)
        {
            if (k.Length == 1 && k[0] == 'p')
            {
                t.Price = decimal.Parse(v);
                return;
            }

            if (k.Length == 3 && k[0] == 's' && k[1] == 'y' && k[2] == 'm')
            {
                t.Symbol = new string(v);
                return;
            }
        }
    }
}