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

        //[TestCase("9*((1+2*3)/2)")]
        public static void TestTemp_CreateObjectStack(string input)
        {
            var res = (UniInk_Speed.InkValue)UniInk_Speed.Evaluate(input);

            var result = res!.Value_int;
            UniInk_Speed.InkValue.Release(res);
        }

        //[Test]
        public static void TestTemp_StructTest() { }
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



/// <summary>Evaluate Parenthis _eg: (xxx)</summary>
/// <remarks>the match will recursive execute <see cref="Evaluate"/> </remarks>
/// <param name="expression"> the expression to Evaluate     </param>
/// <param name="stack"> the object stack to push or pop     </param>
/// <param name="i">the <see cref="expression"/> start index </param>
/// <returns> the evaluate is success or not </returns> 
// private static bool EvaluateParenthis(string expression, List<object> stack, ref int i)
// {
//     if (StartsWithParenthisFromIndex(expression, i, out var len))
//     {
//         var temp   = new List<object>();
//         var _stack = LexerAndFill(expression, i + 1, i + len - 1, temp);
//         var result = ProcessQueue(_stack);
//         stack.Add(result);
//         i += len;
//         return true;
//     }
//
//     return false;
// }