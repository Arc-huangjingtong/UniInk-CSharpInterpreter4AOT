/************************************************************************************************************************
 *  📰 Title    : UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                    *
 *  🔖 Version  : 1.0.0                                                                                                 *
 *  👩‍💻 Author   : Arc (https://github.com/Arc-huangjingtong)                                                            *
 *  🔑 Licence  : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)           *
 *  🤝 Support  : [.NET Framework 4+] [C# 8.0+] [IL2CPP Support]                                                        *
 *  📝 Desc     : [High performance] [zero box & unbox] [zero reflection runtime] [Easy-use] ⚠but                           *
/************************************************************************************************************************/

namespace Arc.UniInk
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Linq;

    public class UniInk_Speed
    {
        /// <summary> Constructor </summary>
        /// <param name="context"  > Set context use as "This" or use internal member variables directly </param>
        /// <param name="variables"> Set variables can replace a key string with value object            </param>
        public UniInk_Speed(object context = null, Dictionary<string, object> variables = null) { }


        /// <summary>用于解释方法的委托</summary>
        private delegate object InternalDelegate(params object[] args);


        /// <summary> Some Escaped Char mapping  </summary>
        protected static readonly Dictionary<char, char> dic_EscapedChar = new()
        {
            { '\\', '\\' },
            { '\'', '\'' },
            { '0', '\0' },
            { 'a', '\a' },
            { 'b', '\b' },
            { 'f', '\f' },
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' },
            { 'v', '\v' }
        };


        /// <summary>Evaluate Operators in <see cref="InkOperator"/></summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private bool EvaluateOperators(string expression, Stack<object> stack, ref int i)
        {
            foreach (var operatorStr in InkOperator.List_Keys)
            {
                if (StartsWithFromIndex(expression, operatorStr, i))
                {
                    stack.Push(InkOperator.Dic_Values[operatorStr]);
                    i += operatorStr.Length - 1;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Evaluate Number _eg: -3.64f </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns>Evaluate is successful?</returns>
        private bool EvaluateNumber(string expression, Stack<object> stack, ref int i)
        {
            if (StartsWithNumbersFromIndex(expression, i, out var numberMatch, out var len))
            {
                stack.Push(numberMatch);
                i += len;
            }

            return false;
        }

        /// <summary>Evaluate Char or Escaped Char  _eg: 'a' '\d'</summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns>
        /// <exception cref="SyntaxException">Illegal character or Unknown escape character </exception>
        private bool EvaluateChar(string expression, Stack<object> stack, ref int i)
        {
            if (StartsWithCharFormIndex(expression, i, out var value, out var len))
            {
                stack.Push(value);
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
        private bool EvaluateParenthis(string expression, Stack<object> stack, ref int i)
        {
            var s = expression[i];

            if (s.Equals(')')) throw new SyntaxException("missing match [)]");

            if (s.Equals('('))
            {
                i++;

                if (stack.Count > 0 && stack.Peek() is InternalDelegate)
                {
                    var expressionsInParenthis = GetExpressionsParenthesized(expression, ref i, true);

                    // if (stack.Pop() is InternalDelegate lambdaDelegate)
                    //     stack.Push(lambdaDelegate(expressionsInParenthis.ConvertAll(str => Evaluate(str))));
                }
                else
                {
                    var expressionsInParenthis = GetExpressionsParenthesized(expression, ref i, false);

                    // stack.Push(Evaluate(expressionsInParenthis[0]));
                }

                return true;
            }

            return false;
        }
        
        /// <summary>Evaluate String _eg:"string" </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private bool EvaluateString(string expression, Stack<object> stack, ref int i)
        {
            if (StartsWithStringFormIndex(expression, i, out var value, out var len))
            {
                stack.Push(value);
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
            var expressionsList = new List<string>();

            stringBuilderCache.Clear();
            var bracketCount = 1;

            for (; i < expression.Length; i++)
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
                    if (!string.IsNullOrWhiteSpace(currentExpressionStr))
                        expressionsList.Add(currentExpressionStr);
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
                throw new SyntaxException($"[{expression}] is missing characters ['{endChar}'] ");
            }

            return expressionsList;
        }


        public class InkValue
        {
            public static readonly Stack<InkValue> pool = new();
            public static InkValue Get() => pool.Count > 0 ? pool.Pop() : new InkValue();

            public static void Release(InkValue value)
            {
                value.Value_String.Clear();
                value.Value_Meta.Clear();
                pool.Push(value);
            }

            public enum InkValueType { Int, Float, Double, Char, String }

            public static InkValue Empty = null;

            public int Value_int { get; set; }
            public float Value_float { get; set; }
            public double Value_double { get; set; }
            public StringBuilder Value_String { get; set; } = new();
            public char Value_char { get; set; }

            public InkValueType ValueType { get; set; }

            public Stack<char> Value_Meta { get; set; } = new();

            public void GetNumber()
            {
                if (ValueType == InkValueType.Int)
                {
                    while (Value_Meta.Count != 0)
                    {
                        Value_int = Value_int * 10 + (Value_Meta.Pop() - '0');
                    }
                }
            }
        }

        public class InkOperator
        {
            public static readonly InkOperator Plus = new("+");
            public static readonly InkOperator Minus = new("-");
            public static readonly InkOperator Multiply = new("*");
            public static readonly InkOperator Divide = new("/");
            public static readonly InkOperator Modulo = new("%");
            public static readonly InkOperator Lower = new("<");
            public static readonly InkOperator Greater = new(">");
            public static readonly InkOperator Equal = new("==");
            public static readonly InkOperator LowerOrEqual = new("<=");
            public static readonly InkOperator GreaterOrEqual = new(">=");
            public static readonly InkOperator NotEqual = new("!=");
            public static readonly InkOperator LogicalNegation = new("!");
            public static readonly InkOperator ConditionalAnd = new("&&");
            public static readonly InkOperator ConditionalOr = new("||");
            public static readonly List<string> List_Keys = new();
            public static readonly Dictionary<string, InkOperator> Dic_Values = new();


            protected static ushort indexer;
            protected ushort OperatorValue { get; }

            protected InkOperator(string name)
            {
                OperatorValue = indexer++;
                List_Keys.Add(name);
                Dic_Values.Add(name, this);
            }

            public override bool Equals(object otherOperator) => otherOperator is InkOperator Operator && OperatorValue == Operator.OperatorValue;

            public override int GetHashCode() => OperatorValue;
        }

        public class SyntaxException : Exception
        {
            public SyntaxException(string message) : base(message) { }
        }

        
        
        /// <summary>Find <see cref="input"/> is whether start with <see cref="value"/> from <see cref="startIndex"/></summary>
        protected static bool StartsWithFromIndex(string input, string value, int startIndex)
        {
            if (input.Length - startIndex < value.Length)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
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

            value = InkValue.Get();
            value.ValueType = InkValue.InkValueType.Int;
            len = 0;

            var pointNum = 0;

            for (var i = startIndex; i < input.Length; i++)
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
                        throw new SyntaxException("[NotSupport]:Too many decimal points, can't calling method with float or double number.");
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
                            case 'f' or 'F':
                                value.ValueType = InkValue.InkValueType.Float;
                                len++;
                                break;
                            case 'd' or 'D':
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
                        value = InkValue.Get();
                        value.ValueType = InkValue.InkValueType.Char;
                        value.Value_char = EscapedChar;
                        i++;
                    }
                    else
                    {
                        throw new SyntaxException($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
                    }
                }
                else if (input[i].Equals('\''))
                {
                    throw new SyntaxException($"Illegal character[{i}] : ['']");
                }
                else
                {
                    value = InkValue.Get();
                    value.ValueType = InkValue.InkValueType.Char;
                    value.Value_char = input[i];
                    i++;
                }

                if (input[i].Equals('\''))
                {
                    len = i - startIndex + 1;
                    return true;
                }


                throw new SyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            len = 0;
            value = null;
            return false;
        }

        /// <summary>Find <see cref="input"/> is whether start with string from <see cref="startIndex"/></summary>
        protected static bool StartsWithStringFormIndex(string input, int startIndex, out InkValue value, out int len)
        {
            var i = startIndex;

            if (i < input.Length - 1 && (input[i].Equals('@') || input[i].Equals('$') && input[i + 1].Equals('\"')))
            {
                throw new Exception("don't support [@] [$]");
            }


            if (input[i].Equals('\"'))
            {
                i++;
                value = InkValue.Get();
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
                            throw new SyntaxException($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
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

                throw new SyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            len = 0;
            value = null;
            return false;
        }
        
        
    }
}