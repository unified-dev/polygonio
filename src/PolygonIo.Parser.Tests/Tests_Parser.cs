using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace PolygonIo.Parser.Tests
{
    [TestClass]
    public class Tests_Parser
    {
        private ulong QuoteCount = 0;
        private ulong TradeCount = 0;

        [TestMethod]
        public void Test_Parse()
        {
            var filename = @"test.data";
            Assert.IsTrue(File.Exists(filename));

            var p = new Parser();
            p.OnQuote += (q) => this.QuoteCount++;
            p.OnTrade += (t) => this.TradeCount++;
            //p.OnBadLine += (string s) => Debug.WriteLine($"Bad Line : {s}");
            //p.OnStatus += (string s) => Debug.WriteLine($"Status: {s}");

            using (TextReader txt = new StreamReader(File.OpenRead(filename)))
            {
                var timer = Stopwatch.StartNew();

                p.Parse(txt); // Events Are Raised

                timer.Stop();
                Debug.WriteLine($"File ReadLine Duration: {timer.Elapsed.TotalSeconds}");
                Debug.WriteLine($"Number of Quotes: {this.QuoteCount:N0}");
                Debug.WriteLine($"Number of Trades: {this.TradeCount:N0}");
            }
        }
    }
}