namespace Sprache.Tests.Scenarios
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class CsvTests
    {
        private static readonly Parser<char> CellSeparator = Parse.MatchChar(',');

        private static readonly Parser<char> QuotedCellDelimiter = Parse.MatchChar('"');

        private static readonly Parser<char> QuoteEscape = Parse.MatchChar('"');

        private static Parser<T> Escaped <T>(Parser<T> following)
        {
            return from escape in QuoteEscape from f in following select f;
        }

        private static readonly Parser<char> QuotedCellContent = Parse.AnyChar.Except(QuotedCellDelimiter).Or(Escaped(QuotedCellDelimiter));

        private static readonly Parser<char> LiteralCellContent = Parse.AnyChar.Except(CellSeparator).Except(Parse.String(Environment.NewLine));

        private static readonly Parser<string> QuotedCell = from open in QuotedCellDelimiter from content in QuotedCellContent.Many().Text() from end in QuotedCellDelimiter select content;

        private static readonly Parser<string> NewLine = Parse.String(Environment.NewLine).Text();

        private static readonly Parser<string> RecordTerminator = Parse.Return("").End().XOr(NewLine.End()).Or(NewLine);

        private static readonly Parser<string> Cell = QuotedCell.XOr(LiteralCellContent.XMany().Text());

        private static readonly Parser<IEnumerable<string>> Record = from leading in Cell from rest in CellSeparator.Then(_ => Cell).Many() from terminator in RecordTerminator select Cons(leading, rest);

        private static readonly Parser<IEnumerable<IEnumerable<string>>> Csv = Record.XMany().End();

        private static IEnumerable<T> Cons <T>(T head, IEnumerable<T> rest)
        {
            yield return head;

            foreach (var item in rest)
            {
                yield return item;
            }
        }

        [Test]
        public void ParsesSimpleList()
        {
            var input = "a,b";
            var r     = Csv.Parse(input);

            //Assert.Single(r);

            var l1 = r.First().ToArray();
            ClassicAssert.AreEqual(2,   l1.Length);
            ClassicAssert.AreEqual("a", l1[0]);
            ClassicAssert.AreEqual("b", l1[1]);
        }

        [Test]
        public void ParsesListWithEmptyEnding()
        {
            var input = "a,b,";
            var r     = Csv.Parse(input);
            //Assert.Single(r);

            var l1 = r.First().ToArray();
            ClassicAssert.AreEqual(3,   l1.Length);
            ClassicAssert.AreEqual("a", l1[0]);
            ClassicAssert.AreEqual("b", l1[1]);
            ClassicAssert.AreEqual("",  l1[2]);
        }

        [Test]
        public void ParsesListWithNewlineEnding()
        {
            var input = "a,b," + Environment.NewLine;
            var r     = Csv.Parse(input);
            //Assert.Single(r);

            var l1 = r.First().ToArray();
            ClassicAssert.AreEqual(3,   l1.Length);
            ClassicAssert.AreEqual("a", l1[0]);
            ClassicAssert.AreEqual("b", l1[1]);
            ClassicAssert.AreEqual("",  l1[2]);
        }

        [Test]
        public void ParsesLines()
        {
            var input = "a,b,c" + Environment.NewLine + "d,e,f";
            var r     = Csv.Parse(input);
            ClassicAssert.AreEqual(2, r.Count());

            var l1 = r.First().ToArray();
            ClassicAssert.AreEqual(3,   l1.Length);
            ClassicAssert.AreEqual("a", l1[0]);
            ClassicAssert.AreEqual("b", l1[1]);
            ClassicAssert.AreEqual("c", l1[2]);

            var l2 = r.Skip(1).First().ToArray();
            ClassicAssert.AreEqual(3,   l2.Length);
            ClassicAssert.AreEqual("d", l2[0]);
            ClassicAssert.AreEqual("e", l2[1]);
            ClassicAssert.AreEqual("f", l2[2]);
        }

        [Test]
        public void IgnoresTrailingNewline()
        {
            var input = "a,b,c" + Environment.NewLine + "d,e,f" + Environment.NewLine;
            var r     = Csv.Parse(input);
            ClassicAssert.AreEqual(2, r.Count());
        }

        [Test]
        public void IgnoresCommasInQuotedCells()
        {
            var input = "a,\"b,c\"";
            var r     = Csv.Parse(input);
            ClassicAssert.AreEqual(2, r.First().Count());
        }

        [Test]
        public void RecognisesDoubledQuotesAsSingleLiteral()
        {
            var input = "a,\"b\"\"c\"";
            var r     = Csv.Parse(input);
            ClassicAssert.AreEqual("b\"c", r.First().ToArray()[1]);
        }

        [Test]
        public void AllowsNewLinesInQuotedCells()
        {
            var input = "a,b,\"c" + Environment.NewLine + "d\"";
            var r     = Csv.Parse(input);
            //Assert.Single(r);
        }

        [Test]
        public void IgnoresEmbeddedQuotesWhenNotFirstCharacter()
        {
            var input = "a\"b";
            var r     = Csv.Parse(input);
            ClassicAssert.AreEqual("a\"b", r.First().First());
        }
    }

}