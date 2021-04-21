using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace PolygonIo.Parser.Tests
{
    [TestClass]
    public class Tests_Parser
    {
        private const string filename = @"test.data";

        private ulong StatusCount = 0;
        private ulong QuoteCount = 0;
        private ulong TradeCount = 0;
        private ulong UnknownElementCount = 0;

        [TestMethod]
        public void Test_Parse()
        {
            Assert.IsTrue(File.Exists(filename), "File Not Found!");

            var p = new Parser();
            p.OnStatus += (s) => this.StatusCount++;
            p.OnQuote += (q) => { this.QuoteCount++; AssertFieldsNotEmpty(q); };
            p.OnTrade += (t) => { this.TradeCount++; AssertFieldsNotEmpty(t); };
            p.OnBadLine += (string s) => Debug.WriteLine($"Bad Line : {s}");

            using (TextReader txt = new StreamReader(File.OpenRead(filename)))
            {
                var timer = Stopwatch.StartNew();

                p.Parse(txt); // Events Are Raised

                timer.Stop();
                Debug.WriteLine($"File ReadLine + Parsing Duration: {timer.Elapsed.TotalSeconds}");
                Debug.WriteLine($"Number of Status Messages: {this.StatusCount:N0}");
                Debug.WriteLine($"Number of Quotes: {this.QuoteCount:N0}");
                Debug.WriteLine($"Number of Trades: {this.TradeCount:N0}");
            }

            //Assert.AreEqual(3_337_759UL, QuoteCount);
            //Assert.AreEqual(857_082UL, TradeCount);
        }

        [TestMethod]
        public void Test_ParseV2()
        {
            Assert.IsTrue(File.Exists(filename), "File Not Found!");

            var p = new Parser();
            p.OnStatus += (s) => this.StatusCount++;
            p.OnQuote += (q) => { this.QuoteCount++; AssertFieldsNotEmpty(q); };
            p.OnTrade += (t) => { this.TradeCount++; AssertFieldsNotEmpty(t); };
            p.OnBadLine += (string s) => Debug.WriteLine($"Bad Line : {s}");
            p.OnUnknown += (s) => this.UnknownElementCount++;

            using (TextReader txt = new StreamReader(File.OpenRead(filename)))
            {
                var timer = Stopwatch.StartNew();

                p.ParseV2(txt); // Events Are Raised

                timer.Stop();
                Debug.WriteLine($"File ReadLine + Parsing Duration: {timer.Elapsed.TotalSeconds}");
                Debug.WriteLine($"Number of Status Messages: {this.StatusCount:N0}");
                Debug.WriteLine($"Number of Quotes: {this.QuoteCount:N0}");
                Debug.WriteLine($"Number of Trades: {this.TradeCount:N0}");
                Debug.WriteLine($"Number of Unknown Elements: {this.UnknownElementCount:N0}");
            }

            //Assert.AreEqual(3_337_759UL, QuoteCount);
            //Assert.AreEqual(857_082UL, TradeCount);
        }

        private static void AssertFieldsNotEmpty(Quote q)
        {
            var conditions = new System.Func<Quote, (bool, string)>[]
            {
                (q) => { return (!string.IsNullOrWhiteSpace(q.Symbol), $"Missing Quote Symbol!"); },
                (q) => { return (q.BidPrice != 0, $"Zero Quote BidPrice!"); },
            };

            foreach (var check in conditions)
            {
                var outcome = check(q);
                var success = outcome.Item1;
                if (!success)
                {
                    string msg = outcome.Item2;
                    Debugger.Break();
                    Assert.Fail(msg);
                }
            }
        }
        private static void AssertFieldsNotEmpty(Trade t)
        {
            var conditions = new System.Func<Trade, (bool, string)>[]
            {
                (t) => { return (!string.IsNullOrWhiteSpace(t.Symbol), $"Missing Trade Symbol!"); },
                (t) => { return (t.Price != 0, $"Zero Trade BidPrice!"); },
            };

            foreach (var check in conditions)
            {
                var outcome = check(t);
                var success = outcome.Item1;
                if (!success)
                {
                    string msg = outcome.Item2;
                    Debugger.Break();
                    Assert.Fail(msg);
                }
            }
        }
   }
}