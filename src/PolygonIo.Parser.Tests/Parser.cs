using System;
using System.IO;

namespace PolygonIo.Parser.Tests
{
    public class Parser
    {
        public event Action<Quote> OnQuote;
        public event Action<Trade> OnTrade;
        public event Action<string> OnStatus;
        public event Action<string> OnBadLine;

        public void Parse(TextReader txt)
        {
            string line = null;
            while ((line = txt.ReadLine()) != null)
            {
                if (!line.StartsWith("[{") || !line.EndsWith("}]"))
                {
                    OnBadLine?.Invoke(line);
                    continue;
                }

                if (line.StartsWith(@"[{""ev"":""status"",""status"":"))
                {
                    OnStatus?.Invoke(line);
                    continue;
                }

                var arrayWithoutSquareBrackets = line.AsSpan(1, line.Length - 1 - 1); // Trim '[' & ']'

                var array = arrayWithoutSquareBrackets;
                do
                {
                    var ixElementStart = array.IndexOf('{') + 1;
                    var ixElementEnd = array.IndexOf('}');
                    var lengthOfElement = ixElementEnd - ixElementStart;

                    var element = array.Slice(ixElementStart, lengthOfElement);

                    Process(element);

                    if (ixElementEnd == array.Length - 1)
                    {
                        break; // no elements remaining
                    }
                    else // Remove element from head of array
                    {
                        array = array.Slice(ixElementEnd + "},".Length);
                    }
                } while (array.Length > 0); // TODO
            }
        }
        private void Process(ReadOnlySpan<char> record)
        {
            switch (record[6])
            {
                case 'Q':
                    OnQuote?.Invoke(Quote.Parse(record));
                    break;
                case 'T':
                    OnTrade.Invoke(Trade.Parse(record));
                    break;
            }
        }
    }
}