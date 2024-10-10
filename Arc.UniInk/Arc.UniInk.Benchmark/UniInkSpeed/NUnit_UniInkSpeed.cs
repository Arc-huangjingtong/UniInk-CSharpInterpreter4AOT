namespace Arc.UniInk.NUnitTest
{

    /*******************************************************************************************************************
    *  📰 Title    :  UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)              *
    *  🔖 Version  :  1.0.0                                                                                           *
    *  😀 Author   :  Arc (https://github.com/Arc-huangjingtong)                                                      *
    *  🔑 Licence  :  MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)     *
    *  🤝 Support  :  [.NET Framework 4+] [C# 9.0+] [IL2CPP Support]                                                  *
    *  📝 Desc     :  [High performance] [zero box & unbox] [zero GC!] [zero reflection runtime] [Easy-use]           *
    *  📦 State    :  [Developing] [0GC]                                                                              *
    /*******************************************************************************************************************/

    // ReSharper disable RedundantLogicalConditionalExpressionOperand
    // ReSharper disable PartialTypeWithSinglePart
    using Arc.UniInk;
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;


    [TestFixture]
    public sealed partial class NUnit_UniInkSpeed
    {
        public static readonly UniInk_Speed Ink = new();

        public bool isInit;

        [OneTimeSetUp]
        public void Test_Initiation()
        {
            if (isInit) return;

            isInit = true;

            Ink.RegisterFunction("SUM", new(list => InkValue.GetIntValue((int)(InkValue)list[0] + (int)(InkValue)list[1] + (int)(InkValue)list[2])));
            Ink.RegisterFunction("LOG", new(prms =>
            {
                Console.WriteLine(prms[0]);
                return null;
            }));

            Ink.RegisterFunction("PAY", new(prms =>
            {
                var param1 = (MyEnum)((int)(InkValue)prms[0]);

                var param2 = (int)((InkValue)(prms[1]));

                PAY(param1, param2);
                return null;
            }));


            Ink.RegisterFunction("GET", new(prms =>
            {
                var param1 = (Card)((InkValue)prms[0]).Value_Object;
                var param2 = (int)(InkValue)prms[1];

                return param2 switch
                {
                    0 => InkValue.GetIntValue(param1.ID)     //
                  , 5 => InkValue.GetIntValue(param1.Rarity) //
                  , _ => InkValue.GetIntValue(0)
                };
            }));


            Ink.RegisterFunction("CRE", new(prms =>
            {
                var param1 = (InkValue)prms[0];
                var param2 = (InkValue)prms[1];

                return new Card() { ID = param1, Rarity = param2 };
            }));

            Ink.RegisterFunction("FLT", new(prms =>
            {
                var param1 = (List<Card>)((InkValue)prms[0]).Value_Object;
                var param2 = (Predicate<object>)((InkValue)prms[1]).Value_Object;

                return FLT(param1, param2);
            }));


            static List<Card> FLT(IList<Card> cards, Predicate<Card> func)
            {
                var list = new System.Collections.Generic.List<Card>();

                foreach (var card in cards)
                {
                    if (func(card))
                    {
                        list.Add(card);
                    }
                }

                return list;
            }


            Ink.RegisterVariable("grower", InkValue.SetGetter(value =>
            {
                value.ValueType = TypeCode.Int32;
                value.Value_int = grower++;
            }));

            Ink.RegisterVariable("Target", InkValue.SetGetter(value =>
            {
                value.ValueType    = TypeCode.Object;
                value.Value_Object = Target;
            }));


            Ink.RegisterVariable("Food",   InkValue.GetIntValue(0));
            Ink.RegisterVariable("Rarity", InkValue.GetIntValue(5));
            Ink.RegisterVariable("ID",     InkValue.GetIntValue(0));
            Ink.RegisterVariable("Config", InkValue.GetObjectValue(Config));
        }


        #region SP : Features Test


        [Repeat(10000)]
        [TestCase("999*123123*321321/999/666 ", ExpectedResult = 232)] // Computation overflow
        public static object Test_Expression_Extreme(string input)
        {
            var result = Ink.Evaluate(input);
            return result;
        }


        #endregion


        #region 1.Basic: Arithmetic Test , return result InkValue and Release it


        [Repeat(10000)]
        [TestCase("+123456789             ", ExpectedResult = +123456789)]
        [TestCase("+123456789+987654321   ", ExpectedResult = +123456789 + 987654321)]
        [TestCase("111 * 111 * 3 /3*3/3   ", ExpectedResult = 111 * 111 * 3 / 3 * 3 / 3)]
        [TestCase("3333333-3+3+  3 - 3    ", ExpectedResult = 3333333 - 3 + 3 + 3 - 3)]
        [TestCase("9*(1+1 + 1 + 1 + 1+1+1)", ExpectedResult = 9 * (1  + 1 + 1 + 1 + 1 + 1 + 1))]
        [TestCase("   999999 + 999999     ", ExpectedResult = 999999 + 999999)]
        [TestCase("9*((1+(1+1)+(1+1))+1+1)", ExpectedResult = 9 * ((1 + (1 + 1) + (1 + 1)) + 1 + 1))]
        [TestCase("9*((1+(2+3)*(4+5))+6+7)", ExpectedResult = 9 * ((1 + (2 + 3) * (4 + 5)) + 6 + 7))]
        [TestCase("9 * ( ( 1 + 2 * 3 ) /2)", ExpectedResult = 9 * ((1 + 2       * 3) / 2))]
        //[TestCase("9 * +5 ",                 ExpectedResult = 9 * +5)] not support but 9 * (+5) is support
        public static int Test_Arithmetic_Int(string input)
        {
            var res    = (InkValue)Ink.Evaluate(input);
            var result = res!.Value_int;
            InkValue.Release(res);

            return result;
        }

        [Repeat(10000)]
        [TestCase("   999999.9999f + 999999.9999f     ",             ExpectedResult = 999999.9999f + 999999.9999f)]
        [TestCase("9.9f*((1.1f+(1.1f+1.1f)+(1.1f+1.1f))+1.1f+1.1f)", ExpectedResult = 9.9f * ((1.1f + (1.1f + 1.1f) + (1.1f + 1.1f)) + 1.1f + 1.1f))]
        [TestCase("+123456789.987654321f  ",                         ExpectedResult = 123456789.987654321f)]
        [TestCase("+123456789.987654321f + 987654321.123456789f",    ExpectedResult = 123456789.987654321f + 987654321.123456789f)]
        [TestCase("111.111f * 111.111f * 3.3f /3.3f*3.3f/3.3f",      ExpectedResult = 111.111f * 111.111f * 3.3f / 3.3f * 3.3f / 3.3f)]
        [TestCase("3333333.3333333f-3.3f+3.3f+  3.3f - 3.3f",        ExpectedResult = 3333333.3333333f - 3.3f + 3.3f + 3.3f - 3.3f)]
        public static float Test_Arithmetic_Float(string input)
        {
            var res    = (InkValue)Ink.Evaluate(input);
            var result = res!.Value_float;
            InkValue.Release(res);

            return result;
        }


        [Repeat(10000)]
        [TestCase("   999999.999d + 999999.999d     ",            ExpectedResult = 999999.999 + 999999.999)]
        [TestCase("9.9*((1.1+(1.1+1.1)+(1.1+1.1))+1.1+1.1)",      ExpectedResult = 9.9 * ((1.1 + (1.1 + 1.1) + (1.1 + 1.1)) + 1.1 + 1.1))]
        [TestCase("+123456789.987654321d  ",                      ExpectedResult = 123456789.987654321)]
        [TestCase("+123456789.987654321d + 987654321.123456789d", ExpectedResult = 123456789.987654321 + 987654321.123456789)]
        [TestCase("111.111 * 111.111 * 3.3 /3.3*3.3/3.3",         ExpectedResult = 111.111 * 111.111 * 3.3 / 3.3 * 3.3 / 3.3)]
        [TestCase("3333333.3333333-3.3+3.3+  3.3 - 3.3",          ExpectedResult = 3333333.3333333 - 3.3 + 3.3 + 3.3 - 3.3)]
        public static double Test_Arithmetic_Double(string input)
        {
            var res    = (InkValue)Ink.Evaluate(input);
            var result = res!.Value_double;
            InkValue.Release(res);

            return result;
        }



        [Repeat(10000)]
        [TestCase("!true && false || true && false ", ExpectedResult = (!true && false) || (true && false))]
        [TestCase("1 > 2 || 2 > 1 || 2==1          ", ExpectedResult = 1 > 2            || 2 > 1  || 2 == 1)]
        [TestCase("1 < 2 || 2 ==1 || 2 < 1         ", ExpectedResult = 1 < 2            || 2 == 1 || 2 < 1)]
        [TestCase("1 >= 2 && 2 >= 1                ", ExpectedResult = 1 >= 2 && 2           >= 1)]
        [TestCase("1 <= 2 || 2 <= 1                ", ExpectedResult = 1 <= 2 || 2           <= 1)]
        [TestCase("1 == 2 && 2 == 1                ", ExpectedResult = 1 == 2 && 2           == 1)]
        [TestCase("1 != 2 || 2 != 1                ", ExpectedResult = 1 != 2 || 2           != 1)]
        public static bool Test_Arithmetic_Bool(string input)
        {
            var res    = (InkValue)Ink.Evaluate(input);
            var result = res!.Value_bool;
            InkValue.Release(res);
            return result;
        }


        #endregion


        #region 2.Complex: variable declaration and assignment


        [Repeat(10000)]
        [TestCase("var a = 123 ;;  var b = a + 1 ; a + b    ",        ExpectedResult = 123 + 123 + 1)]
        [TestCase("var aaa= 123 ;  var bbb =aaa + 1 ; aaa + bbb    ", ExpectedResult = 123 + 123 + 1)]
        public static int Test_Expression_Variable(string input)
        {
            var res = Ink.Evaluate(input);

            var result = 0;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_int;
                InkValue.Release(inkValue);
            }

            return result;
        }

        [Repeat(10000)]
        [TestCase("var a = 123f ;  var b = a + 1f ; a + b    ", ExpectedResult = 123f + 123f + 1f)]
        public static float Test_Expression_Variable2(string input)
        {
            var res = Ink.Evaluate(input);

            var result = 0f;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_float;
                InkValue.Release(inkValue);
            }

            return result;
        }


        [Repeat(10000)]
        [TestCase("var a = 123d ;  var b = a + 1d ; a + b    ", ExpectedResult = 123d + 123d + 1d)]
        public static double Test_Expression_Variable3(string input)
        {
            var res = Ink.Evaluate(input);

            var result = 0d;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_double;
                InkValue.Release(inkValue);
            }

            return result;
        }


        [Repeat(10000)]
        [TestCase("var a = true ;  var b = !a ; a && b         ", ExpectedResult = true && !true)]
        [TestCase("var a = CRE(1,3) ;   GET(a, Rarity) == 3    ", ExpectedResult = true)]
        public static bool Test_Expression_Variable4(string input)
        {
            var res = Ink.Evaluate(input);

            var result = false;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_bool;
                InkValue.Release(inkValue);
            }

            return result;
        }


        #endregion


        #region 3.Function: Function call


        //[Repeat(10000)]
        [TestCase("SUM(SUM(1,2,3),SUM(1,2,3),1) + 123456789 ",  ExpectedResult = 1         + 2 + 3 + 1 + 2 + 3 + 1 + 123456789)]
        [TestCase("SUM(SUM(1,2,-3),SUM(1,2,3),1) + SUM(1,2,3)", ExpectedResult = 1 + 2 - 3 + 1 + 2 + 3 + 1 + 1 + 2 + 3)]
        [TestCase("SUM(1,1-2,3)",                               ExpectedResult = 3)]
        [TestCase("SUM(1,-2,3)",                                ExpectedResult = 2)]
        [TestCase("SUM(1,2,-3)",                                ExpectedResult = 1 + 2 - 3)]
        [TestCase("SUM(1,2,3);SUM(1,2,3);",                     ExpectedResult = 6)]
        [TestCase("SUM(1,2,3);",                                ExpectedResult = 6)]
        public static int Test_Expression_Function(string input)
        {
            var res = Ink.Evaluate(input);

            var result = 0;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_int;
                InkValue.Release(inkValue);
            }

            Console.WriteLine(InkValue.GetTime);
            Console.WriteLine(InkValue.ReleaseTime);

            return result;
        }

        [TestCase("PAY(Food,100);PAY(Food,100)")]
        [TestCase("LOG(\"Hello World ! \"+\"Hello World ! \" ) ")]
        public static void Test_Expression_Function2(string input)
        {
            var res = Ink.Evaluate(input);

            if (res is InkValue inkValue)
            {
                InkValue.Release(inkValue);
            }

            Console.WriteLine(InkValue.GetTime);
            Console.WriteLine(InkValue.ReleaseTime);
        }


        #endregion


        #region 4.Best Practice: Lambda Function


        [Repeat(10000)]
        [TestCase("FLT(Config,var b => GET(b, Rarity) == 2)")]
        [TestCase("var cards = FLT(Config,var c => GET(c, Rarity) == 2  && GET(c, ID) == 1)")]
        public static void Test_Expression_Lambda(string input)
        {
            var res = Ink.Evaluate(input);

            if (res is InkValue inkValue)
            {
                if (inkValue.Value_Object is List<Card> cards)
                {
                    Console.WriteLine(cards.Count);
                    foreach (var card in cards)
                    {
                        Console.WriteLine(card.ID);
                        Console.WriteLine(card.Rarity);
                    }
                }

                InkValue.Release(inkValue);
            }

            // Console.WriteLine(InkValue.GetTime);
            // Console.WriteLine(InkValue.ReleaseTime);
        }


        #endregion


        #region 5.Best Practice: Property Getter (setter will be supported in the future...)


        [Repeat(10000)]
        [TestCase("grower + 100000")]
        [TestCase("GET(Target, 1001)")]
        public static void Test_Expression_Getter(string input)
        {
            var test = Ink.Evaluate(input);
            if (test is InkValue value)
            {
                //Console.WriteLine(value.Value_int); // each time , the result will be different 

                InkValue.Release(value);
            }
        }


        #endregion


        #region SP : Special Test


        [Repeat(10000)]
        [TestCase(" ",  ExpectedResult = null)]
        [TestCase("",   ExpectedResult = null)] // string.Empty
        [TestCase(";;", ExpectedResult = null)]
        [TestCase(";",  ExpectedResult = null)]
        //[TestCase(null, ExpectedResult = null)] // null will throw exception
        public static object Test_Expression_SP(string input)
        {
            return Ink.Evaluate(input);
        }


        #endregion



        [Test]
        public static void Test_Temp()
        {
            const string TestInput_Arithmetic_01 = "12345678+87654321-1*2*3*4*5*6*7*8*9+9*8*7*6*5*4*3*2*1+1*2*3*4*5*6*7*8*9-87654321-12345678"; // 

            var res = Ink.Evaluate(TestInput_Arithmetic_01);

            if (res is InkValue inkValue)
            {
                Console.WriteLine(inkValue.Value_int);
                InkValue.Release(inkValue);
            }
        }

        ///////////////////////////////////////////////  Extension Test ////////////////////////////////////////////////

        //[Repeat(1)]
        ///////////////////////////////////////
        [TestCase("  if ( 1 > 2 )       "
                + "  {                  "
                + "     123             "
                + "  }                  "
                + "  else               "
                + "  {                  "
                + "    return 456       "
                + "  }                  ")]
        ///////////////////////////////////////
        [TestCase("  if ( 1 > 2 )       "
                + "  {                  "
                + "     123             "
                + "  }                  "
                + "  else if ( 3 > 6 )  "
                + "  {                  "
                + "     456             "
                + "  }                  "
                + "  else               "
                + "  {                  "
                + "    return 666       "
                + "  }                  ")]
        ///////////////////////////////////////
        [TestCase("  if ( 1 > 2 )       "
                + "  {                  "
                + "     123             "
                + "  }                  "
                + "  else if ( 3 > 6 )  "
                + "  {                  "
                + "     456;            "
                + "  }                  "
                + "  else if ( 3 < 6 )  "
                + "  {                  "
                + "    return 456;      "
                + "  }                  "
                + "  else               "
                + "  {                  "
                + "    return 666       "
                + "  }                  ")]
        public static void Test_Expression_IfStatements(string input)
        {
            var test = Ink.Evaluate_IfStatement(input);
            if (test is InkValue value)
            {
#if DEBUG
                Console.WriteLine(value.Value_int); // each time , the result will be different
#endif

                InkValue.Release(value);
            }
#if DEBUG
            Console.WriteLine(InkValue.GetTime);
            Console.WriteLine(InkValue.ReleaseTime);
#endif
        }



        ///////////////////////////////////////////////  Helper Object  ////////////////////////////////////////////////

        public enum MyEnum { Food }

        public int grower = 100;

        public static Card Target => new() { ID = 1, Rarity = 2 };


        public       int FoodNum;
        public const int Food = (int)MyEnum.Food;

        public static void PAY(MyEnum @enum, int num)
        {
            if (@enum != MyEnum.Food)
            {
                return;
            }
            else
            {
                num = num > 0 ? num : 0;
                //  @this.FoodNum -= num;
                //Console.WriteLine(" 支付成功 支付了" + num + "元");
            }

            //  @this.FoodNum -= num;
            //Console.WriteLine(" 支付成功 支付了" + num + "元");
        }

        public static void GET(Card card, int id)
        {
            if (card.ID == id)
            {
                Console.WriteLine("ID:" + card.ID);
            }
            else
            {
                Console.WriteLine("ID:" + card.Rarity);
            }
        }


        public class Card
        {
            public int ID;
            public int Rarity;
        }


        public Card CRE(int id, int rarity) => new() { ID = id, Rarity = rarity };


        public List<Card> Config = new List<Card>()
        {
            new Card() { ID = 1, Rarity = 2 }, new Card() { ID = 2, Rarity = 2 }, new Card() { ID = 3, Rarity = 3 }
          , new Card() { ID = 4, Rarity = 4 }, new Card() { ID = 5, Rarity = 5 }, new Card() { ID = 6, Rarity = 6 }
        };
    }

}