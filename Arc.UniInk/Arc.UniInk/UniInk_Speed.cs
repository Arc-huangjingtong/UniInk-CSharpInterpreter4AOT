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
            // var numberMatch = regex_Number.Match(expression, i, expression.Length - i);
            // //make sure match number sign is not a operator
            // if (numberMatch.Success && (!numberMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is InkOperator))
            // {
            //     i += numberMatch.Length - 1;
            //
            //     if (numberMatch.Groups["type"].Success)
            //     {
            //         var type = numberMatch.Groups["type"].Value;
            //         var numberNoType = numberMatch.Value.Replace(type, string.Empty);
            //
            //         if (dic_numberParseFunc.TryGetValue(type, out var parseFunc))
            //         {
            //             stack.Push(parseFunc(numberNoType));
            //         }
            //     }
            //     else if (numberMatch.Groups["hasdecimal"].Success) //without the type suffix as double
            //     {
            //         stack.Push(double.Parse(numberMatch.Value));
            //     }
            //     else
            //     {
            //         stack.Push(int.Parse(numberMatch.Value));
            //     }
            //
            //     return true;
            // }

            return false;
        }


        public class InkValue
        {
            public static readonly Stack<InkValue> pool = new();
            public static InkValue Get() => pool.Count > 0 ? pool.Pop() : new InkValue();

            public static void Release(InkValue value)
            {
                value.Value_Meta.Clear();
                pool.Push(value);
            }

            public enum InkValueType { Int, Float, Double, }

            public static InkValue Empty = null;

            public int Value_int { get; set; }
            public float Value_float { get; set; }
            public double Value_double { get; set; }

            public InkValueType ValueType { get; set; }

            public Stack<char> Value_Meta { get; set; } = new();

            public void GetNumber()
            {
                if (ValueType== InkValueType.Int)
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

        ///  
        public static bool StartsWithNumbersFromIndex(string input, int startIndex, out InkValue value, out int len)
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
                            case 'f':
                            case 'F':
                                value.ValueType = InkValue.InkValueType.Float;
                                len++;
                                break;
                            case 'd':
                            case 'D':
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
    }
}