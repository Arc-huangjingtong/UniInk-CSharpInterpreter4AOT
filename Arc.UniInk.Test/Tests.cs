namespace Arc.UniInk.Test
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
        [TestCase("true&&false")]
        public void Test(string script)
        {
            var Ink = new UniInk();
            var ans = Ink.Evaluate($"{script}");

            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}=" + $"{ans}");
        }

        [Test]
        [TestCase("var w = 2+4;return w;")] //测试四种返回时的行为
        [TestCase("var w = 4-2;w;")]
        [TestCase("var w = 4-2;")]
        [TestCase("4/2;")]
        [TestCase("Test();")]
        [TestCase("Test(@\"aaa\"+\"aaaaa\");")]
        [TestCase("Test(Test(aaa));")]
        [TestCase("Test(Test(aaa)+\"aaaaa\");")]
        [TestCase("this.Test3();")] //测试扩展方法
        [TestCase("TestA(x => (x > 0) && (int)MyEnum.A == 1);")] //测试lambda表达式
        [TestCase("MyEnum.A;")] //测试枚举
        [TestCase("TestA(x => (x > 0) && (A == B));")]
        //[TestCase("TestC<MyEnum>(A);")] //测试无返回值的函数
        public void Test_02(string script)
        {
            var test = new HelperClass();
            var Ink = new UniInk(test);
            Ink.Types.Add(typeof(MyEnum));
            Ink.StaticTypesForExtensionsMethods.Add(typeof(ExtensionClass));
            var ans = Ink.ScriptEvaluate($"{script}");
          
            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}={ans}");
        }
    }

    public enum MyEnum { A, B, C }


    public class HelperClass
    {
        public int Id = 233;

        public string aaa = "2222";

        public MyEnum A => MyEnum.A;
        public MyEnum B => MyEnum.B;
        public MyEnum C => MyEnum.C;


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

        public static string TestA(Predicate<int> predicate)
        {
            Console.WriteLine("predicate");
            return "action";
        }

        public static void TestB()
        {
            
            Console.WriteLine("actionB");
        }

        public static T TestC<T>(T t)
        {
            Console.WriteLine("actionB");
            return t;
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