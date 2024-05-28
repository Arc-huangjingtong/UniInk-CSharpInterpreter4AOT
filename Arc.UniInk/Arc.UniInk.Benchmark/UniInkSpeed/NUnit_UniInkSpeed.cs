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
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Arc.UniInk;


    [TestFixture]
    public sealed partial class NUnit_UniInkSpeed
    {
        private static readonly UniInk_Speed UniInk_Speed = new();

        [OneTimeSetUp]
        public static void Test_Initiation()
        {
            UniInk_Speed.RegisterFunction("CRE", new(_ => TestInk.Create(), Array.Empty<Type>(), typeof(TestInk)));
            UniInk_Speed.RegisterFunction("SUM", new(list => InkValue.GetIntValue((int)((InkValue)list[0] + (InkValue)list[1] + (InkValue)list[2])), new[] { typeof(int), typeof(int), typeof(int) }, typeof(int)));
            UniInk_Speed.RegisterFunction("LOG", new(prms =>
            {
                Console.WriteLine(prms[0]);
                return null;
            }, new[] { typeof(string) }, typeof(void)));
        }


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
            if (input == "9.9*((1.1+(1.1+1.1)+(1.1+1.1))+1.1+1.1)")
            {
                Console.WriteLine(input);
            }

            var res    = (InkValue)UniInk_Speed.Evaluate(input);
            var result = res!.Value_double;
            InkValue.Release(res);

            return result;
        }



        [Repeat(10000)]
        [TestCase("!true && false || true && false",  ExpectedResult = (!true && false) || (true && false))]
        [TestCase("1 > 2 || 2 > 1 || 2==1         ",  ExpectedResult = 1 > 2            || 2 > 1  || 2 == 1)]
        [TestCase("1 < 2 || 2 ==1 || 2 < 1         ", ExpectedResult = 1 < 2            || 2 == 1 || 2 < 1)]
        [TestCase("1 >= 2 && 2 >= 1               ",  ExpectedResult = 1 >= 2 && 2           >= 1)]
        [TestCase("1 <= 2 || 2 <= 1               ",  ExpectedResult = 1 <= 2 || 2           <= 1)]
        [TestCase("1 == 2 && 2 == 1               ",  ExpectedResult = 1 == 2 && 2           == 1)]
        [TestCase("1 != 2 || 2 != 1               ",  ExpectedResult = 1 != 2 || 2           != 1)]
        public static bool Test_Arithmetic_Bool(string input)
        {
            var res    = (InkValue)UniInk_Speed.Evaluate(input);
            var result = res!.Value_bool;
            InkValue.Release(res);

            return result;
        }



        [Repeat(10000)]
        [TestCase("SUM(SUM(1,2,3),SUM(1,2,3),1) + 123456789")]
        [TestCase("LOG(\"Hello World ! \"+\"Hello World ! \" ) ")]
        [TestCase("var a = CRE()                               ")]
        public static void Test_Expression_Function(string input)
        {
            var res = UniInk_Speed.Evaluate(input);

            if (res is InkValue inkValue)
            {
                //Console.WriteLine(inkValue.Value_String);
                InkValue.Release(inkValue);
            }
        }

        [Repeat(10000)]
        [TestCase("var a = 123 ; a + 123456789              ")]
        public static void Test_Expression_Scripts(string input)
        {
            UniInk_Speed.Evaluate(input);
        }


        [Test]
        public static void Test_Temp() { }


        public static Func<List<object>, object> FuncDelegate2 => list => TestFunc(list[0] as T1, list[1] as TestInk);


        public static int TestFunc(T1 a, TestInk b) => 2;



        public class TestInk
        {
            public static TestInk Create() => new TestInk();
        }


        public class T1 : TestInk { }

        public Action<int> Test22 => c => Console.WriteLine(c);
    }

}



// Architecture Design
// 1. Lexer      : 词法分析器,把字符串转换成Token
// 2. Parser     : 语法分析器,把Token转换成AST
// 3. Interpreter: 解释器,执行AST

// 词法分析器目前支持:常用基本类型(int,float,double,string,char,bool)的识别 , 函数名的识别 , 运算符的识别 , 空格的忽略

//GC消耗:
// 声明变量的时候,会使用一个字符串

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
//
// "
// "LOG(""Debug测试---P1L:"" +GET(CARD(P1ID, 1)?[0], ID));
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