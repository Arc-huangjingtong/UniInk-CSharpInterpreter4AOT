namespace Arc.UniInk.NUnitTest
{

    using System;
    using NUnit.Framework;
    using Arc.UniInk;


    [TestFixture]
    public sealed partial class NUnit_UniInkSpeed
    {
        //private static readonly UniInk_Speed Ink = new UniInk_Speed();

        [Repeat(100000)]
        [TestCase("123456789+987654321", ExpectedResult = 1111111110)]
        [TestCase("111*111*3/3*3/3",     ExpectedResult = 12__321)]
        [TestCase("3333333-3+3+3-3",     ExpectedResult = 3333333)]
        [TestCase("999999+999999  ",     ExpectedResult = 1999998)]
        public static int Test_EvaluateNumber_Int(string input)
        {
            var res    = (UniInk_Speed.InkValue)UniInk_Speed.Evaluate(input, 0, input.Length);
            var result = res!.Value_int;
            UniInk_Speed.InkValue.Release(res);


            return result;
        }

        [TestCase("9*((1+2*3)/2)")]
        public static void TestTemp_CreateObjectStack(string input)
        {
            var res = (UniInk_Speed.InkValue)UniInk_Speed.Evaluate(input);

            var result = res!.Value_int;
            UniInk_Speed.InkValue.Release(res);
        }

        [Test]
        public static void TestTemp_StructTest()
        {
            var input = "9*((1+2*3)/2)";
            input = "9999999999999";

            Span<char> span = input.ToCharArray();

            var wrapper = new ObjectWrapper();
            wrapper.Value_int = 123;
            wrapper.Value_int = 124;

            wrapper.Value_Meta = span.Slice(1, 3);
            wrapper.Calculate();


            Console.WriteLine(wrapper.Value_int);
        }


        public ref struct ObjectWrapper
        {
            public int   Value_int   { get; set; }
            public bool  Value_bool  { get; set; }
            public char  Value_char  { get; set; }
            public float Value_float { get; set; }

            public object Value_object { get; set; }

            public double Value_double { get; set; }

            public Span<char> Value_Meta { get; set; }
            // public string Value_Meta { get; set; }


            public void Calculate()
            {
                Value_int = 0;
                var indexer = 0;
                for (var i = Value_Meta.Length - 1 ; i >= 0 ; i--)
                {
                    if (char.IsDigit(Value_Meta[i]))
                    {
                        Value_int += (Value_Meta[i] - '0') * (int)Math.Pow(10, indexer);
                        indexer++;
                    }
                }
            }

            public static ObjectWrapper operator +(ObjectWrapper left, ObjectWrapper right)
            {
                left.Calculate();
                right.Calculate();

                var answer = new ObjectWrapper();

                answer.Value_int = left.Value_int + right.Value_int;


                return answer;
            }
        }


        /// <summary>In UniInk , every valueType is structWrapper , No Boxing!</summary>
        public ref struct InkValue
        {
            public enum InkValueType
            {
                Int
              , Float
              , Boolean
              , Double
              , Char
              , String
            }


            //- - - - - - Values
            public int    Value_int;
            public bool   Value_bool;
            public char   Value_char;
            public float  Value_float;
            public double Value_double;

            //- - - - - - Settings
            public bool         isCalculate;
            public Span<char>   Value_Meta;
            public InkValueType ValueType;


            /// <summary>Calculate the value</summary>
            public void Calculate(InkValueType type)
            {
                if (isCalculate) return;


                if (type == InkValueType.Int)
                {
                    Value_int = 0;

                    for (var i = Value_Meta.Length - 1 ; i >= 0 ; i--)
                    {
                        Value_int += Value_int * 10 + (Value_Meta[i] - '0');
                    }
                }

                isCalculate = true;
            }


            public static InkValue operator +(InkValue left, InkValue right)
            {
                if (!left.isCalculate || !right.isCalculate)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = new InkValue { ValueType = InkValueType.Int };

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int + right.Value_int;
                    answer.isCalculate = true;

                    return answer;
                }

                throw new Exception("not mach left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
            }

            public static InkValue operator -(InkValue left, InkValue right)
            {
                if (!left.isCalculate || !right.isCalculate)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = new InkValue { ValueType = InkValueType.Int };

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int - right.Value_int;
                    answer.isCalculate = true;
                    return answer;
                }

                throw new Exception("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
            }

            public static InkValue operator *(InkValue left, InkValue right)
            {
                if (!left.isCalculate || !right.isCalculate)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = new InkValue { ValueType = InkValueType.Int };

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int * right.Value_int;
                    answer.isCalculate = true;

                    return answer;
                }

                throw new Exception("left.ValueType != InkValueType.Int || right.ValueType != InkValueType.Int");
            }

            public static InkValue operator /(InkValue left, InkValue right)
            {
                if (!left.isCalculate || !right.isCalculate)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = new InkValue { ValueType = InkValueType.Int };

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int / right.Value_int;
                    answer.isCalculate = true;

                    return answer;
                }

                throw new Exception("left.ValueType != InkValueType.Int || right.ValueType != InkValueType.Int");
            }
        }
    }

}


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