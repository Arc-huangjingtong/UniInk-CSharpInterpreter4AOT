namespace Arc.UniInk.NunitTest
{

    using Arc.UniInk;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq.Expressions;


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

        [Test, Repeat(100)]
        public void Test()
        {
            // 使用具体的参数值调用委托
            var result = compiled(333, 3); // result将会是8

            Assert.AreEqual(result, 330);
        }

        Func<int, int, int> compiled;

        [OneTimeSetUp]
        public void Test_SetUp()
        {
            // 使用表达式API来创建表达式树
            var paramA        = Expression.Parameter(typeof(int), "a");
            var paramB        = Expression.Parameter(typeof(int), "b");
            var sumExpression = Expression.Subtract(paramA, paramB);


            // 创建lambda表达式代表这个表达式树
            var lambda = Expression.Lambda<Func<int, int, int>>(sumExpression, paramA, paramB);

            // 编译lambda表达式，生成可执行的委托
            compiled = lambda.Compile();
        }
    }

}