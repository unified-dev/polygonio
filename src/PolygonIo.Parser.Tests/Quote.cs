using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PolygonIo.Parser.Tests
{
    public struct Quote
    {
        public int AskExchangeId;
        public decimal AskPrice;
        public decimal AskSize;
        public int BidExchangeId;
        public decimal BidSize;

        public string Symbol;
        public decimal BidPrice;
        public DateTimeOffset Timestamp;

        public static Quote Parse(ReadOnlySpan<char> element)
        {
            Debug.Assert(element[0] != '{' && element[element.Length - 1] != '}');

            Quote q = new Quote();
            
            var data = element;
            while (data.Length > 0)
            {
                var lenKey = data.IndexOf(':') - 2;
                var key = data.Slice(1, lenKey);

                var isWrappedValue = false;
                var ixValue = key.Length + 3; // two quotes and a colon
                var lenValue = 1;
                switch (data[ixValue])
                {
                    case '"':
                        isWrappedValue = true;
                        ixValue += 1;
                        lenValue = data.Slice(ixValue).IndexOf('"');
                        break;
                    case '[':
                        isWrappedValue = true;
                        ixValue += 1;
                        lenValue = data.Slice(ixValue).IndexOf(']');
                        break;
                    default:
                        isWrappedValue = false;
                        var x = data.Slice(ixValue);
                        var ix = x.IndexOf(',');
                        lenValue = (ix == -1) ? x.Length : ix;
                        break;
                }
                var val = data.Slice(ixValue, lenValue);

                Assign(ref q, key, val);

                var ixNextSlice = isWrappedValue ? ixValue + lenValue + 2 : ixValue + lenValue;
                // ",
                // ],
                // EOL

                if (ixNextSlice < data.Length)
                {
                    ixNextSlice += data[ixNextSlice] == ',' ? 1 : 0;
                    data = data.Slice(ixNextSlice);
                }
                else
                {
                    break;
                }
            }

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assign(ref Quote q, ReadOnlySpan<char> k, ReadOnlySpan<char> v)
        {
            if (k.Length == 2 && k[0] == 'b' && k[1] == 'p')
            {
                q.BidPrice = decimal.Parse(v);
                return;
            }
            
            if (k.Length == 1 && k[0] == 't')
            {
                q.Timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(ulong.Parse(v));
            }

            if (k.Length == 3 && k[0] == 's' && k[1] == 'y' && k[2] == 'm')
            {
                q.Symbol = new string(v);
                return;
            }
        }
        public override string ToString()
        {
            return $"{Symbol} bp={BidPrice}";
        }
    }
}