namespace Arc.UniInk.NUnitTest
{

    using System;
    using System.Threading;
    using BenchmarkDotNet.Attributes;
    using LinqyCalculator;
    using NUnit.Framework;
    using CodingSeb.ExpressionEvaluator;
    using ParsecSharp.Examples;


    /*
     * | Method                                | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
     * |-------------------------------------- |------------:|----------:|----------:|------:|--------:|---------:|--------:|----------:|------------:|
     * | TEST_Arithmetic__UniInkSpeed          |   296.06 us |  0.968 us |  0.905 us |  1.00 |    0.00 |        - |       - |      68 B |        1.00 |
     * | TEST_IfStatement__UniInkSpeed         |    11.05 us |  0.043 us |  0.038 us |  0.04 |    0.00 |        - |       - |      64 B |        0.94 |
     * | TEST_Arithmetic__ExpressionEvaluator  | 2,621.71 us | 50.110 us | 59.653 us |  8.87 |    0.23 | 390.6250 |  3.9063 | 2068334 B |   30,416.68 |
     * | TEST_Arithmetic__Sprache              | 2,678.89 us | 21.823 us | 19.345 us |  9.05 |    0.07 | 535.1563 | 27.3438 | 2817145 B |   41,428.60 |
     * | TEST_Arithmetic__ParsecSharp          | 1,063.70 us |  3.801 us |  3.555 us |  3.59 |    0.02 | 162.1094 |       - |  851830 B |   12,526.91 |
     * | TEST_Arithmetic__UniInkSpeed_Compiled |   164.65 us |  0.610 us |  0.540 us |  0.56 |    0.00 |        - |       - |     658 B |        9.68 |
     *
     *
     * summary : UniInkSpeed is fast , and UniInkSpeed support more features. would you like to try it?
     */


    [TestFixture]
    [MemoryDiagnoser]
    public class Benchmark_UniInkSpeed
    {
        public Benchmark_UniInkSpeed() => TEST_INITIATION();


        public const string TestInput_Arithmetic_Int_01 = "12345678+87654321-1*2*3*4*5*6*7*8*9+9*8*7*6*5*4*3*2*1+1*2*3*4*5*6*7*8*9-87654321-12345678";

        public const string TestInput_Arithmetic_Int_02 = "1+2+3+4+5+6+7+8+9+10+11+12+13+14+15+16+17+18+19+20+21+22+23+24+25+26+27+28+29+30+31+32+33+"
                                                        + "34+35+36+37+38+39+40+41+42+43+44+45+46+47+48+49+50+51+52+53+54+55+56+57+58+59+60+61+62+63+"
                                                        + "64+65+66+67+68+69+70+71+72+73+74+75+76+77+78+79+80+81+82+83+84+85+86+87+88+89+90+91+92+93+"
                                                        + "94+95+96+97+98+99+100";

        public const string TestInput_Arithmetic_Int_03 = "1*2*3*4*5*6*7*8*9*10*11*12/12/11/10/9/8/7/6/5/4/3/2/1";

        public const string TestInput_Arithmetic_Int_04 = "0-1-2-3-4-5-6-7-8-9-10-11-12-13-14-15-16-17-18-19-20-21-22-23-24-25-26-27-28-29-30-31-32-33-"
                                                        + "34-35-36-37-38-39-40-41-42-43-44-45-46-47-48-49-50-51-52-53-54-55-56-57-58-59-60-61-62-63-64-"
                                                        + "65-66-67-68-69-70-71-72-73-74-75-76-77-78-79-80-81-82-83-84-85-86-87-88-89-90-91-92-93-94-95-"
                                                        + "96-97-98-99-100";

        public const string TestInput_IfStatement = @" if ( 1 > 2 )      
                                                      {                 
                                                         123            
                                                      }                 
                                                      else if ( 3 > 6 ) 
                                                      {                 
                                                         456            
                                                      }                 
                                                      else if ( 3 < 6 ) 
                                                      {                 
                                                        return 456      
                                                      }                 
                                                      else              
                                                      {                 
                                                        return 666      
                                                      }                 ";


        [OneTimeSetUp]
        public void TEST_INITIATION() => new NUnit_UniInkSpeed().Test_Initiation();


        [Benchmark(Baseline = true)] [Test]
        public void TEST_Arithmetic__UniInkSpeed()
        {
            var result1 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_01);
            var result2 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_02);
            var result3 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_03);
            var result4 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_04);

#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif
        }

        [Benchmark] [Test]
        public void TEST_IfStatement__UniInkSpeed()
        {
            var Ink  = NUnit_UniInkSpeed.Ink;
            var test = Ink.Evaluate_IfStatement(TestInput_IfStatement);
            if (test is InkValue value)
            {
                //Console.WriteLine(value.Value_int); // each time , the result will be different

                InkValue.Release(value);
            }
        }



        private static readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();

        [Benchmark] [Test]
        public void TEST_Arithmetic__ExpressionEvaluator()
        {
            var result1 = _expressionEvaluator.Evaluate<int>(TestInput_Arithmetic_Int_01);
            var result2 = _expressionEvaluator.Evaluate<int>(TestInput_Arithmetic_Int_02);
            var result3 = _expressionEvaluator.Evaluate<int>(TestInput_Arithmetic_Int_03);
            var result4 = _expressionEvaluator.Evaluate<int>(TestInput_Arithmetic_Int_04);

#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif
        }

        [Benchmark] [Test]
        public void TEST_Arithmetic__Sprache()
        {
            var result1 = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_01).Compile().Invoke();
            var result2 = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_02).Compile().Invoke();
            var result3 = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_03).Compile().Invoke();
            var result4 = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_04).Compile().Invoke();
#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif
        }

        [Benchmark] [Test]
        public void TEST_Arithmetic__ParsecSharp()
        {
            var result1 = Integer.Parser.Parse(TestInput_Arithmetic_Int_01).Value.Value;
            var result2 = Integer.Parser.Parse(TestInput_Arithmetic_Int_02).Value.Value;
            var result3 = Integer.Parser.Parse(TestInput_Arithmetic_Int_03).Value.Value;
            var result4 = Integer.Parser.Parse(TestInput_Arithmetic_Int_04).Value.Value;

#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif
        }


        public static readonly InkSyntaxList Compiled_TestInput_Arithmetic_Int_01 =
            NUnit_UniInkSpeed.Ink.CompileLexerAndFill(TestInput_Arithmetic_Int_01, 0, TestInput_Arithmetic_Int_01.Length - 1);

        public static readonly InkSyntaxList Compiled_TestInput_Arithmetic_Int_02 =
            NUnit_UniInkSpeed.Ink.CompileLexerAndFill(TestInput_Arithmetic_Int_02, 0, TestInput_Arithmetic_Int_02.Length - 1);

        public static readonly InkSyntaxList Compiled_TestInput_Arithmetic_Int_03 =
            NUnit_UniInkSpeed.Ink.CompileLexerAndFill(TestInput_Arithmetic_Int_03, 0, TestInput_Arithmetic_Int_03.Length - 1);

        public static readonly InkSyntaxList Compiled_TestInput_Arithmetic_Int_04 =
            NUnit_UniInkSpeed.Ink.CompileLexerAndFill(TestInput_Arithmetic_Int_04, 0, TestInput_Arithmetic_Int_04.Length - 1);


        [Benchmark] [Test] [Repeat(100)]
        public void TEST_Arithmetic__UniInkSpeed_Compiled()
        {
            var result1 = UniInk_Speed.ExecuteProcess(Compiled_TestInput_Arithmetic_Int_01);
            var result2 = UniInk_Speed.ExecuteProcess(Compiled_TestInput_Arithmetic_Int_02);
            var result3 = UniInk_Speed.ExecuteProcess(Compiled_TestInput_Arithmetic_Int_03);
            var result4 = UniInk_Speed.ExecuteProcess(Compiled_TestInput_Arithmetic_Int_04);

            InkSyntaxList.Recover(Compiled_TestInput_Arithmetic_Int_01);
            InkSyntaxList.Recover(Compiled_TestInput_Arithmetic_Int_02);
            InkSyntaxList.Recover(Compiled_TestInput_Arithmetic_Int_03);
            InkSyntaxList.Recover(Compiled_TestInput_Arithmetic_Int_04);

#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif
        }

        public static readonly Func<double> Compiled_TestInput_Arithmetic_Int_01_Sprache = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_01).Compile();
        public static readonly Func<double> Compiled_TestInput_Arithmetic_Int_02_Sprache = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_02).Compile();
        public static readonly Func<double> Compiled_TestInput_Arithmetic_Int_03_Sprache = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_03).Compile();
        public static readonly Func<double> Compiled_TestInput_Arithmetic_Int_04_Sprache = ExpressionParser.ParseExpression(TestInput_Arithmetic_Int_04).Compile();

        // [Benchmark] [TestCase(ExpectedResult = 362881)] [Repeat(100)]
        // Debate : The Compiled Sprache is very fast, but the test answer is static and not dynamic.
        //| TEST_Arithmetic_Compiled__Sprache     |        14.94 ns |      0.286 ns |       0.267 ns |        14.95 ns |        - |       - |         - |
        public double TEST_Arithmetic__Sprache__Compiled()
        {
            var result1 = Compiled_TestInput_Arithmetic_Int_01_Sprache();
            var result2 = Compiled_TestInput_Arithmetic_Int_02_Sprache();
            var result3 = Compiled_TestInput_Arithmetic_Int_03_Sprache();
            var result4 = Compiled_TestInput_Arithmetic_Int_04_Sprache();

#if DEBUG
            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
#endif

            return result1 + result2 + result3 + result4;
        }
    }

}