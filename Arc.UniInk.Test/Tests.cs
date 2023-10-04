﻿using System.Text.RegularExpressions;


namespace Arc.UniInk.Test
{
    using System;
    using Arc.UniInk;
    using NUnit.Framework;
    using System.Collections.Generic;

    //🔴🟠🟡🟢🔵🟣🟤⚫⚪
    [TestFixture]
    public class Tests
    {
        protected static readonly Regex endOfStringInterpolationRegex = new("^('\"'|[^}\"])*[}\"]", RegexOptions.Compiled);
        protected static readonly Regex stringBeginningForEndBlockRegex = new("[$]?[@]?[\"]$", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithDollar = new("^([^\"{\\\\]|\\\\[\\\\\"0abfnrtv])*[\"{]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithoutDollar = new("^([^\"\\\\]|\\\\[\\\\\"0abfnrtv])*[\"]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithDollarWithAt = new("^[^\"{]*[\"{]", RegexOptions.Compiled);
        
        [OneTimeSetUp]
        public void SetUpTest()
        {
            TestContext.Progress.WriteLine("simple math operation");
            var test = new HelperClass();
            Ink.Context = test;
            Ink.Types.Add(typeof(MyEnum));
            Ink.Types.Add(typeof(HelperClass));
        }
        
        public readonly UniInk Ink = new();


        [Test]
        [TestCase("2+4")]
        [TestCase("(2+4)+6")]
        [TestCase("45-2")]
        [TestCase("4*2")]
        [TestCase("4/2")]
        [TestCase("4%2")]
        [TestCase("4*2+3")]
        [TestCase("true&&false")]
        [TestCase(" (45 * 2) + 3")]
        [TestCase(" 65 > 7 ? 3 : 2 ")]
        [TestCase("Avg(1,2,3,4,5,6,7,8,9,10)")]
        [TestCase("HelperClass")]
        public void Test(string script)
        {
            var ans = Ink.Evaluate($"{script}");
            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}=" + $"{ans}");
        }

        [Test]
        [TestCase("var w = 2+4;return w;")] //测试四种返回时的行为
        [TestCase("var w = 4-2;w;")]
        [TestCase("int w = 4-2;   w +=3;return w;")]
        [TestCase("var w = 4-2;")]
        [TestCase("4/2;")]
        [TestCase("Test();")]
        [TestCase("var ccc= new(HelperClass); return ccc.Id;")]
        [TestCase("Test(\"aaa\"+\"aaaaa\");")]
        [TestCase("Test(Test(aaa));")]
        [TestCase("Test(Test(aaa)+\"aaaaa\");")]
        [TestCase("this.Test3();")] //测试扩展方法
        [TestCase("TestA(x => (x > 0) && (int)MyEnum.A == 1);")] //测试lambda表达式
        [TestCase("MyEnum.A;")] //测试枚举
        [TestCase("TestA(x => (x > 0) && (A == B));")]
        [TestCase("TestA(x => x == D);")]
        [TestCase("TestC<MyEnum>(A);")]
        [TestCase("TestD<MyEnum,MyEnum>(A);")] //测试多泛型参数
        [TestCase(" TestC<List<int>>(D);  ")] //测试多泛型参数
        [TestCase("if(3>5){return 3;}else{return 5;}")] //测试if else
        [TestCase("if(3>5){return 3;}else if(3==5){return 3;}else{return 5;}")] //测试if else if else
        public void Test_02(string script) //TestA(x => x == D);
        {
            Ink.StaticTypesForExtensionsMethods.Add(typeof(ExtensionClass));
            var ans = Ink.ScriptEvaluate($"{script}");
            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}={ans}");
        }

        // [TestCase("List<List<int>>")]
        // [TestCase("<List<int>>")]
        // public void Test_03(string genericsTypes)
        // {
        //     Regex genericsEndOnlyOneTrim = new(@"(?>\s*)[>](?>\s*)$", RegexOptions.Compiled);
        //     
        //     var inputStr = genericsTypes.TrimStart(' ', '<'); 
        //     var str = genericsEndOnlyOneTrim.Replace(inputStr, "");
        //     
        //     TestContext.Progress.WriteLine($"✅:{str}");
        //     
        //     Console.WriteLine(str);
        // }

        // [TestCase("<List<int>>")]
        // public void Test_String(string script)
        // {
        //     var test = new HelperClass();
        //     var Ink = new UniInk(test);
        //     Ink.Types.Add(typeof(MyEnum));
        //     Ink.StaticTypesForExtensionsMethods.Add(typeof(ExtensionClass));
        //     var ans = Ink.ScriptEvaluate($"{script}");
        //     Assert.NotNull(ans);
        //     TestContext.Progress.WriteLine($"✅:{script}={ans}");
        // }
        // [Test]
        // [TestCase(typeof(int), typeof(long))]
        // [TestCase(typeof(int), typeof(decimal))]
        // public void Test_Type(Type fromType, Type toType)
        // {
        //     var ans = IsCastable(fromType, toType);
        //     TestContext.Progress.WriteLine($"✅:={ans}");
        // }

        private static bool IsCastable(Type fromType, Type toType)
        {
            return toType.IsAssignableFrom(fromType);
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
}


/// 匹配C#代码中的变量或函数名
/// sign: 匹配变量或函数名前的加号或减号
/// prefixOperator: 匹配变量或函数名前的自增或自减运算符
/// varKeyword: 匹配变量声明关键字var
/// nullConditional: 匹配空条件运算符?
/// inObject: 匹配变量或函数名前的句点(.)，表示该变量或函数是类的成员
/// name: 匹配变量或函数名
/// assignationOperator: 匹配赋值运算符和一些算术或位运算符
/// assignmentPrefix: 匹配赋值运算符前的算术或位运算符
/// postfixOperator: 匹配变量或函数名后的自增或自减运算符
/// isGeneric: 匹配泛型类型参数
/// genTag: 匹配泛型类型参数中的尖括号
/// isFunction: 匹配函数参数列表的左括号