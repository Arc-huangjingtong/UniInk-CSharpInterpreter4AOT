using System;
using System.Reflection;
using System.Text.RegularExpressions;


namespace Sprache.Tests
{

    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    /// <summary>
    /// These tests exist in order to verify that the modification that is applied to
    /// the regex passed to every call to the <see cref="Parse.Regex(string,string)"/>
    /// or <see cref="Parse.Regex(Regex,string)"/> methods does not change the results
    /// in any way.
    /// </summary>
    public class RegexTests
    {
        private const string _startsWithCarrot = "^([a-z]{3})([0-9]{3})$";
        private const string _alternation      = "(this)|(that)|(the other)";

        private static readonly MethodInfo _optimizeRegexMethod = typeof(Parse).GetMethod("OptimizeRegex", BindingFlags.NonPublic | BindingFlags.Static);

        [Test]
        public void OptimizedRegexIsNotSuccessfulWhenTheMatchIsNotAtTheBeginningOfTheInput()
        {
            var regexOriginal  = new Regex("[a-z]+");
            var regexOptimized = OptimizeRegex(regexOriginal);

            const string input = "123abc";

            //Assert.Matches(regexOriginal, input);
            //Assert.DoesNotMatch(regexOptimized, input);
        }

        [Test]
        public void OptimizedRegexIsSuccessfulWhenTheMatchIsAtTheBeginningOfTheInput()
        {
            var regexOriginal  = new Regex("[a-z]+");
            var regexOptimized = OptimizeRegex(regexOriginal);

            const string input = "abc123";

            //Assert.Matches(regexOriginal, input);
            //Assert.Matches(regexOptimized, input);
        }

        [Theory]
        [TestCase(_startsWithCarrot, RegexOptions.None,            "abc123")] // TestName = "Starts with ^, no options, success"
        [TestCase(_startsWithCarrot, RegexOptions.ExplicitCapture, "abc123")] // TestName = "Starts with ^, explicit capture, success"
        [TestCase(_startsWithCarrot, RegexOptions.None,            "123abc")] // TestName = "Starts with ^, no options, failure"
        [TestCase(_startsWithCarrot, RegexOptions.ExplicitCapture, "123abc")] // TestName = "Starts with ^, explicit capture, failure"
        [TestCase(_alternation,      RegexOptions.None,            "abc123")] // TestName = "Alternation, no options, success"
        [TestCase(_alternation,      RegexOptions.ExplicitCapture, "abc123")] // TestName = "Alternation, explicit capture, success"
        [TestCase(_alternation,      RegexOptions.None,            "that")]   // TestName = "Alternation, no options, failure"
        [TestCase(_alternation,      RegexOptions.ExplicitCapture, "that")]   // TestName = "Alternation, explicit capture, failure"
        public void RegexOptimizationDoesNotChangeRegexBehavior(string pattern, RegexOptions options, string input)
        {
            var regexOriginal  = new Regex(pattern, options);
            var regexOptimized = OptimizeRegex(regexOriginal);

            var matchOriginal = regexOriginal.Match(input);
            var matchModified = regexOptimized.Match(input);

            ClassicAssert.AreEqual(matchOriginal.Success,      matchModified.Success);
            ClassicAssert.AreEqual(matchOriginal.Value,        matchModified.Value);
            ClassicAssert.AreEqual(matchOriginal.Groups.Count, matchModified.Groups.Count);

            for (int i = 0 ; i < matchModified.Groups.Count ; i++)
            {
                ClassicAssert.AreEqual(matchOriginal.Groups[i].Success, matchModified.Groups[i].Success);
                ClassicAssert.AreEqual(matchOriginal.Groups[i].Value,   matchModified.Groups[i].Value);
            }
        }

        /// <summary>
        /// Calls the <see cref="Parse.OptimizeRegex"/> method via reflection.
        /// </summary>
        private static Regex OptimizeRegex(Regex regex)
        {
            // Reflection isn't the best way of verifying behavior,
            // but cluttering the public api sucks worse.

            if (_optimizeRegexMethod == null)
            {
                throw new Exception("Unable to locate a private static method named " + "\"OptimizeRegex\" in the Parse class. Has it been renamed?");
            }

            var optimizedRegex = (Regex)_optimizeRegexMethod.Invoke(null, new object[] { regex });
            return optimizedRegex;
        }
    }

}