namespace Arc.UniInk.NUnitTest
{

    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Arc.UniInk.NunitTest;
    using BenchmarkDotNet.Attributes;



    [MemoryDiagnoser]               // we need to enable it in explicit way
    [RyuJitX64Job, LegacyJitX86Job] // let's run the benchmarks for 32 & 64 bit
    public class Benchmarks
    {
        [Benchmark]
        public byte[] EmptyArray() => Array.Empty<byte>();

        [Benchmark]
        public byte[] EightBytes() => new byte[8];

        [Benchmark]
        public byte[] SomeLinq()
        {
            return Enumerable.Range(0, 100).Where(i => i % 2 == 0).Select(i => (byte)i).ToArray();
        }
    }


    [MemoryDiagnoser]
    public class Benchmark_UniInkSpeed
    {
        public Benchmark_UniInkSpeed() { }

        private const string input1 = "+2222+(333-3+3-3)";
        private const string input2 = "333-3";
        private const string input3 = "333*3";
        private const string input4 = "333/3";
        private const string input5 = "333+3";
        private const string input6 = "1111111+1111111";
        private const string input7 = "9999999+9999999";

        [Benchmark]
        public void INT_Limit()
        {
            var res1 = 333 - 3;
            var res2 = 333 * 3;
            var res3 = 333 / 3;
            var res4 = 333     + 3;
            var res5 = 1111111 + 1111111;
            var res6 = 9999999 + 9999999;
            var sum  = res1    + res2 + res3 + res4 + res5 + res6;
        }


        [Benchmark]
        public void INT_2()
        {
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input2);
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input3);
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input4);
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input5);
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input6);
            NUnit_UniInkSpeed.Test_EvaluateNumber_Int(input7);
        }

        [Benchmark]
        public void INT_3()
        {
            NUnit_UniInk.Test_EvaluateNumber(input2);
            NUnit_UniInk.Test_EvaluateNumber(input3);
            NUnit_UniInk.Test_EvaluateNumber(input4);
            NUnit_UniInk.Test_EvaluateNumber(input5);
            NUnit_UniInk.Test_EvaluateNumber(input6);
            NUnit_UniInk.Test_EvaluateNumber(input7);
        }
        //
        // [Benchmark]
        // public void INT_4() => Main3();

        public static void Main2()
        {
            // 创建参数表达式
            var paramA = Expression.Parameter(typeof(int), "a");
            var paramB = Expression.Parameter(typeof(int), "b");
            var paramC = Expression.Parameter(typeof(int), "c");

            // 构建 b + c 的表达式
            var bPlusC = Expression.Add(paramB, paramC);

            // 构建 a * (b + c) 的表达式
            var aTimesBPlusC = Expression.Multiply(paramA, bPlusC);

            // 创建一个表示该表达式的 lambda 表达式
            var lambda = Expression.Lambda<Func<int, int, int, int>>(aTimesBPlusC, new ParameterExpression[] { paramA, paramB, paramC });

            // 将 lambda 表达式编译成委托（delegate）
            var compiledLambda = lambda.Compile();

            // 执行委托，并输出结果
            var result = compiledLambda(2, 3, 4); // 这里的2, 3, 4 分别是参数a, b, c的值
            Console.WriteLine(result);            // 输出结果为 2 * (3 + 4) = 14
        }

        public static void Main3()
        {
            // 定义一个名为 "i" 的参数
            var i = Expression.Parameter(typeof(int), "i");

            // 构建表达式树
            var lambda = Expression.Lambda<Func<int, bool>>(Expression.Equal(Expression.Modulo(i, Expression.Constant(2)), Expression.Constant(0)), i);

            // 编译表达式树为委托
            var compiledLambda = lambda.Compile();

            // 生成一个从 0 到 100 的整数序列，过滤出偶数，然后将其转换为 byte 类型
            var result = Enumerable.Range(0, 100).Where(compiledLambda).Select(i => (byte)i).ToArray();
        }
    }

}