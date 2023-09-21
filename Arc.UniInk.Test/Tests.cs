using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework.Internal;


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
        [TestCase("45-2")]
        [TestCase("4*2")]
        [TestCase("4/2")]
        [TestCase("4%2")]
        [TestCase("4*2+3")]
        [TestCase("true&&false")]
        [TestCase(" (45 * 2) + 3")]
        [TestCase(" 65>7 ? 3 : 2 ")]
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
        [TestCase("TestA(x => x == D);")]
        [TestCase("TestC<MyEnum>(A);")]
        [TestCase("TestD<MyEnum,MyEnum>(A);")] //测试多泛型参数
        public void Test_02(string script) //TestA(x => x == D);
        {
            var test = new HelperClass();
            var Ink = new UniInk(test);
            Ink.Types.Add(typeof(MyEnum));
            Ink.StaticTypesForExtensionsMethods.Add(typeof(ExtensionClass));
            var ans = Ink.ScriptEvaluate($"{script}");

            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}={ans}");
        }

        [TestCase("List<List<int>>")]
        [TestCase("<List<int>>")]
        public void Test_03(string genericsTypes)
        {
            Regex genericsEndOnlyOneTrim = new(@"(?>\s*)[>](?>\s*)$", RegexOptions.Compiled);
            var inputStr = genericsTypes.TrimStart(' ', '<'); //首先去掉开头的<
            var str = genericsEndOnlyOneTrim.Replace(inputStr, "");
            TestContext.Progress.WriteLine($"✅:{str}");
            Console.WriteLine(str);
        }
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

    public static List<int> D = new List<int>();


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

    public static string TestA(Predicate<List<int>> predicate)
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

    public static T2 TestD<T1, T2>(T2 t)
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

////匹配 泛型类型参数列表的末尾
//private static readonly Regex genericsEndOnlyOneTrim = new(@"(?>\s*)[>](?>\s*)$", RegexOptions.Compiled);