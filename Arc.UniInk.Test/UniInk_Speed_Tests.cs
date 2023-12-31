﻿namespace Arc.UniInk.NunitTest
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

        [TestCase("+2222+(333-3+3-3)"), Repeat(1000)]
        public void Test_EvaluateNumber(string input)
        {
            var res = Ink.Evaluate(input, 0, input.Length);

            Assert.AreEqual(res, 2552);
        }

        [TestCase("333-3"), Repeat(100)]
        public void Test_EvaluateNumber2(string input)
        {
            var res = Ink.Evaluate(input, 0, input.Length);

            Assert.AreEqual(res, 330);
        }

      
    }
}