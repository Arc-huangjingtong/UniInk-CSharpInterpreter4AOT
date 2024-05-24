namespace Arc.UniInk
{

    /*******************************************************************************************************************
    *📰 Title    :  UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                 *
    *🔖 Version  :  1.0.0                                                                                              *
    *😀 Author   :  Arc (https://github.com/Arc-huangjingtong)                                                         *
    *🔑 Licence  :  MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)        *
    *🤝 Support  :  [.NET Framework 4+] [C# 9.0+] [IL2CPP Support]                                                     *
    *📝 Desc     :  [High performance] [zero box & unbox] [zero reflection runtime] [Easy-use]                         *
    /*******************************************************************************************************************/

    // ReSharper disable PartialTypeWithSinglePart
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary> the C# Evaluator easy to use </summary>
    public partial class UniInk_Speed
    {
        /// <summary> Constructor </summary>
        /// <param name="context"  > Set context use as "This" or use internal member variables directly </param>
        /// <param name="variables"> Set variables can replace a key string with value object            </param>
        public UniInk_Speed(object context = null, Dictionary<string, object> variables = null) { }

        /// <summary> Evaluate a expression or simple scripts    </summary>
        /// <returns> return the result object                   </returns>
        public static object Evaluate(string expression) => Evaluate(expression, 0, expression.Length);

        /// <summary> Evaluate a expression or simple scripts    </summary>
        /// <returns> return the result object                   </returns>
        public static object Evaluate(string expression, int startIndex, int endIndex)
        {
            var syntaxList = LexerAndFill(expression, startIndex, endIndex);
            var evalAnswer = ProcessQueue(syntaxList);

            return evalAnswer;
        }

        /// <summary> Clear the cache in UniInk anytime          </summary>
        /// <remarks> Internal  cache pool will be clear         </remarks>
        public static void ClearCache()
        {
            InkValue.ReleasePool();
            InkSyntaxList.ReleasePool();
        }


        /// <summary> UniInk Lexer :   Fill the SyntaxList       </summary>
        public static InkSyntaxList LexerAndFill(string expression, int startIndex, int endIndex)
        {
            var keys = InkSyntaxList.Get();

            for (var i = startIndex ; i <= endIndex && i < expression.Length ; i++)
            {
                if (char.IsWhiteSpace(expression[i])) continue;

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

                InkSyntaxException.Throw($"Invalid character : [{(int)expression[i]}:{expression[i]}] at [{i}  {expression}] ");
            }

            return keys;
        }

        /// <summary> UniInk SyntaxList : Process the SyntaxList </summary>
        public static object ProcessQueue(InkSyntaxList syntaxList)
        {
            if (syntaxList.Count == 0)
            {
                InkSyntaxException.Throw("Empty expression and Empty stack !");
            }

            ProcessQueue_Parenthis(syntaxList);
            ProcessQueue_Operators(syntaxList, 0, syntaxList.Count - 1);

            var cache = syntaxList.CastOther[0];

            InkSyntaxList.Release(syntaxList);

            return cache;
        }

        public static void ProcessQueue_Parenthis(InkSyntaxList keys)
        {
            var hasParenthis = true;

            while (hasParenthis)
            {
                int startIndex, endIndex;

                (hasParenthis, startIndex, endIndex) = FindSection(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight);

                if (!hasParenthis) continue;

                if (startIndex > 0 && keys[startIndex - 1] is InkFunction)
                {
                    ProcessQueue_Functions(keys, startIndex, endIndex);
                }
                else
                {
                    ProcessQueue_Operators(keys, startIndex + 1, endIndex - 1);
                }


                keys.SetDirty(startIndex);
                keys.SetDirty(endIndex);
            }
        }

        public static void ProcessQueue_Operators(InkSyntaxList keys, int _startIndex, int _endIndex)
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

        public static void ProcessQueue_Functions(InkSyntaxList keys, int paramStart, int paramEnd)
        {
            //找到指定范围内的函数，并且执行它
            InkFunction  func = keys[paramStart - 1] as InkFunction;
            List<object> prms = new();

            for (var i = paramStart + 1 ; i <= paramEnd - 1 ; i++)
            {
                object current;

                if (keys.IndexDirty[i])
                {
                    if (keys.CastOther[i] == null) continue;

                    current = keys.CastOther[i];
                }
                else
                {
                    current = keys[i];
                }

                if (current == null || Equals(current, InkOperator.Dot)) continue;

                if (current is InkValue inkValue)
                {
                    var array = inkValue.Value_Meta.ToList();
                    array.Reverse();
                    current = new string(array.ToArray());
                }


                prms.Add(current);
            }

            var result = func.FuncDelegate.Method.Invoke(null, prms.ToArray());

            keys.SetDirty(result, paramStart - 1, paramEnd);
        }


        /// <summary>In UniInk , every valueType is Object , No Boxing!</summary>
        public partial class InkValue
        {
            public static readonly Stack<InkValue> pool = new();

            public static InkValue Get() => pool.Count > 0 ? pool.Pop() : new();

            public static InkValue DeepCopy(InkValue value)
            {
                var answer = Get();

                answer.ValueType    = value.ValueType;
                answer.Value_int    = value.Value_int;
                answer.Value_float  = value.Value_float;
                answer.Value_double = value.Value_double;
                answer.Value_char   = value.Value_char;
                answer.Value_bool   = value.Value_bool;
                answer.isCalculate  = value.isCalculate;

                foreach (var meta in value.Value_Meta)
                {
                    answer.Value_Meta.Push(meta);
                }

                return answer;
            }

            public static void ReleasePool() => pool.Clear();


            public static void Release(InkValue value)
            {
                value.Value_Meta.Clear();
                value.isCalculate = false;
                pool.Push(value);
            }

            public static void Release(InkValue value1, InkValue value2)
            {
                Release(value1);
                Release(value2);
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
            public void Calculate()
            {
                if (isCalculate) return;

                switch (ValueType)
                {
                    case InkValueType.Int :
                    {
                        var length = 0;

                        Value_int = 0;
                        while (Value_Meta.Count != 0)
                        {
                            var current = Value_Meta.Pop() - '0';
                            Value_int += current * (int)Math.Pow(10, length++);
                        }

                        break;
                    }
                    case InkValueType.Float :
                    {
                        var length  = 0;
                        var isPoint = true;

                        Value_float = 0;
                        while (Value_Meta.Count != 0)
                        {
                            while (isPoint)
                            {
                                var current = Value_Meta.Pop();

                                if (current == '.')
                                {
                                    isPoint = false;
                                    continue;
                                }

                                Value_float = Value_float / 10 + (current - '0') * 0.1f;
                            }

                            Value_float += (Value_Meta.Pop() - '0') * (int)Math.Pow(10, length++);
                        }

                        break;
                    }
                    case InkValueType.Double :
                    {
                        var length  = 0;
                        var isPoint = true;

                        Value_double = 0;
                        while (Value_Meta.Count != 0)
                        {
                            while (isPoint)
                            {
                                var current = Value_Meta.Pop();

                                if (current == '.')
                                {
                                    isPoint = false;
                                    continue;
                                }

                                Value_double = Value_double / 10 + (current - '0') * 0.1d;
                            }

                            Value_double += (Value_Meta.Pop() - '0') * Math.Pow(10, length++);
                        }

                        break;
                    }
                    case InkValueType.Char :
                    {
                        Value_char = Value_Meta.Pop();
                        break;
                    }
                    case InkValueType.Boolean :
                    {
                        Value_bool = Value_Meta.Pop() == 't';
                        break;
                    }
                }

                isCalculate = true;
            }


            public static InkValue operator +(InkValue left, InkValue right)
            {
                if (left is null && right is null)
                {
                    InkSyntaxException.Throw("left is null && right is null");
                }

                var answer = Get();

                answer.ValueType = left!.ValueType;

                left.Calculate();
                right!.Calculate();

                switch (answer.ValueType)
                {
                    case InkValueType.Int :
                        answer.Value_int = left.Value_int + right.Value_int;
                        break;
                    case InkValueType.Float :
                        answer.Value_float = left.Value_float + right.Value_float;
                        break;
                    case InkValueType.Double :
                        answer.Value_double = left.Value_double + right.Value_double;
                        break;
                    default : throw new InkSyntaxException("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
                }

                answer.isCalculate = true;


                Release(left, right);

                return answer;
            }

            public static InkValue operator -(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    InkSyntaxException.Throw("left is null || right is null");
                }

                var answer = Get();

                answer.ValueType = left!.ValueType;

                left.Calculate();
                right!.Calculate();

                switch (answer.ValueType)
                {
                    case InkValueType.Int :
                        answer.Value_int = left.Value_int - right.Value_int;
                        break;
                    case InkValueType.Float :
                        answer.Value_float = left.Value_float - right.Value_float;
                        break;
                    case InkValueType.Double :
                        answer.Value_double = left.Value_double - right.Value_double;
                        break;
                    default : throw new InkSyntaxException("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
                }

                answer.isCalculate = true;


                Release(left, right);

                return answer;
            }

            public static InkValue operator *(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    InkSyntaxException.Throw("left is null || right is null");
                }

                var answer = Get();

                answer.ValueType = left!.ValueType;

                left.Calculate();
                right!.Calculate();

                switch (answer.ValueType)
                {
                    case InkValueType.Int :
                        answer.Value_int = left.Value_int * right.Value_int;
                        break;
                    case InkValueType.Float :
                        answer.Value_float = left.Value_float * right.Value_float;
                        break;
                    case InkValueType.Double :
                        answer.Value_double = left.Value_double * right.Value_double;
                        break;
                    default : throw new InkSyntaxException("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
                }

                answer.isCalculate = true;


                Release(left, right);

                return answer;
            }

            public static InkValue operator /(InkValue left, InkValue right)
            {
                if (left is null || right is null)
                {
                    InkSyntaxException.Throw("left is null || right is null");
                }

                var answer = Get();

                answer.ValueType = left!.ValueType;

                left.Calculate();
                right!.Calculate();

                switch (answer.ValueType)
                {
                    case InkValueType.Int :
                        answer.Value_int = left.Value_int / right.Value_int;
                        break;
                    case InkValueType.Float :
                        answer.Value_float = left.Value_float / right.Value_float;
                        break;
                    case InkValueType.Double :
                        answer.Value_double = left.Value_double / right.Value_double;
                        break;
                    default : throw new InkSyntaxException("left.ValueType == InkValueType.Int || right.ValueType == InkValueType.Int");
                }

                answer.isCalculate = true;


                Release(left, right);

                return answer;
            }
        }


        public partial class InkSyntaxList
        {
            public static readonly Stack<InkSyntaxList> pool = new();
            public static          InkSyntaxList        Get() => pool.Count > 0 ? pool.Pop() : new InkSyntaxList();

            public static void ReleasePool() => pool.Clear();

            public static void Release(InkSyntaxList value)
            {
                value.ObjectList.Clear();
                value.CastOther.Clear();
                value.IndexDirty.Clear();

                pool.Push(value);
            }

            public readonly List<object> ObjectList = new(30);
            public readonly List<object> CastOther  = new(30);
            public readonly List<bool>   IndexDirty = new(30);



            public void Add(object value)
            {
                ObjectList.Add(value);
                CastOther.Add(null);
                IndexDirty.Add(false);
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
        public partial class InkOperator
        {
            public static readonly Dictionary<int, InkOperator> Dic_Values = new();

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
            public static readonly InkOperator At             = new("@\"", -1);
            public static readonly InkOperator Dollar         = new("$\"", -1);
            public static readonly InkOperator Hash           = new("#", -1);


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
            public static readonly InkOperator KeyTrue     = new("true", 20);
            public static readonly InkOperator KeyFalse    = new("false", 20);
            public static readonly InkOperator Empty       = new("😊", short.MaxValue);

            /// <summary>the lower the value, the higher the priority</summary>
            public readonly short PriorityIndex;

            public readonly string Name;

            /// <summary>the indexer of the operator   </summary>
            protected static short indexer;

            /// <summary>the only value of the operator</summary>
            protected readonly short OperatorValue = indexer++;



            protected InkOperator(string name, short priorityIndex)
            {
                PriorityIndex = priorityIndex;
                Name          = name;
                var hash = GetStringSliceHashCode(name, 0, name.Length - 1);
                Dic_Values.Add(hash, this);
            }

            public override bool Equals(object otherOperator) => otherOperator is InkOperator Operator && OperatorValue == Operator.OperatorValue;

            public override int    GetHashCode() => OperatorValue;
            public override string ToString()    => $"Operator : {Name}  Priority : {PriorityIndex}";



            public static object InkOperator_Plus(object left, object right)
            {
                switch (left)
                {
                    case null when right is InkValue rightValue :
                    {
                        rightValue.Calculate();
                        return rightValue;
                    }
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



        public partial class InkFunction
        {
            public InkFunction(Delegate func)
            {
                FuncDelegate = func;
                ParamTypes   = func.Method.GetParameters().Select(p => p.ParameterType).ToArray();
                ReturnType   = func.Method.ReturnType;
            }


            public readonly Type[] ParamTypes;

            public readonly Type ReturnType;

            public readonly Delegate FuncDelegate;
        }



        /////////////////////////////////////////////// Mapping  Data   ////////////////////////////////////////////////


        /// <summary> Some UnaryPostfix operators func mapping </summary>
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

        /// <summary> Some Escaped Char mapping                </summary>
        protected static readonly Dictionary<char, char> dic_EscapedChar = new()
        {
            { '\\', '\\' }, { '\'', '\'' } //
          , { '0', '\0' }, { 'a', '\a' }   //
          , { 'b', '\b' }, { 'f', '\f' }   //
          , { 'n', '\n' }, { 'r', '\r' }   //
          , { 't', '\t' }, { 'v', '\v' }   //
        };

        public static readonly Action<string> LOG = Test1;

        /// <summary> Some Global Functions mapping            </summary>
        protected static readonly Dictionary<string, InkFunction> GlobalFunctions = new() { { "LOG", new InkFunction(LOG) } };


        public static readonly Func<int, int, int> ADD = Test2;

        public static void Test1(string str) => Console.WriteLine(str);

        public static int Test2(int item1, int item2) => item1 + item2;

        /////////////////////////////////////////////// Parsing Methods ////////////////////////////////////////////////

        protected delegate bool ParsingMethodDelegate(string expression, InkSyntaxList stack, ref int i);

        /// <summary> The Parsing Methods for <see cref="Evaluate"/> </summary>
        protected static readonly List<ParsingMethodDelegate> ParsingMethods = new()
        {
            EvaluateOperators //
          , EvaluateFunction  //
          , EvaluateNumber    //
          , EvaluateChar      //
          , EvaluateString    //
          , EvaluateBool      //
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
                if (StartsWithInputStrFromIndex(expression, operatorStr.Value.Name, i))
                {
                    keys.Add(operatorStr.Value);
                    i += operatorStr.Value.Name.Length - 1;
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

        /// <summary> Evaluate Bool _eg: true false                  </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected static bool EvaluateBool(string expression, InkSyntaxList keys, ref int i)
        {
            if (StartsWithBoolFromIndex(expression, i, out var boolMatch, out var len))
            {
                keys.Add(boolMatch);
                i += len;
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

        /// <summary> Evaluate Function _eg: LOG("Hello World")      </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected static bool EvaluateFunction(string expression, InkSyntaxList keys, ref int i)
        {
            if (StartsWithInputStrFromIndex(expression, "LOG", i))
            {
                keys.Add(GlobalFunctions["LOG"]);
                i += 2;
                return true;
            }

            return false;
        }


        /////////////////////////////////////////////// Helping Methods ////////////////////////////////////////////////

        /// <summary>Find <see cref="input"/> is whether start with <see cref="value"/> from <see cref="startIndex"/></summary>
        protected static bool StartsWithInputStrFromIndex(string input, string value, int startIndex)
        {
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

        /// <summary>Find <see cref="input"/> is whether start with numbers from <see cref="startIndex"/>            </summary>
        protected static bool StartsWithNumbersFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
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
                    len++;
                    pointNum++;

                    if (pointNum > 1)
                    {
                        InkSyntaxException.Throw("[NotSupport]:Too many decimal points, can't calling method with float or double number.");
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

                    switch (input[i])
                    {
                        case 'f' or 'F' :
                            value.ValueType = InkValue.InkValueType.Float;
                            len++;
                            break;
                        case 'd' or 'D' :
                            value.ValueType = InkValue.InkValueType.Double;
                            len++;
                            break;
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

        /// <summary>Find <see cref="input"/> is whether start with bool from <see cref="startIndex"/>        </summary>
        protected static bool StartsWithBoolFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
            if (input[startIndex].Equals('t') || input[startIndex].Equals('f'))
            {
                value           = InkValue.Get();
                value.ValueType = InkValue.InkValueType.Boolean;

                if (input[startIndex].Equals('t'))
                {
                    value.Value_bool = true;
                    len              = 4;
                }
                else
                {
                    value.Value_bool = false;
                    len              = 5;
                }

                return true;
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
        protected static (bool result, int startIndex, int endIndex) FindSection(InkSyntaxList keys, InkOperator sct_start, InkOperator sct_end)
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

            if (startIndex > endIndex)
            {
                InkSyntaxException.Throw($"Missing match {sct_end}");
            }

            return (startIndex != -1 && endIndex != -1, startIndex, endIndex);
        }

        /// <summary>Get the highest priority operator in the <see cref="keys"/>              </summary>
        /// <param name="keys"> the keys to find the highest priority operator in               </param>
        protected static (InkOperator @operator, int index) GetHighestPriorityOperator(InkSyntaxList keys, int startIndex, int endIndex)
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

        /// <summary> Get the hash code of the string from <see cref="startIndex"/> to <see cref="endIndex"/> </summary>
        protected static int GetStringSliceHashCode(string str, int startIndex, int endIndex)
        {
            var hash = 0;
            for (var i = startIndex ; i <= endIndex ; i++)
            {
                hash = hash * 31 + str[i];
            }

            return hash;
        }
    }


    /// <summary>UniInk Syntax Exception</summary>
    public class InkSyntaxException : Exception
    {
        public InkSyntaxException(string message) : base(message) { }
        public static void Throw(string  message) => throw new InkSyntaxException(message);
    }

}
//1034 lines of code


// TODO_LIST:
//😊 [浮点型，整形，双精度] 基本的数学运算(加减乘除, 乘方, 余数, 逻辑运算, 位运算) 二元运算符 ,且支持自动优先级 
// 2. 非成员方法调用(单参数,多参数,默认参数,可变参数,泛型方法?) 所用使用的函数必须全部是注册的方法，不应该支持调用未注册的方法，成员方法等
// 3. 赋值运算符 =
// 4. 声明变量 var
// 5. 基本的逻辑语句 if else
// 6. 支持类型的隐式转换


// Architecture Design
// 1. Lexer      : 词法分析器,把字符串转换成Token
// 2. Parser     : 语法分析器,把Token转换成AST
// 3. Interpreter: 解释器,执行AST