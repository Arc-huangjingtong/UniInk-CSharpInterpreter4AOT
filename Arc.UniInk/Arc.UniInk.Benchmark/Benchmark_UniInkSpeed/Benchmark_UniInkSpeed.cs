namespace Arc.UniInk.NUnitTest
{

    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Arc.UniInk.NunitTest;
    using BenchmarkDotNet.Attributes;
    using LinqyCalculator;
    using NUnit.Framework;



    [MemoryDiagnoser]                // we need to enable it in explicit way
    [RyuJitX64Job] [LegacyJitX86Job] // let's run the benchmarks for 32 & 64 bit
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

        private const string input1 = "2222+(333-3+3-3)";
        private const string input2 = "333-3";
        private const string input3 = "333*3";
        private const string input4 = "333/3";
        private const string input5 = "333+3";
        private const string input6 = "1111111+1111111";
        private const string input7 = "9999999+9999999";

        private UniInk _uniInk = new UniInk();

        //[Benchmark] [Test]
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

        //[Benchmark] [Test]
        public void INT_UniInkNoSpeed()
        {
            _uniInk.ScriptEvaluate(input1 + ";");
            _uniInk.ScriptEvaluate(input2 + ";");
            _uniInk.ScriptEvaluate(input3 + ";");
            _uniInk.ScriptEvaluate(input4 + ";");
            _uniInk.ScriptEvaluate(input5 + ";");
            _uniInk.ScriptEvaluate(input6 + ";");
            _uniInk.ScriptEvaluate(input7 + ";");
        }



        [Benchmark] [Test]
        public void INT_UniInk()
        {
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input1);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input2);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input3);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input4);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input5);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input6);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input7);
        }

        private const string inputEmpty = "  ";

        [Benchmark] [Test]
        public void Temp()
        {
            var temp = UniInk_Speed.LexerAndFill(inputEmpty, 0, inputEmpty.Length);

            UniInk_Speed.InkSyntaxList.Release(temp);
        }

        // [Benchmark] [Test]
        public void INT_Sprache()
        {
            var parsed1 = ExpressionParser.ParseExpression(input1).Compile().Invoke();
            var parsed2 = ExpressionParser.ParseExpression(input2).Compile().Invoke();
            var parsed3 = ExpressionParser.ParseExpression(input3).Compile().Invoke();
            var parsed4 = ExpressionParser.ParseExpression(input4).Compile().Invoke();
            var parsed5 = ExpressionParser.ParseExpression(input5).Compile().Invoke();
            var parsed6 = ExpressionParser.ParseExpression(input6).Compile().Invoke();
            var parsed7 = ExpressionParser.ParseExpression(input7).Compile().Invoke();
        }

        // [Benchmark] [Test]
        public void INT_Linqy()
        {
            var num_1 = int.Parse("2222");
            var num_2 = float.Parse("0.5");
            var num_3 = double.Parse("0.6");
        }

        private const string num1 = "2222";
        private const string num2 = "0.5";
        private const string num3 = "0.6";


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