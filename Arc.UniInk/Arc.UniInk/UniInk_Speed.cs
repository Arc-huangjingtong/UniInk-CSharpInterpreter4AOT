/************************************************************************************************************************
 *  📰 Title    : UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                    *
 *  🔖 Version  : 1.0.0                                                                                                 *
 *  👩‍💻 Author   : Arc (https://github.com/Arc-huangjingtong)                                                            *
 *  🔑 Licence  : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)           *
 *  🤝 Support  : [.NET Framework 4+] [C# 8.0+] [IL2CPP Support]                                                        *
 *  📝 Desc     : [High performance] [zero box & unbox] [zero reflection runtime] [Easy-use]                                                      *
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


        protected readonly Regex regex_Operator;


        public class InkOperator
        {
            public static readonly InkOperator Plus = new(); //+
            public static readonly InkOperator Minus = new(); //-
            public static readonly InkOperator Multiply = new();//*
            public static readonly InkOperator Divide = new();// /
            public static readonly InkOperator Modulo = new();// %
            public static readonly InkOperator Lower = new();// <
            public static readonly InkOperator Greater = new();// >
            public static readonly InkOperator Equal = new();// ==
            public static readonly InkOperator LowerOrEqual = new();// <=
            public static readonly InkOperator GreaterOrEqual = new();// >=
            public static readonly InkOperator NotEqual = new();// !=
            public static readonly InkOperator LogicalNegation = new();// !
            public static readonly InkOperator ConditionalAnd = new();// &&
            public static readonly InkOperator ConditionalOr = new();// || 


            protected static ushort indexer;
            protected ushort OperatorValue { get; }

            protected InkOperator() 
            {
                indexer++;
                OperatorValue = indexer;
            }

            public bool Equals(InkOperator otherOperator)
            {
                return otherOperator != null && OperatorValue == otherOperator.OperatorValue;
            }

            public override int GetHashCode()
            {
                return OperatorValue.GetHashCode();
            }
        }
    }
}