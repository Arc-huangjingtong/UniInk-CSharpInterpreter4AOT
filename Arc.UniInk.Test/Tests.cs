using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Arc.UniInk.Test
{
    using Arc.UniInk;
    using NUnit.Framework;

    //🔴🟠🟡🟢🔵🟣🟤⚫⚪
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void SetUpTest()
        {
            TestContext.Progress.WriteLine("simple math operation");
        }


        [Test]
        [TestCase("(2+4)+6")]
        [TestCase("4-2")]
        [TestCase("4*2")]
        [TestCase("4/2")]
        [TestCase("4%2")]
        [TestCase("4*2+3")]
        [TestCase("(true&&false)")]
        public void Test(string script)
        {
            var Ink = new UniInk();
            var ans = Ink.Evaluate($"{script}");

            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}=" + $"{ans}");
        }

        [Test]
        [TestCase("var a = 2+4;return a;")] //测试四种返回时的行为
        [TestCase("var a = 4-2;a;")]
        [TestCase("var a = 4-2;")]
        [TestCase("4/2;")]
        [TestCase("Test();")]
        [TestCase("Test(@\"aaa\"+\"aaaaa\");")]
        [TestCase("Test(Test(aaa));")]
        [TestCase("Test(Test(aaa)+\"aaaaa\");")]
        [TestCase("Test3()")]
        public void Test_02(string script)
        {
            var test = new HelperClass();
            var Ink = new UniInk(test);
            var ans = Ink.ScriptEvaluate($"{script}");

            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}={ans}");
        }
    }


    public class HelperClass
    {
        public int Id = 233;

        public string aaa = "2222";

        ///无参数的测试函数
        public static int Test()
        {
            Console.WriteLine("test");
            return 1;
        }

        ///有参数的测试函数
        public static string Test(string str)
        {
            Console.WriteLine(str);
            return str;
        }
    }
    
    
    public static class ExtensionClass
    {
        public static string Test3(this HelperClass str)
        {
            Console.WriteLine(str);
            return str.ToString();
        }
    }
}