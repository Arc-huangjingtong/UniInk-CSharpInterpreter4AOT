namespace Sprache.Tests
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework.Legacy;


    internal static class AssertParser
    {
        public static void SucceedsWithOne <T>(Parser<IEnumerable<T>> parser, string input, T expectedResult)
        {
            SucceedsWith(parser, input, t =>
            {
                //ClassicAssert.Single(t);
                ClassicAssert.AreEqual(expectedResult, t.Single());
            });
        }

        public static void SucceedsWithMany <T>(Parser<IEnumerable<T>> parser, string input, IEnumerable<T> expectedResult)
        {
            SucceedsWith(parser, input, t => ClassicAssert.True(t.SequenceEqual(expectedResult)));
        }

        public static void SucceedsWithAll(Parser<IEnumerable<char>> parser, string input)
        {
            SucceedsWithMany(parser, input, input.ToCharArray());
        }

        public static void SucceedsWith <T>(Parser<T> parser, string input, Action<T> resultAssertion)
        {
            parser.TryParse(input).IfFailure(f =>
            {
                ClassicAssert.True(false, $"Parsing of \"input\" failed unexpectedly. f");
                return f;
            }).IfSuccess(s =>
            {
                resultAssertion(s.Value);
                return s;
            });
        }

        public static void Fails <T>(Parser<T> parser, string input)
        {
            FailsWith(parser, input, _ => { });
        }

        public static void FailsAt <T>(Parser<T> parser, string input, int position)
        {
            FailsWith(parser, input, f => ClassicAssert.AreEqual(position, f.Remainder.Position));
        }

        public static void FailsWith <T>(Parser<T> parser, string input, Action<IResult<T>> resultAssertion)
        {
            parser.TryParse(input).IfSuccess(s =>
            {
                ClassicAssert.True(false, $"Expected failure but succeeded with {s.Value}.");
                return s;
            }).IfFailure(f =>
            {
                resultAssertion(f);
                return f;
            });
        }
    }

}