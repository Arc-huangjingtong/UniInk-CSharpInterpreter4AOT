﻿namespace Arc.UniInk
{

    /*******************************************************************************************************************
     *  📰 Title    : UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)              |*
     *  🔖 Version  : 1.0.0                                                                                           |*
     *  😀 Author   : Arc (https://github.com/Arc-huangjingtong)                                                      |*
     *  🔑 Licence  : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)     |*
     *  🤝 Support  : [.NET Framework 4+] [C# 9.0+] [IL2CPP Support]                                                  |*
     *  📝 Desc     : [High performance] [zero box & unbox] [zero reflection runtime] [Easy-use] ⚠but                 |*
    /*******************************************************************************************************************/

    using System;
    using System.Collections.Generic;
    using System.Text;


    public partial class UniInk_Speed
    {
        /// <summary> Constructor </summary>
        /// <param name="context"  > Set context use as "This" or use internal member variables directly </param>
        /// <param name="variables"> Set variables can replace a key string with value object            </param>
        public UniInk_Speed(object context = null, Dictionary<string, object> variables = null) { }

        private static readonly List<ParsingMethodDelegate> ParsingMethods = new()
        {
            EvaluateOperators, EvaluateNumber, EvaluateChar
          , EvaluateParenthis, EvaluateString,
        };


        private delegate bool ParsingMethodDelegate(string expression, Queue<object> stack, ref int i);


        /// <summary>用于解释方法的委托</summary>
        protected delegate object InternalDelegate(params object[] args);


        /// <summary> Some Escaped Char mapping  </summary>
        protected static readonly Dictionary<char, char> dic_EscapedChar = new()
        {
            { '\\', '\\' }, { '\'', '\'' }, { '0', '\0' }
          , { 'a', '\a' }, { 'b', '\b' }, { 'f', '\f' }
          , { 'n', '\n' }, { 'r', '\r' }, { 't', '\t' }
          , { 'v', '\v' }
        };


        /// <summary>Evaluate Operators in <see cref="InkOperator"/></summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private static bool EvaluateOperators(string expression, Queue<object> stack, ref int i)
        {
            foreach (var operatorStr in InkOperator.Dic_Values)
            {
                if (StartsWithInputStrFromIndex(expression, operatorStr.Key, i))
                {
                    stack.Enqueue(operatorStr.Value);
                    i += operatorStr.Key.Length - 1;
                    return true;
                }
            }
            // foreach (var operatorStr in InkOperator.List_Keys)
            // {
            //     if (StartsWithInputStrFromIndex(expression, operatorStr, i))
            //     {
            //         stack.Push(InkOperator.Dic_Values[operatorStr]);
            //         i += operatorStr.Length - 1;
            //         return true;
            //     }
            // }

            return false;
        }

        /// <summary>Evaluate Number _eg: -3.64f </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns>Evaluate is successful?</returns>
        private static bool EvaluateNumber(string expression, Queue<object> stack, ref int i)
        {
            if (StartsWithNumbersFromIndex(expression, i, out var numberMatch, out var len))
            {
                stack.Enqueue(numberMatch);
                i += len - 1;
                return true;
            }

            return false;
        }

        /// <summary>Evaluate Char or Escaped Char  _eg: 'a' '\d'</summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns>
        /// <exception cref="InkSyntaxException">Illegal character or Unknown escape character </exception>
        private static bool EvaluateChar(string expression, Queue<object> stack, ref int i)
        {
            if (StartsWithCharFormIndex(expression, i, out var value, out var len))
            {
                stack.Enqueue(value);
                i += len;
                return true;
            }

            return false;
        }

        /// <summary>Evaluate Parenthis _eg: (xxx)</summary>
        /// <remarks>the match will recursive execute <see cref="Evaluate"/> </remarks>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private static bool EvaluateParenthis(string expression, Queue<object> stack, ref int i)
        {
            if (StartsWithParenthisFromIndex(expression, i, out var len))
            {
                var temp   = new Queue<object>();
                var result = Internal_Evaluate(expression, i + 1, i + len - 1, temp);
                stack.Enqueue(result);
                i += len;
                return true;
            }

            return false;
        }

        public static readonly Queue<object> Queue = new();

        /// <summary> Evaluate a expression       </summary>
        /// <returns> return the result object    </returns>
        public static object Evaluate(string expression, int startIndex, int endIndex)
        {
            Queue.Clear();
            return Internal_Evaluate(expression, startIndex, endIndex, Queue);
        }

        private static object Internal_Evaluate(string expression, int startIndex, int endIndex, Queue<object> stack)
        {
            for (var i = startIndex ; i <= endIndex && i < expression.Length ; i++)
            {
                var any = false;
                foreach (var parsingMethod in ParsingMethods)
                {
                    if (parsingMethod(expression, stack, ref i))
                    {
                        any = true;
                        break;
                    }
                }

                if (any) continue;
                if (char.IsWhiteSpace(expression[i])) continue;

                throw new InkSyntaxException($"Invalid character : [{(int)expression[i]}:{expression[i]}] at [{i}  {expression}] ");
            }

            var ans = ProcessQueue(stack);

            return ans;
        }


        public static object ProcessQueue(Queue<object> queue)
        {
            if (queue.Count == 0) throw new InkSyntaxException("Empty expression and Empty stack");

            object cache = null;
            while (queue.Count > 0)
            {
                var pop = queue.Dequeue();

                if (pop is InkOperator @operator)
                {
                    var left  = cache;
                    var right = queue.Dequeue();

                    cache = dic_OperatorsFunc[@operator](left, right);
                }
                else
                {
                    cache = pop;
                }
            }

            return cache;
        }


        /// <summary>Evaluate String _eg:"string" </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private static bool EvaluateString(string expression, Queue<object> stack, ref int i)
        {
            if (StartsWithStringFormIndex(expression, i, out var value, out var len))
            {
                stack.Enqueue(value);
                i += len;
                return true;
            }

            return false;
        }

        private readonly StringBuilder stringBuilderCache = new();

        /// <summary>Get a expression list between [startChar] and [endChar]</summary>
        /// <remarks>⚠️The startChar , endChar and separator must be different</remarks>
        private List<string> GetExpressionsParenthesized(string expression, ref int i, bool checkSeparator, char separator = ',', char startChar = '(', char endChar = ')')
        {
            stringBuilderCache.Clear();

            var expressionsList = new List<string>();

            var bracketCount = 1;

            for (; i < expression.Length ; i++)
            {
                var s = expression[i];

                if (s is '\'' or '\"')
                {
                    // var internalStringMatch = regex_String.Match(expression, i, expression.Length - i);
                    // if (internalStringMatch.Success)
                    // {
                    //     stringBuilderCache.Append(internalStringMatch.Value);
                    //     i += internalStringMatch.Length - 1;
                    //     continue;
                    // }
                    //
                    // var internalCharMatch = regex_Char.Match(expression, i, expression.Length - i);
                    // if (internalCharMatch.Success)
                    // {
                    //     stringBuilderCache.Append(internalCharMatch.Value);
                    //     i += internalCharMatch.Length - 1;
                    //     continue;
                    // }
                }


                if (s.Equals(startChar)) bracketCount++;
                if (s.Equals(endChar)) bracketCount--;

                if (bracketCount == 0)
                {
                    var currentExpressionStr = stringBuilderCache.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(currentExpressionStr)) expressionsList.Add(currentExpressionStr);
                    break;
                }

                if (bracketCount == 1 && checkSeparator && s.Equals(separator))
                {
                    var currentExpressionStr = stringBuilderCache.ToString().Trim();
                    expressionsList.Add(currentExpressionStr);
                    stringBuilderCache.Clear();
                }
                else
                {
                    stringBuilderCache.Append(s);
                }
            }

            if (bracketCount > 0)
            {
                throw new InkSyntaxException($"[{expression}] is missing characters ['{endChar}'] ");
            }

            return expressionsList;
        }


        /// <summary>In UniInk , every valueType is Object , No Boxing!</summary>
        public class InkValue
        {
            public static readonly InkValue        Empty = null;
            public static readonly Stack<InkValue> pool  = new();

            public static InkValue Get()     => pool.Count > 0 ? pool.Pop() : new InkValue();
            public static void     Release() => pool.Clear();

            public static void Release(InkValue value)
            {
                value.Value_String.Clear();
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


            public int           Value_int    { get; set; }
            public bool          Value_bool   { get; set; }
            public char          Value_char   { get; set; }
            public float         Value_float  { get; set; }
            public double        Value_double { get; set; }
            public StringBuilder Value_String { get; set; } = new();

            public InkValueType ValueType { get; set; }

            public Stack<char> Value_Meta { get; set; } = new();

            public bool isCalculate = false;

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



        /// <summary>UniInk Operator : Custom your own Operator!</summary>
        protected internal class InkOperator
        {
            public static readonly Dictionary<string, InkOperator> Dic_Values = new();

            //priority refer to : https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/
            public static readonly InkOperator ParenthisLeft      = new("(", 1);   //1.圆括号 (  - 用于改变默认的优先级。
            public static readonly InkOperator ParenthisRight     = new(")", 1);   //1.圆括号 )  - 用于改变默认的优先级。
            public static readonly InkOperator Dot                = new(".", 2);   //2.成员访问 .
            public static readonly InkOperator SquareBracketStart = new("[", 2);   //2.数组索引 []
            public static readonly InkOperator SquareBracketEnd   = new("]", 2);   //2.数组索引 []
            public static readonly InkOperator Increment          = new("++", 2);  //2.suffix ++ (prefix 3) 
            public static readonly InkOperator Decrement          = new("--", 2);  //2.suffix -- (prefix 3)
            public static readonly InkOperator LogicalNegation    = new("!", 3);   //3.逻辑非 !
            public static readonly InkOperator BitwiseComplement  = new("~", 3);   //3.位 非 ~
            public static readonly InkOperator Cast               = new("()", 4);  //4.显式类型转换
            public static readonly InkOperator Multiply           = new("*", 5);   //5.乘 *
            public static readonly InkOperator Divide             = new("/", 5);   //5.除 /
            public static readonly InkOperator Modulo             = new("%", 5);   //5.取模 %
            public static readonly InkOperator Plus               = new("+", 6);   //6.加 + (一元加号优先级3) 
            public static readonly InkOperator Minus              = new("-", 6);   //6.减 - (一元减号优先级3)
            public static readonly InkOperator LeftShift          = new("<<", 7);  //7.左移 <<
            public static readonly InkOperator RightShift         = new(">>", 7);  //7.右移 >>
            public static readonly InkOperator Lower              = new("<", 8);   //8.小于 <
            public static readonly InkOperator Greater            = new(">", 8);   //8.大于 >
            public static readonly InkOperator LowerOrEqual       = new("<=", 8);  //8.小于等于 <=
            public static readonly InkOperator GreaterOrEqual     = new(">=", 8);  //8.大于等于 >=
            public static readonly InkOperator Equal              = new("==", 9);  //9.等于 ==     (等价比较运算
            public static readonly InkOperator NotEqual           = new("!=", 9);  //9.不等于 !=   (等价比较运算
            public static readonly InkOperator BitwiseAnd         = new("&", 10);  //8.按位与 &
            public static readonly InkOperator BitwiseXor         = new("^", 11);  //9.按位异或 ^
            public static readonly InkOperator BitwiseOr          = new("|", 12);  //10.按位或 |
            public static readonly InkOperator ConditionalAnd     = new("&&", 13); //11.逻辑与 &&  (短路逻辑运算
            public static readonly InkOperator ConditionalOr      = new("||", 14); //12.逻辑或 ||  (短路逻辑运算
            public static readonly InkOperator Conditional        = new("?:", 15); //15.条件运算 ?: - 三元条件运算符。
            public static readonly InkOperator Assign             = new("=", 16);  //16.赋值 =、加等 +=、减等 -=、乘等 *=、除等 /=、模等 %=、左移等 <<=、右移等 >>=、按位与等 &=、按位或等 |=、按位异或等 ^= - 赋值运算。
            public static readonly InkOperator Comma              = new(",", 16);  //17.逗号 , - 用于分隔表达式
            public static readonly InkOperator Lambda             = new("=>", 17); //17. Lambda 表达式

            protected static short indexer;


            protected readonly short OperatorValue;

            /// <summary>the lower the value, the higher the priority</summary>
            protected readonly short PriorityIndex;

            protected InkOperator(string name, short priorityIndex)
            {
                OperatorValue = indexer++;
                PriorityIndex = priorityIndex;

                Dic_Values.Add(name, this);
            }

            public override bool Equals(object otherOperator) => otherOperator is InkOperator Operator && OperatorValue == Operator.OperatorValue;
            public override int  GetHashCode()                => OperatorValue;

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


        /// <summary>UniInk Syntax Tree : Custom your own AST , you can create your own Language</summary>
        protected internal class SyntaxTree
        {
            public static readonly SyntaxTree        Empty = null;
            public static readonly Stack<SyntaxTree> Pool  = new();

            public static SyntaxTree Get()     => Pool.Count > 0 ? Pool.Pop() : new SyntaxTree();
            public static void       Release() => Pool.Clear();

            public static void Release(SyntaxTree tree)
            {
                tree.Parent = null;
                tree.Children.Clear();
                tree.Value = null;
                Pool.Push(tree);
            }

            public SyntaxTree(SyntaxTree parent = null) => Parent = parent;


            public SyntaxTree Parent;

            public List<SyntaxTree> Children = new(4);

            public object Value;
        }


        // 9*((1+2*3)/2)
        // 9 * ( ( 1 + 2 * 3 ) / 2 ) 
        // * { 9 {/ {+ {1 , {* 2 3 }, 2 } 
        

        /// <summary>Find <see cref="input"/> is whether start with [(] from <see cref="startIndex"/></summary>
        protected static bool StartsWithParenthisFromIndex(string input, int startIndex, out int len)
        {
            if (input.Length < startIndex)
            {
                throw new Exception("input.Length < startIndex");
            }

            len = 0;

            if (input[startIndex].Equals(')')) throw new InkSyntaxException("missing match [)]");

            if (input[startIndex].Equals('('))
            {
                var bracketCount = 0;

                for (var i = startIndex ; i < input.Length ; i++)
                {
                    var s = input[i];

                    if (s.Equals(')')) bracketCount--;
                    if (s.Equals('(')) bracketCount++;

                    if (bracketCount == 0)
                    {
                        len = i - startIndex;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Find <see cref="input"/> is whether start with <see cref="value"/> from <see cref="startIndex"/></summary>
        protected static bool StartsWithInputStrFromIndex(string input, string value, int startIndex)
        {
            if (input.Length < startIndex)
            {
                throw new Exception("input.Length < startIndex");
            }

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

        /// <summary>Find <see cref="input"/> is whether start with numbers from <see cref="startIndex"/></summary>
        protected static bool StartsWithNumbersFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
            if (input.Length < startIndex)
            {
                throw new Exception("input.Length < startIndex");
            }

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
                                break;
                            case 'd' or 'D' :
                                value.ValueType = InkValue.InkValueType.Double;
                                len++;
                                break;
                        }
                    }

                    return true;
                }
            }

            return true;
        }

        /// <summary>Find <see cref="input"/> is whether start with char from <see cref="startIndex"/></summary>
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


                throw new InkSyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            len   = 0;
            value = null;
            return false;
        }

        /// <summary>Find <see cref="input"/> is whether start with string from <see cref="startIndex"/></summary>
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
                var stringBuilder = value.Value_String;


                while (i < input.Length)
                {
                    if (input[i].Equals('\\'))
                    {
                        i++;

                        if (dic_EscapedChar.TryGetValue(input[i], out var EscapedChar))
                        {
                            stringBuilder.Append(EscapedChar);
                            i++;
                        }
                        else
                        {
                            throw new InkSyntaxException($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
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
                        stringBuilder.Append(input[i]);
                        i++;
                    }
                }

                throw new InkSyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            len   = 0;
            value = null;
            return false;
        }


        /// <summary> Some UnaryPostfix Operators mark</summary>
        protected internal static readonly Dictionary<InkOperator, Func<object, object, object>> dic_OperatorsFunc = new()
        {
            { InkOperator.Plus, InkOperator.InkOperator_Plus }         //
          , { InkOperator.Minus, InkOperator.InkOperator_Minus }       //
          , { InkOperator.Multiply, InkOperator.InkOperator_Multiply } //
          , { InkOperator.Divide, InkOperator.InkOperator_Divide }     //
          , { InkOperator.Modulo, (_,          _) => null }     //
          , { InkOperator.Lower, (_,           _) => null }     //
          , { InkOperator.Greater, (_,         _) => null }     //
          , { InkOperator.Equal, (_,           _) => null }     //
          , { InkOperator.LowerOrEqual, (_,    _) => null }     //
          , { InkOperator.GreaterOrEqual, (_,  _) => null }     //
          , { InkOperator.NotEqual, (_,        _) => null }     //
          , { InkOperator.LogicalNegation, (_, _) => null }     //
          , { InkOperator.ConditionalAnd, (_,  _) => null }     //
          , { InkOperator.ConditionalOr, (_,   _) => null }     //
        };
    }


    /// <summary>UniInk Syntax Exception</summary>
    public class InkSyntaxException : Exception
    {
        public InkSyntaxException(string message) : base(message) { }
    }

    
    
}