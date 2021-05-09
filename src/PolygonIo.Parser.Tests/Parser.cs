using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PolygonIo.Parser.Tests
{
    public class Parser
    {
        public event Action<Quote> OnQuote;
        public event Action<Trade> OnTrade;
        public event Action<string> OnStatus;
        public event Action<string> OnUnknown;
        public event Action<string> OnBadLine;

        #region Version 2
        public void ParseV2(TextReader txt)
        {
            while (TryReadLine(txt, out string line))
            {
                if (!line.StartsWith("[{") || !line.EndsWith("}]"))
                {
                    OnBadLine?.Invoke(line);
                    continue;
                }

                int ix = 0;
                while (ix < line.Length && line[ix] == '[')
                {
                    ProcessArray(line.AsSpan(1), out int processedArrayContentsLength);

                    if (processedArrayContentsLength == -1)
                        break;

                    ix += processedArrayContentsLength;
                    ix += 1; // to skip closing ']'
                }
            }
        }
        private bool TryReadLine(TextReader txt, out string line)
        {
            line = txt.ReadLine();

            return string.IsNullOrWhiteSpace(line) ? false : true;
        }
        private void ProcessArray(ReadOnlySpan<char> elements, out int processedArrayContentsLength)
        {
            processedArrayContentsLength = -1;

            int ix = 0;
            while (ix < elements.Length && elements[ix] == '{')
            {
                ix++; // Skip opening '{'
                ProcessElement(elements.Slice(ix), out int processedElementLength);

                if (processedElementLength == -1)
                    break;

                ix += processedElementLength;
                ix += 1; // to skip closing '}'

                processedArrayContentsLength = ix;

                // Skip Separator
                if (ix + 1 < elements.Length && elements[ix] == ',')
                {
                    ix++;
                }
            }
        }
        private void ProcessElement(ReadOnlySpan<char> element, out int processedElementLength)
        {
            if (element.ToString().Contains("},{"))
                Debugger.Break();

            if (element[1] == 'e' && element[2] == 'v' &&
                element[0] == '"' && element[3] == '"')
            {
                switch (element[6])
                {
                    case 'Q':
                        Quote q = ProcessQuote(element, out processedElementLength);
                        OnQuote?.Invoke(q);
                        return;
                    case 'T':
                        Trade t = ProcessTrade(element, out processedElementLength);
                        OnTrade?.Invoke(t);
                        return;
                    case 's':
                        string status = "status";
                        if (element.Slice(6, status.Length).SequenceEqual(status))
                        {
                            OnStatus?.Invoke(element.ToString());
                            processedElementLength = element.IndexOf('}');
                            return;
                        }
                        break;
                }

            }
            OnUnknown?.Invoke(element.ToString());
            processedElementLength = element.IndexOf('}');
        }

        private Trade ProcessTrade(ReadOnlySpan<char> element, out int processedElementLength)
        {
            Trade t = new Trade();

            int ix = 0;
            bool isLast = false;
            while (!isLast)
            {
                ProcessKeyValue(element, ref ix, out isLast, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value);

                Trade.Assign(ref t, key, value);
            }
            processedElementLength = ix;
            return t;
        }

        private Quote ProcessQuote(ReadOnlySpan<char> element, out int processedElementLength)
        {
            Quote q = new Quote();

            int ix = 0;
            bool isLast = false;
            while (!isLast)
            {
                ProcessKeyValue(element, ref ix, out isLast, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value);

                Quote.Assign(ref q, key, value);
            }
            processedElementLength = ix;
            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessKeyValue(ReadOnlySpan<char> element, ref int ix, out bool isLast, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
        {
            ProcessValue(element.Slice(ix), out int keyLength, out bool _, out bool _);
            ix += 1; // keyIsWrapped ? 1 : 0; // always true for keys
            key = element.Slice(ix, keyLength);
            ix += keyLength;
            ix += 1; // keyIsWrapped ? 1 : 0; // always true for keys

            ix += 1; // to skip key-value separator ':'

            ProcessValue(element.Slice(ix), out int valueLength, out bool valueIsWrapped, out isLast); // Slice(ix) without skipping: TBD if quote or bracket or number
            ix += valueIsWrapped ? 1 : 0; // leading quote or array bracket
            value = element.Slice(ix, valueLength);
            ix += valueLength;
            ix += valueIsWrapped ? 1 : 0; // trailing quote or array bracket

            ix += isLast ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessValue(ReadOnlySpan<char> valueCandidate, out int valueLength, out bool valueIsWrapped, out bool isLast)
        {
            switch (valueCandidate[0])
            {
                case '"': // JSON String
                    ProcessQuotedValue('"', valueCandidate, out valueLength, out valueIsWrapped, out isLast);
                    return;
                case '[': // JSON Array
                    ProcessQuotedValue(']', valueCandidate, out valueLength, out valueIsWrapped, out isLast);
                    return;
                default: // Number
                    ProcessNumber(valueCandidate, out valueLength, out valueIsWrapped, out isLast);
                    return;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessNumber(ReadOnlySpan<char> valueCandidate, out int valueLength, out bool valueIsWrapped, out bool isLast)
        {
            /*
             *    "k"=n,   => isLast:false
             *    "k"=n}]  => isLast:true
             *    "k"=n},{ => isLast:true
             */

            valueIsWrapped = false;

            int ix = 0;
            for (; ix < valueCandidate.Length; ix++)
            {
                if (!char.IsNumber(valueCandidate[ix]) && valueCandidate[ix] != '.')
                    break;
            }
            valueLength = ix;

            if (ix == valueCandidate.Length)
            {
                // malformed or truncated record
                isLast = true;
                return;
            }

            if (ix < valueCandidate.Length)
            {
                if (valueCandidate[ix] == ',')
                {
                    isLast = false;
                    return;
                }

                if (valueCandidate[ix] == '}')
                {
                    isLast = true;
                    return;
                }
            }
            throw new InvalidOperationException();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProcessQuotedValue(char quote, ReadOnlySpan<char> valueCandidate, out int valueLength, out bool valueIsWrapped, out bool isLast)
        {
            valueIsWrapped = true;
            valueCandidate = valueCandidate.Slice(1); // Trim Leading Quote '"'
            valueLength = valueCandidate.IndexOf(quote);

            isLast = (valueCandidate.Length <= valueLength + 2 + 2); // BUG! Fails to account for quoted value at end of record
        }
        #endregion

        public void Parse(TextReader txt)
        {
            string line = null;
            while ((line = txt.ReadLine()) != null)
            {
                Thread.Sleep(100);

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
                    OnTrade?.Invoke(Trade.Parse(record));
                    break;
            }
        }
    }
}