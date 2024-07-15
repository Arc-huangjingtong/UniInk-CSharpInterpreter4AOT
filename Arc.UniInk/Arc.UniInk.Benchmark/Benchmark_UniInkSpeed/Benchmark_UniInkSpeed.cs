namespace Arc.UniInk.NUnitTest
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using BenchmarkDotNet.Attributes;
    using LinqyCalculator;
    using NUnit.Framework;


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

        public const string TestInput_Arithmetic_Int_03 = "1*2*3*4*5*6*7*8*9*10*11*12*13/13/12/11/10/9/8/7/6/5/4/3/2/1";

        public const string TestInput_Arithmetic_Int_04 = "0-1-2-3-4-5-6-7-8-9-10-11-12-13-14-15-16-17-18-19-20-21-22-23-24-25-26-27-28-29-30-31-32-33-"
                                                        + "34-35-36-37-38-39-40-41-42-43-44-45-46-47-48-49-50-51-52-53-54-55-56-57-58-59-60-61-62-63-64-"
                                                        + "65-66-67-68-69-70-71-72-73-74-75-76-77-78-79-80-81-82-83-84-85-86-87-88-89-90-91-92-93-94-95-"
                                                        + "96-97-98-99-100";



        [OneTimeSetUp]
        public void TEST_INITIATION() => new NUnit_UniInkSpeed().Test_Initiation();


        [Benchmark] [Test]
        public void TEST_Arithmetic__UniInk_Speed()
        {
            var result1 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_01);
            var result2 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_02);
            var result3 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_03);
            var result4 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_04);

            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
        }

        [Benchmark] [Test]
        public void TEST_Arithmetic__UniInk_()
        {
            var result1 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_01);
            var result2 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_02);
            var result3 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_03);
            var result4 = NUnit_UniInkSpeed.Test_Arithmetic_Int(TestInput_Arithmetic_Int_04);

            Console.WriteLine(result1 + "|" + result2 + "|" + result3 + "|" + result4);
        }



        //   [Benchmark] [Test]
        public void TEST_SCRIPTS_SPEED_COMPILE()
        {
            UniInk_Speed.ExecuteProcess(SyntaxList);
            InkSyntaxList.Recover(SyntaxList);
        }

        //[Benchmark] [Test]
        public void TEST_SCRIPTS_SPEED_COMPILEING()
        {
            var syntax = NUnit_UniInkSpeed.UniInk_Speed.CompileLexerAndFill(ScriptsPAY, 0, ScriptsPAY.Length - 1);
            InkSyntaxList.ReleaseAll(syntax);
        }


        [Benchmark] [Test]
        public void INT_UniInk()
        {
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input1);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input2);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input3);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input4);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input5);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input6);
            // NUnit_UniInkSpeed.Test_Arithmetic_Int(input7);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
            NUnit_UniInkSpeed.Test_Arithmetic_Int(input9);
        }

        //[Benchmark] [Test]
        public void BOOL_UNIINK()
        {
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest1);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest2);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest3);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest4);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest5);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest6);
            NUnit_UniInkSpeed.Test_Arithmetic_Bool(inputBoolTest7);
        }



        private const string FLT = "FLT(Config,var c => GET(c, Rarity) == 2)";

        //[Benchmark] [Test]
        public void TEST_SCRIPTS_SPEED2() => NUnit_UniInkSpeed.Test_Expression_Variable(input9);

        // [Benchmark] [Test]
        public void INT_Limit()
        {
            var res1 = num333 - num3;
            var res2 = num333 * num3;
            var res3 = num333 / num3;
            var res4 = num333     + num3;
            var res5 = num1111111 + num1111111;
            var res6 = num9999999 + num9999999;
            Sum = res1 + res2 + res3 + res4 + res5 + res6;
        }



        // [Benchmark] [Test]
        public void INT_UniInkNoSpeed()
        {
            _uniInkClassic.ScriptEvaluate(input1 + ";");
            _uniInkClassic.ScriptEvaluate(input2 + ";");
            _uniInkClassic.ScriptEvaluate(input3 + ";");
            _uniInkClassic.ScriptEvaluate(input4 + ";");
            _uniInkClassic.ScriptEvaluate(input5 + ";");
            _uniInkClassic.ScriptEvaluate(input6 + ";");
            _uniInkClassic.ScriptEvaluate(input7 + ";");
        }



        public static readonly Dictionary<string, Delegate> Test3 = new() { { HelloWorld, Test2 } };

        public static void LOG(string str) { }



        //  [Benchmark] [Test]
        public void TEST_SCRIPTS()
        {
            _uniInkClassic.ScriptEvaluate(Scripts2);
        }



        public static bool MyTest(MyEnum aa)
        {
            return aa == MyEnum.D;
        }

        public enum MyEnum { A, B, C, D }

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


        public const string        ScriptsPAY = "PAY(Food,100);PAY(Food,100)";
        public       InkSyntaxList SyntaxList;


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


        public class EnumContainer <T> where T : struct, Enum
        {
            private List<T> values = new List<T>();

            public void Add(T value)
            {
                values.Add(value);
            }

            public IEnumerable<T> GetValues()
            {
                return values;
            }
        }


        public int num333     = 333;
        public int num3       = 3;
        public int num1111111 = 1111111;
        public int num9999999 = 9999999;
        public int Sum;

        private readonly UniInk_Classic _uniInkClassic = new UniInk_Classic();

        private const string input1     = "2222+(333-3+3-3)";
        private const string input2     = "333-3";
        private const string input3     = "333*3";
        private const string input4     = "333/3";
        private const string input5     = "333+3";
        private const string input6     = "1111111+1111111";
        private const string input7     = "9999999+9999999";
        private const string input9     = "SUM(SUM(1,2,3),SUM(1,2,3),1) + 123456789";
        private const string input10    = "+123456789";
        private const string HelloWorld = "Hello World";
        private const string Scripts1   = "LOG(\"Hello World ! \" )             ";
        private const string Scripts2   = "var a = 1; a + 12;";
        private const string Scripts3   = "var a = 1; a + 12";
        private const string inputEmpty = "  ";
        private const string Scripts4   = "var a = 123 ;  var b = a + 1 ; a + b    ";



        public const string inputBoolTest1 = "!true && false || true && false";
        public const string inputBoolTest2 = "1 > 2 || 2 > 1 || 2==1         ";
        public const string inputBoolTest3 = "1 < 2 || 2 ==1 || 2 < 1        ";
        public const string inputBoolTest4 = "1 >= 2 && 2 >= 1               ";
        public const string inputBoolTest5 = "1 <= 2 || 2 <= 1               ";
        public const string inputBoolTest6 = "1 == 2 && 2 == 1               ";
        public const string inputBoolTest7 = "1 != 2 || 2 != 1               ";


        public static Action<string> Test2 = LOG;
    }

}