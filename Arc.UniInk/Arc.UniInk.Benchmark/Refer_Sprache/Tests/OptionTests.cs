namespace Sprache.Tests
{

    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class OptionTests
    {
        private readonly Parser<IOption<char>> ParserOptionalSelect = Parse.MatchChar('a').Optional().Select(o => o.Select(c => char.ToUpperInvariant(c)));

        private readonly Parser<IOption<string>> ParserOptionalSelectMany =
                from o1 in Parse.MatchChar('a').Optional()
                from o2 in Parse.MatchChar('b').Optional()
                select o1.SelectMany(c1 => o2.Select(c2 => $"{c2}{c1}"));

        private readonly Parser<IOption<string>> ParserOptionalLinq =
                from o1 in Parse.MatchChar('a').Optional()
                from o2 in Parse.MatchChar('b').Optional()
                select (from c1 in o1 from c2 in o2 select $"{c2}{c1}");

        private void AssertSome<T>(IOption<T> option, T expected) =>  ClassicAssert.True(option.IsDefined && option.Get().Equals(expected));

        [Test]
        public void TestSelect() => AssertParser.SucceedsWith(ParserOptionalSelect, "a", o => AssertSome(o, 'A'));

        [Test]
        public void TestSelectManySome() => AssertParser.SucceedsWith(ParserOptionalSelectMany, "ab", o => AssertSome(o, "ba"));

        [Test]
        public void TestSelectManyNone() => AssertParser.SucceedsWith(ParserOptionalSelectMany, "b", o => ClassicAssert.True(o.IsEmpty));

        [Test]
        public void TestSelectManyLinq() => AssertParser.SucceedsWith(ParserOptionalLinq, "ab", o => AssertSome(o, "ba"));
    }
}