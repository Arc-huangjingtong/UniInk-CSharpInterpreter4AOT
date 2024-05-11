using System.Collections.Generic;
using System.Linq;


namespace Sprache.Tests
{

    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class ParseRefTests
    {
        private static Parser<string> ParserHash = Parse.MatchString("#").Text().Named("something");

        private static Parser<string> ParserIdentifier = (from id in Parse.MatchString("id").Text() select id).Named("identifier");

        private static Parser<string> Parser1UnderTest =
            (from _0 in Parse.Ref(() => ParserIdentifier)
             from _1 in Parse.Ref(() => ParserHash)
             from _2 in Parse.MatchString("_")
             select "alternative_1")
            .Or(from _0 in Parse.Ref(() => ParserIdentifier)
                from _1 in Parse.Ref(() => ParserHash)
                select "alternative_2")
            .Or(from _0 in ParserIdentifier select _0);

        private static Parser<string> Parser2UnderTest = 
            (from _0 in Parse.MatchString("a").Text()
             from _1 in Parse.Ref(() => Parser2UnderTest)
             select _0 + _1)
            .Or(from _0 in Parse.MatchString("b").Text()
                from _1 in Parse.Ref(() => Parser2UnderTest)
                select _0 + _1)
            .Or(from _0 in Parse.MatchString("0").Text() select _0);

        private static Parser<string> Parser3UnderTest =
            (from _0 in Parse.Ref(() => Parser3UnderTest)
             from _1 in Parse.MatchString("a").Text()
             select _0 + _1)
            .Or(from _0 in Parse.MatchString("b").Text()
                from _1 in Parse.Ref(() => Parser3UnderTest)
                select _0 + _1)
            .Or(from _0 in Parse.MatchString("0").Text() select _0);

        private static Parser<string> Parser4UnderTest =
            from _0 in Parse.Ref(() => Parser4UnderTest)
            select "simplest left recursion";

        private static Parser<string> Parser5UnderTest =
          (from _0 in Parse.MatchString("_").Text()
           from _1 in Parse.Ref(() => Parser5UnderTest)
           select _0 + _1)
          .Or(from _0 in Parse.MatchString("+").Text()
              from _1 in Parse.Ref(() => Parser5UnderTest)
              select _0 + _1)
          .Or(Parse.Return(""));

        [Test]
        public void MultipleRefs() => AssertParser.SucceedsWith(Parser1UnderTest, "id=1", o => ClassicAssert.AreEqual("id", o));

        [Test]
        public void RecursiveParserWithoutLeftRecursion() => AssertParser.SucceedsWith(Parser2UnderTest, "ababba0", o => ClassicAssert.AreEqual("ababba0", o));

        [Test]
        public void RecursiveParserWithLeftRecursion() => Assert.Throws<ParseException>(() => Parser3UnderTest.TryParse("b0"));

        [Test]
        public void SimplestLeftRecursion() => Assert.Throws<ParseException>(() => Parser4UnderTest.TryParse("test"));

        [Test]
        public void EmptyAlternative1() => AssertParser.SucceedsWith(Parser5UnderTest, "_+_+a", o => ClassicAssert.AreEqual("_+_+", o));

        [Test]
        public void Issue166()
        {
            var letterA = Parse.MatchChar('a');
            var letterReferenced = Parse.Ref(() => letterA);
            var someAlternative = letterReferenced.Or(letterReferenced);

            ClassicAssert.False(someAlternative.TryParse("b").WasSuccessful);
        }

        private static readonly Parser<IEnumerable<char>> ASeq =
            (from first in Parse.Ref(() => ASeq)
             from comma in Parse.MatchChar(',')
             from rest in Parse.MatchChar('a').Once()
             select first.Concat(rest))
            .Or(Parse.MatchChar('a').Once());

        [Test]
        public void DetectsLeftRecursion()
        {
            Assert.Throws<ParseException>(() => ASeq.TryParse("a,a,a"));
        }

        private static readonly Parser<IEnumerable<char>> ABSeq =
            (from first in Parse.Ref(() => BASeq)
             from rest in Parse.MatchChar('a').Once()
             select first.Concat(rest))
            .Or(Parse.MatchChar('a').Once());

        private static readonly Parser<IEnumerable<char>> BASeq =
            (from first in Parse.Ref(() => ABSeq)
             from rest in Parse.MatchChar('b').Once()
             select first.Concat(rest))
            .Or(Parse.MatchChar('b').Once());

        [Test]
        public void DetectsMutualLeftRecursion()
        {
            Assert.Throws<ParseException>(() => ABSeq.End().TryParse("baba"));
        }
    }
}
