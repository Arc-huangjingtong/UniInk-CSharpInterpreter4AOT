namespace Arc.UniInk.NunitTest
{
    using Arc.UniInk;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    [TestFixture]
    public class UniInk_Speed_Tests
    {
        private static readonly UniInk_Speed Ink = new();

        [TestCase("2222"),Repeat(100)]
        public void Test_EvaluateNumber(string input)
        {
            // UniInk_Speed.StartsWithNumbersFromIndex(input, 0, out var numberMatch, out var len);
            //
            // var answer = int.Parse(input);
            // numberMatch.GetNumber();
            //
            // Assert.AreEqual(answer, numberMatch.Value_int);
            // TestContext.Progress.WriteLine($"✅:{numberMatch.Value_int}=" + $"{answer}");
        }
        [TestCase("2222"),Repeat(100)]
        public void Test_EvaluateNumber2(string input)
        {
            var answer = int.Parse(input);

            //Assert.AreEqual(answer, numberMatch.Value_int);
            TestContext.Progress.WriteLine($"✅=" + $"{answer}");
        }
    }
}