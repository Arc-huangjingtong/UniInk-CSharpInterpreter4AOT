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
        public static readonly UniInk_Speed UniInk_Speed = new();

        #region 1.Basic: Arithmetic Test , return result InkValue and Release it


        [Repeat(10000)]
        [TestCase("+123456789             ", ExpectedResult = +123456789)]
        [TestCase("+123456789+987654321   ", ExpectedResult = +123456789 + 987654321)]
        [TestCase("111 * 111 * 3 /3*3/3   ", ExpectedResult = 111 * 111 * 3 / 3 * 3 / 3)]
        [TestCase("3333333-3+3+  3 - 3    ", ExpectedResult = 3333333 - 3 + 3 + 3 - 3)]
        [TestCase("9*(1+1 + 1 + 1 + 1+1+1)", ExpectedResult = 9 * (1  + 1 + 1 + 1 + 1 + 1 + 1))]
        [TestCase("   999999 + 999999     ", ExpectedResult = 999999 + 999999)]
        [TestCase("9*((1+(1+1)+(1+1))+1+1)", ExpectedResult = 9 * ((1 + (1 + 1) + (1 + 1)) + 1 + 1))]
        [TestCase("9 * ( ( 1 + 2 * 3 ) /2)", ExpectedResult = 9 * ((1 + 2 * 3) / 2))]
        public static int Test_Arithmetic_Int(string input)
        {
            var res    = (InkValue)UniInk_Speed.Evaluate(input);
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
            var res    = (InkValue)UniInk_Speed.Evaluate(input);
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
            var res    = (InkValue)UniInk_Speed.Evaluate(input);
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
            var res    = (InkValue)UniInk_Speed.Evaluate(input);
            var result = res!.Value_bool;
            InkValue.Release(res);
            return result;
        }


        #endregion


        #region 2.Complex: variable declaration and assignment


        [Repeat(10000)]
        [TestCase("var a = 123 ;  var b = a + 1 ; a + b    ",         ExpectedResult = 123 + 123 + 1)]
        [TestCase("var aaa= 123 ;  var bbb =aaa + 1 ; aaa + bbb    ", ExpectedResult = 123 + 123 + 1)]
        public static int Test_Expression_Variable(string input)
        {
            var res = UniInk_Speed.Evaluate(input);

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
            var res = UniInk_Speed.Evaluate(input);

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
            var res = UniInk_Speed.Evaluate(input);

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
            var res = UniInk_Speed.Evaluate(input);

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


        [Repeat(10000)]
        [TestCase("SUM(SUM(1,2,3),SUM(1,2,3),1) + 123456789 ", ExpectedResult = 1 + 2 + 3 + 1 + 2 + 3 + 1 + 123456789)]
        [TestCase("SUM(SUM(1,2,3),SUM(1,2,3),1) + SUM(1,2,3)", ExpectedResult = 1 + 2 + 3 + 1 + 2 + 3 + 1 + 1 + 2 + 3)]
        public static int Test_Expression_Function(string input)
        {
            var res = UniInk_Speed.Evaluate(input);

            var result = 0;

            if (res is InkValue inkValue)
            {
                result = inkValue.Value_int;
                InkValue.Release(inkValue);
            }

            return result;
        }

        [TestCase("PAY(Food,100);PAY(Food,100)")]
        [TestCase("LOG(\"Hello World ! \"+\"Hello World ! \" ) ")]
        public static void Test_Expression_Function2(string input)
        {
            var res = UniInk_Speed.Evaluate(input);

            if (res is InkValue inkValue)
            {
                InkValue.Release(inkValue);
            }

            Console.WriteLine(InkValue.GetTime);
            Console.WriteLine(InkValue.ReleaseTime);
        }


        #endregion


        #region 4.Best Practice: Lambda Function


        //[Repeat(10000)]
        [TestCase("FLT(Config,var c => GET(c, Rarity) == 2  && GET(c, ID) == 1)")]
        public static void Test_Expression_Lambda(string input)
        {
            var res = UniInk_Speed.Evaluate(input);

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



        [Test]
        public static void Test_Temp() { }


        public enum MyEnum { Food }

        public int FoodNum;

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

        public static List<Card> FLT(IList<Card> cards, Predicate<Card> func)
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


        public bool isInit;

        [OneTimeSetUp]
        public void Test_Initiation()
        {
            if (isInit) return;


            isInit = true;
            UniInk_Speed.RegisterFunction("SUM", new(list => InkValue.GetIntValue((int)(InkValue)list[0] + (int)(InkValue)list[1] + (int)(InkValue)list[2])));
            UniInk_Speed.RegisterFunction("LOG", new(prms =>
            {
                Console.WriteLine(prms[0]);
                return null;
            }));

            UniInk_Speed.RegisterFunction("PAY", new(prms =>
            {
                var param1 = (MyEnum)((int)(InkValue)prms[0]);

                var param2 = (int)((InkValue)(prms[1]));

                PAY(param1, param2);
                return null;
            }));

            UniInk_Speed.RegisterFunction("FLT", new(prms =>
            {
                var param1 = (List<Card>)((InkValue)prms[0]).Value_Object;
                var param2 = (Predicate<object>)((InkValue)prms[1]).Value_Object;

                return FLT(param1, param2);
            }));


            UniInk_Speed.RegisterFunction("GET", new(prms =>
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


            UniInk_Speed.RegisterFunction("CRE", new(prms =>
            {
                var param1 = (int)(InkValue)prms[0];
                var param2 = (int)(InkValue)prms[1];

                return new Card() { ID = param1, Rarity = param2 };
            }));

            UniInk_Speed.RegisterVariable("Food",   InkValue.GetIntValue(0));
            UniInk_Speed.RegisterVariable("Rarity", InkValue.GetIntValue(5));
            UniInk_Speed.RegisterVariable("ID",     InkValue.GetIntValue(0));
            UniInk_Speed.RegisterVariable("Config", InkValue.GetObjectValue(Config));
        }
    }

}
//不优雅的地方：(头顶的乌云QAQ
//如何处理枚举值？
//在函数中，将参数先转成InkValue，再转成int，再转成enum，实现0GC
//没有办法，为了避免枚举类型装箱,只能这样做，InkValue会在函数结束后自动释放
//如果用泛型处理,则不可避免的反射出Type类型，或者是运行时创建动态类型，这不符合设计理念，其实上述已经是最优解，如果有人能自动化这个步骤
//那么这个解决方案就是最优解了



// ☑️ PAY(Food, 150);
// ☑️ var cards = FLT(Config,c => GET(c, Rarity) == 2);
//    var scard = FLT(cards ,c => GET(c, TYPE)   == 1);
// ☑️var card  = PICK(scard);
// ☑️GAIN(C,card);
// ☑️REFRESH(1000205);


// Architecture Design
// 1. Lexer      : 词法分析器,把字符串转换成Token
// 2. Parser     : 语法分析器,把Token转换成AST
// 3. Interpreter: 解释器,执行AST

// 词法分析器目前支持:常用基本类型(int,float,double,string,char,bool)的识别 , 函数名的识别 , 运算符的识别 , 空格的忽略

//GC消耗: 近乎完美的实现了0GC

//Feature:
// 1.☑️️️ 支持运算符的优先级,支持括号改变优先级
// 2.❎ 支持函数的调用,包括内置得lambda函数,自定义函数,尽管这很难实现,但是这是一个很重要的特性
// 3.☑️ 支持变量的声明和赋值[var] [=],支持变量的作用域
// 4.❎ 支持if else 语句,等基本的逻辑语句
// 5.☑️️️ 支持类型的隐式转换
// 6.☑️ 支持沙盒环境,不允许访问外部的变量和函数
// 7.☑️ 支持自定义的运算符
// 8.☑️ 字符串的运算优化,在解释器中拼接字符串,或者进行字符串的操作时,可以减少很多的GC

// TODO_LIST:
//😊 [浮点型，整形，双精度] 基本的数学运算(加减乘除, 乘方, 余数, 逻辑运算, 位运算) 二元运算符 ,且支持自动优先级 
// 2. 非成员方法调用(单参数,多参数,默认参数,可变参数,泛型方法?) 所用使用的函数必须全部是注册的方法，不应该支持调用未注册的方法，成员方法等


//暂时不支持
// || 和 && 导致得短路特性,本质上很容易实现,但是这个特性不是所有人都需要,所以暂时不支持，可以自行实现

// Feature Point: 最终目标是完善功能，用于替换DuelAction中的代码
// ☑️:DMG(CARD(G1), GET(G1, 2004)); //函数嵌套的调用
// ☑️:var card = C1;                //支持自定义变量的声明，且变量是自定义类型
// ☑️:var cost = GET(card, COST);
// ☑️:LOG(""Debug测试---C1cost :"" +cost); //字符串运算：是否需要支持(现已支持)？ 字符串的拼接在解释器中，能体现出很大的优势
// var a1 = PICK(FLT(CardConfig,c => GET(c,TYPE)==1));
// 标识符是【=>】左边是变量，右边是表达式，表达式中的变量是左边变量,左边变量是传入的参数
// var _cards != FLT(DECK(P1ID), x!=>GET(x, TYPE) !=!= 1&&GET(x, ATK) >!= 2); 最难的，FLT，自定义解析流程或者自定义的lambda表达式
// LOG(""一共在卡组中检索到"" + NUM(_cards) + ""张符合条件的卡牌"");
// var _card != PICK(_cards);
// LOG(""随机抽取了一张卡牌,它的ID是"" + GET(_card, ID));
// SET(_card, POS, 5);
// ADD(_card, ATK, 2);
// LOG(""Debug测试---P1L:"" +GET(CARD(P1ID, 1)?[0], ID));
// LOG(""Debug测试---P1R:"" +GET(CARD(P1ID, 2)?[0], ID));
// LOG(""Debug测试---P1M:"" +GET(CARD(P1ID, 3)?[0], ID));
// LOG(""Debug测试---P1H && :"" +NUM(CARD(P1ID, 5)));
//
// LOG(""Debug测试---P2L:"" +GET(CARD(P2ID, 1)?[0], ID));
// LOG(""Debug测试---P2R:"" +GET(CARD(P2ID, 2)?[0], ID));
// LOG(""Debug测试---P2M:"" +GET(CARD(P2ID, 3)?[0], ID));
// LOG(""Debug测试---P1H && :"" +NUM(CARD(P2ID, 5)));"
// "DMG_P(P2, 1);
// DMG_P(P1, 1);"
// "_r != GRID(P1ID, R)
// _l != GRID(P1ID, L)
// _m != GRID(P1ID, M)
// _m2 != GRID(P2ID(), M)
//
// AAB(_r, 2001, true, 2) --2001 鼓舞
// AAB(_l, 2002, true, 2) --2002 冷箭
// AAB(_m, 2003, true, 2) --2003 驻营
// AAB(_m2, 2004, true, 2) --2004 灼烧
// "
//
// ADD(P1,"DAMAGE_NUM",1);
// BUF(C1,ATK,1);
// ADD(Target,soliders)//Target是打出的目标,soliders代指这张卡描述上的兵力,也可以自定义
// LOG("技能触发了");
//
//
//
//
// DEL(C1);
// ADD(Target,Soldiers);
// ADD(ALL1,Soldiers);
//
//
// "SET(C1, INJURY, 0);
// RAB(C1,1001);"
// DMG(OPP(C1), 2);
// DMG(OPP(C1), 4);
// "var v1=GET(C1,DAMAGE)*2;
// SET(C1,DAMAGE,v1);"
// "var v1=GET(C1,ATK);
// DMG(OPP(C1), v1);"
// SET(C1,DAMAGE,0);
// EXA(C1,OPP(C1));
// ADD(C1,INJURY,-2);
// ADD(C1,INJURY,-1);
// "var v1=GET(C1,ATK)*2;
// var v2=GET(C1,HP)*2;
// SET(C1,ATK,v1);
// SET(C1,HP,v2);"
// DES(C3);
// EXC(C3);
// BUF(C1, ATK, 1);
// SET(C1,EARLY,2);
// EXC(C1);
// "var a4 = GET(C1,Shielder);
// BUF(C1,ATK,a4);"
// DMG_P(P2,1);
//
//
// BUF(C2, ATK, 1);
// math.r && om() <!= 0.5  &&  DMG(OPP(C2),1);
// "local a != GET(C2,INJURY)-1;
// SET(C2,INJURY,a);"
//    DMG(CARD(G1), GET(G1, 2004));
//    DMG(CARD(G1), GET(G1, 2004));
//    DMG(ALL1, GET(G1, 2004));
//    DMG(ALL1, GET(G1, 2004));
//
//
//
//
//
//
// BUF(Target,ATK,1);
//
//
//
// "BUF(C1,ATK,3,false);
// BUF(C1,HP,2,false);"
// "<span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">var v1 = GET(C1,HP);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">SET(C1,ATK,v1);</span>"
// "<span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">var v1 = GET(C1,HP);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">SET(C1,ATK,v1);</span>"
// "<span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">AAB(OPP_G(C1), 2004, false, 2);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span>"
// "var v1 = GET(C3,POS);
// var g1 = GRID(P2,v1);
// AAB(g1, 2004, false, 1);"
// "var v1 = DELTA * 3;
// BUF(C1,HP,v1);"
// "<span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">v1 = GET(C1,INJURY);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">DMG_P(P1,v1);</span>"
// <span style="font-family: 宋体;font-size: 13px;color: #FFFFFF;">BUF(C1,ATK,2);</span>
// <span style="font-family: 宋体;font-size: 13px;color: #FFFFFF;">BUF(C1,ATK,2);</span>
// SET(C1,INJURY,3);
// DES(PICK(ALL2));
// DES(C1);DMG_P(P2,10);
// "var v1 = GET(C1,V1)%2;
// BUF(C1,ATK,v1);
// BUF(C1,HP,2*v1);
// ADD(C1,V1,1);"
// <span style="font-family: 宋体;font-size: 13px;color: #FFFFFF;">BUF(M1, ATK, 2,false);</span>
// "<span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">var v1 = GET(M1,ATK);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">BUF(C1, ATK, v1);</span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">
// </span><span style=""font-family: 宋体;font-size: 13px;color: #FFFFFF;"">BUF(C1, HP, v1);</span>"
// BUF(PICK(ALL2),ATK,-6,false);
//
//
//
//
// "var a1 = ALLShielder1;
// var a4 = a1/2;
// BUF(C1,ATK,a4);"
// ADD(OTHER,1,0,0);
// "var a4 = GET(C1,Shielder);
// BUF(C1,ATK,a4);"
// "var c1 = FLT(ALL1,GET(c,Shielder)>0);
// AAB(c1,1012);"
// "var a1 = ALLShielder1;
// var a4 = a1/3;
// BUF(C1,ATK,a4);"
// "var a1 = FLT(OTHER,GET(c,20000401)!=0);
// ADD(a1,1,0,0);"
// "var a4 = GET(C1,Shielder);
// BUF(C1,ATK,a4);"
// "var c1 = FLT(ALL1,GET(c,Archer)>0);
// AAB(c1,1013);"
// "var c1 = FLT(ALL1,GET(c,Archer)>0);
// AAB(c1,1013);"
// "var v1=GET(C1,DAMAGE)*2;
// SET(C1,DAMAGE,v1);"
// DMG_P(P2,1);
// "var a1 = GET(P2,""DAMAGE_NUM"");
// var a2 = a1/3;
// BUF(C1,ATK,a2);"
// BUF(C1,ATK,1);
// "var v1=GET(C2,DAMAGE)*2;
// SET(C2,DAMAGE,v1);"
// BUF(C1,ATK,-4);
// CHANGE(C1,4,1);
// DMG(C1,3);
// BUF(C1,ATK,2);
// ADD(ALL1,1);
// "var a4 = SoldierTypeNum(C1);
// BUF(C1,ATK,a4);"
// "var a1 = GET_D(TURN_NUM);
// BUF(C1,ATK,a1,false);"
// BUF(C1,ATK,99);
// DES(C1);
// ADD(C1,INJURY,-3);
// SET(C1,INJURY,0);
// BUF(C1,ATK,3);
// BUF(C1,ATK,1);
// "var a4 = SoldierTypeNum(C1);
// BUF(C1,ATK,a4);"
// "var a1 = GET(C1,Archer)+GET(C1,Rider)+GET(C1,Shielder);
// var a4 = a1/3;
// BUF(C1,ATK,a4);"
// BUF(C1,ATK,1,true);
// CHANGE(C1,1,2);
// CHANGE(C1,4,2);
// BUF(C1,ATK,6);
// ADD(OTHER,0,0,ARG4);
// "var a1 = GET(L1,Shielder);
// var a2 = GET(M1,Shielder);
// var a3 = GET(R1,Shielder);
// SET(L1,Shielder,0);
// SET(M1,Shielder,0);
// SET(R1,Shielder,0);
// var a4 = a1+a2+a3;
// BUF(C1,ATK,a4,true);"
// ADD(ALL1,1,0,0);
// BUF(C1,ATK,1,false);
// ADD(C1,1,0,0);
// BUF(C1,ATK,5);
// BUF(C1,ATK,2e);
// ADD(C1,INJURY,-1);
// CHANGE(C1,4,2);
// CHANGE(C1,4,1);
// "if (CHANCE(50))
//     return CHANGE(C1,4,1);
// else
//     return CHANGE(C1,4,2);"
// "if (CHANCE(50))
//     return CHANGE(C1,4,1);
// else
//     return CHANGE(C1,4,2);"
// BUF(C1,ATK,6);
// ADD(C1,INJURY,-1);
// "var a1 = PICK(FLT(CardConfig,GET(c,TYPE)==1));
// CRE(a1,P1,7);
// ADD(P1,""GETCARD1_NUM"",1);"
// "var a1 = GET(P1,""GETCARD1_NUM"");
// BUF(C1,ATK,a1,true);"
// "var a1 = PICK(FLT(CardConfig,GET(c,TYPE)==1));
// CRE(a1,P1,7);
// ADD(P1,""GETCARD1_NUM"",1);"
// "var a1 = PICK(FLT(CardConfig,GET(c,TYPE)==1));
// CRE(a1,P1,7);
// ADD(P1,""GETCARD1_NUM"",1);"
// BUF(OTHER,ATK,1);
// BUF(ALL1,ATK,1);
// "var a1 = GET(R1,ATK)+GET(M1,ATK);
// BUF(C1,ATK,a1,true);"
// "var a1 = GET(L1,ATK)+GET(M1,ATK);
// BUF(C1,ATK,a1,true);"
// "var a1 = GET(R1,ATK)+GET(L1,ATK);
// BUF(C1,ATK,a1,true);"
// "SET_D(RestTurnNum_Officer,6);
// DES(C1);
// SET_D(RestTurnNum_Officer,2);"
// "var c1 = NEAR(C1);
// ADD(c1,3);
// BUF(ALL1,ATK,2,true);"
// "var c1 = NEAR(C1);
// ADD(c1,1);
// BUF(C1,ATK,6,true);"
// BUF(C1,ATK,3);
// BUF(C1,ATK,3);
// BUF(C1,ATK,5);

// if (collection is ICollection<T> objs)
// {
//     int count = objs.Count;
//     if (count > 0)
//     {
//         this.EnsureCapacity(this._size + count);
//         if (index < this._size)
//             Array.Copy((Array) this._items, index, (Array) this._items, index + count, this._size - index);
//         if (this == objs)
//         {
//             Array.Copy((Array) this._items, 0,             (Array) this._items, index,     index);
//             Array.Copy((Array) this._items, index + count, (Array) this._items, index * 2, this._size - index);
//         }
//         else
//         {
//             T[] array = new T[count];
//             objs.CopyTo(array, 0);
//             array.CopyTo((Array) this._items, index);
//         }
//         this._size += count;
//     }
// }
// else
// {
//     foreach (T obj in collection)
//         this.Insert(index++, obj);
// }
// 上述代码摘自List<T>的AddRange方法,我有一个疑问，为什么需要创建一个新的数组，然后再拷贝到原数组中，而不是直接拷贝到原数组中呢？