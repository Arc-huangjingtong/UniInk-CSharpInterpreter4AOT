namespace Sprache.Tests
{

    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class ParseTests
    {
        [Test]
        public void Parser_OfChar_AcceptsThatChar()
        {
            AssertParser.SucceedsWithOne(Parse.MatchChar('a').Once(), "a", 'a');
        }

        [Test]
        public void Parser_OfChar_AcceptsOnlyOneChar()
        {
            AssertParser.SucceedsWithOne(Parse.MatchChar('a').Once(), "aaa", 'a');
        }

        [Test]
        public void Parser_OfChar_DoesNotAcceptNonMatchingChar()
        {
            AssertParser.FailsAt(Parse.MatchChar('a').Once(), "b", 0);
        }

        [Test]
        public void Parser_OfChar_DoesNotAcceptEmptyInput()
        {
            AssertParser.Fails(Parse.MatchChar('a').Once(), "");
        }

        [Test]
        public void Parser_OfChars_AcceptsAnyOfThoseChars()
        {
            var parser = Parse.MatchChars('a', 'b', 'c').Once();
            AssertParser.SucceedsWithOne(parser, "a", 'a');
            AssertParser.SucceedsWithOne(parser, "b", 'b');
            AssertParser.SucceedsWithOne(parser, "c", 'c');
        }

        [Test]
        public void Parser_OfChars_UsingString_AcceptsAnyOfThoseChars()
        {
            var parser = Parse.MatchChars("abc").Once();
            AssertParser.SucceedsWithOne(parser, "a", 'a');
            AssertParser.SucceedsWithOne(parser, "b", 'b');
            AssertParser.SucceedsWithOne(parser, "c", 'c');
        }

        [Test]
        public void Parser_OfManyChars_AcceptsEmptyInput()
        {
            AssertParser.SucceedsWithAll(Parse.MatchChar('a').Many(), "");
        }

        [Test]
        public void Parser_OfManyChars_AcceptsManyChars()
        {
            AssertParser.SucceedsWithAll(Parse.MatchChar('a').Many(), "aaa");
        }

        [Test]
        public void Parser_OfAtLeastOneChar_DoesNotAcceptEmptyInput()
        {
            AssertParser.Fails(Parse.MatchChar('a').AtLeastOnce(), "");
        }

        [Test]
        public void Parser_OfAtLeastOneChar_AcceptsOneChar()
        {
            AssertParser.SucceedsWithAll(Parse.MatchChar('a').AtLeastOnce(), "a");
        }

        [Test]
        public void Parser_OfAtLeastOneChar_AcceptsManyChars()
        {
            AssertParser.SucceedsWithAll(Parse.MatchChar('a').AtLeastOnce(), "aaa");
        }

        [Test]
        public void ConcatenatingParsers_ConcatenatesResults()
        {
            var p = Parse.MatchChar('a').Once().Then(a => Parse.MatchChar('b').Once().Select(b => a.Concat(b)));
            AssertParser.SucceedsWithAll(p, "ab");
        }

        [Test]
        public void ReturningValue_DoesNotAdvanceInput()
        {
            var p = Parse.Return(1);
            AssertParser.SucceedsWith(p, "abc", n => ClassicAssert.AreEqual(1, n));
        }

        [Test]
        public void ReturningValue_ReturnsValueAsResult()
        {
            var p = Parse.Return(1);
            var r = (Result<int>)p.TryParse("abc");
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void CanSpecifyParsersUsingQueryComprehensions()
        {
            var p = from a in Parse.MatchChar('a').Once() from bs in Parse.MatchChar('b').Many() from cs in Parse.MatchChar('c').AtLeastOnce() select a.Concat(bs).Concat(cs);

            AssertParser.SucceedsWithAll(p, "abbbc");
        }

        [Test]
        public void WhenFirstOptionSucceedsButConsumesNothing_SecondOptionTried()
        {
            var p = Parse.MatchChar('a').Many().XOr(Parse.MatchChar('b').Many());
            AssertParser.SucceedsWithAll(p, "bbb");
        }

        [Test]
        public void WithXOr_WhenFirstOptionFailsAndConsumesInput_SecondOptionNotTried()
        {
            var first  = Parse.MatchChar('a').Once().Concat(Parse.MatchChar('b').Once());
            var second = Parse.MatchChar('a').Once();
            var p      = first.XOr(second);
            AssertParser.FailsAt(p, "a", 1);
        }

        [Test]
        public void WithOr_WhenFirstOptionFailsAndConsumesInput_SecondOptionTried()
        {
            var first  = Parse.MatchChar('a').Once().Concat(Parse.MatchChar('b').Once());
            var second = Parse.MatchChar('a').Once();
            var p      = first.Or(second);
            AssertParser.SucceedsWithAll(p, "a");
        }

        [Test]
        public void ParsesString_AsSequenceOfChars()
        {
            var p = Parse.String("abc");
            AssertParser.SucceedsWithAll(p, "abc");
        }

        [Test]
        public void WithMany_WhenLastElementFails_FailureReportedAtLastElement()
        {
            var ab = from a in Parse.MatchChar('a') from b in Parse.MatchChar('b') select "ab";

            var p = ab.Many().End();

            AssertParser.FailsAt(p, "ababaf", 4);
        }

        [Test]
        public void WithXMany_WhenLastElementFails_FailureReportedAtLastElement()
        {
            var ab = from a in Parse.MatchChar('a') from b in Parse.MatchChar('b') select "ab";

            var p = ab.XMany().End();

            AssertParser.FailsAt(p, "ababaf", 5);
        }

        [Test]
        public void ExceptStopsConsumingInputWhenExclusionParsed()
        {
            var exceptAa = Parse.AnyChar.Except(Parse.String("aa")).Many().Text();
            AssertParser.SucceedsWith(exceptAa, "abcaab", r => ClassicAssert.AreEqual("abc", r));
        }

        [Test]
        public void UntilProceedsUntilTheStopConditionIsMetAndReturnsAllButEnd()
        {
            var untilAa = Parse.AnyChar.Until(Parse.String("aa")).Text();
            var r       = untilAa.TryParse("abcaab");
            //Assert.IsType<Result<string>>(r);
            var s = (Result<string>)r;
            ClassicAssert.AreEqual("abc", s.Value);
            ClassicAssert.AreEqual(5,     s.Remainder.Position);
        }

        [Test]
        public void OptionalParserConsumesInputOnSuccessfulMatch()
        {
            var optAbc = Parse.String("abc").Text().Optional();
            var r      = optAbc.TryParse("abcd");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Value.IsDefined);
            ClassicAssert.AreEqual(3,     r.Remainder.Position);
            ClassicAssert.AreEqual("abc", r.Value.Get());
        }

        [Test]
        public void OptionalParserDoesNotConsumeInputOnFailedMatch()
        {
            var optAbc = Parse.String("abc").Text().Optional();
            var r      = optAbc.TryParse("d");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Value.IsEmpty);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void XOptionalParserConsumesInputOnSuccessfulMatch()
        {
            var optAbc = Parse.String("abc").Text().XOptional();
            var r      = optAbc.TryParse("abcd");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Value.IsDefined);
            ClassicAssert.AreEqual(3,     r.Remainder.Position);
            ClassicAssert.AreEqual("abc", r.Value.Get());
        }

        [Test]
        public void XOptionalParserDoesNotConsumeInputOnFailedMatch()
        {
            var optAbc = Parse.String("abc").Text().XOptional();
            var r      = optAbc.TryParse("d");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Value.IsEmpty);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void XOptionalParserFailsOnPartialMatch()
        {
            var optAbc = Parse.String("abc").Text().XOptional();
            AssertParser.FailsAt(optAbc, "abd", 2);
            AssertParser.FailsAt(optAbc, "aa",  1);
        }

        [Test]
        public void RegexParserConsumesInputOnSuccessfulMatch()
        {
            var digits = Parse.Regex(@"\d+");
            var r      = digits.TryParse("123d");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual("123", r.Value);
            ClassicAssert.AreEqual(3,     r.Remainder.Position);
        }

        [Test]
        public void RegexParserDoesNotConsumeInputOnFailedMatch()
        {
            var digits = Parse.Regex(@"\d+");
            var r      = digits.TryParse("d123");
            ClassicAssert.False(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void RegexMatchParserConsumesInputOnSuccessfulMatch()
        {
            var digits = Parse.RegexMatch(@"\d(\d*)");
            var r      = digits.TryParse("123d");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual("123", r.Value.Value);
            ClassicAssert.AreEqual("23",  r.Value.Groups[1].Value);
            ClassicAssert.AreEqual(3,     r.Remainder.Position);
        }

        [Test]
        public void RegexMatchParserDoesNotConsumeInputOnFailedMatch()
        {
            var digits = Parse.RegexMatch(@"\d+");
            var r      = digits.TryParse("d123");
            ClassicAssert.False(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void PositionedParser()
        {
            var pos = (from s in Parse.String("winter").Text() select new PosAwareStr { Value = s }).Positioned();
            var r   = pos.TryParse("winter");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Value.Pos.Pos);
            ClassicAssert.AreEqual(6, r.Value.Length);
        }

        [Test]
        public void XAtLeastOnceParser_WhenLastElementFails_FailureReportedAtLastElement()
        {
            var ab = Parse.String("ab").Text();
            var p  = ab.XAtLeastOnce().End();
            AssertParser.FailsAt(p, "ababaf", 5);
        }

        [Test]
        public void XAtLeastOnceParser_WhenFirstElementFails_FailureReportedAtFirstElement()
        {
            var ab = Parse.String("ab").Text();
            var p  = ab.XAtLeastOnce().End();
            AssertParser.FailsAt(p, "d", 0);
        }

        [Test]
        public void NotParserConsumesNoInputOnFailure()
        {
            var notAb = Parse.String("ab").Text().Not();
            AssertParser.FailsAt(notAb, "abc", 0);
        }

        [Test]
        public void NotParserConsumesNoInputOnSuccess()
        {
            var notAb = Parse.String("ab").Text().Not();
            var r     = notAb.TryParse("d");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void IgnoreCaseParser()
        {
            var ab = Parse.MatchCharIgnoreCase("ab").Text();
            AssertParser.SucceedsWith(ab, "Ab", m => ClassicAssert.AreEqual("Ab", m));
        }

        [Test]
        public void RepeatParserConsumeInputOnSuccessfulMatch()
        {
            var repeated = Parse.MatchChar('a').Repeat(3);
            var r        = repeated.TryParse("aaabbb");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(3, r.Remainder.Position);
        }

        [Test]
        public void RepeatParserDoesntConsumeInputOnFailedMatch()
        {
            var repeated = Parse.MatchChar('a').Repeat(3);
            var r        = repeated.TryParse("bbbaaa");
            ClassicAssert.True(!r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void RepeatParserCanParseWithCountOfZero()
        {
            var repeated = Parse.MatchChar('a').Repeat(0);
            var r        = repeated.TryParse("bbb");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void RepeatParserCanParseAMinimumNumberOfValues()
        {
            var repeated = Parse.MatchChar('a').Repeat(4, 5);

            // Test failure.
            var r = repeated.TryParse("aaa");
            ClassicAssert.False(r.WasSuccessful);
            ClassicAssert.AreEqual(0, r.Remainder.Position);

            // Test success.
            r = repeated.TryParse("aaaa");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(4, r.Remainder.Position);
        }

        [Test]
        public void RepeatParserCanParseAMaximumNumberOfValues()
        {
            var repeated = Parse.MatchChar('a').Repeat(4, 5);

            var r = repeated.TryParse("aaaa");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(4, r.Remainder.Position);

            r = repeated.TryParse("aaaaa");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(5, r.Remainder.Position);

            r = repeated.TryParse("aaaaaa");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual(5, r.Remainder.Position);
        }

        [Test]
        public void RepeatParserErrorMessagesAreReadable()
        {
            var repeated = Parse.MatchChar('a').Repeat(4, 5);

            var expectedMessage        = "Parsing failure: Unexpected 'end of input'; expected 'a' between 4 and 5 times, but found 3";
            var expectedColumnPosition = 1;

            try
            {
                var r = repeated.Parse("aaa");
            }
            catch (ParseException ex)
            {
                // ClassicAssert.StartsWith(expectedMessage, ex.Message);
                ClassicAssert.AreEqual(expectedColumnPosition, ex.Position.Column);
            }
        }

        [Test]
        public void RepeatExactlyParserErrorMessagesAreReadable()
        {
            var repeated = Parse.MatchChar('a').Repeat(4);

            var expectedMessage        = "Parsing failure: Unexpected 'end of input'; expected 'a' 4 times, but found 3";
            var expectedColumnPosition = 1;

            try
            {
                var r = repeated.Parse("aaa");
            }
            catch (ParseException ex)
            {
                //Assert.StartsWith(expectedMessage, ex.Message);
                ClassicAssert.AreEqual(expectedColumnPosition, ex.Position.Column);
            }
        }

        [Test]
        public void RepeatParseWithOnlyMinimum()
        {
            var repeated = Parse.MatchChar('a').Repeat(4, null);


            ClassicAssert.AreEqual(4,  repeated.TryParse("aaaa").Remainder.Position);
            ClassicAssert.AreEqual(7,  repeated.TryParse("aaaaaaa").Remainder.Position);
            ClassicAssert.AreEqual(10, repeated.TryParse("aaaaaaaaaa").Remainder.Position);

            try
            {
                repeated.Parse("aaa");
            }
            catch (ParseException ex)
            {
                //Assert.StartsWith("Parsing failure: Unexpected 'end of input'; expected 'a' minimum 4 times, but found 3", ex.Message);
            }
        }

        [Test]
        public void RepeatParseWithOnlyMaximum()
        {
            var repeated = Parse.MatchChar('a').Repeat(null, 6);

            ClassicAssert.AreEqual(4, repeated.TryParse("aaaa").Remainder.Position);
            ClassicAssert.AreEqual(6, repeated.TryParse("aaaaaa").Remainder.Position);
            ClassicAssert.AreEqual(6, repeated.TryParse("aaaaaaaaaa").Remainder.Position);
            ClassicAssert.AreEqual(0, repeated.TryParse("").Remainder.Position);
        }

        [Test]
        public void CanParseSequence()
        {
            var sequence = Parse.MatchChar('a').DelimitedBy(Parse.MatchChar(','));
            var r        = sequence.TryParse("a,a,a");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Remainder.AtEnd);
        }

        [Test]
        public void DelimitedWithMinimumAndMaximum()
        {
            var sequence = Parse.MatchChar('a').DelimitedBy(Parse.MatchChar(','), 3, 4);
            ClassicAssert.AreEqual(3, sequence.TryParse("a,a,a").Value.Count());
            ClassicAssert.AreEqual(4, sequence.TryParse("a,a,a,a").Value.Count());
            ClassicAssert.AreEqual(4, sequence.TryParse("a,a,a,a,a").Value.Count());
            Assert.Throws<ParseException>(() => sequence.Parse("a,a"));
        }

        [Test]
        public void DelimitedWithMinimum()
        {
            var sequence = Parse.MatchChar('a').DelimitedBy(Parse.MatchChar(','), 3, null);
            ClassicAssert.AreEqual(3, sequence.TryParse("a,a,a").Value.Count());
            ClassicAssert.AreEqual(4, sequence.TryParse("a,a,a,a").Value.Count());
            ClassicAssert.AreEqual(5, sequence.TryParse("a,a,a,a,a").Value.Count());
            Assert.Throws<ParseException>(() => sequence.Parse("a,a"));
        }

        [Test]
        public void DelimitedWithMaximum()
        {
            var sequence = Parse.MatchChar('a').DelimitedBy(Parse.MatchChar(','), null, 4);
            //Assert.Single(sequence.TryParse("a").Value);
            ClassicAssert.AreEqual(3, sequence.TryParse("a,a,a").Value.Count());
            ClassicAssert.AreEqual(4, sequence.TryParse("a,a,a,a").Value.Count());
            ClassicAssert.AreEqual(4, sequence.TryParse("a,a,a,a,a").Value.Count());
        }

        [Test]
        public void FailGracefullyOnSequence()
        {
            var sequence = Parse.MatchChar('a').XDelimitedBy(Parse.MatchChar(','));
            AssertParser.FailsWith(sequence, "a,a,b", _ =>
            {
                // Assert.Contains("unexpected 'b'", result.Message);
                //Assert.Contains("a",              result.Expectations);
            });
        }

        [Test]
        public void CanParseContained()
        {
            var parser = Parse.MatchChar('a').Contained(Parse.MatchChar('['), Parse.MatchChar(']'));
            var r      = parser.TryParse("[a]");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.True(r.Remainder.AtEnd);
        }

        [Test]
        public void TextSpanParserReturnsTheSpanOfTheParsedValue()
        {
            var parser = from leading in Parse.WhiteSpace.Many() from span in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit).Span() from trailing in Parse.WhiteSpace.Many() select span;

            var r = parser.TryParse("  Hello!");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.False(r.Remainder.AtEnd);

            var id = r.Value;
            ClassicAssert.AreEqual("Hello", id.Value);
            ClassicAssert.AreEqual(5,       id.Length);

            ClassicAssert.AreEqual(2, id.Start.Pos);
            ClassicAssert.AreEqual(1, id.Start.Line);
            ClassicAssert.AreEqual(3, id.Start.Column);

            ClassicAssert.AreEqual(7, id.End.Pos);
            ClassicAssert.AreEqual(1, id.End.Line);
            ClassicAssert.AreEqual(8, id.End.Column);
        }

        [Test]
        public void PreviewParserAlwaysSucceedsLikeOptionalParserButDoesntConsumeAnyInput()
        {
            var parser = Parse.MatchChar('a').XAtLeastOnce().Text().Token().Preview();
            var r      = parser.TryParse("   aaa   ");

            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.AreEqual("aaa", r.Value.GetOrDefault());
            ClassicAssert.AreEqual(0,     r.Remainder.Position);

            r = parser.TryParse("   bbb   ");
            ClassicAssert.True(r.WasSuccessful);
            ClassicAssert.IsNull(r.Value.GetOrDefault());
            ClassicAssert.AreEqual(0, r.Remainder.Position);
        }

        [Test]
        public void PreviewParserIsSimilarToPositiveLookaheadInRegex()
        {
            var parser = from test in Parse.String("test").Token().Preview() from testMethod in Parse.String("testMethod").Token().Text() select testMethod;

            var result = parser.Parse("   testMethod  ");
            ClassicAssert.AreEqual("testMethod", result);
        }

        [Test]
        public void CommentedParserConsumesWhiteSpaceLikeTokenParserAndAddsLeadingAndTrailingComments()
        {
            var parser = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit).Commented();

            // whitespace only
            var result = parser.Parse("    \t hello123   \t\r\n  ");
            ClassicAssert.AreEqual("hello123", result.Value);
            ClassicAssert.IsEmpty(result.LeadingComments);
            ClassicAssert.IsEmpty(result.TrailingComments);

            // leading comments
            result = parser.End().Parse(" /* My identifier */ world321   ");
            ClassicAssert.AreEqual("world321", result.Value);
            //Assert.Single(result.LeadingComments);
            ClassicAssert.IsEmpty(result.TrailingComments);
            ClassicAssert.AreEqual("My identifier", result.LeadingComments.Single().Trim());

            // trailing comments
            result = parser.End().Parse("    \t hello123   // what's that? ");
            ClassicAssert.AreEqual("hello123", result.Value);
            ClassicAssert.IsEmpty(result.LeadingComments);
            //Assert.Single(result.TrailingComments);
            ClassicAssert.AreEqual("what's that?", result.TrailingComments.Single().Trim());
        }

        [Test]
        public void CommentedParserConsumesAllLeadingCommentsAndOnlyOneTrailingCommentIfItIsOnTheSameLine()
        {
            var parser = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit).Commented();

            // leading and trailing comments
            var result = parser.Parse(@" // leading comments!
            /* more leading comments! */

            helloWorld // trailing comments!

            // more trailing comments! (these comments don't belong to the parsed value)");

            ClassicAssert.AreEqual("helloWorld",             result.Value);
            ClassicAssert.AreEqual(2,                        result.LeadingComments.Count());
            ClassicAssert.AreEqual("leading comments!",      result.LeadingComments.First().Trim());
            ClassicAssert.AreEqual("more leading comments!", result.LeadingComments.Last().Trim());
            //Assert.Single(result.TrailingComments);
            ClassicAssert.AreEqual("trailing comments!", result.TrailingComments.First().Trim());

            // multiple leading and trailing comments
            result = parser.Parse(@" // leading comments!

            /* multiline leading comments
            this is the second line */

            test321

            // trailing comments! these comments doesn't belong to the parsed value
            /* --==-- */");
            ClassicAssert.AreEqual("test321",           result.Value);
            ClassicAssert.AreEqual(2,                   result.LeadingComments.Count());
            ClassicAssert.AreEqual("leading comments!", result.LeadingComments.First().Trim());
            //Assert.StartsWith("multiline leading comments", result.LeadingComments.Last().Trim());
            //Assert.EndsWith("this is the second line", result.LeadingComments.Last().Trim());
            ClassicAssert.IsEmpty(result.TrailingComments);
        }

        [Test]
        public void CommentedParserAcceptsMultipleTrailingCommentsAsLongAsTheyStartOnTheSameLine()
        {
            var parser = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit).Commented();

            // trailing comments
            var result = parser.Parse("    \t hello123   /* one */ /* two */ /* " + @"
                three */ // this is not a trailing comment
                // neither this");
            ClassicAssert.AreEqual("hello123", result.Value);
            ClassicAssert.False(result.LeadingComments.Any());
            ClassicAssert.True(result.TrailingComments.Any());

            var trailing = result.TrailingComments.ToArray();
            ClassicAssert.AreEqual(3,       trailing.Length);
            ClassicAssert.AreEqual("one",   trailing[0].Trim());
            ClassicAssert.AreEqual("two",   trailing[1].Trim());
            ClassicAssert.AreEqual("three", trailing[2].Trim());

            // leading and trailing comments
            result = parser.Parse(@" // leading comments!
            /* more leading comments! */
            helloWorld /* one*/ // two!
            // more trailing comments! (that don't belong to the parsed value)");
            ClassicAssert.AreEqual("helloWorld",             result.Value);
            ClassicAssert.AreEqual(2,                        result.LeadingComments.Count());
            ClassicAssert.AreEqual("leading comments!",      result.LeadingComments.First().Trim());
            ClassicAssert.AreEqual("more leading comments!", result.LeadingComments.Last().Trim());

            trailing = result.TrailingComments.ToArray();
            ClassicAssert.AreEqual(2,      trailing.Length);
            ClassicAssert.AreEqual("one",  trailing[0].Trim());
            ClassicAssert.AreEqual("two!", trailing[1].Trim());
        }

        [Test]
        public void CommentedParserAcceptsCustomizedCommentParser()
        {
            var cp     = new CommentParser("#", "{", "}", "\n");
            var parser = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit).Commented(cp);

            // leading and trailing comments
            var result = parser.Parse(@" # leading comments!
            { more leading comments! }

            helloWorld # trailing comments!

            # more trailing comments! (these comments don't belong to the parsed value)");

            ClassicAssert.AreEqual("helloWorld",             result.Value);
            ClassicAssert.AreEqual(2,                        result.LeadingComments.Count());
            ClassicAssert.AreEqual("leading comments!",      result.LeadingComments.First().Trim());
            ClassicAssert.AreEqual("more leading comments!", result.LeadingComments.Last().Trim());
            //Assert.Single(result.TrailingComments);
            ClassicAssert.AreEqual("trailing comments!", result.TrailingComments.First().Trim());

            // multiple leading and trailing comments
            result = parser.Parse(@" # leading comments!

            { multiline leading comments
            this is the second line }

            test321

            # trailing comments! these comments doesn't belong to the parsed value
            { --==-- }");
            ClassicAssert.AreEqual("test321",           result.Value);
            ClassicAssert.AreEqual(2,                   result.LeadingComments.Count());
            ClassicAssert.AreEqual("leading comments!", result.LeadingComments.First().Trim());
            //Assert.StartsWith("multiline leading comments", result.LeadingComments.Last().Trim());
            //Assert.EndsWith("this is the second line", result.LeadingComments.Last().Trim());
            ClassicAssert.IsEmpty(result.TrailingComments);
        }
    }

}