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
        public Benchmark_UniInkSpeed() => Initiation();

        [OneTimeSetUp]
        public void Initiation()
        {
            var test = new NUnit_UniInkSpeed();

            test.Test_Initiation();

            //Compile
            //SyntaxList = NUnit_UniInkSpeed.UniInk_Speed.CompileLexerAndFill(ScriptsPAY, 0, ScriptsPAY.Length - 1);
        }


        public const string ScriptsPAY = "PAY(Food,100);PAY(Food,100)";



        public InkSyntaxList SyntaxList;

        // [Benchmark] [Test]
        public void TEMP()
        {
            var inkValue0 = InkValue.GetIntValue(0);
            var inkValue2 = InkValue.GetIntValue(2);

            PAY(inkValue0, inkValue2, this);

            InkValue.Release(inkValue0);
            InkValue.Release(inkValue2);
        }

        public enum MyEnum2 { Food }

        public int FoodNum;

        public const int Food = (int)MyEnum2.Food;

        public static void PAY(object obj1, object obj2, object obj3)
        {
            var num1  = (MyEnum2)(int)(InkValue)obj1;
            var num2  = (int)(InkValue)obj2;
            var @this = (Benchmark_UniInkSpeed)obj3;

            if (num1 == MyEnum2.Food)
            {
                @this.FoodNum += num2;
            }
        }



        //   [Benchmark] [Test]
        public void TEST_SCRIPTS_SPEED_COMPILE()
        {
            NUnit_UniInkSpeed.UniInk_Speed.ExecuteProcess(SyntaxList);
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


        [Benchmark] [Test]
        public void TEST_SCRIPTS_SPEED() => NUnit_UniInkSpeed.Test_Expression_Variable(ScriptsPAY);

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
            _uniInk.ScriptEvaluate(input1 + ";");
            _uniInk.ScriptEvaluate(input2 + ";");
            _uniInk.ScriptEvaluate(input3 + ";");
            _uniInk.ScriptEvaluate(input4 + ";");
            _uniInk.ScriptEvaluate(input5 + ";");
            _uniInk.ScriptEvaluate(input6 + ";");
            _uniInk.ScriptEvaluate(input7 + ";");
        }



        public static readonly Dictionary<string, Delegate> Test3 = new() { { HelloWorld, Test2 } };

        public static void LOG(string str) { }



        //  [Benchmark] [Test]
        public void TEST_SCRIPTS()
        {
            _uniInk.ScriptEvaluate(Scripts2);
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

        private readonly UniInk _uniInk = new UniInk();

        private const string input1     = "2222+(333-3+3-3)";
        private const string input2     = "333-3";
        private const string input3     = "333*3";
        private const string input4     = "333/3";
        private const string input5     = "333+3";
        private const string input6     = "1111111+1111111";
        private const string input7     = "9999999+9999999";
        private const string  input9     = "SUM(SUM(1,2,3),SUM(1,2,3),1) + 123456789";
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

        public string Name;

        public static Action<string> Test2 = LOG;
    }

}