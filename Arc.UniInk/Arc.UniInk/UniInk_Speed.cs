﻿namespace Arc.UniInk
{

    /*******************************************************************************************************************
    *📰 Title    : UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                  *
    *🔖 Version  : 1.0.0                                                                                               *
    *😀 Author   : Arc (https://github.com/Arc-huangjingtong)                                                          *
    *🔑 Licence  : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)         *
    *🤝 Support  : [.NET Framework 4+] [C# 9.0+] [IL2CPP Support]                                                      *
    *📝 Desc     : [High performance] [zero box & unbox] [zero reflection runtime] [Easy-use]                          *
    /*******************************************************************************************************************/

    // ReSharper disable PartialTypeWithSinglePart
    using System;
    using System.Linq;
    using System.Collections.Generic;


    /// <summary> the C# Evaluator easy to use </summary>
    public partial class UniInk_Speed
    {
        /// <summary> Constructor </summary>
        /// <param name="context"  > Set context use as "This" or use internal member variables directly </param>
        /// <param name="variables"> Set variables can replace a key string with value object            </param>
        public UniInk_Speed(object context = null, Dictionary<string, object> variables = null) { }

        /// <summary> Evaluate a expression       </summary>
        /// <returns> return the result object    </returns>
        public static object Evaluate(string expression) => Evaluate(expression, 0, expression.Length);

        /// <summary> Evaluate a expression       </summary>
        /// <returns> return the result object    </returns>
        public static object Evaluate(string expression, int startIndex, int endIndex)
        {
            var syntaxList = LexerAndFill(expression, startIndex, endIndex);
            var evalAnswer = ProcessQueue(syntaxList);

            InkSyntaxList.Release(syntaxList);

            return evalAnswer;
        }

        /// <summary> Clear the cache in UniInk   </summary>
        public static void ClearCache()
        {
            InkValue.ReleasePool();
            InkSyntaxList.ReleasePool();
        }

        private static InkSyntaxList LexerAndFill(string expression, int startIndex, int endIndex)
        {
            var keys = InkSyntaxList.Get();

            for (var i = startIndex ; i <= endIndex && i < expression.Length ; i++)
            {
                var any = false;

                foreach (var parsingMethod in ParsingMethods)
                {
                    if (parsingMethod(expression, keys, ref i))
                    {
                        any = true;
                        break;
                    }
                }

                if (any) continue;
                if (char.IsWhiteSpace(expression[i])) continue;

                InkSyntaxException.Throw($"Invalid character : [{(int)expression[i]}:{expression[i]}] at [{i}  {expression}] ");
            }

            return keys;
        }


        public static object ProcessQueue(InkSyntaxList keys)
        {
            InkSyntaxException.ThrowIfTrue(keys.Count == 0, "Empty expression and Empty stack !");

            ProcessQueue_Parenthis(keys);
            ProcessQueue_Operators(keys, 0, keys.Count - 1);

            var cache = keys.CastOther[0];

            return cache;
        }

        private static void ProcessQueue_Parenthis(InkSyntaxList keys)
        {
            var hasParenthis = true;

            while (hasParenthis)
            {
                int startIndex, endIndex;

                (hasParenthis, startIndex, endIndex) = FindSection(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight);

                if (!hasParenthis) continue;

                ProcessQueue_Operators(keys, startIndex + 1, endIndex - 1);

                keys.SetDirty(startIndex);
                keys.SetDirty(endIndex);

                // var cache = ProcessQueue_Internal(keys, startIndex + 1, endIndex);
                //
                // keys.SetDirty(cache, startIndex, endIndex);
            }
        }

        private static void ProcessQueue_Operators(InkSyntaxList keys, int _startIndex, int _endIndex)
        {
            var hasOperators = true;

            while (hasOperators)
            {
                var (curOperator, index) = GetHighestPriorityOperator(keys, _startIndex, _endIndex);

                if (Equals(curOperator, InkOperator.Empty))
                {
                    hasOperators = false;
                    continue;
                }

                object left  = null;
                object right = null;

                var startIndex = index;
                var endIndex   = index;


                for (var i = index - 1 ; i >= 0 ; i--) // Left
                {
                    if (keys.IndexDirty[i])
                    {
                        if (keys.CastOther[i] == null) continue;

                        left              = keys.CastOther[i];
                        keys.CastOther[i] = null;

                        startIndex = i;

                        break;
                    }

                    left = keys[i];

                    startIndex = i;

                    break;
                }

                for (var i = index + 1 ; i < keys.Count ; i++) // Right
                {
                    if (keys.IndexDirty[i])
                    {
                        if (keys.CastOther[i] == null) continue;

                        right             = keys.CastOther[i];
                        keys.CastOther[i] = null;

                        endIndex = i;

                        break;
                    }

                    right = keys[i];

                    endIndex = i;

                    break;
                }

                if (dic_OperatorsFunc.TryGetValue(curOperator, out var func))
                {
                    var result = func(left, right);

                    keys.SetDirty(result, startIndex, endIndex);
                }
                else
                {
                    InkSyntaxException.Throw($"Unknown Operator : {curOperator}");
                }
            }
        }



        /// <summary>In UniInk , every valueType is Object , No Boxing!</summary>
        public partial class InkValue
        {
            public static readonly InkValue        Empty = new();
            public static readonly Stack<InkValue> pool  = new();

            public static InkValue Get()         => pool.Count > 0 ? pool.Pop() : new InkValue();
            public static void     ReleasePool() => pool.Clear();

            public static void Release(InkValue value)
            {
                value.Value_Meta.Clear();
                value.isCalculate = false;
                pool.Push(value);
            }



            public enum InkValueType
            {
                Int
              , Float
              , Boolean
              , Double
              , Char
              , String
            }


            public int    Value_int    { get; set; }
            public bool   Value_bool   { get; set; }
            public char   Value_char   { get; set; }
            public float  Value_float  { get; set; }
            public double Value_double { get; set; }

            public InkValueType ValueType { get; set; }

            public Stack<char> Value_Meta { get; } = new();

            public bool isCalculate;

            /// <summary>Calculate the value</summary>
            public void Calculate(InkValueType type)
            {
                if (isCalculate)
                {
                    return;
                }

                if (type == InkValueType.Int)
                {
                    Value_int = 0;
                    while (Value_Meta.Count != 0)
                    {
                        Value_int = Value_int * 10 + (Value_Meta.Pop() - '0');
                    }
                }

                isCalculate = true;
            }


            public static InkValue operator +(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    throw new Exception("left is null || right is null");
                }


                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = Get();
                    answer.ValueType = InkValueType.Int;

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int + right.Value_int;
                    answer.isCalculate = true;

                    Release(left);
                    Release(right);
                    return answer;
                }

                throw new Exception("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
            }

            public static InkValue operator -(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = Get();
                    answer.ValueType = InkValueType.Int;

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int - right.Value_int;
                    answer.isCalculate = true;
                    Release(left);
                    Release(right);
                    return answer;
                }

                throw new Exception("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
            }

            public static InkValue operator *(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = Get();
                    answer.ValueType = InkValueType.Int;

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int * right.Value_int;
                    answer.isCalculate = true;

                    Release(left);
                    Release(right);
                    return answer;
                }

                throw new Exception("left.ValueType != InkValueType.Int || right.ValueType != InkValueType.Int");
            }

            public static InkValue operator /(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    throw new Exception("left is null || right is null");
                }

                if (left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int)
                {
                    var answer = Get();
                    answer.ValueType = InkValueType.Int;

                    left.Calculate(InkValueType.Int);
                    right.Calculate(InkValueType.Int);

                    answer.Value_int   = left.Value_int / right.Value_int;
                    answer.isCalculate = true;

                    Release(left);
                    Release(right);
                    return answer;
                }

                throw new Exception("left.ValueType != InkValueType.Int || right.ValueType != InkValueType.Int");
            }
        }


        public partial class InkSyntaxList
        {
            public static readonly InkSyntaxList        Empty = new();
            public static readonly Stack<InkSyntaxList> pool  = new();

            public static InkSyntaxList Get()         => pool.Count > 0 ? pool.Pop() : new InkSyntaxList();
            public static void          ReleasePool() => pool.Clear();

            public static void Release(InkSyntaxList value)
            {
                value.ObjectList.Clear();
                value.IndexDirty.Clear();
                value.CastOther.Clear();
                pool.Push(value);
            }

            private readonly List<object> ObjectList = new(10);
            public readonly  List<bool>   IndexDirty = new(10);
            public readonly  List<object> CastOther  = new(10);


            public void Add(object value)
            {
                ObjectList.Add(value);
                CastOther.Add(null);
                IndexDirty.Add(false);
            }

            public void RemoveAt(int index)
            {
                ObjectList.RemoveAt(index);
                IndexDirty.RemoveAt(index);
                CastOther.RemoveAt(index);
            }



            public int Count => ObjectList.Count;

            public object this[int index] => ObjectList[index];

            public void SetDirty(object other, int start, int end)
            {
                for (var i = start ; i <= end ; i++)
                {
                    IndexDirty[i] = true;
                }

                CastOther[start] = other;
            }

            public void SetDirty(int index) => IndexDirty[index] = true;
        }



        /// <summary>UniInk Operator : Custom your own Operator!</summary>
        protected partial class InkOperator
        {
            public static readonly Dictionary<string, InkOperator> Dic_Values = new();

            //priority refer to : https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/
            //keyword  refer to : https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/
            public static readonly InkOperator ParenthisLeft  = new("(", 1);   //1.圆括号 (  - 用于改变默认的优先级。
            public static readonly InkOperator ParenthisRight = new(")", 1);   //1.圆括号 )  - 用于改变默认的优先级。
            public static readonly InkOperator Dot            = new(".", 2);   //2.成员访问 .
            public static readonly InkOperator BracketStart   = new("[", 2);   //2.数组索引 []
            public static readonly InkOperator BracketEnd     = new("]", 2);   //2.数组索引 []
            public static readonly InkOperator Increment      = new("++", 2);  //2.suffix ++ (prefix 3) 
            public static readonly InkOperator Decrement      = new("--", 2);  //2.suffix -- (prefix 3)
            public static readonly InkOperator LogicalNOT     = new("!", 3);   //3.逻辑非 !
            public static readonly InkOperator BitNot         = new("~", 3);   //3.位 非 ~
            public static readonly InkOperator Cast           = new("()", 4);  //4.显式类型转换
            public static readonly InkOperator Multiply       = new("*", 5);   //5.乘 *
            public static readonly InkOperator Divide         = new("/", 5);   //5.除 /
            public static readonly InkOperator Modulo         = new("%", 5);   //5.取模 %
            public static readonly InkOperator Plus           = new("+", 6);   //6.加 + (一元加号优先级3) 
            public static readonly InkOperator Minus          = new("-", 6);   //6.减 - (一元减号优先级3)
            public static readonly InkOperator LeftShift      = new("<<", 7);  //7.左移 <<
            public static readonly InkOperator RightShift     = new(">>", 7);  //7.右移 >>
            public static readonly InkOperator Lower          = new("<", 8);   //8.小于 <
            public static readonly InkOperator Greater        = new(">", 8);   //8.大于 >
            public static readonly InkOperator LowerOrEqual   = new("<=", 8);  //8.小于等于 <=
            public static readonly InkOperator GreaterOrEqual = new(">=", 8);  //8.大于等于 >=
            public static readonly InkOperator Equal          = new("==", 9);  //9.等于 ==     (等价比较运算
            public static readonly InkOperator NotEqual       = new("!=", 9);  //9.不等于 !=   (等价比较运算
            public static readonly InkOperator BitwiseAnd     = new("&", 10);  //8.按位与 &
            public static readonly InkOperator BitwiseXor     = new("^", 11);  //9.按位异或 ^
            public static readonly InkOperator BitwiseOr      = new("|", 12);  //10.按位或 |
            public static readonly InkOperator ConditionalAnd = new("&&", 13); //11.逻辑与 &&  (短路逻辑运算
            public static readonly InkOperator ConditionalOr  = new("||", 14); //12.逻辑或 ||  (短路逻辑运算
            public static readonly InkOperator Conditional    = new("?:", 15); //15.条件运算 ?: - 三元条件运算符。
            public static readonly InkOperator Assign         = new("=", 16);  //16.赋值 =、加等 +=、减等 -=、乘等 *=、除等 /=、模等 %=、左移等 <<=、右移等 >>=、按位与等 &=、按位或等 |=、按位异或等 ^= - 赋值运算。
            public static readonly InkOperator Comma          = new(",", 16);  //17.逗号 , - 用于分隔表达式
            public static readonly InkOperator Lambda         = new("=>", 17); //17. Lambda 表达式
            public static readonly InkOperator BraceLeft      = new("{", 20);
            public static readonly InkOperator BraceRight     = new("}", 20);
            public static readonly InkOperator Semicolon      = new(";", 20);
            public static readonly InkOperator Colon          = new(":", -1);
            public static readonly InkOperator QuestionMark   = new("?", -1);
            public static readonly InkOperator At             = new("@", -1);
            public static readonly InkOperator Hash           = new("#", -1);
            public static readonly InkOperator Dollar         = new("$", -1);


            public static readonly InkOperator KeyIf       = new("if", 20);
            public static readonly InkOperator KeyElse     = new("else", 20);
            public static readonly InkOperator KeySwitch   = new("switch", 20);
            public static readonly InkOperator KeyWhile    = new("while", 20);
            public static readonly InkOperator KeyFor      = new("for", 20);
            public static readonly InkOperator KeyForeach  = new("foreach", 20);
            public static readonly InkOperator KeyIn       = new("in", 20);
            public static readonly InkOperator KeyReturn   = new("return", 20);
            public static readonly InkOperator KeyBreak    = new("break", 20);
            public static readonly InkOperator KeyContinue = new("continue", 20);
            public static readonly InkOperator KeyVar      = new("var", 20);
            public static readonly InkOperator Empty       = new("😊", short.MaxValue);

            /// <summary>the lower the value, the higher the priority</summary>
            public readonly short PriorityIndex;

            /// <summary>the indexer of the operator   </summary>
            protected static short indexer;

            /// <summary>the only value of the operator</summary>
            protected readonly short OperatorValue = indexer++;



            protected InkOperator(string name, short priorityIndex)
            {
                PriorityIndex = priorityIndex;
                Dic_Values.Add(name, this);
            }

            public override bool Equals(object otherOperator) => otherOperator is InkOperator Operator && OperatorValue == Operator.OperatorValue;

            public override int    GetHashCode() => OperatorValue;
            public override string ToString()    => $"Operator : {Dic_Values.FirstOrDefault(pair => Equals(pair.Value, this)).Key}  Priority : {PriorityIndex}";



            public static object InkOperator_Plus(object left, object right)
            {
                switch (left)
                {
                    case null : return right;
                    case InkValue leftValue when right is InkValue rightValue :
                    {
                        return leftValue + rightValue;
                    }
                    
                    default : throw new InkSyntaxException($"unknown type{left}--{right}");
                }
            }

            public static object InkOperator_Minus(object left, object right)
            {
                switch (left)
                {
                    case null : return right;
                    case InkValue leftValue when right is InkValue rightValue :
                    {
                        return leftValue - rightValue;
                    }
                    default : throw new InkSyntaxException($"unknown type{left}--{right}");
                }
            }

            public static object InkOperator_Multiply(object left, object right)
            {
                switch (left)
                {
                    case null : return right;
                    case InkValue leftValue when right is InkValue rightValue :
                    {
                        return leftValue * rightValue;
                    }
                    default : throw new InkSyntaxException($"unknown type{left}--{right}");
                }
            }

            public static object InkOperator_Divide(object left, object right)
            {
                switch (left)
                {
                    case null : return right;
                    case InkValue leftValue when right is InkValue rightValue :
                    {
                        return leftValue / rightValue;
                    }
                    default : throw new InkSyntaxException($"unknown type{left}--{right}");
                }
            }
        }


        /// <summary> Translate Method Delegate   </summary>
        protected delegate object InternalDelegate(params object[] args);


        /////////////////////////////////////////////// Mapping  Data   ////////////////////////////////////////////////


        /// <summary>Some UnaryPostfix Operators mark</summary>
        protected static readonly Dictionary<InkOperator, Func<object, object, object>> dic_OperatorsFunc = new()
        {
            { InkOperator.Plus, InkOperator.InkOperator_Plus }         //
          , { InkOperator.Minus, InkOperator.InkOperator_Minus }       //
          , { InkOperator.Multiply, InkOperator.InkOperator_Multiply } //
          , { InkOperator.Divide, InkOperator.InkOperator_Divide }     //
          , { InkOperator.Modulo, (_,         _) => null }             //
          , { InkOperator.Lower, (_,          _) => null }             //
          , { InkOperator.Greater, (_,        _) => null }             //
          , { InkOperator.Equal, (_,          _) => null }             //
          , { InkOperator.LowerOrEqual, (_,   _) => null }             //
          , { InkOperator.GreaterOrEqual, (_, _) => null }             //
          , { InkOperator.NotEqual, (_,       _) => null }             //
          , { InkOperator.LogicalNOT, (_,     _) => null }             //
          , { InkOperator.ConditionalAnd, (_, _) => null }             //
          , { InkOperator.ConditionalOr, (_,  _) => null }             //
        };

        /// <summary> Some Escaped Char mapping   </summary>
        protected static readonly Dictionary<char, char> dic_EscapedChar = new()
        {
            { '\\', '\\' }, { '\'', '\'' }, { '0', '\0' }
          , { 'a', '\a' }, { 'b', '\b' }, { 'f', '\f' }
          , { 'n', '\n' }, { 'r', '\r' }, { 't', '\t' }
          , { 'v', '\v' }
        };


        /////////////////////////////////////////////// Parsing Methods ////////////////////////////////////////////////

        protected delegate bool ParsingMethodDelegate(string expression, InkSyntaxList stack, ref int i);

        /// <summary> The Parsing Methods for <see cref="Evaluate"/> </summary>
        protected static readonly List<ParsingMethodDelegate> ParsingMethods = new()
        {
            EvaluateOperators, EvaluateNumber, EvaluateChar
          , EvaluateString,
        };

        /// <summary> Evaluate Operators in<see cref="InkOperator"/> </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns> 
        protected static bool EvaluateOperators(string expression, InkSyntaxList keys, ref int i)
        {
            foreach (var operatorStr in InkOperator.Dic_Values) 
            {
                if (StartsWithInputStrFromIndex(expression, operatorStr.Key, i))
                {
                    keys.Add(operatorStr.Value);
                    i += operatorStr.Key.Length - 1;
                    return true;
                }
            }

            return false;
        }

        /// <summary> Evaluate Number _eg: -3.64f                    </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected static bool EvaluateNumber(string expression, InkSyntaxList keys, ref int i)
        {
            if (StartsWithNumbersFromIndex(expression, i, out var numberMatch, out var len))
            {
                keys.Add(numberMatch);
                i += len - 1;
                return true;
            }

            return false;
        }

        /// <summary> Evaluate Char or Escaped Char  _eg: 'a' '\d'   </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i">the <see cref="expression"/> start index   </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected static bool EvaluateChar(string expression, InkSyntaxList keys, ref int i)
        {
            if (StartsWithCharFormIndex(expression, i, out var value, out var len))
            {
                keys.Add(value);
                i += len;
                return true;
            }

            return false;
        }

        /// <summary> Evaluate String _eg:"string"                   </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns> 
        protected static bool EvaluateString(string expression, InkSyntaxList keys, ref int i)
        {
            if (StartsWithStringFormIndex(expression, i, out var value, out var len))
            {
                keys.Add(value);
                i += len;
                return true;
            }

            return false;
        }



        /////////////////////////////////////////////// Helping Methods ////////////////////////////////////////////////

        /// <summary>Find <see cref="input"/> is whether start with <see cref="value"/> from <see cref="startIndex"/></summary>
        protected static bool StartsWithInputStrFromIndex(string input, string value, int startIndex)
        {
            InkSyntaxException.ThrowIfTrue(input.Length < startIndex, "input.Length < startIndex");

            if (input.Length - startIndex < value.Length)
            {
                return false;
            }

            for (var i = 0 ; i < value.Length ; i++)
            {
                if (input[i + startIndex] != value[i])
                {
                    return false;
                }
            }


            return true;
        }

        /// <summary>Find <see cref="input"/> is whether start with numbers from <see cref="startIndex"/>     </summary>
        protected static bool StartsWithNumbersFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
            InkSyntaxException.ThrowIfTrue(input.Length < startIndex, "input.Length < startIndex");

            value           = InkValue.Get();
            value.ValueType = InkValue.InkValueType.Int;
            len             = 0;

            var pointNum = 0;

            for (var i = startIndex ; i < input.Length ; i++)
            {
                if (char.IsDigit(input[i]))
                {
                    len++;
                    value.Value_Meta.Push(input[i]);
                }
                else if (input[i] == '.')
                {
                    pointNum++;

                    if (pointNum > 1)
                    {
                        throw new InkSyntaxException("[NotSupport]:Too many decimal points, can't calling method with float or double number.");
                    }

                    value.ValueType = InkValue.InkValueType.Double;
                    value.Value_Meta.Push(input[i]);
                }
                else
                {
                    if (len == 0)
                    {
                        InkValue.Release(value);
                        return false;
                    }

                    if (i < input.Length - 1)
                    {
                        switch (input[i])
                        {
                            case 'f' or 'F' :
                                value.ValueType = InkValue.InkValueType.Float;
                                len++;
                                len++;
                                break;
                            case 'd' or 'D' :
                                value.ValueType = InkValue.InkValueType.Double;
                                len++;
                                len++;
                                break;
                        }
                    }

                    return true;
                }
            }

            return true;
        }

        /// <summary>Find <see cref="input"/> is whether start with char from <see cref="startIndex"/>        </summary>
        protected static bool StartsWithCharFormIndex(string input, int startIndex, out InkValue value, out int len)
        {
            var i = startIndex;
            if (input[i].Equals('\''))
            {
                i++;
                if (input[i].Equals('\\'))
                {
                    i++;

                    if (dic_EscapedChar.TryGetValue(input[i], out var EscapedChar))
                    {
                        value            = InkValue.Get();
                        value.ValueType  = InkValue.InkValueType.Char;
                        value.Value_char = EscapedChar;
                        i++;
                    }
                    else
                    {
                        throw new InkSyntaxException($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
                    }
                }
                else if (input[i].Equals('\''))
                {
                    throw new InkSyntaxException($"Illegal character[{i}] : ['']");
                }
                else
                {
                    value            = InkValue.Get();
                    value.ValueType  = InkValue.InkValueType.Char;
                    value.Value_char = input[i];
                    i++;
                }

                if (input[i].Equals('\''))
                {
                    len = i - startIndex + 1;
                    return true;
                }


                InkSyntaxException.Throw($"Illegal character[{i}] : too many characters in a character literal");
            }

            len   = 0;
            value = null;
            return false;
        }

        /// <summary>Find <see cref="input"/> is whether start with string from <see cref="startIndex"/>      </summary>
        protected static bool StartsWithStringFormIndex(string input, int startIndex, out InkValue value, out int len)
        {
            var i = startIndex;

            if (i < input.Length - 1 && (input[i].Equals('@') || (input[i].Equals('$') && input[i + 1].Equals('\"'))))
            {
                throw new Exception("don't support [@] [$]");
            }


            if (input[i].Equals('\"'))
            {
                i++;
                value           = InkValue.Get();
                value.ValueType = InkValue.InkValueType.String;


                while (i < input.Length)
                {
                    if (input[i].Equals('\\'))
                    {
                        i++;

                        if (dic_EscapedChar.TryGetValue(input[i], out var EscapedChar))
                        {
                            value.Value_Meta.Push(EscapedChar);
                            i++;
                        }
                        else
                        {
                            InkSyntaxException.Throw($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
                        }
                    }
                    else if (input[i].Equals('\"'))
                    {
                        i++;
                        len = i - startIndex;
                        return true;
                    }
                    else
                    {
                        value.Value_Meta.Push(input[i]);
                        i++;
                    }
                }

                InkSyntaxException.Throw($"Illegal character[{i}] : too many characters in a character literal");
            }

            len   = 0;
            value = null;
            return false;
        }


        /// <summary>Find the section between <see cref="sct_start"/> and <see cref="sct_end"/>               </summary>
        /// <param name="keys"     > the keys to find section in                                                </param>
        /// <param name="sct_start"> the start section key : the last  find before <see cref="sct_end"/>        </param>
        /// <param name="sct_end"  > the end   section key : the first find after  <see cref="sct_start"/>      </param>
        /// <returns> the result is success or not , the start index and end index of the section             </returns>
        private static (bool result, int startIndex, int endIndex) FindSection(InkSyntaxList keys, InkOperator sct_start, InkOperator sct_end)
        {
            var startIndex = -1;
            var endIndex   = -1;
            var length     = keys.Count;

            for (var i = 0 ; i < length ; i++)
            {
                if (keys.IndexDirty[i])
                {
                    continue;
                }

                if (Equals(keys[i], sct_start))
                {
                    startIndex = i;
                }
                else if (Equals(keys[i], sct_end))
                {
                    endIndex = i;
                    break;
                }
            }

            InkSyntaxException.ThrowIfTrue(startIndex > endIndex, $"Missing match {sct_start}");

            return (startIndex != -1 && endIndex != -1, startIndex, endIndex);
        }

        /// <summary>Get the highest priority operator in the <see cref="keys"/>              </summary>
        /// <param name="keys"> the keys to find the highest priority operator in               </param>
        private static (InkOperator @operator, int index) GetHighestPriorityOperator(InkSyntaxList keys, int startIndex, int endIndex)
        {
            var index            = -1;
            var priorityOperator = InkOperator.Empty;

            for (var i = startIndex ; i <= endIndex ; i++)
            {
                if (keys.IndexDirty[i])
                {
                    continue;
                }

                if (keys[i] is InkOperator @operator)
                {
                    if (@operator.PriorityIndex < priorityOperator.PriorityIndex)
                    {
                        index            = i;
                        priorityOperator = @operator;
                    }
                }
            }

            return (priorityOperator, index);
        }
    }


    /// <summary>UniInk Syntax Exception</summary>
    public class InkSyntaxException : Exception
    {
        public InkSyntaxException(string message) : base(message) { }

        public static void Throw(string message) => throw new InkSyntaxException(message);

        public static void ThrowIfTrue(bool condition, string message)
        {
            if (condition) throw new InkSyntaxException(message);
        }
    }

}
//859 lines of code


// TODO_LIST:
//😊 基本的数学运算(加减乘除, 乘方, 余数, 逻辑运算, 位运算) 二元运算符 ,且支持自动优先级 
// 2. 非成员方法调用(单参数,多参数,默认参数,可变参数,泛型方法?) 所用使用的函数必须全部是注册的方法，不应该支持调用未注册的方法，成员方法等
// 3. 赋值运算符 =
// 4. 声明变量 var
// 5. 基本的逻辑语句 if else
// 6. 支持类型的隐式转换

// 9 * ( ( 1 + 2 * 3 ) / 2 ) 
// * { 9 {/ {+ {1 , {* 2 3 }, 2 } 


// Architecture Design
// 1. Lexer : 词法分析器,把字符串转换成Token
// 2. Parser: 语法分析器,把Token转换成AST
// 3. Interpreter: 解释器,执行AST