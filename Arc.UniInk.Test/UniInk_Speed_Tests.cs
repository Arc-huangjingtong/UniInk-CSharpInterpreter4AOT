namespace Arc.UniInk.NunitTest
{

    using Arc.UniInk;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;


    [TestFixture]
    [SimpleJob(RuntimeMoniker.Net472, baseline : true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp30)]
    [SimpleJob(RuntimeMoniker.NativeAot70)]
    [SimpleJob(RuntimeMoniker.Mono)]
    [RPlotExporter]
    public class UniInk_Speed_Tests
    {
        private static readonly UniInk_Speed Ink = new();



        [Repeat(10000)]
        [TestCase("+2222+(333-3+3-3)", ExpectedResult = 2552)]
        [TestCase("333-3",             ExpectedResult = 330)]
        public int Test_EvaluateNumber_Int(string input)
        {
            var res = UniInk_Speed.Evaluate(input, 0, input.Length);

            if (res is UniInk_Speed.InkValue value)
            {
                res = value.ValueType switch
                {
                    UniInk_Speed.InkValue.InkValueType.Int     => value.Value_int               //
                  , UniInk_Speed.InkValue.InkValueType.Float   => value.Value_float             //
                  , UniInk_Speed.InkValue.InkValueType.Double  => value.Value_double            //
                  , UniInk_Speed.InkValue.InkValueType.Boolean => value.Value_bool              // 
                  , UniInk_Speed.InkValue.InkValueType.Char    => value.Value_char              //
                  , UniInk_Speed.InkValue.InkValueType.String  => value.Value_String.ToString() // 
                  , _                                          => throw new ArgumentOutOfRangeException()
                };
                UniInk_Speed.InkValue.Release(value);
            }

            return (int)res;
        }

        [Repeat(10000)]
        [TestCase("+2222+(333-3+3-3)", ExpectedResult = 2552)]
        [TestCase("333-3",             ExpectedResult = 330)]
        public int Test_EvaluateNumber_Int2(string input)
        {
            var res    = UniInk_Speed.Evaluate(input, 0, input.Length) as UniInk_Speed.InkValue;
            var result = res.Value_int;
            UniInk_Speed.InkValue.Release(res);
            return result;
        }

        [Params("+2222+(333-3+3-3)")]
        public string Input1;

        [Benchmark]
        public void TestBenchmark() => UniInk_Speed.Evaluate(Input1, 0, Input1.Length);


        // [Test, Repeat(1)]
        // public void Test()
        // {
        //     // 使用具体的参数值调用委托
        //     var result = compiled(333, 3); // result将会是8
        //
        //     Assert.AreEqual(result, 330);
        // }
        //
        // Func<int, int, int> compiled;
        //
        // [OneTimeSetUp]
        // public void Test_SetUp()
        // {
        //     // 使用表达式API来创建表达式树
        //     var paramA        = Expression.Parameter(typeof(int), "a");
        //     var paramB        = Expression.Parameter(typeof(int), "b");
        //     var sumExpression = Expression.Subtract(paramA, paramB);
        //
        //
        //     // 创建lambda表达式代表这个表达式树
        //     var lambda = Expression.Lambda<Func<int, int, int>>(sumExpression, paramA, paramB);
        //
        //     // 编译lambda表达式，生成可执行的委托
        //     compiled = lambda.Compile();
        // }
    }


   

}