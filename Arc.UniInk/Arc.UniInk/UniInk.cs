﻿/*****************************************************************************************************************
 *    Title     : ExpressionEvaluator-AOT (https://github.com/Arc-huangjingtong/ExpressionEvaluator-AOT)
 *    Version   : 1.0.0
 *    Author    : Arc
 *    Licence   : MIT (https://github.com/Arc-huangjingtong/ExpressionEvaluator-AOT/blob/Format_Arc/LICENSE.md)
 *    Origin    : ExpressionEvaluator(https://github.com/codingseb/ExpressionEvaluator)
/*****************************************************************************************************************/

namespace Arc.UniInk
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;


    /// <summary>表达式求值器:简单的解释器，用于解释符串表达式/脚本</summary>
    public class ExpressionEvaluator
    {
        #region Regex

        /// 匹配C#代码中的变量或函数名
        /// sign: 匹配变量或函数名前的加号或减号。
        /// prefixOperator: 匹配变量或函数名前的自增或自减运算符。
        /// varKeyword: 匹配变量声明关键字var。
        /// nullConditional: 匹配空条件运算符?。
        /// inObject: 匹配变量或函数名前的句点(.)，表示该变量或函数是类的成员。
        /// name: 匹配变量或函数名。
        /// assignationOperator: 匹配赋值运算符和一些算术或位运算符。
        /// assignmentPrefix: 匹配赋值运算符前的算术或位运算符。
        /// postfixOperator: 匹配变量或函数名后的自增或自减运算符。
        /// isGeneric: 匹配泛型类型参数。
        /// genTag: 匹配泛型类型参数中的尖括号。
        /// isFunction: 匹配函数参数列表的左括号。
        protected static readonly Regex varOrFunctionRegEx =new(@"^((?<sign>[+-])|(?<prefixOperator>[+][+]|--)|(?<varKeyword>var)\s+|((?<nullConditional>[?])?(?<inObject>\.))?)(?<name>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)((?<assignationOperator>(?<assignmentPrefix>[+\-*/%&|^]|<<|>>|\?\?)?=(?![=>]))|(?<postfixOperator>([+][+]|--)(?![\p{L}_0-9]))|((?<isgeneric>[<](?>([\p{L}_](?>[\p{L}_0-9]*)|(?>\s+)|[,])+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?(?<isfunction>[(])?))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        //language=regex
        ///匹配C#代码中的数字
        ///sign: 匹配数字字面量前面的符号，可以是加号或减号。
        ///hasDecimal: 匹配数字字面量是否包含小数点。
        ///type: 匹配数字字面量的类型后缀，可以是u、l、d、f、m等。
        private const string numberRegexOrigPattern = @"^(?<sign>[+-])?([0-9][0-9_{1}]*[0-9]|\d)(?<hasdecimal>{0}?([0-9][0-9_]*[0-9]|\d)(e[+-]?([0-9][0-9_]*[0-9]|\d))?)?(?<type>ul|[fdulm])?";

        ///匹配C#代码中的数字
        private string numberRegexPattern;

        //匹配C#代码中的二进制和十六进制数字字面量
        ///sign : 匹配数字字面量前面的符号，可以是加号或减号。
        ///value: 匹配数字字面量的值，可以包含下划线以提高可读性。
        ///type : 匹配数字字面量的进制类型，可以是x（十六进制）或b（二进制）。
        protected static readonly Regex otherBasesNumberRegex = new("^(?<sign>[+-])?(?<value>0(?<type>x)([0-9a-f][0-9a-f_]*[0-9a-f]|[0-9a-f])|0(?<type>b)([01][01_]*[01]|[01]))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        //匹配 字符串前面的$或者@符号或者“双引号
        protected static readonly Regex stringBeginningRegex = new("^(?<interpolated>[$])?(?<escaped>[@])?[\"]", RegexOptions.Compiled);

        //匹配 单字符中的转义和非转移字符
        private static readonly Regex internalCharRegex = new(@"^['](\\[\\'0abfnrtv]|[^'])[']", RegexOptions.Compiled);

        //匹配 空值条件运算符
        private static readonly Regex indexingBeginningRegex = new(@"^(?<nullConditional>[?])?\[", RegexOptions.Compiled);

        //匹配 数组或者二位数组
        private static readonly Regex arrayTypeDetectionRegex = new(@"^(\s*(\[(?>(?>\s+)|[,])*)\])+", RegexOptions.Compiled);

        //匹配 赋值运算符或后缀运算符。
        private static readonly Regex assignationOrPostFixOperatorRegex = new(@"^(?>\s*)((?<assignmentPrefix>[+\-*/%&|^]|<<|>>|\?\?)?=(?![=>])|(?<postfixOperator>([+][+]|--)(?![\p{L}_0-9])))");

        //匹配 泛型
        private static readonly Regex genericsDecodeRegex = new("(?<name>[^,<>]+)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?", RegexOptions.Compiled);

        //匹配 泛型类型参数列表的末尾
        private static readonly Regex genericsEndOnlyOneTrim = new(@"(?>\s*)[>](?>\s*)$", RegexOptions.Compiled);

        //匹配 $标记的字符串末尾
        protected static readonly Regex endOfStringWithDollar = new("^([^\"{\\\\]|\\\\[\\\\\"0abfnrtv])*[\"{]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithoutDollar = new("^([^\"\\\\]|\\\\[\\\\\"0abfnrtv])*[\"]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithDollarWithAt = new("^[^\"{]*[\"{]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithoutDollarWithAt = new("^[^\"]*[\"]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringInterpolationRegex = new("^('\"'|[^}\"])*[}\"]", RegexOptions.Compiled);
        protected static readonly Regex stringBeginningForEndBlockRegex = new("[$]?[@]?[\"]$", RegexOptions.Compiled);
        protected static readonly Regex lambdaExpressionRegex = new(@"^(?>\s*)(?<args>((?>\s*)[(](?>\s*)([\p{L}_](?>[\p{L}_0-9]*)(?>\s*)([,](?>\s*)[\p{L}_][\p{L}_0-9]*(?>\s*))*)?[)])|[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)=>(?<expression>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        protected static readonly Regex lambdaArgRegex = new(@"[\p{L}_](?>[\p{L}_0-9]*)", RegexOptions.Compiled);
        
        protected static readonly Regex initInNewBeginningRegex = new($@"^(?>\s*){{", RegexOptions.Compiled);
        
        protected static readonly Regex functionArgKeywordsRegex = new(@"^\s*(?<keyword>out|ref|in)\s+((?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)\s+(?=[\p{L}_]))?(?<toEval>(?<varName>[\p{L}_](?>[\p{L}_0-9]*))\s*(=.*)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static readonly Regex instanceCreationWithNewKeywordRegex = new(@"^new(?=\w)\s*((?<isAnonymous>[{])|((?<name>[\p{L}_][\p{L}_0-9]*)(?>\s*)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?(?>\s*)((?<isfunction>[(])|(?<isArray>\[)|(?<isInit>[{{]))?))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected string CastRegexPattern = @"^\((?>\s*)(?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)(?>\s*)\)";


        // 仅限于脚本模式下
        /// 匹配部分语法代码快的开始
        private static readonly Regex blockKeywordsBeginningRegex = new(@"^(?>\s*)(?<keyword>while|for|foreach|if|else(?>\s*)if|catch)(?>\s*)[(]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配foreach语法
        private static readonly Regex foreachParenThisEvaluationRegex = new(@"^(?>\s*)(?<variableName>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)(?<in>in)(?>\s*)(?<collection>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配if语法的开始
        private static readonly Regex blockKeywordsWithoutParenthesesBeginningRegex = new(@"^(?>\s*)(?<keyword>else|do|try|finally)(?![\p{L}_0-9])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配花括号的开始
        private static readonly Regex blockBeginningRegex = new(@"^(?>\s*)[{]", RegexOptions.Compiled);

        /// 匹配return关键字
        private static readonly Regex returnKeywordRegex = new(@"^return((?>\s*)|\()", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// 匹配 ; 结束符
        private static readonly Regex nextIsEndOfExpressionRegex = new(@"^(?>\s*)[;]", RegexOptions.Compiled);

        #endregion

        #region Enums

        protected enum IfBlockEvaluatedState { NoBlockEvaluated, If, ElseIf }

        protected enum TryBlockEvaluatedState { NoBlockEvaluated, Try, Catch }

        #endregion

        #region DictionaryData (Primary types, number suffix, escaped chars, operators management, default vars and functions)

        private static readonly Dictionary<string, Type> primaryTypesDict = new()
        {
            { "object", typeof(object) },
            { "string", typeof(string) },
            { "bool", typeof(bool) },
            { "bool?", typeof(bool?) },
            { "byte", typeof(byte) },
            { "byte?", typeof(byte?) },
            { "char", typeof(char) },
            { "char?", typeof(char?) },
            { "decimal", typeof(decimal) },
            { "decimal?", typeof(decimal?) },
            { "double", typeof(double) },
            { "double?", typeof(double?) },
            { "short", typeof(short) },
            { "short?", typeof(short?) },
            { "int", typeof(int) },
            { "int?", typeof(int?) },
            { "long", typeof(long) },
            { "long?", typeof(long?) },
            { "sbyte", typeof(sbyte) },
            { "sbyte?", typeof(sbyte?) },
            { "float", typeof(float) },
            { "float?", typeof(float?) },
            { "ushort", typeof(ushort) },
            { "ushort?", typeof(ushort?) },
            { "uint", typeof(uint) },
            { "uint?", typeof(uint?) },
            { "ulong", typeof(ulong) },
            { "ulong?", typeof(ulong?) },
            { "void", typeof(void) }
        };

        /// 强转类型字典
        /// 基于 https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2012/y5b434w4(v=vs.110)?redirectedfrom=MSDN
        private static readonly Dictionary<Type, Type[]> implicitCastDict = new()
        {
            { typeof(sbyte), new[] { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(byte), new[] { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(short), new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(ushort), new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(int), new[] { typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(uint), new[] { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(long), new[] { typeof(float), typeof(double), typeof(decimal) } },
            { typeof(char), new[] { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(float), new[] { typeof(double) } },
            { typeof(ulong), new[] { typeof(float), typeof(double), typeof(decimal) } },
        };

        /// 数字后缀字典  Always Case insensitive, like in C#
        private static readonly Dictionary<string, Func<string, CultureInfo, object>> numberSuffixToParse = new(StringComparer.OrdinalIgnoreCase)
        {
            { "f", (number, culture) => float.Parse(number, NumberStyles.Any, culture) },
            { "d", (number, culture) => double.Parse(number, NumberStyles.Any, culture) },
            { "u", (number, culture) => uint.Parse(number, NumberStyles.Any, culture) },
            { "l", (number, culture) => long.Parse(number, NumberStyles.Any, culture) },
            { "ul", (number, culture) => ulong.Parse(number, NumberStyles.Any, culture) },
            { "m", (number, culture) => decimal.Parse(number, NumberStyles.Any, culture) }
        };

        /// 转义字符串字典
        private static readonly Dictionary<char, string> stringEscapedCharDict = new()
        {
            { '\\', @"\" },
            { '"', "\"" },
            { '0', "\0" },
            { 'a', "\a" },
            { 'b', "\b" },
            { 'f', "\f" },
            { 'n', "\n" },
            { 'r', "\r" },
            { 't', "\t" },
            { 'v', "\v" }
        };

        /// 转义字符字典
        private static readonly Dictionary<char, char> charEscapedCharDict = new()
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

        /// 操作符字典
        public Dictionary<string, ExpressionOperator> operatorsDictionary = new(StringComparer.Ordinal)
        {
            { "+", ExpressionOperator.Plus },
            { "-", ExpressionOperator.Minus },
            { "*", ExpressionOperator.Multiply },
            { "/", ExpressionOperator.Divide },
            { "%", ExpressionOperator.Modulo },
            { "<", ExpressionOperator.Lower },
            { ">", ExpressionOperator.Greater },
            { "<=", ExpressionOperator.LowerOrEqual },
            { ">=", ExpressionOperator.GreaterOrEqual },
            { "is", ExpressionOperator.Is },
            { "==", ExpressionOperator.Equal },
            { "!=", ExpressionOperator.NotEqual },
            { "&&", ExpressionOperator.ConditionalAnd },
            { "||", ExpressionOperator.ConditionalOr },
            { "!", ExpressionOperator.LogicalNegation },
            { "~", ExpressionOperator.BitwiseComplement },
            { "&", ExpressionOperator.LogicalAnd },
            { "|", ExpressionOperator.LogicalOr },
            { "^", ExpressionOperator.LogicalXor },
            { "<<", ExpressionOperator.ShiftBitsLeft },
            { ">>", ExpressionOperator.ShiftBitsRight },
            { "??", ExpressionOperator.NullCoalescing },
        };

        /// 一元左操作符
        protected static readonly List<ExpressionOperator> UnaryPrefixOperators = new();

        /// 一元右操作符
        protected static readonly List<ExpressionOperator> UnaryPostfixOperators = new()
        {
            ExpressionOperator.LogicalNegation, // !a 逻辑取反
            ExpressionOperator.BitwiseComplement, // ~a 位运算取反
            ExpressionOperator.UnaryPlus, // +a 一元加号,表示正数符号
            ExpressionOperator.UnaryMinus // -a 一元减号,表示负数符号
        };

        /// 二元操作符计算逻辑 ||这是一个字典的列表,记录了所有操作的具体方法
        protected static readonly Dictionary<ExpressionOperator, Func<object, object, object>> OperatorsEvaluation = new()
        {
            { ExpressionOperator.UnaryPlus, (_, right) => +(int)right }, // 一元加号,表示正数符号
            { ExpressionOperator.UnaryMinus, (_, right) => -(int)right }, // 一元减号,表示负数符号
            { ExpressionOperator.LogicalNegation, (_, right) => !(bool)right }, // 逻辑取反
            { ExpressionOperator.BitwiseComplement, (_, right) => ~(int)right }, // 位运算取反
            { ExpressionOperator.Cast, (left, right) => ChangeType(right, (Type)left) }, // 强制类型转换
            { ExpressionOperator.Multiply, (left, right) => (int)left * (int)right }, // 乘法
            { ExpressionOperator.Divide, (left, right) => (int)left / (int)right }, // 除法
            { ExpressionOperator.Modulo, (left, right) => (int)left % (int)right }, // 取余
            {
                ExpressionOperator.Plus, (left, right) =>
                {
                    if (!left.GetType().IsValueType || !right.GetType().IsValueType) return null;
                    if (left is IConvertible leftConvertible)
                    {
                        left = leftConvertible.ToInt32(null);
                    }

                    if (right is IConvertible rightConvertible)
                    {
                        right = rightConvertible.ToInt32(null);
                    }

                    return (int)left + (int)right; //报错InvalidCastException: Specified cast is not valid.
                }
            }, // 加法
            { ExpressionOperator.Minus, (left, right) => (int)left - (int)right }, // 减法
            { ExpressionOperator.ShiftBitsLeft, (left, right) => (int)left << (int)right }, // 左移 
            { ExpressionOperator.ShiftBitsRight, (left, right) => (int)left >> (int)right }, // 右移
            { ExpressionOperator.Lower, (left, right) => (int)left < (int)right }, // 小于
            { ExpressionOperator.Greater, (left, right) => (int)left > (int)right }, // 大于
            { ExpressionOperator.LowerOrEqual, (left, right) => (int)left <= (int)right }, // 小于等于
            { ExpressionOperator.GreaterOrEqual, (left, right) => (int)left >= (int)right }, // 大于等于
            { ExpressionOperator.Is, (left, right) => left != null && ((ClassOrEnumType)right).Type.IsInstanceOfType(left) }, // 类型判断
            { ExpressionOperator.Equal, (left, right) => left == right }, // 等于
            { ExpressionOperator.NotEqual, (left, right) => left != right }, // 不等于
            { ExpressionOperator.LogicalAnd, (left, right) => (int)left & (int)right }, // 逻辑与
            { ExpressionOperator.LogicalXor, (left, right) => (int)left ^ (int)right }, // 逻辑异或
            { ExpressionOperator.LogicalOr, (left, right) => (int)left | (int)right }, // 逻辑或
            {
                ExpressionOperator.ConditionalAnd, (left, right) =>
                {
                    if (left is BubbleExceptionContainer leftExceptionContainer)
                    {
                        leftExceptionContainer.Throw();
                        return null; // this line is never reached
                    }

                    if (!(bool)left) return false;

                    if (right is BubbleExceptionContainer rightExceptionContainer)
                    {
                        rightExceptionContainer.Throw();
                        return null; // this line is never reached
                    }

                    return (bool)left && (bool)right; // 条件与
                }
            },
            {
                ExpressionOperator.ConditionalOr, (left, right) =>
                {
                    if (left is BubbleExceptionContainer leftExceptionContainer)
                    {
                        leftExceptionContainer.Throw();
                        return null; // this line is never reached
                    }

                    if ((bool)left) return true;

                    if (right is BubbleExceptionContainer rightExceptionContainer)
                    {
                        rightExceptionContainer.Throw();
                        return null; // this line is never reached
                    }

                    return (bool)left || (bool)right; // 条件或
                }
            },
            { ExpressionOperator.NullCoalescing, (left, right) => left ?? right }, // 空合并
        };

        /// <summary>默认变量</summary>
        private Dictionary<string, object> defaultVariables = new(StringComparer.Ordinal)
        {
            { "Pi", Math.PI },
            { "E", Math.E },
            { "null", null },
            { "true", true },
            { "false", false },
            { "this", null }
        };

        ///简单的浮点数计算函数
        private Dictionary<string, Func<double, double>> simpleDoubleMathFuncDictionary = new(StringComparer.Ordinal)
        {
            { "Abs", Math.Abs },
            { "Acos", Math.Acos },
            { "Asin", Math.Asin },
            { "Atan", Math.Atan },
            { "Ceiling", Math.Ceiling },
            { "Cos", Math.Cos },
            { "Cosh", Math.Cosh },
            { "Exp", Math.Exp },
            { "Floor", Math.Floor },
            { "Log10", Math.Log10 },
            { "Sin", Math.Sin },
            { "Sinh", Math.Sinh },
            { "Sqrt", Math.Sqrt },
            { "Tan", Math.Tan },
            { "Tanh", Math.Tanh },
            { "Truncate", Math.Truncate },
        };

        ///复杂的双参数计算函数
        private Dictionary<string, Func<double, double, double>> doubleDoubleMathFuncDictionary = new(StringComparer.Ordinal)
        {
            { "Atan2", Math.Atan2 }, { "IEEERemainder", Math.IEEERemainder }, { "Log", Math.Log }, { "Pow", Math.Pow },
        };

        ///复杂的基本函数
        private Dictionary<string, Func<ExpressionEvaluator, List<string>, object>> complexStandardFuncDictionary = new(StringComparer.Ordinal)
        {
            { "Array", (self, args) => args.ConvertAll(self.Evaluate).ToArray() },
            {
                "ArrayOfType", (self, args) =>
                {
                    Array sourceArray = args.Skip(1).Select(self.Evaluate).ToArray();
                    var typedArray = Array.CreateInstance((Type)self.Evaluate(args[0]), sourceArray.Length);
                    Array.Copy(sourceArray, typedArray, sourceArray.Length);

                    return typedArray;
                }
            },
            { "Avg", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Sum() / (double)args.Count },
            {
                "default", (self, args) =>
                {
                    var argValue = self.Evaluate(args[0]);

                    if (argValue is ClassOrEnumType classOrTypeName)
                        return Activator.CreateInstance(classOrTypeName.Type);
                    else
                        return null;
                }
            },
            { "in", (self, args) => args.Skip(1).ToList().ConvertAll(self.Evaluate).Contains(self.Evaluate(args[0])) },
            { "List", (self, args) => args.ConvertAll(self.Evaluate) },
            {
                "ListOfType", (self, args) =>
                {
                    var type = (Type)self.Evaluate(args[0]);
                    Array sourceArray = args.Skip(1).Select(self.Evaluate).ToArray();
                    var typedArray = Array.CreateInstance(type, sourceArray.Length);
                    Array.Copy(sourceArray, typedArray, sourceArray.Length);

                    var typeOfList = typeof(List<>).MakeGenericType(type);

                    var list = Activator.CreateInstance(typeOfList);

                    typeOfList.GetMethod("AddRange")?.Invoke(list, new object[] { typedArray });

                    return list;
                }
            },
            { "Max", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Max() },
            { "Min", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Min() },
            {
                "new", (self, args) =>
                {
                    var cArgs = args.ConvertAll(self.Evaluate);
                    return cArgs[0] is ClassOrEnumType classOrEnumType ? Activator.CreateInstance(classOrEnumType.Type, cArgs.Skip(1).ToArray()) : null;
                }
            },
            {
                "Round", (self, args) =>
                {
                    if (args.Count == 3)
                    {
                        return Math.Round(Convert.ToDouble(self.Evaluate(args[0])), Convert.ToInt32(self.Evaluate(args[1])), (MidpointRounding)self.Evaluate(args[2]));
                    }
                    else if (args.Count == 2)
                    {
                        var arg2 = self.Evaluate(args[1]);

                        if (arg2 is MidpointRounding midpointRounding)
                            return Math.Round(Convert.ToDouble(self.Evaluate(args[0])), midpointRounding);
                        else
                            return Math.Round(Convert.ToDouble(self.Evaluate(args[0])), Convert.ToInt32(arg2));
                    }
                    else if (args.Count == 1) { return Math.Round(Convert.ToDouble(self.Evaluate(args[0]))); }

                    else throw new ArgumentException();
                }
            },
            { "Sign", (self, args) => Math.Sign(Convert.ToDouble(self.Evaluate(args[0]))) },
            { "typeof", (self, args) => ((ClassOrEnumType)self.Evaluate(args[0])).Type },
        };

        #endregion

        #region Caches

        /// <summary>
        /// 是否将已经解析的类型缓存起来，以便下次更快地解析,默认关闭 <para/>
        /// 缓存是静态字典TypesResolutionCaching(所以它被所有启用了CacheTypesResolutions的ExpressionEvaluator实例共享)
        /// </summary>
        public bool CacheTypesResolutions { get; set; }

        /// <summary> 用于类型解析的共享缓存 </summary>
        public static Dictionary<string, Type> TypesResolutionCaching { get; set; } = new();

        /// <summary> 清除所有ExpressionEvaluator缓存 </summary>
        public static void ClearAllCaches() => TypesResolutionCaching.Clear();


        protected ExpressionEvaluationEventArg _evaluationEventArg;
        protected VariableEvaluationEventArg _variableEvaluationEventArg;
        protected FunctionEvaluationEventArg _functionEvaluationEventArg;
        protected IndexingPreEvaluationEventArg _indexingPreEvaluationEventArg;
        protected ParameterCastEvaluationEventArg _parameterCastEvaluationEventArg;
        protected VariablePreEvaluationEventArg _variablePreEvaluationEventArg;
        protected FunctionPreEvaluationEventArg _functionPreEvaluationEventArg;

        #endregion

        #region 程序集, 命名空间, 类型列表

        private static IList<Assembly> staticAssemblies;
        private IList<Assembly> assemblies;

        /// <summary>
        /// 解析类型所需的所有程序集<para/>
        /// 默认情况下，当前AppDomain中加载的所有程序集<para/>
        /// </summary>
        public IList<Assembly> Assemblies
        {
            get => assemblies ?? (assemblies = staticAssemblies) ?? (assemblies = staticAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList());
            set => assemblies = value;
        }

        /// <summary>
        /// 在其中查找类型的所有命名空间<para/>
        /// 等价于<c>using Namespace;</c>
        /// </summary>
        public IList<string> Namespaces { get; set; } = new List<string>
        {
            "System",
            "System.Linq",
            "System.IO",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Collections",
            "System.Collections.Generic",
            "System.Collections.Specialized",
            "System.Globalization"
        };

        /// <summary>添加或删除要在表达式中管理的特定类型。</summary>
        public IList<Type> Types { get; set; } = new List<Type>();

        /// <summary>出于安全目的，要在表达式求值中阻止或保持不可用的类型列表</summary>
        public IList<Type> TypesToBlock { get; set; } = new List<Type>();

        /// <summary>用于查找扩展方法的静态类型列表</summary>
        public IList<Type> StaticTypesForExtensionsMethods { get; set; } = new List<Type>
        {
            typeof(Enumerable) // 用于Linq扩展方法
        };

        #endregion

        #region Options

        /// <summary>是否区分大小写:默认为真</summary>
        public bool OptionCaseSensitiveEvaluationActive
        {
            get => optionCaseSensitiveEvaluationActive;
            set
            {
                optionCaseSensitiveEvaluationActive = value;
                Variables = Variables;
                operatorsDictionary = new Dictionary<string, ExpressionOperator>(operatorsDictionary, StringComparerForCasing);
                defaultVariables = new Dictionary<string, object>(defaultVariables, StringComparerForCasing);
                simpleDoubleMathFuncDictionary = new Dictionary<string, Func<double, double>>(simpleDoubleMathFuncDictionary, StringComparerForCasing);
                doubleDoubleMathFuncDictionary = new Dictionary<string, Func<double, double, double>>(doubleDoubleMathFuncDictionary, StringComparerForCasing);
                complexStandardFuncDictionary = new Dictionary<string, Func<ExpressionEvaluator, List<string>, object>>(complexStandardFuncDictionary, StringComparerForCasing);
            }
        }


        /// <summary>
        /// 设置变量时,是否自己控制比较器<para/>
        /// 真: 变量字典将保留为给定状态，因此变量在求值器外保持不变，并且键的比较器可以由用户定义<para/>
        /// 假: 变量字典的引用在内部被复制，以遵循OptionCaseSensitiveEvaluationActive，并使用内部受保护的比较器进行键的比较<para/>
        /// 默认为false
        /// </summary>
        public bool OptionVariablesPersistenceCustomComparer { get; set; }


        /// <summary>是否区分大小写</summary>
        private bool optionCaseSensitiveEvaluationActive = true;

        /// <summary>是否区分大小写:导致的通配符规则</summary>
        private RegexOptions regexOption => optionCaseSensitiveEvaluationActive ? RegexOptions.None : RegexOptions.IgnoreCase;

        /// <summary>是否区分大小写:导致的字符串比较器规则</summary>
        private StringComparison StringComparisonForCasing => optionCaseSensitiveEvaluationActive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        /// <summary>是否区分大小写:导致的字符串比较器规则</summary>
        private StringComparer StringComparerForCasing => OptionCaseSensitiveEvaluationActive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// 真: 所有不带小数和精度后缀的数都被视为double类型 <para/>
        /// 假: 所有不带小数和精度后缀的数都被视为int类型 (警告:部分操作会导致四舍五入<para/>
        /// 默认:假
        /// </summary>
        public bool OptionForceIntegerNumbersEvaluationsAsDoubleByDefault { get; set; }


        /// <summary>
        /// 用来求值的数字的文化信息.<para/>
        /// 与OptionNumberParsingDecimalSeparator和OptionNumberParsingThousandSeparator同步。<para/>
        /// 所以总是设置一个完整的CultureInfo对象，不要直接改变CultureInfoForNumberParsing.NumberFormat.NumberDecimalSeparator和CultureInfoForNumberParsing.NumberFormat.NumberGroupSeparator属性。<para/>
        /// 警告，如果在分隔符中使用逗号，也要更改OptionFunctionArgumentsSeparator和OptionInitializersSeparator，否则会产生冲突
        /// </summary>
        public CultureInfo CultureInfoForNumberParsing
        {
            get => cultureInfoForNumberParsing;

            set
            {
                cultureInfoForNumberParsing = value;

                OptionNumberParsingDecimalSeparator = cultureInfoForNumberParsing.NumberFormat.NumberDecimalSeparator;
                OptionNumberParsingThousandSeparator = cultureInfoForNumberParsing.NumberFormat.NumberGroupSeparator;
            }
        }

        private CultureInfo cultureInfoForNumberParsing = CultureInfo.InvariantCulture.Clone() as CultureInfo;

        private string optionNumberParsingDecimalSeparator = ".";

        /// <summary>
        /// 设置解释器数字的小数分隔符(默认: ".")<para/>
        /// 警告：如果同时使用逗号更改OptionFunctionArgumentsSeparator和OptionInitializersSeparator，则会产生冲突。<para/>
        /// 修改CultureInfoForNumberParsing
        /// </summary>
        public string OptionNumberParsingDecimalSeparator
        {
            get => optionNumberParsingDecimalSeparator;
            set
            {
                optionNumberParsingDecimalSeparator = value ?? ".";
                CultureInfoForNumberParsing.NumberFormat.NumberDecimalSeparator = optionNumberParsingDecimalSeparator;

                numberRegexPattern = string.Format(numberRegexOrigPattern, optionNumberParsingDecimalSeparator != null ? Regex.Escape(optionNumberParsingDecimalSeparator) : ".", optionNumberParsingThousandSeparator != null ? Regex.Escape(optionNumberParsingThousandSeparator) : "");
            }
        }

        private string optionNumberParsingThousandSeparator = string.Empty;

        /// <summary>
        /// 允许在解析表达式时更改数字的千位分隔符<para/>
        /// 默认为 string.Empty<para/>
        /// 警告，如果使用逗号，也改变OptionFunctionArgumentsSeparator和OptionInitializersSeparator，否则会产生冲突。<para/>
        /// 修改 CultureInfoForNumberParsing。
        /// </summary>
        public string OptionNumberParsingThousandSeparator
        {
            get => optionNumberParsingThousandSeparator;

            set
            {
                optionNumberParsingThousandSeparator = value ?? string.Empty;
                CultureInfoForNumberParsing.NumberFormat.NumberGroupSeparator = value ?? string.Empty;

                numberRegexPattern = string.Format(numberRegexOrigPattern, optionNumberParsingDecimalSeparator != null ? Regex.Escape(optionNumberParsingDecimalSeparator) : ".", optionNumberParsingThousandSeparator != null ? Regex.Escape(optionNumberParsingThousandSeparator) : "");
            }
        }

        /// <summary>设置函数参数的分隔符 (默认: “,”)</summary>
        /// <remarks>警告:设置为空格会产生冲突</remarks>
        public string OptionFunctionArgumentsSeparator { get; set; } = ",";

        /// <summary>设置关键字new之后的“{”和“}”之间的对象和集合初始化的分隔符 (默认: “,”)</summary>
        /// <remarks>警告:OptionNumberParsingDecimalSeparator设置为逗号","会和此产生冲突</remarks>
        public string OptionInitializersSeparator { get; set; } = ",";


        /// <summary>允许使用内存中的任何内联命名空间 见: <see cref="EInlineNamespacesRule"/></summary>
        public EInlineNamespacesRule OptionEInlineNamespacesRule { get; set; } = EInlineNamespacesRule.AllowAll;

        /// <summary>此列表用于允许或阻止取决于<see cref="OptionEInlineNamespacesRule"/>内联写入的命名空间列表。<para/>
        /// 类型依赖的直接访问 <see cref="Namespaces"/> 不受此列表的影响。
        /// </summary>
        public List<string> InlineNamespacesList { get; set; } = new();

        private Func<ExpressionEvaluator, List<string>, object> newMethodMem;

        /// <summary>
        /// T:(默认)允许使用默认函数new(ClassNam，…)创建对象实例。
        /// F:禁用此功能。
        /// </summary>
        public bool OptionNewFunctionEvaluationActive
        {
            get => complexStandardFuncDictionary.ContainsKey("new");
            set
            {
                if (value && !complexStandardFuncDictionary.ContainsKey("new"))
                {
                    complexStandardFuncDictionary["new"] = newMethodMem;
                }
                else if (!value && complexStandardFuncDictionary.ContainsKey("new"))
                {
                    newMethodMem = complexStandardFuncDictionary["new"];
                    complexStandardFuncDictionary.Remove("new");
                }
            }
        }

        /// <summary>
        /// T:(默认)允许使用 C# 语法 new ClassName(...) 创建对象实例。<para/>
        /// F:禁用此功能。<para/>
        /// </summary>
        public bool OptionNewKeywordEvaluationActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许在类上调用静态方法<para/>
        /// F:禁用此功能<para/>
        /// </summary>
        public bool OptionStaticMethodsCallActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许获取类的静态属性。<para/>
        /// F:禁用此功能。<para/>
        /// </summary>
        public bool OptionStaticPropertiesGetActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许在对象上调用实例方法。<para/>
        /// F:禁用此功能。<para/>
        /// </summary>
        public bool OptionInstanceMethodsCallActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许获取对象的实例属性<para/>
        /// F:禁用此功能<para/>
        /// </summary>
        public bool OptionInstancePropertiesGetActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许通过索引或键获取对象，如<code>IndexedObject[indexOrKey]</code><para/>
        /// F:禁用此功能<para/>
        /// </summary>
        public bool OptionIndexerActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许使用双引号对字符串进行解释<para/>
        /// F:禁用此功能<para/>
        /// </summary>
        public bool OptionStringEvaluationActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许使用单引号对字符进行解释。<para/>
        /// F:禁用此功能。<para/>
        /// </summary>
        public bool OptionCharEvaluationActive { get; set; } = true;

        /// <summary>
        /// T:(默认)能否在脚本内部调用解释器Evaluate
        /// F:如果为安全起见设置为false(同时确保ExpressionEvaluator类型在TypesToBlock列表中)
        /// </summary>
        public bool OptionEvaluateFunctionActive { get; set; } = true;

        /// <summary>
        /// T:(默认)能否在脚本内部调用解释器ScriptEvaluate
        /// F:如果为安全起见设置为false(同时确保ExpressionEvaluator类型在TypesToBlock列表中)
        /// </summary>
        public bool OptionScriptEvaluateFunctionActive { get; set; } = true;


        /// <summary>
        /// T:(默认)允许将值赋给变量中的变量字典 with (=, +=, -=, *=, /=, %=, &amp;=, |=, ^=, &lt;&lt;=, &gt;&gt;=, ++ or --)<para/>
        /// F:则禁用此功能<para/>
        /// </summary>
        public bool OptionVariableAssignationActive { get; set; } = true;

        /// <summary>
        /// T:(默认)允许使用 (=, +=, -=, *=, /=, %=, &amp;=, |=, ^=, &lt;&lt;=, &gt;&gt;=, ++ or --)<para/>
        /// F:则禁用此功能<para/>
        /// </summary>
        public bool OptionPropertyOrFieldSetActive { get; set; } = true;

        /// <summary>
        /// T:(默认)则允许对类似于集合、列表、数组和字典的索引元素进行赋值（使用=、+=、-=、*=、/=、%=、&=、|=、^=、<<=、>>=、++或--）<para/>
        /// F:则禁用此功能<para/>
        /// </summary>
        public bool OptionIndexingAssignationActive { get; set; } = true;


        /// <summary>
        /// 设置在脚本中找不到关键字return时如何反应。默认为ReturnLastResult
        /// </summary>
        public EOnNoReturnKeywordMode NoReturnKeywordMode { get; set; }

        /// <summary>
        /// T:(默认)则在每个表达式之后，ScriptEvaluate需要有一个分号[;]<para/>
        /// F:则允许在脚本的最后一个表达式省略分号<para/>
        /// </summary>
        public bool OptionNeedScriptEndSemicolon { get; set; } = true;

        /// <summary>
        /// T:(默认)则在调用扩展方法失败时，将检测所有定义的该方法的重载，以确定是使用了错误的参数定义和调用方法，还是方法未定义。<para/>
        /// F:则不成功的扩展方法调用将始终导致"Method {name} is not defined on type {type}"（方法{name}在类型{type}上未定义）<para/>
        /// </summary>
        public bool OptionDetectExtensionMethodsOverloadsOnExtensionMethodNotFound { get; set; } = true;

        /// <summary>
        /// T:(默认)则允许在表达式中定义多表达式lambda（而不是在脚本中）<para/>
        /// F:则如果不在脚本中，只能定义简单的表达式lambda<para/>
        /// </summary>
        public bool OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate { get; set; } = true;

        #endregion

        #region Reflection Flags

        private BindingFlags InstanceBindingFlag => BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Static | (OptionCaseSensitiveEvaluationActive ? 0 : BindingFlags.IgnoreCase);
        private BindingFlags StaticBindingFlag => BindingFlags.Public | BindingFlags.Static | (OptionCaseSensitiveEvaluationActive ? 0 : BindingFlags.IgnoreCase);

        #endregion

        #region 自定义和动态求值

        private Dictionary<string, object> variables = new(StringComparer.Ordinal);

        /// <summary>计算堆栈初始化次数，以确定是否到达了表达式入口点。在这种情况下，应该抛出传输的异常。</summary>
        private int evaluationStackCount;

        /// <summary>如果设置了，该对象将使用它的字段、属性和方法作为全局变量和函数</summary>
        public object Context
        {
            get => defaultVariables["this"];
            set => defaultVariables["this"] = value;
        }

        /// <summary>
        /// 的当前实例计算的表达式和脚本中可以使用的变量名/值字典 <see cref="ExpressionEvaluator"/><para/>
        /// 警告:复制给定的字典以管理大小写。
        /// </summary>
        public Dictionary<string, object> Variables
        {
            get => variables;
            set
            {
                if (OptionVariablesPersistenceCustomComparer)
                {
                    variables = value;
                }
                else
                {
                    variables = value == null ? new Dictionary<string, object>(StringComparerForCasing) : new Dictionary<string, object>(value, StringComparerForCasing);
                }
            }
        }

        /// <summary>在解释脚本前触发</summary>
        public event EventHandler<ExpressionEvaluationEventArg> OnEvaluatScript;

        /// <summary>在计算表达式之前触发</summary>
        /// <remarks>允许在此修改表达式以计算或强制返回值</remarks>
        public event EventHandler<ExpressionEvaluationEventArg> ExpressionEvaluating;

        /// <summary>在返回表达式求值之前被触发。</summary>
        /// <remarks>允许即时修改返回值</remarks>
        public event EventHandler<ExpressionEvaluationEventArg> ExpressionEvaluated;

        /// <summary>在变量、字段或属性解析之前激发。</summary>
        /// 允许动态定义变量和相应的值。<para/>
        /// 允许取消这个变量的解释(视为它不存在)<para/>
        public event EventHandler<VariablePreEvaluationEventArg> PreEvaluateVariable;

        /// <summary>在函数或方法解析之前触发。</summary>
        /// 允许动态定义函数或方法及其对应的值。<para/>
        /// 允许取消对该函数的评估（将其视为不存在）。<para/>
        public event EventHandler<FunctionPreEvaluationEventArg> PreEvaluateFunction;

        /// <summary>在索引解析之前触发。</summary>
        /// 允许动态定义索引和对应的值。<para/>
        /// 允许取消对该索引的评估（将其视为不存在）。
        public event EventHandler<IndexingPreEvaluationEventArg> PreEvaluateIndexing;

        /// <summary>如果未找到变量、字段或属性，则触发。</summary>
        /// 允许动态定义变量及其对应的值。<para/>
        public event EventHandler<VariableEvaluationEventArg> EvaluateVariable;

        /// <summary>如果未找到函数或方法，则触发。</summary>
        /// 允许动态定义函数或方法及其对应的值。<para/>
        public event EventHandler<FunctionEvaluationEventArg> EvaluateFunction;

        /// <summary>当参数对于函数来说不是正确的类型时触发</summary>
        /// 允许动态定义自定义参数转换以使函数调用正常运行
        public event EventHandler<ParameterCastEvaluationEventArg> EvaluateParameterCast;

        #endregion

        #region 构造函数和可重写的init方法

        /// <summary>默认构造器</summary>
        public ExpressionEvaluator()
        {
            DefaultDecimalSeparatorInit();
            Init();
        }

        /// <summary>带有初始化变量的构造函数</summary>
        /// <param name="variables">解释器中可以使用的变量</param>
        public ExpressionEvaluator(Dictionary<string, object> variables) : this() => Variables = variables;

        /// <summary>具有上下文初始化的构造函数</summary>
        /// <param name="context">提出它的字段、属性和方法的上下文</param>
        public ExpressionEvaluator(object context) : this() => Context = context;

        /// <summary>带有初始化变量的构造函数</summary>
        /// <param name="variables">解释器中可以使用的变量</param>
        /// <param name="optionVariablesPersistenceCustomComparer">在设置<see cref="Variables"/>前设置<see cref="OptionVariablesPersistenceCustomComparer"/></param>
        public ExpressionEvaluator(Dictionary<string, object> variables, bool optionVariablesPersistenceCustomComparer) : this()
        {
            OptionVariablesPersistenceCustomComparer = optionVariablesPersistenceCustomComparer;
            Variables = variables;
        }

        /// <summary>具有变量和上下文初始化的构造函数</summary>
        /// <param name="context">提出它的字段、属性和方法的上下文</param>
        /// <param name="variables">解释器中可以使用的变量</param>
        public ExpressionEvaluator(object context, Dictionary<string, object> variables) : this()
        {
            Context = context;
            Variables = variables;
        }

        /// <summary>具有变量和上下文初始化的构造函数</summary>
        /// <param name="context">提出它的字段、属性和方法的上下文</param>
        /// <param name="variables">解释器中可以使用的变量</param>
        /// <param name="optionVariablesPersistenceCustomComparer">在设置<see cref="Variables"/>前设置<see cref="OptionVariablesPersistenceCustomComparer"/></param>
        public ExpressionEvaluator(object context, Dictionary<string, object> variables, bool optionVariablesPersistenceCustomComparer) : this()
        {
            OptionVariablesPersistenceCustomComparer = optionVariablesPersistenceCustomComparer;
            Context = context;
            Variables = variables;
        }

        private void DefaultDecimalSeparatorInit() //默认十进制，分隔符初始化为"."
        {
            numberRegexPattern = string.Format(numberRegexOrigPattern, @"\.", string.Empty);

            CultureInfoForNumberParsing.NumberFormat.NumberDecimalSeparator = ".";
        }

        /// <summary>所有构造函数的初始化方法</summary>
        protected virtual void Init() { }

        #endregion

        #region 主要的求值方法(Expressions and scripts)

        #region 脚本

        /// <summary>
        /// 求值脚本(用分号分隔的多个表达式)<para/> 支持一些条件、循环等c#代码流管理关键字
        /// </summary>
        /// <typeparam name="T">要对表达式的结果进行强制转换的类型</typeparam>
        /// <param name="script">求值的脚本</param>
        /// <returns>最后一次求值表达式的结果</returns>
        public T ScriptEvaluate<T>(string script) => (T)ScriptEvaluate(script);

        /// <summary>解释脚本(用分号分隔的多个表达式),支持一些条件、循环等c#代码流管理关键字</summary>
        /// <param name="script">需要求值的脚本字符串</param>
        /// <returns>最后一次求值表达式的结果</returns>
        public object ScriptEvaluate(string script)
        {
            OnEvaluatScript?.Invoke(this, _evaluationEventArg.Refresh(script, this));
            
            script = _evaluationEventArg.Expression;

            var isReturn = false;
            var isBreak = false;
            var isContinue = false;

            var result = ScriptEvaluate(script, ref isReturn, ref isBreak, ref isContinue);

            if (isBreak) throw new SyntaxErrorException("无效关键字:[break]");
            if (isContinue) throw new SyntaxErrorException("无效关键字:[continue]");
            return result;
        }

        private object ScriptEvaluate(string script, ref bool valueReturned, ref bool breakCalled, ref bool continueCalled)
        {
            object lastResult = null;
            var isReturn = valueReturned;
            var isBreak = breakCalled;
            var isContinue = continueCalled;
            var startOfExpression = 0;
            var ifBlockEvaluatedState = IfBlockEvaluatedState.NoBlockEvaluated;
            var tryBlockEvaluatedState = TryBlockEvaluatedState.NoBlockEvaluated;
            var ifElseStatementsList = new List<List<string>>();
            var tryStatementsList = new List<List<string>>();

            script = script.TrimEnd(); //修剪末尾的空格

            var result = (object)null;

            var i = 0;

            while (!isReturn && !isBreak && !isContinue && i < script.Length)
            {
                var blockKeywordsWithoutParenthesesBeginningMatch = blockKeywordsWithoutParenthesesBeginningRegex.Match(script.Substring(i));
                var blockKeywordsBeginMatch = blockKeywordsBeginningRegex.Match(script.Substring(i));
                var str = script.Substring(startOfExpression, i - startOfExpression);
                
                
                if (string.IsNullOrWhiteSpace(str)&& (blockKeywordsBeginMatch.Success || blockKeywordsWithoutParenthesesBeginningMatch.Success))
                {
                    i += blockKeywordsBeginMatch.Success ? blockKeywordsBeginMatch.Length : blockKeywordsWithoutParenthesesBeginningMatch.Length;
                    var keyword = blockKeywordsBeginMatch.Success ? 
                        blockKeywordsBeginMatch.Groups["keyword"].Value.Replace(" ", "").Replace("\t", "") 
                        : (blockKeywordsWithoutParenthesesBeginningMatch?.Groups["keyword"].Value ?? string.Empty);
                    var keywordAttributes = blockKeywordsBeginMatch.Success ? GetExpressionsParenthesized(script, ref i, true, ";") : null;

                    if (blockKeywordsBeginMatch.Success) i++;

                    var blockBeginningMatch = blockBeginningRegex.Match(script.Substring(i));

                    var subScript = string.Empty;

                    if (blockBeginningMatch.Success)
                    {
                        i += blockBeginningMatch.Length;

                        subScript = GetScriptBetweenCurlyBrackets(script, ref i);

                        i++;
                    }
                    else
                    {
                        var continueExpressionParsing = true;
                        startOfExpression = i;

                        while (i < script.Length && continueExpressionParsing)
                        {
                            if (TryParseStringAndParenthisAndCurlyBrackets(ref i)) { }
                            else if (script.Length - i > 2 && script.Substring(i, 3).Equals("';'"))
                            {
                                i += 2;
                            }
                            else if (script[i] == ';')
                            {
                                subScript = script.Substring(startOfExpression, i + 1 - startOfExpression);
                                continueExpressionParsing = false;
                            }

                            i++;
                        }
                        
                        if (string.IsNullOrWhiteSpace(subScript)) throw new SyntaxErrorException($"[{keyword}] 语句后无指令");
                    }

                    if (keyword.Equals("elseif", StringComparisonForCasing))
                    {
                        if (ifBlockEvaluatedState == IfBlockEvaluatedState.NoBlockEvaluated)
                        {
                            throw new SyntaxErrorException("[else if] 没有对应的 [if]");
                        }
                        else
                        {
                            ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                            ifBlockEvaluatedState = IfBlockEvaluatedState.ElseIf;
                        }
                    }
                    else if (keyword.Equals("else", StringComparisonForCasing))
                    {
                        if (ifBlockEvaluatedState == IfBlockEvaluatedState.NoBlockEvaluated)
                        {
                            throw new SyntaxErrorException("[else] 没有对应的 [if]");
                        }
                        else
                        {
                            ifElseStatementsList.Add(new List<string> { "true", subScript });
                            ifBlockEvaluatedState = IfBlockEvaluatedState.NoBlockEvaluated;
                        }
                    }
                    else if (keyword.Equals("catch", StringComparisonForCasing))
                    {
                        if (tryBlockEvaluatedState == TryBlockEvaluatedState.NoBlockEvaluated)
                            throw new SyntaxErrorException(" [catch] 没有对应的  [try] ");

                        tryStatementsList.Add(new List<string> { "catch", keywordAttributes.Count > 0 ? keywordAttributes[0] : null, subScript });
                        tryBlockEvaluatedState = TryBlockEvaluatedState.Catch;
                    }
                    else if (keyword.Equals("finally", StringComparisonForCasing))
                    {
                        if (tryBlockEvaluatedState == TryBlockEvaluatedState.NoBlockEvaluated)
                            throw new SyntaxErrorException(" [finally] 没有对应的  [try]");

                        tryStatementsList.Add(new List<string> { "finally", subScript });
                        tryBlockEvaluatedState = TryBlockEvaluatedState.NoBlockEvaluated;
                    }
                    else
                    {
                        ExecuteBlocksStacks();

                        if (keyword.Equals("if", StringComparisonForCasing))
                        {
                            ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                            ifBlockEvaluatedState = IfBlockEvaluatedState.If;
                            tryBlockEvaluatedState = TryBlockEvaluatedState.NoBlockEvaluated;
                        }
                        else if (keyword.Equals("try", StringComparisonForCasing))
                        {
                            tryStatementsList.Add(new List<string> { subScript });
                            ifBlockEvaluatedState = IfBlockEvaluatedState.NoBlockEvaluated;
                            tryBlockEvaluatedState = TryBlockEvaluatedState.Try;
                        }
                        else if (keyword.Equals("do", StringComparisonForCasing))
                        {
                            if ((blockKeywordsBeginMatch = blockKeywordsBeginningRegex.Match(script.Substring(i))).Success && blockKeywordsBeginMatch.Groups["keyword"].Value.Equals("while", StringComparisonForCasing))
                            {
                                i += blockKeywordsBeginMatch.Length;
                                keywordAttributes = GetExpressionsParenthesized(script, ref i, true, ";");

                                i++;

                                Match nextIsEndOfExpressionMatch;

                                if ((nextIsEndOfExpressionMatch = nextIsEndOfExpressionRegex.Match(script.Substring(i))).Success)
                                {
                                    i += nextIsEndOfExpressionMatch.Length;

                                    do
                                    {
                                        lastResult = ScriptEvaluate(subScript, ref isReturn, ref isBreak, ref isContinue);

                                        if (isBreak)
                                        {
                                            isBreak = false;
                                            break;
                                        }

                                        if (isContinue)
                                        {
                                            isContinue = false;
                                        }
                                    } while (!isReturn && (bool)ManageJumpStatementsOrExpressionEval(keywordAttributes[0]));
                                }
                                else
                                {
                                    throw new SyntaxErrorException("A [;] character is missing. (After the do while condition)");
                                }
                            }
                            else
                            {
                                throw new SyntaxErrorException("No [while] keyword after the [do] keyword and block");
                            }
                        }
                        else if (keyword.Equals("while", StringComparisonForCasing))
                        {
                            while (!isReturn && (bool)ManageJumpStatementsOrExpressionEval(keywordAttributes[0]))
                            {
                                lastResult = ScriptEvaluate(subScript, ref isReturn, ref isBreak, ref isContinue);

                                if (isBreak)
                                {
                                    isBreak = false;
                                    break;
                                }

                                if (isContinue)
                                {
                                    isContinue = false;
                                }
                            }
                        }
                        else if (keyword.Equals("for", StringComparisonForCasing))
                        {
                            void forAction(int index)
                            {
                                if (keywordAttributes.Count > index && !keywordAttributes[index].Trim().Equals(string.Empty))
                                    ManageJumpStatementsOrExpressionEval(keywordAttributes[index]);
                            }

                            for (forAction(0); !isReturn && (bool)ManageJumpStatementsOrExpressionEval(keywordAttributes[1]); forAction(2))
                            {
                                lastResult = ScriptEvaluate(subScript, ref isReturn, ref isBreak, ref isContinue);

                                if (isBreak)
                                {
                                    isBreak = false;
                                    break;
                                }

                                if (isContinue)
                                {
                                    isContinue = false;
                                }
                            }
                        }
                        else if (keyword.Equals("foreach", StringComparisonForCasing))
                        {
                            var foreachParenthisEvaluationMatch = foreachParenThisEvaluationRegex.Match(keywordAttributes[0]);

                            if (!foreachParenthisEvaluationMatch.Success)
                            {
                                throw new SyntaxErrorException("wrong foreach syntax");
                            }
                            else if (!foreachParenthisEvaluationMatch.Groups["in"].Value.Equals("in", StringComparisonForCasing))
                            {
                                throw new SyntaxErrorException("no [in] keyword found in foreach");
                            }
                            else
                            {
                                foreach (var foreachValue in (IEnumerable)Evaluate(foreachParenthisEvaluationMatch.Groups["collection"].Value))
                                {
                                    Variables[foreachParenthisEvaluationMatch.Groups["variableName"].Value] = foreachValue;

                                    lastResult = ScriptEvaluate(subScript, ref isReturn, ref isBreak, ref isContinue);

                                    if (isBreak || isReturn)
                                    {
                                        isBreak = false;
                                        break;
                                    }

                                    if (isContinue)
                                    {
                                        isContinue = false;
                                    }
                                }
                            }
                        }
                    }

                    startOfExpression = i;
                }
                else
                {
                    ExecuteBlocksStacks();

                    var executed = false;

                    if (TryParseStringAndParenthisAndCurlyBrackets(ref i)) { }
                    else if (script.Length - i > 2 && script.Substring(i, 3).Equals("';'"))
                    {
                        i += 2;
                    }
                    else if (script[i] == ';')
                    {
                        lastResult = ScriptExpressionEvaluate(ref i);
                        executed = true;
                    }

                    if (!OptionNeedScriptEndSemicolon && i == script.Length - 1 && !executed)
                    {
                        i++;
                        lastResult = ScriptExpressionEvaluate(ref i);
                        startOfExpression--;
                    }

                    ifBlockEvaluatedState = IfBlockEvaluatedState.NoBlockEvaluated;
                    tryBlockEvaluatedState = TryBlockEvaluatedState.NoBlockEvaluated;

                    if (OptionNeedScriptEndSemicolon || i < script.Length) i++;
                }
            }

            if (!script.Substring(startOfExpression).Trim().Equals(string.Empty) && !isReturn && !isBreak && !isContinue && OptionNeedScriptEndSemicolon)
                throw new SyntaxErrorException($"{script} 中缺少 [;] 字符");

            ExecuteBlocksStacks();

            valueReturned = isReturn;
            breakCalled = isBreak;
            continueCalled = isContinue;

            if (isReturn || NoReturnKeywordMode == EOnNoReturnKeywordMode.ReturnLastResult)
                return lastResult;
            if (NoReturnKeywordMode == EOnNoReturnKeywordMode.ReturnNull)
                return null;
            throw new SyntaxErrorException("没有找到 [return] 关键字");

            void ExecuteBlocksStacks()
            {
                ExecuteTryList();
                ExecuteIfList();
            }

            void ExecuteTryList()
            {
                if (tryStatementsList.Count > 0)
                {
                    if (tryStatementsList.Count == 1)
                    {
                        throw new SyntaxErrorException("try语句至少需要一个catch或一个finally语句。");
                    }

                    try
                    {
                        lastResult = ScriptEvaluate(tryStatementsList[0][0], ref isReturn, ref isBreak, ref isContinue);
                    }
                    catch (Exception exception)
                    {
                        var atLeasOneCatch = false;

                        foreach (var catchStatement in tryStatementsList.Skip(1).TakeWhile(e => e[0].Equals("catch")))
                        {
                            if (catchStatement[1] != null)
                            {
                                var exceptionVariable = catchStatement[1].Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                var exceptionName = exceptionVariable[0];

                                if (exceptionVariable.Length >= 2)
                                {
                                    if (!((ClassOrEnumType)Evaluate(exceptionVariable[0])).Type.IsAssignableFrom(exception.GetType()))
                                        continue;

                                    exceptionName = exceptionVariable[1];
                                }

                                Variables[exceptionName] = exception;
                            }

                            lastResult = ScriptEvaluate(catchStatement[2], ref isReturn, ref isBreak, ref isContinue);
                            atLeasOneCatch = true;
                            break;
                        }

                        if (!atLeasOneCatch)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        if (tryStatementsList.Last()[0].Equals("finally"))
                        {
                            lastResult = ScriptEvaluate(tryStatementsList.Last()[1], ref isReturn, ref isBreak, ref isContinue);
                        }
                    }

                    tryStatementsList.Clear();
                }
            }

            void ExecuteIfList()
            {
                if (ifElseStatementsList.Count <= 0) return;
                var ifScript = ifElseStatementsList.Find(statement => (bool)ManageJumpStatementsOrExpressionEval(statement[0]))?[1];

                if (!string.IsNullOrEmpty(ifScript))
                    lastResult = ScriptEvaluate(ifScript, ref isReturn, ref isBreak, ref isContinue);

                ifElseStatementsList.Clear();
            }

            bool TryParseStringAndParenthisAndCurlyBrackets(ref int index)
            {
                var parsed = true;
                var internalStringMatch = stringBeginningRegex.Match(script.Substring(index));

                if (internalStringMatch.Success)
                {
                    var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(script.Substring(index + internalStringMatch.Length), internalStringMatch);
                    index += innerString.Length - 1;
                }
                else if (script[index] == '(')
                {
                    index++;
                    GetExpressionsParenthesized(script, ref index, false);
                }
                else if (script[index] == '{')
                {
                    index++;
                    GetScriptBetweenCurlyBrackets(script, ref index);
                }
                else
                {
                    var charMatch = internalCharRegex.Match(script.Substring(index));

                    if (charMatch.Success)
                        index += charMatch.Length - 1;

                    parsed = false;
                }

                return parsed;
            }

            //依次解释指定段落的脚本
            object ScriptExpressionEvaluate(ref int index)
            {
                var expression = script.Substring(startOfExpression, index - startOfExpression);

                startOfExpression = index + 1;

                return ManageJumpStatementsOrExpressionEval(expression);
            }

            //管理跳转语句或表达式求值
            object ManageJumpStatementsOrExpressionEval(string expression)
            {
                expression = expression.Trim(); //修剪空字符串

                if (expression.Equals("break", StringComparisonForCasing))
                {
                    isBreak = true;
                    return result;
                }

                if (expression.Equals("continue", StringComparisonForCasing))
                {
                    isContinue = true;
                    return result;
                }

                if (expression.StartsWith("throw ", StringComparisonForCasing))
                {
                    if (Evaluate(expression.Remove(0, 6)) is Exception exception) //移除throw 关键字
                    {
                        throw exception; //ExceptionDispatchInfo.Capture(exception).Throw();
                    }
                    else
                    {
                        throw new SyntaxErrorException("[throw]后 缺少[Exception]实例");
                    }
                }

                expression = returnKeywordRegex.Replace(expression, match =>
                {
                    if (OptionCaseSensitiveEvaluationActive && !match.Value.StartsWith("return"))
                        return match.Value;

                    isReturn = true;
                    return match.Value.Contains("(") ? "(" : string.Empty;
                });

                return Evaluate(expression);
            }
        }

        #endregion

        #region 表达式

        /// <summary>解释指定的数学或伪C#表达式</summary>
        /// <typeparam name="T">将表达式的结果转换为哪种类型</typeparam>
        /// <param name="expression">要计算的数学或伪C#表达式</param>
        /// <returns>如果语法以指定的类型正确转换，则运算的结果</returns>
        public T Evaluate<T>(string expression) => (T)Evaluate(expression);

        private List<ParsingMethodDelegate> parsingMethods;


        /// <summary>解释一系列方法</summary>
        private List<ParsingMethodDelegate> ParsingMethods =>
            parsingMethods ??= new List<ParsingMethodDelegate>
            {
                EvaluateCast,
                EvaluateNumber,
                EvaluateInstanceCreationWithNewKeyword,
                EvaluateVarOrFunc,
                EvaluateOperators,
                EvaluateChar,
                EvaluateParenthis,
                EvaluateIndexing,
                EvaluateString,
                EvaluateTernaryConditionalOperator,
            };


        /// <summary>计算指定的数学表达式或伪c#表达式</summary>
        /// <param name="expression">要计算的数学表达式或伪c#表达式</param>
        /// <returns>如果语法正确，返回操作的结果</returns>
        public object Evaluate(string expression)
        {
            expression = expression.Trim();

            var stack = new Stack<object>();

            evaluationStackCount++;

            try
            {
                //生成一个事件信息
                var EventArg = new ExpressionEvaluationEventArg(expression, this);

                //触发事件
                ExpressionEvaluating?.Invoke(this, EventArg);

                //获取新的表达式
                expression = EventArg.Expression;

                object result;
                if (EventArg.HasValue)
                {
                    result = EventArg.Value;
                }
                else
                {
                    //如果是lambda表达式，则入栈
                    if (GetLambdaExpression(expression, stack))
                        return stack.Pop(); //然后出栈


                    for (var i = 0; i < expression.Length; i++)
                    {
                        if (!ParsingMethods.Any(parsingMethod => parsingMethod(expression, stack, ref i)))
                        {
                            var s = expression.Substring(i, 1);

                            if (!s.Trim().Equals(string.Empty))
                                throw new SyntaxErrorException($"无效的字符 [{(int)s[0]}:{s}]");
                        }
                    }

                    result = ProcessStack(stack);

                    EventArg = new ExpressionEvaluationEventArg(expression, this, result);

                    ExpressionEvaluated?.Invoke(this, EventArg);

                    if (EventArg.HasValue)
                        result = EventArg.Value;
                }

                return result;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                var exceptionToThrow = exception.InnerException;

                while (exceptionToThrow is TargetInvocationException && exceptionToThrow.InnerException != null)
                    exceptionToThrow = exceptionToThrow.InnerException;

                throw exceptionToThrow;

                // ExceptionDispatchInfo.Capture(exceptionToThrow).Throw();
                // Will not go here but need to return something to avoid compilation errors.
            }
            finally
            {
                evaluationStackCount--;
            }
        }

        #endregion

        #endregion

        #region 子部分求值方法(protected virtual)

        /// <summary>解析强转</summary>
        private bool EvaluateCast(string expression, Stack<object> stack, ref int i)
        {
            var castMatch = Regex.Match(expression.Substring(i), CastRegexPattern, regexOption);

            if (castMatch.Success)
            {
                var typeName = castMatch.Groups["typeName"].Value;

                var typeIndex = 0;
                var type = EvaluateType(typeName, ref typeIndex);

                if (type != null)
                {
                    i += castMatch.Length - 1;
                    stack.Push(type);
                    stack.Push(ExpressionOperator.Cast);
                    return true;
                }
            }

            return false;
        }

        /// <summary>解析数字</summary>
        /// <remarks>可以识别<code>int a=666_666</code>这种分隔符</remarks>
        /// 所有的数字会被转换为double类型或者int类型
        private bool EvaluateNumber(string expression, Stack<object> stack, ref int i)
        {
            var restOfExpression = expression.Substring(i);
            var numberMatch = Regex.Match(restOfExpression, numberRegexPattern, RegexOptions.IgnoreCase);
            var otherBaseMatch = otherBasesNumberRegex.Match(restOfExpression);
            //匹配进制类型成功 && ( 前面无符号 || 栈为空 || 栈顶为运算符 )
            if (otherBaseMatch.Success && (!otherBaseMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                i += otherBaseMatch.Length;
                i--;

                var baseValue = otherBaseMatch.Groups["type"].Value.Equals("b") ? 2 : 16;

                if (otherBaseMatch.Groups["sign"].Success)
                {
                    var value = otherBaseMatch.Groups["value"].Value.Replace("_", "").Substring(2);
                    stack.Push(otherBaseMatch.Groups["sign"].Value.Equals("-") ? -Convert.ToInt32(value, baseValue) : Convert.ToInt32(value, baseValue));
                }
                else
                {
                    stack.Push(Convert.ToInt32(otherBaseMatch.Value.Replace("_", "").Substring(2), baseValue));
                }

                return true;
            }

            //匹配成功 && ( 前面无符号 || 栈为空 || 栈顶为运算符 )
            if (numberMatch.Success && (!numberMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                i += numberMatch.Length;
                i--;

                if (numberMatch.Groups["type"].Success)
                {
                    var type = numberMatch.Groups["type"].Value;
                    var numberNoType = numberMatch.Value.Replace(type, string.Empty).Replace("_", "");

                    if (numberSuffixToParse.TryGetValue(type, out var parseFunc))
                    {
                        stack.Push(parseFunc(numberNoType, CultureInfoForNumberParsing));
                    }
                }
                else if (OptionForceIntegerNumbersEvaluationsAsDoubleByDefault || numberMatch.Groups["hasdecimal"].Success)
                {
                    stack.Push(double.Parse(numberMatch.Value.Replace("_", ""), NumberStyles.Any, CultureInfoForNumberParsing));
                }
                else
                {
                    stack.Push(int.Parse(numberMatch.Value.Replace("_", ""), NumberStyles.Any, CultureInfoForNumberParsing));
                }

                return true;
            }

            //匹配失败
            return false;
        }

        /// <summary>解析实例化</summary>
        private bool EvaluateInstanceCreationWithNewKeyword(string expression, Stack<object> stack, ref int i)
        {
            if (!OptionNewKeywordEvaluationActive) return false;

            var instanceCreationMatch = instanceCreationWithNewKeywordRegex.Match(expression.Substring(i));

            if (instanceCreationMatch.Success && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                void InitSimpleObjet(object element, List<string> initArgs)
                {
                    var variable = "V" + Guid.NewGuid().ToString().Replace("-", "");

                    Variables[variable] = element;

                    initArgs.ForEach(subExpr =>
                    {
                        if (subExpr.Contains("="))
                        {
                            var trimmedSubExpr = subExpr.TrimStart();

                            Evaluate($"{variable}{(trimmedSubExpr.StartsWith("[") ? string.Empty : ".")}{trimmedSubExpr}");
                        }
                        else
                        {
                            throw new SyntaxErrorException($" [{subExpr}]中的 '=' 字符缺失,它在对象初始化器中。它必须包含一个。");
                        }
                    });

                    Variables.Remove(variable);
                }

                i += instanceCreationMatch.Length;

                if (instanceCreationMatch.Groups["isAnonymous"].Success)
                {
                    object element = null;

                    var initArgs = GetExpressionsParenthesized(expression, ref i, true, OptionInitializersSeparator, "{", "}");

                    InitSimpleObjet(element, initArgs);

                    stack.Push(element);
                }
                else
                {
                    var completeName = instanceCreationMatch.Groups["name"].Value;
                    var genericTypes = instanceCreationMatch.Groups["isgeneric"].Value;

                    var typeIndex = 0;
                    var type = EvaluateType(completeName + genericTypes, ref typeIndex);

                    if (type == null || (typeIndex > 0 && typeIndex < completeName.Length))
                        throw new SyntaxErrorException($"Type or class {completeName}{genericTypes} is unknown");

                    void Init(object element, List<string> initArgs)
                    {
                        if (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type)) // && !typeof(ExpandoObject).IsAssignableFrom(type)
                        {
                            var methodInfo = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

                            initArgs.ForEach(subExpr => methodInfo?.Invoke(element, new object[] { Evaluate(subExpr) }));
                        }
                        else if (typeof(IDictionary).IsAssignableFrom(type) && initArgs.All(subExpr => subExpr.TrimStart().StartsWith("{"))) // && !typeof(ExpandoObject).IsAssignableFrom(type)
                        {
                            initArgs.ForEach(subExpr =>
                            {
                                var subIndex = subExpr.IndexOf("{", StringComparison.Ordinal) + 1;

                                var subArgs = GetExpressionsParenthesized(subExpr, ref subIndex, true, OptionInitializersSeparator, "{", "}");

                                if (subArgs.Count == 2)
                                {
                                    var indexedObject = element;
                                    var index = Evaluate(subArgs[0]);
                                    // indexedObject[index] = Evaluate(subArgs[1]);
                                }
                                else
                                {
                                    throw new SyntaxErrorException($"的初始化中的参数数目错误 [{subExpr}]");
                                }
                            });
                        }
                        else
                        {
                            InitSimpleObjet(element, initArgs);
                        }
                    }

                    if (instanceCreationMatch.Groups["isfunction"].Success)
                    {
                        var constructorArgs = GetExpressionsParenthesized(expression, ref i, true, OptionFunctionArgumentsSeparator);
                        i++;

                        var cArgs = constructorArgs.ConvertAll(Evaluate);

                        var element = Activator.CreateInstance(type, cArgs.ToArray());

                        var blockBeginningMatch = blockBeginningRegex.Match(expression.Substring(i));

                        if (blockBeginningMatch.Success)
                        {
                            i += blockBeginningMatch.Length;

                            var initArgs = GetExpressionsParenthesized(expression, ref i, true, OptionInitializersSeparator, "{", "}");

                            Init(element, initArgs);
                        }
                        else
                        {
                            i--;
                        }

                        stack.Push(element);
                    }
                    else if (instanceCreationMatch.Groups["isInit"].Success)
                    {
                        var element = Activator.CreateInstance(type, new object[0]);

                        var initArgs = GetExpressionsParenthesized(expression, ref i, true, OptionInitializersSeparator, "{", "}");

                        Init(element, initArgs);

                        stack.Push(element);
                    }
                    else if (instanceCreationMatch.Groups["isArray"].Success)
                    {
                        var arrayArgs = GetExpressionsParenthesized(expression, ref i, true, OptionInitializersSeparator, "[", "]");
                        i++;
                        Array array = null;

                        if (arrayArgs.Count > 0)
                        {
                            array = Array.CreateInstance(type, arrayArgs.ConvertAll(subExpression => Convert.ToInt32(Evaluate(subExpression))).ToArray());
                        }

                        var initInNewBeginningMatch = initInNewBeginningRegex.Match(expression.Substring(i));

                        if (initInNewBeginningMatch.Success)
                        {
                            i += initInNewBeginningMatch.Length;

                            var arrayElements = GetExpressionsParenthesized(expression, ref i, true, OptionInitializersSeparator, "{", "}");

                            if (array == null)
                                array = Array.CreateInstance(type, arrayElements.Count);

                            Array.Copy(arrayElements.ConvertAll(Evaluate).ToArray(), array, arrayElements.Count);
                        }

                        stack.Push(array);
                    }
                    else
                    {
                        throw new SyntaxErrorException($"A new expression requires that type be followed by (), [] or {{}}(Check : {instanceCreationMatch.Value})");
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>解析变量或函数</summary>
        private bool EvaluateVarOrFunc(string expression, Stack<object> stack, ref int i)
        {
            var varFuncMatch = varOrFunctionRegEx.Match(expression.Substring(i));

            //有var 关键字 但 没有赋值运算符
            if (varFuncMatch.Groups["varKeyword"].Success && !varFuncMatch.Groups["assignationOperator"].Success)
                throw new SyntaxErrorException($"隐式变量未初始化! [var {varFuncMatch.Groups["name"].Value}]");

            if (varFuncMatch.Success //匹配成功
                && (!varFuncMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator) //前缀没有符号 且栈为空 或者栈顶为运算符 
                && !operatorsDictionary.ContainsKey(varFuncMatch.Value.Trim()) //且不是已注册的运算符
                && (!operatorsDictionary.ContainsKey(varFuncMatch.Groups["name"].Value) || varFuncMatch.Groups["inObject"].Success)) //且不是已注册的运算符 或者在对象中
            {
                var varFuncName = varFuncMatch.Groups["name"].Value;
                var genericsTypes = varFuncMatch.Groups["isgeneric"].Value;
                var inObject = varFuncMatch.Groups["inObject"].Success;

                i += varFuncMatch.Length;

                //是方法的情况
                if (varFuncMatch.Groups["isfunction"].Success)
                {
                    //找到括号包裹的部分(参数)
                    var funcArgs = GetExpressionsParenthesized(expression, ref i, true, OptionFunctionArgumentsSeparator);

                    //如果是对象的方法,或者是this的方法
                    if (inObject || (Context?.GetType().GetMethods(InstanceBindingFlag).Any(methodInfo => methodInfo.Name.Equals(varFuncName, StringComparisonForCasing)) ?? false))
                    {
                        if (inObject && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
                            throw new SyntaxErrorException($"[{varFuncMatch.Value})] 必须紧跟一个对象"); //只有点

                        var obj = inObject ? stack.Pop() : Context;
                        var keepObj = obj;
                        var objType = obj?.GetType();
                        var inferedGenericsTypes = obj?.GetType().GenericTypeArguments;
                        ValueTypeNestingTrace valueTypeNestingTrace = null;

                        //安全拦截
                        if (obj != null && TypesToBlock.Contains(obj.GetType())) throw new SecurityException($"{obj.GetType().FullName} : 类型获取受限");
                        if (obj is Type staticType && TypesToBlock.Contains(staticType)) throw new SecurityException($"{staticType.FullName} : 类型获取受限");
                        if (obj is ClassOrEnumType classOrType && TypesToBlock.Contains(classOrType.Type)) throw new SecurityException($"{classOrType.Type} : 类型获取受限");

                        try
                        {
                            if (obj is NullConditionalNullValue)
                            {
                                stack.Push(obj);
                            }
                            else if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                            {
                                stack.Push(new NullConditionalNullValue());
                            }
                            else if (obj is BubbleExceptionContainer)
                            {
                                stack.Push(obj);
                                return true;
                            }
                            else
                            {
                                var functionPreEvaluationEventArg = new FunctionPreEvaluationEventArg(varFuncName, funcArgs, this, obj, genericsTypes, GetConcreteTypes);

                                PreEvaluateFunction?.Invoke(this, functionPreEvaluationEventArg);

                                if (functionPreEvaluationEventArg.CancelEvaluation)
                                    throw new SyntaxErrorException($"[{objType}] 对象没有名为 [{varFuncName}] 的方法.");

                                if (functionPreEvaluationEventArg.FunctionReturnedValue)
                                {
                                    stack.Push(functionPreEvaluationEventArg.Value);
                                }
                                else
                                {
                                    var argIndex = 0;
                                    var argsWithKeywords = new List<ArgKeywordsEncaps>();

                                    var oArgs = funcArgs.ConvertAll(arg =>
                                    {
                                        var functionArgKeywordsMatch = functionArgKeywordsRegex.Match(arg);
                                        object argValue;

                                        if (functionArgKeywordsMatch.Success)
                                        {
                                            var argKeywordEncaps = new ArgKeywordsEncaps { Index = argIndex, Keyword = functionArgKeywordsMatch.Groups["keyword"].Value, VariableName = functionArgKeywordsMatch.Groups["varName"].Value };

                                            argsWithKeywords.Add(argKeywordEncaps);

                                            if (functionArgKeywordsMatch.Groups["typeName"].Success)
                                            {
                                                var fixedType = ((ClassOrEnumType)Evaluate(functionArgKeywordsMatch.Groups["typeName"].Value)).Type;

                                                variables[argKeywordEncaps.VariableName] = new StronglyTypedVariable { Type = fixedType, Value = GetDefaultValueOfType(fixedType) };
                                            }
                                            else if (!variables.ContainsKey(argKeywordEncaps.VariableName))
                                            {
                                                variables[argKeywordEncaps.VariableName] = null;
                                            }

                                            argValue = Evaluate(functionArgKeywordsMatch.Groups["toEval"].Value);
                                        }
                                        else
                                        {
                                            argValue = Evaluate(arg);
                                        }

                                        argIndex++;
                                        return argValue;
                                    });

                                    var flag = DetermineInstanceOrStatic(ref objType, ref obj, ref valueTypeNestingTrace);

                                    if (!OptionStaticMethodsCallActive && (flag & BindingFlags.Static) != 0)
                                        throw new SyntaxErrorException($"[{objType}] 找不到函数: [{varFuncName}].");
                                    if (!OptionInstanceMethodsCallActive && (flag & BindingFlags.Instance) != 0)
                                        throw new SyntaxErrorException($"[{objType}] 找不到函数: \"{varFuncName}\".");

                                    // 寻找标准实例或公共方法
                                    var methodInfo = GetRealMethod(ref objType, ref obj, varFuncName, flag, oArgs, genericsTypes, inferedGenericsTypes, argsWithKeywords);

                                    // 如果找不到，检查obj是否是expandoObject或类似对象
                                    if (obj is IDictionary<string, object> dictionaryObject && (dictionaryObject[varFuncName] is InternalDelegate || dictionaryObject[varFuncName] is Delegate)) //obj is IDynamicMetaObjectProvider &&
                                    {
                                        if (dictionaryObject[varFuncName] is InternalDelegate internalDelegate)
                                            stack.Push(internalDelegate(oArgs.ToArray()));
                                        else if (dictionaryObject[varFuncName] is Delegate del)
                                            stack.Push(del.DynamicInvoke(oArgs.ToArray()));
                                    }
                                    else if (objType.GetProperty(varFuncName, InstanceBindingFlag) is PropertyInfo instancePropertyInfo && (instancePropertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)) || instancePropertyInfo.PropertyType == typeof(Delegate)) && instancePropertyInfo.GetValue(obj) is Delegate del)
                                    {
                                        stack.Push(del.DynamicInvoke(oArgs.ToArray()));
                                    }
                                    else
                                    {
                                        var isExtention = false;

                                        // if not found try to Find extension methods.
                                        if (methodInfo == null && obj != null)
                                        {
                                            oArgs.Insert(0, obj);
                                            objType = obj.GetType();

                                            object extentionObj = null;
                                            for (var e = 0; e < StaticTypesForExtensionsMethods.Count && methodInfo == null; e++)
                                            {
                                                var type = StaticTypesForExtensionsMethods[e];
                                                methodInfo = GetRealMethod(ref type, ref extentionObj, varFuncName, StaticBindingFlag, oArgs, genericsTypes, inferedGenericsTypes, argsWithKeywords, true);
                                                isExtention = methodInfo != null;
                                            }
                                        }

                                        if (methodInfo != null)
                                        {
                                            var argsArray = oArgs.ToArray();
                                            stack.Push(methodInfo.Invoke(isExtention ? null : obj, argsArray));
                                            argsWithKeywords.FindAll(argWithKeyword => argWithKeyword.Keyword.Equals("out", StringComparisonForCasing) || argWithKeyword.Keyword.Equals("ref", StringComparisonForCasing)).ForEach(outOrRefArg => AssignVariable(outOrRefArg.VariableName, argsArray[outOrRefArg.Index + (isExtention ? 1 : 0)]));
                                        }
                                        else if (objType.GetProperty(varFuncName, StaticBindingFlag) is PropertyInfo staticPropertyInfo && (staticPropertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)) || staticPropertyInfo.PropertyType == typeof(Delegate)) && staticPropertyInfo.GetValue(obj) is Delegate del2)
                                        {
                                            stack.Push(del2.DynamicInvoke(oArgs.ToArray()));
                                        }
                                        else
                                        {
                                            var functionEvaluationEventArg = new FunctionEvaluationEventArg(varFuncName, funcArgs, this, obj ?? keepObj, genericsTypes, GetConcreteTypes);

                                            EvaluateFunction?.Invoke(this, functionEvaluationEventArg);

                                            if (functionEvaluationEventArg.FunctionReturnedValue)
                                            {
                                                stack.Push(functionEvaluationEventArg.Value);
                                            }
                                            else
                                            {
                                                if (OptionDetectExtensionMethodsOverloadsOnExtensionMethodNotFound)
                                                {
                                                    var query = from type in StaticTypesForExtensionsMethods
                                                        where !type.IsGenericType && type.IsSealed && !type.IsNested
                                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                                        where method.IsDefined(typeof(ExtensionAttribute), false)
                                                        where method.GetParameters()[0].ParameterType == objType // static extMethod(this outType, ...)
                                                        select method;

                                                    if (query.Any())
                                                    {
                                                        var fnArgsPrint = string.Join(",", funcArgs);
                                                        var fnOverloadsPrint = "";

                                                        foreach (var mi in query)
                                                        {
                                                            var parInfo = mi.GetParameters();
                                                            fnOverloadsPrint += string.Join(",", parInfo.Select(x => x.ParameterType.FullName ?? x.ParameterType.Name)) + "\n";
                                                        }

                                                        throw new SyntaxErrorException($"[{objType}] extension method \"{varFuncName}\" has no overload for arguments: {fnArgsPrint}. Candidates: {fnOverloadsPrint}");
                                                    }
                                                }

                                                throw new SyntaxErrorException($"[{objType}] 对象方法  [{varFuncName}] ");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (SecurityException) { throw; }
                        catch (SyntaxErrorException) { throw; }
                        catch (NullReferenceException nullException)
                        {
                            stack.Push(new BubbleExceptionContainer(nullException));

                            return true;
                        }
                        catch (Exception ex)
                        {
                            //Transport the exception in stack.
                            var nestedException = new SyntaxErrorException($"The call of the method \"{varFuncName}\" on type [{objType}] generate this error : {ex.InnerException?.Message ?? ex.Message}", ex);
                            stack.Push(new BubbleExceptionContainer(nestedException));
                            return true; //Signals an error to the parsing method array call                          
                        }
                    }
                    else
                    {
                        var functionPreEvaluationEventArg = new FunctionPreEvaluationEventArg(varFuncName, funcArgs, this, null, genericsTypes, GetConcreteTypes);

                        PreEvaluateFunction?.Invoke(this, functionPreEvaluationEventArg);

                        if (functionPreEvaluationEventArg.CancelEvaluation)
                        {
                            throw new SyntaxErrorException($"Function [{varFuncName}] unknown in expression : [{expression.Replace("\r", "").Replace("\n", "")}]");
                        }
                        else if (functionPreEvaluationEventArg.FunctionReturnedValue)
                        {
                            stack.Push(functionPreEvaluationEventArg.Value);
                        }
                        else if (DefaultFunctions(varFuncName, funcArgs, out var funcResult))
                        {
                            stack.Push(funcResult);
                        }
                        else if (Variables.TryGetValue(varFuncName, out var o) && o is InternalDelegate lambdaExpression)
                        {
                            stack.Push(lambdaExpression.Invoke(funcArgs.ConvertAll(Evaluate).ToArray()));
                        }
                        else if (Variables.TryGetValue(varFuncName, out o) && o is Delegate delegateVar)
                        {
                            stack.Push(delegateVar.DynamicInvoke(funcArgs.ConvertAll(Evaluate).ToArray()));
                        }
                        else if (Variables.TryGetValue(varFuncName, out o) && o is MethodsGroupEncaps methodsGroupEncaps)
                        {
                            var args = funcArgs.ConvertAll(Evaluate);
                            List<object> modifiedArgs = null;
                            MethodInfo methodInfo = null;

                            for (var m = 0; methodInfo == null && m < methodsGroupEncaps.MethodsGroup.Length; m++)
                            {
                                modifiedArgs = new List<object>(args);

                                methodInfo = TryToCastMethodParametersToMakeItCallable(methodsGroupEncaps.MethodsGroup[m], modifiedArgs, genericsTypes, new Type[0], methodsGroupEncaps.ContainerObject);
                            }

                            if (methodInfo != null)
                                stack.Push(methodInfo.Invoke(methodsGroupEncaps.ContainerObject, modifiedArgs?.ToArray()));
                        }
                        else
                        {
                            var functionEvaluationEventArg = new FunctionEvaluationEventArg(varFuncName, funcArgs, this, genericTypes: genericsTypes, evaluateGenericTypes: GetConcreteTypes);

                            EvaluateFunction?.Invoke(this, functionEvaluationEventArg);

                            if (functionEvaluationEventArg.FunctionReturnedValue)
                            {
                                stack.Push(functionEvaluationEventArg.Value);
                            }
                            else
                            {
                                throw new SyntaxErrorException($"Function [{varFuncName}] unknown in expression : [{expression.Replace("\r", "").Replace("\n", "")}]");
                            }
                        }
                    }
                }
                //是变量，对象的情况
                else
                {
                    if (inObject || Context?.GetType().GetProperties(InstanceBindingFlag).Any(propInfo => propInfo.Name.Equals(varFuncName, StringComparisonForCasing)) == true || Context?.GetType().GetFields(InstanceBindingFlag).Any(fieldInfo => fieldInfo.Name.Equals(varFuncName, StringComparisonForCasing)) == true)
                    {
                        if (inObject && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
                            throw new SyntaxErrorException($"[{varFuncMatch.Value}] 后面必须有一个对象");

                        var obj = inObject ? stack.Pop() : Context;
                        var keepObj = obj;
                        var objType = obj?.GetType();
                        ValueTypeNestingTrace valueTypeNestingTrace = null;

                        if (obj != null && TypesToBlock.Contains(obj.GetType()))
                            throw new SecurityException($"{obj.GetType().FullName} 获取阻塞");
                        if (obj is Type staticType && TypesToBlock.Contains(staticType))
                            throw new SecurityException($"{staticType.FullName} 获取阻塞");
                        if (obj is ClassOrEnumType classOrType && TypesToBlock.Contains(classOrType.Type))
                            throw new SecurityException($"{classOrType.Type} 获取阻塞");


                        try
                        {
                            if (obj is NullConditionalNullValue)
                            {
                                stack.Push(obj);
                            }
                            else if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                            {
                                stack.Push(new NullConditionalNullValue());
                            }
                            else if (obj is BubbleExceptionContainer)
                            {
                                stack.Push(obj);
                                return true;
                            }
                            else
                            {
                                var variablePreEvaluationEventArg = new VariablePreEvaluationEventArg(varFuncName, this, obj, genericsTypes, GetConcreteTypes);

                                PreEvaluateVariable?.Invoke(this, variablePreEvaluationEventArg);

                                if (variablePreEvaluationEventArg.CancelEvaluation)
                                {
                                    throw new SyntaxErrorException($"[{objType}] 对象没有名为 [{varFuncName}] 的公共属性或成员", new Exception("变量求值已取消"));
                                }

                                if (variablePreEvaluationEventArg.HasValue)
                                {
                                    stack.Push(variablePreEvaluationEventArg.Value);
                                }
                                else
                                {
                                    var flag = DetermineInstanceOrStatic(ref objType, ref obj, ref valueTypeNestingTrace);

                                    if (!OptionStaticPropertiesGetActive && (flag & BindingFlags.Static) != 0)
                                        throw new SyntaxErrorException($"[{objType}] 对象没有命名的公共属性或字段 [{varFuncName}] ");
                                    if (!OptionInstancePropertiesGetActive && (flag & BindingFlags.Instance) != 0)
                                        throw new SyntaxErrorException($"[{objType}] 对象没有命名的公共属性或字段 [{varFuncName}].");

                                    var isDynamic = (flag & BindingFlags.Instance) != 0 && obj is IDictionary<string, object>; //&& obj is IDynamicMetaObjectProvider
                                    var dictionaryObject = obj as IDictionary<string, object>;

                                    MemberInfo member = isDynamic ? null : objType?.GetProperty(varFuncName, flag);
                                    object varValue = null; //TODO:
                                    var assign = true;


                                    if (member == null && !isDynamic)
                                        member = objType?.GetField(varFuncName, flag);

                                    if (member == null && !isDynamic)
                                    {
                                        var methodsGroup = objType?.GetMember(varFuncName, flag).OfType<MethodInfo>().ToArray();

                                        if (methodsGroup.Length > 0)
                                            varValue = new MethodsGroupEncaps { ContainerObject = obj, MethodsGroup = methodsGroup };
                                    }

                                    var pushVarValue = true;

                                    if (isDynamic)
                                    {
                                        if (!varFuncMatch.Groups["assignationOperator"].Success || varFuncMatch.Groups["assignmentPrefix"].Success)
                                            varValue = dictionaryObject.TryGetValue(varFuncName, out var value) ? value : null;
                                        else
                                            pushVarValue = false;
                                    }

                                    var isVarValueSet = false;
                                    if (member == null && pushVarValue)
                                    {
                                        var variableEvaluationEventArg = new VariableEvaluationEventArg(varFuncName, this, obj ?? keepObj, genericsTypes, GetConcreteTypes);
                                        EvaluateVariable?.Invoke(this, variableEvaluationEventArg);

                                        if (variableEvaluationEventArg.HasValue)
                                        {
                                            varValue = variableEvaluationEventArg.Value;
                                            isVarValueSet = true;
                                        }
                                    }

                                    //Var去设置值 且 不是动态的 且 值为null 且 pushVarValue为true
                                    if (!isVarValueSet && !isDynamic && varValue == null && pushVarValue)
                                    {
                                        varValue = (member as PropertyInfo)?.GetValue(obj);
                                        varValue ??= (member as FieldInfo)?.GetValue(obj);

                                        //TODO: 这里有问题
                                        if (varValue?.GetType().IsPrimitive ?? false)
                                        {
                                            stack.Push(valueTypeNestingTrace = new ValueTypeNestingTrace { Container = valueTypeNestingTrace ?? obj, Member = member, Value = varValue });

                                            pushVarValue = false;
                                        }
                                    }

                                    if (pushVarValue) stack.Push(varValue);


                                    if (OptionPropertyOrFieldSetActive)
                                    {
                                        if (varFuncMatch.Groups["assignationOperator"].Success)
                                        {
                                            varValue = ManageKindOfAssignation(expression, ref i, varFuncMatch, () => varValue, stack);
                                        }
                                        else if (varFuncMatch.Groups["postfixOperator"].Success)
                                        {
                                            //不是++就是--;
                                            varValue = varFuncMatch.Groups["postfixOperator"].Value.Equals("++") ? (int)varValue + 1 : (int)varValue - 1;
                                        }
                                        else
                                        {
                                            assign = false;
                                        }

                                        if (assign)
                                        {
                                            if (isDynamic)
                                            {
                                                dictionaryObject[varFuncName] = varValue;
                                            }
                                            else if (valueTypeNestingTrace != null)
                                            {
                                                valueTypeNestingTrace.Value = varValue;
                                                valueTypeNestingTrace.AssignValue();
                                            }
                                            else
                                            {
                                                throw new NotImplementedException();
                                                //((dynamic)member).SetValue(obj, varValue);
                                            }
                                        }
                                    }
                                    else if (varFuncMatch.Groups["assignationOperator"].Success) i -= varFuncMatch.Groups["assignationOperator"].Length;
                                    else if (varFuncMatch.Groups["postfixOperator"].Success) i -= varFuncMatch.Groups["postfixOperator"].Length;
                                }
                            }
                        }
                        catch (SecurityException)
                        {
                            throw;
                        }
                        catch (SyntaxErrorException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            //Transport the exception in stack.
                            var nestedException = new SyntaxErrorException($"[{objType}] object has no public Property or Member named \"{varFuncName}\".", ex);
                            stack.Push(new BubbleExceptionContainer(nestedException));
                            i--;
                            return true; //Signals an error to the parsing method array call
                        }
                    }
                    else
                    {
                        var variablePreEvaluationEventArg = new VariablePreEvaluationEventArg(varFuncName, this, genericTypes: genericsTypes, evaluateGenericTypes: GetConcreteTypes);

                        PreEvaluateVariable?.Invoke(this, variablePreEvaluationEventArg);

                        if (variablePreEvaluationEventArg.CancelEvaluation)
                        {
                            throw new SyntaxErrorException($"Variable [{varFuncName}] unknown in expression : [{expression}]");
                        }
                        else if (variablePreEvaluationEventArg.HasValue)
                        {
                            stack.Push(variablePreEvaluationEventArg.Value);
                        }
                        else if (defaultVariables.TryGetValue(varFuncName, out var varValueToPush))
                        {
                            stack.Push(varValueToPush);
                        }
                        else if ((Variables.TryGetValue(varFuncName, out var cusVarValueToPush) || varFuncMatch.Groups["assignationOperator"].Success || (stack.Count == 1 && stack.Peek() is ClassOrEnumType && string.IsNullOrWhiteSpace(expression.Substring(i)))) && (cusVarValueToPush == null || !TypesToBlock.Contains(cusVarValueToPush.GetType())))
                        {
                            if (stack.Count == 1 && stack.Peek() is ClassOrEnumType classOrEnum)
                            {
                                if (Variables.ContainsKey(varFuncName))
                                    throw new SyntaxErrorException($"Can not declare a new variable named [{varFuncName}]. A variable with this name already exists");
                                if (varFuncMatch.Groups["varKeyword"].Success)
                                    throw new SyntaxErrorException("Can not declare a variable with type and var keyword.");
                                if (varFuncMatch.Groups["dynamicKeyword"].Success)
                                    throw new SyntaxErrorException("Can not declare a variable with type and dynamic keyword.");

                                stack.Pop();

                                Variables[varFuncName] = new StronglyTypedVariable { Type = classOrEnum.Type, Value = GetDefaultValueOfType(classOrEnum.Type), };
                            }

                            if (cusVarValueToPush is StronglyTypedVariable typedVariable)
                                cusVarValueToPush = typedVariable.Value;

                            stack.Push(cusVarValueToPush);

                            if (OptionVariableAssignationActive)
                            {
                                var assign = true;

                                if (varFuncMatch.Groups["assignationOperator"].Success)
                                {
                                    cusVarValueToPush = ManageKindOfAssignation(expression, ref i, varFuncMatch, () => cusVarValueToPush, stack);
                                }
                                else if (varFuncMatch.Groups["postfixOperator"].Success)
                                {
                                    throw new NotImplementedException();
                                    //cusVarValueToPush = varFuncMatch.Groups["postfixOperator"].Value.Equals("++") ? (dynamic)cusVarValueToPush + 1 : (dynamic)cusVarValueToPush - 1;
                                }
                                else if (varFuncMatch.Groups["prefixOperator"].Success)
                                {
                                    throw new NotImplementedException();
                                    //stack.Pop();
                                    //cusVarValueToPush = varFuncMatch.Groups["prefixOperator"].Value.Equals("++") ? (dynamic)cusVarValueToPush + 1 : (dynamic)cusVarValueToPush - 1;
                                    //stack.Push(cusVarValueToPush);
                                }
                                else
                                {
                                    assign = false;
                                }

                                if (assign)
                                {
                                    AssignVariable(varFuncName, cusVarValueToPush);
                                }
                            }
                            else if (varFuncMatch.Groups["assignationOperator"].Success)
                            {
                                i -= varFuncMatch.Groups["assignationOperator"].Length;
                            }
                            else if (varFuncMatch.Groups["postfixOperator"].Success)
                            {
                                i -= varFuncMatch.Groups["postfixOperator"].Length;
                            }
                        }
                        else
                        {
                            var staticType = EvaluateType(expression, ref i, varFuncName, genericsTypes);

                            if (staticType != null)
                            {
                                stack.Push(new ClassOrEnumType { Type = staticType });
                            }
                            else
                            {
                                var variableEvaluationEventArg = new VariableEvaluationEventArg(varFuncName, this, genericTypes: genericsTypes, evaluateGenericTypes: GetConcreteTypes);

                                EvaluateVariable?.Invoke(this, variableEvaluationEventArg);

                                if (variableEvaluationEventArg.HasValue)
                                {
                                    stack.Push(variableEvaluationEventArg.Value);
                                }
                                else
                                {
                                    throw new SyntaxErrorException($"变量 [{varFuncName}] 在脚本中未知 : [{expression}]");
                                }
                            }
                        }
                    }

                    i--;
                }

                if (varFuncMatch.Groups["sign"].Success)
                {
                    var temp = stack.Pop();
                    stack.Push(varFuncMatch.Groups["sign"].Value.Equals("+") ? ExpressionOperator.UnaryPlus : ExpressionOperator.UnaryMinus);
                    stack.Push(temp);
                }

                return true;
            }

            return false;
        }

        /// <summary>解析类型</summary>
        private Type EvaluateType(string expression, ref int i, string currentName = "", string genericsTypes = "")
        {
            var typeName = $"{currentName}{((i < expression.Length && expression.Substring(i)[0] == '?') ? "?" : "")}";
            var staticType = GetTypeByFriendlyName(typeName, genericsTypes);

            // For inline namespace parsing
            if (staticType == null)
            {
                if (OptionEInlineNamespacesRule != EInlineNamespacesRule.BlockAll)
                {
                    var subIndex = 0;
                    var namespaceMatch = varOrFunctionRegEx.Match(expression.Substring(i));

                    while (staticType == null && namespaceMatch.Success && !namespaceMatch.Groups["sign"].Success && !namespaceMatch.Groups["assignationOperator"].Success && !namespaceMatch.Groups["postfixOperator"].Success && !namespaceMatch.Groups["isfunction"].Success && i + subIndex < expression.Length && !typeName.EndsWith("?"))
                    {
                        subIndex += namespaceMatch.Length;
                        typeName += $"{namespaceMatch.Groups["inObject"].Value}{namespaceMatch.Groups["name"].Value}{((i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?') ? "?" : "")}";

                        staticType = GetTypeByFriendlyName(typeName, namespaceMatch.Groups["isgeneric"].Value);

                        if (staticType != null)
                        {
                            if (OptionEInlineNamespacesRule == EInlineNamespacesRule.AllowOnlyInlineNamespacesList && !InlineNamespacesList.Contains(staticType.Namespace))
                            {
                                staticType = null;
                            }
                            else
                            {
                                i += subIndex;
                            }

                            break;
                        }

                        namespaceMatch = varOrFunctionRegEx.Match(expression.Substring(i + subIndex));
                    }
                }
                else
                {
                    var subIndex = 0;
                    var typeMatch = varOrFunctionRegEx.Match(expression.Substring(i));

                    if (staticType == null && typeMatch.Success && !typeMatch.Groups["sign"].Success && !typeMatch.Groups["assignationOperator"].Success && !typeMatch.Groups["postfixOperator"].Success && !typeMatch.Groups["isfunction"].Success && !typeMatch.Groups["inObject"].Success && i + subIndex < expression.Length && !typeName.EndsWith("?"))
                    {
                        subIndex += typeMatch.Length;
                        typeName += $"{typeMatch.Groups["name"].Value}{((i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?') ? "?" : "")}";

                        staticType = GetTypeByFriendlyName(typeName, typeMatch.Groups["isgeneric"].Value);

                        if (staticType != null)
                        {
                            i += subIndex;
                        }
                    }
                }
            }

            if (typeName.EndsWith("?") && staticType != null) i++;

            // For nested type parsing
            if (staticType != null)
            {
                var subIndex = 0;
                var nestedTypeMatch = varOrFunctionRegEx.Match(expression.Substring(i + subIndex));
                while (nestedTypeMatch.Success && !nestedTypeMatch.Groups["sign"].Success && !nestedTypeMatch.Groups["assignationOperator"].Success && !nestedTypeMatch.Groups["postfixOperator"].Success && !nestedTypeMatch.Groups["isfunction"].Success)
                {
                    subIndex = nestedTypeMatch.Length;
                    typeName += $"+{nestedTypeMatch.Groups["name"].Value}{((i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?') ? "?" : "")}";

                    var nestedType = GetTypeByFriendlyName(typeName, nestedTypeMatch.Groups["isgeneric"].Value);
                    if (nestedType != null)
                    {
                        i += subIndex;
                        staticType = nestedType;

                        if (typeName.EndsWith("?"))
                            i++;
                    }
                    else
                    {
                        break;
                    }

                    nestedTypeMatch = varOrFunctionRegEx.Match(expression.Substring(i));
                }
            }

            Match arrayTypeMatch;

            if (i < expression.Length && (arrayTypeMatch = arrayTypeDetectionRegex.Match(expression.Substring(i))).Success)
            {
                var arrayType = GetTypeByFriendlyName(staticType + arrayTypeMatch.Value);
                if (arrayType != null)
                {
                    i += arrayTypeMatch.Length;
                    staticType = arrayType;
                }
            }

            return staticType;
        }

        /// <summary>解析字符</summary>
        private bool EvaluateChar(string expression, Stack<object> stack, ref int i)
        {
            if (!OptionCharEvaluationActive)
                return false;

            var s = expression.Substring(i, 1);

            if (s.Equals("'"))
            {
                i++;

                if (expression.Substring(i, 1).Equals(@"\"))
                {
                    i++;
                    var escapedChar = expression[i];

                    if (charEscapedCharDict.ContainsKey(escapedChar))
                    {
                        stack.Push(charEscapedCharDict[escapedChar]);
                        i++;
                    }
                    else
                    {
                        throw new SyntaxErrorException("Not known escape sequence in literal character");
                    }
                }
                else if (expression.Substring(i, 1).Equals("'"))
                {
                    throw new SyntaxErrorException("Empty literal character is not valid");
                }
                else
                {
                    stack.Push(expression[i]);
                    i++;
                }

                if (expression.Substring(i, 1).Equals("'"))
                {
                    return true;
                }
                else
                {
                    throw new SyntaxErrorException("Too much characters in the literal character");
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>解析操作符</summary> 
        private bool EvaluateOperators(string expression, Stack<object> stack, ref int i)
        {
            var regexPattern = "^(" + string.Join("|", operatorsDictionary.Keys.OrderByDescending(key => key.Length).Select(Regex.Escape)) + ")";

            var match = Regex.Match(expression.Substring(i), regexPattern, optionCaseSensitiveEvaluationActive ? RegexOptions.None : RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var op = match.Value;

                if (op.Equals("+") && (stack.Count == 0 || (stack.Peek() is ExpressionOperator previousOp && !UnaryPrefixOperators.Contains(previousOp))))
                    stack.Push(ExpressionOperator.UnaryPlus);
                else if (op.Equals("-") && (stack.Count == 0 || (stack.Peek() is ExpressionOperator previousOp2 && !UnaryPrefixOperators.Contains(previousOp2))))
                    stack.Push(ExpressionOperator.UnaryMinus);
                else
                    stack.Push(operatorsDictionary[op]);
                i += op.Length - 1;
                return true;
            }

            return false;
        }

        /// <summary>解析三目运算符</summary>
        private bool EvaluateTernaryConditionalOperator(string expression, Stack<object> stack, ref int i)
        {
            if (expression.Substring(i, 1).Equals("?"))
            {
                var condition = (bool)ProcessStack(stack);

                var restOfExpression = expression.Substring(i + 1);

                for (var j = 0; j < restOfExpression.Length; j++)
                {
                    var s2 = restOfExpression.Substring(j, 1);

                    var internalStringMatch = stringBeginningRegex.Match(restOfExpression.Substring(j));

                    if (internalStringMatch.Success)
                    {
                        var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(restOfExpression.Substring(j + internalStringMatch.Length), internalStringMatch);
                        j += innerString.Length - 1;
                    }
                    else if (s2.Equals("("))
                    {
                        j++;
                        GetExpressionsParenthesized(restOfExpression, ref j, false);
                    }
                    else if (s2.Equals(":"))
                    {
                        stack.Clear();

                        stack.Push(condition ? Evaluate(restOfExpression.Substring(0, j)) : Evaluate(restOfExpression.Substring(j + 1)));

                        i = expression.Length;

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>解析圆括号</summary>
        private bool EvaluateParenthis(string expression, Stack<object> stack, ref int i)
        {
            var s = expression.Substring(i, 1);

            if (s.Equals(")"))
                throw new Exception($"To much ')' characters are defined in expression : [{expression}] : no corresponding '(' fund.");

            if (s.Equals("("))
            {
                i++;

                if (stack.Count > 0 && stack.Peek() is InternalDelegate)
                {
                    var expressionsInParenthis = GetExpressionsParenthesized(expression, ref i, true);

                    var lambdaDelegate = stack.Pop() as InternalDelegate;

                    stack.Push(lambdaDelegate(expressionsInParenthis.ConvertAll(Evaluate).ToArray()));
                }
                else
                {
                    ChangeToUnaryPlusOrMinus(stack);

                    var expressionsInParenthis = GetExpressionsParenthesized(expression, ref i, false);

                    stack.Push(Evaluate(expressionsInParenthis[0]));
                }

                return true;
            }

            return false;
        }

        /// <summary>加减号转换为正负号</summary>
        private void ChangeToUnaryPlusOrMinus(Stack<object> stack)
        {
            if (stack.Count > 0 && stack.Peek() is ExpressionOperator op && (op.Equals(ExpressionOperator.Plus) || op.Equals(ExpressionOperator.Minus)))
            {
                stack.Pop();

                if (stack.Count == 0 || stack.Peek() is ExpressionOperator)
                {
                    stack.Push(op.Equals(ExpressionOperator.Plus) ? ExpressionOperator.UnaryPlus : ExpressionOperator.UnaryMinus);
                }
                else
                {
                    stack.Push(op);
                }
            }
        }

        /// <summary>解析索引器</summary>
        private bool EvaluateIndexing(string expression, Stack<object> stack, ref int i)
        {
            if (!OptionIndexerActive) return false;

            var indexingBeginningMatch = indexingBeginningRegex.Match(expression.Substring(i));

            if (indexingBeginningMatch.Success)
            {
                i += indexingBeginningMatch.Length;

                var left = stack.Pop();

                var indexingArgs = GetExpressionsParenthesized(expression, ref i, true, startChar: "[", endChar: "]");

                if (left is NullConditionalNullValue or BubbleExceptionContainer)
                {
                    stack.Push(left);
                    return true;
                }

                if (indexingBeginningMatch.Groups["nullConditional"].Success && left == null)
                {
                    stack.Push(new NullConditionalNullValue());
                    return true;
                }

                var indexingPreEvaluationEventArg = new IndexingPreEvaluationEventArg(indexingArgs, this, left);

                PreEvaluateIndexing?.Invoke(this, indexingPreEvaluationEventArg);

                if (indexingPreEvaluationEventArg.CancelEvaluation)
                {
                    throw new SyntaxErrorException($"[{left.GetType()}] 没有索引器");
                }

                if (indexingPreEvaluationEventArg.HasValue)
                {
                    stack.Push(indexingPreEvaluationEventArg.Value);
                }
                else
                {
                    throw new NotImplementedException();
                    // Match assignationOrPostFixOperatorMatch = null;
                    // dynamic valueToPush = null;
                    // List<dynamic> oIndexingArgs = indexingArgs.ConvertAll(Evaluate);
                    // PropertyInfo[] itemProperties = null;
                    //
                    // if (!(left is IDictionary<string, object>))
                    // {
                    //     var type = ((object)left).GetType();
                    //
                    //     if (type.IsArray && OptionForceIntegerNumbersEvaluationsAsDoubleByDefault)
                    //     {
                    //         oIndexingArgs = oIndexingArgs.ConvertAll(o => o is double ? (int)o : o);
                    //     }
                    //     else
                    //     {
                    //         itemProperties = type.GetProperties().Where(property => property.GetIndexParameters().Length > 0 && property.GetIndexParameters().Length == oIndexingArgs.Count && property.GetIndexParameters().All(parameter => parameter.ParameterType.IsAssignableFrom(oIndexingArgs[parameter.Position].GetType()))).ToArray();
                    //
                    //         if (itemProperties.Length == 0 && OptionForceIntegerNumbersEvaluationsAsDoubleByDefault)
                    //         {
                    //             var backupIndexedArgs = oIndexingArgs.ToList();
                    //
                    //             itemProperties = type.GetProperties().Where(property => property.Name.Equals("Item") && property.GetIndexParameters().Length == oIndexingArgs.Count && property.GetIndexParameters().All(parameter =>
                    //             {
                    //                 if (parameter.ParameterType.IsAssignableFrom(((object)oIndexingArgs[parameter.Position]).GetType()))
                    //                 {
                    //                     return true;
                    //                 }
                    //                 else if (parameter.ParameterType == typeof(int) && oIndexingArgs[parameter.Position] is double)
                    //                 {
                    //                     oIndexingArgs[parameter.Position] = (int)oIndexingArgs[parameter.Position];
                    //                     return true;
                    //                 }
                    //                 else
                    //                 {
                    //                     return false;
                    //                 }
                    //             })).ToArray();
                    //
                    //             if (itemProperties.Length == 0)
                    //                 oIndexingArgs = backupIndexedArgs;
                    //         }
                    //     }
                    // }
                    //
                    // object GetIndexedObject()
                    // {
                    //     if (left is IDictionary<string, object> dictionaryLeft)
                    //         return dictionaryLeft[oIndexingArgs[0]];
                    //     else if (itemProperties?.Length > 0)
                    //         return itemProperties[0].GetValue(left, oIndexingArgs.Cast<object>().ToArray());
                    //     else
                    //         return left[oIndexingArgs[0]];
                    // }
                    //
                    // if (OptionIndexingAssignationActive && (assignationOrPostFixOperatorMatch = assignationOrPostFixOperatorRegex.Match(expression.Substring(i + 1))).Success)
                    // {
                    //     i += assignationOrPostFixOperatorMatch.Length + 1;
                    //
                    //     var postFixOperator = assignationOrPostFixOperatorMatch.Groups["postfixOperator"].Success;
                    //     var exceptionContext = postFixOperator ? "++ or -- operator" : "an assignation";
                    //
                    //     if (stack.Count > 1)
                    //         throw new EESyntaxErrorException($"The left part of {exceptionContext} must be a variable, a property or an indexer.");
                    //
                    //     if (indexingBeginningMatch.Groups["nullConditional"].Success)
                    //         throw new EESyntaxErrorException($"Null conditional is not usable left to {exceptionContext}");
                    //
                    //     if (postFixOperator)
                    //     {
                    //         if (left is IDictionary<string, object> dictionaryLeft)
                    //         {
                    //             valueToPush = assignationOrPostFixOperatorMatch.Groups["postfixOperator"].Value.Equals("++") ? dictionaryLeft[oIndexingArgs[0]]++ : dictionaryLeft[oIndexingArgs[0]]--;
                    //         }
                    //         else if (itemProperties?.Length > 0)
                    //         {
                    //             valueToPush = itemProperties[0].GetValue(left, oIndexingArgs.Cast<object>().ToArray());
                    //             itemProperties[0].SetValue(left, assignationOrPostFixOperatorMatch.Groups["postfixOperator"].Value.Equals("++") ? valueToPush + 1 : valueToPush - 1, oIndexingArgs.Cast<object>().ToArray());
                    //         }
                    //         else
                    //         {
                    //             valueToPush = assignationOrPostFixOperatorMatch.Groups["postfixOperator"].Value.Equals("++") ? left[oIndexingArgs[0]]++ : left[oIndexingArgs[0]]--;
                    //         }
                    //     }
                    //     else
                    //     {
                    //         valueToPush = ManageKindOfAssignation(expression, ref i, assignationOrPostFixOperatorMatch, GetIndexedObject);
                    //
                    //         if (left is IDictionary<string, object> dictionaryLeft)
                    //             dictionaryLeft[oIndexingArgs[0]] = valueToPush;
                    //         else if (itemProperties?.Length > 0)
                    //             itemProperties[0].SetValue(left, valueToPush, oIndexingArgs.Cast<object>().ToArray());
                    //         else
                    //             left[oIndexingArgs[0]] = valueToPush;
                    //
                    //         stack.Clear();
                    //     }
                    // }
                    // else
                    // {
                    //     valueToPush = GetIndexedObject();
                    // }
                    //
                    // stack.Push(valueToPush);
                }

                return true;
            }

            return false;
        }

        /// <summary>解析字符串</summary>
        private bool EvaluateString(string expression, Stack<object> stack, ref int i)
        {
            if (!OptionStringEvaluationActive) return false;

            var stringBeginningMatch = stringBeginningRegex.Match(expression.Substring(i));

            if (stringBeginningMatch.Success)
            {
                var isEscaped = stringBeginningMatch.Groups["escaped"].Success;
                var isInterpolated = stringBeginningMatch.Groups["interpolated"].Success;

                i += stringBeginningMatch.Length;

                var stringRegexPattern = new Regex($"^[^{(isEscaped ? "" : @"\\")}{(isInterpolated ? "{}" : "")}\"]*");

                var endOfString = false;

                var resultString = new StringBuilder();

                while (!endOfString && i < expression.Length)
                {
                    var stringMatch = stringRegexPattern.Match(expression.Substring(i, expression.Length - i));

                    resultString.Append(stringMatch.Value);
                    i += stringMatch.Length;

                    if (expression.Substring(i)[0] == '"')
                    {
                        if (expression.Substring(i).Length > 1 && expression.Substring(i)[1] == '"')
                        {
                            i += 2;
                            resultString.Append(@"""");
                        }
                        else
                        {
                            endOfString = true;
                            stack.Push(resultString.ToString());
                        }
                    }
                    else if (expression.Substring(i)[0] == '{')
                    {
                        i++;

                        if (expression.Substring(i)[0] == '{')
                        {
                            resultString.Append("{");
                            i++;
                        }
                        else
                        {
                            var innerExp = new StringBuilder();
                            var bracketCount = 1;
                            for (; i < expression.Length; i++)
                            {
                                if (i + 3 <= expression.Length && expression.Substring(i, 3).Equals("'\"'"))
                                {
                                    innerExp.Append("'\"'");
                                    i += 2;
                                }
                                else
                                {
                                    var internalStringMatch = stringBeginningRegex.Match(expression.Substring(i));

                                    if (internalStringMatch.Success)
                                    {
                                        var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expression.Substring(i + internalStringMatch.Length), internalStringMatch);
                                        innerExp.Append(innerString);
                                        i += innerString.Length - 1;
                                    }
                                    else
                                    {
                                        var s = expression.Substring(i, 1);

                                        if (s.Equals("{")) bracketCount++;

                                        if (s.Equals("}"))
                                        {
                                            bracketCount--;
                                            i++;
                                            if (bracketCount == 0) break;
                                        }

                                        innerExp.Append(s);
                                    }
                                }
                            }

                            if (bracketCount > 0)
                            {
                                var beVerb = bracketCount == 1 ? "is" : "are";
                                throw new Exception($"{bracketCount} '}}' character {beVerb} missing in expression : [{expression}]");
                            }

                            var obj = Evaluate(innerExp.ToString());

                            if (obj is BubbleExceptionContainer bubbleExceptionContainer)
                                bubbleExceptionContainer.Throw();

                            resultString.Append(obj);
                        }
                    }
                    else if (expression.Substring(i, expression.Length - i)[0] == '}')
                    {
                        i++;

                        if (expression.Substring(i, expression.Length - i)[0] == '}')
                        {
                            resultString.Append("}");
                            i++;
                        }
                        else
                        {
                            throw new SyntaxErrorException("A character '}' must be escaped in a interpolated string.");
                        }
                    }
                    else if (expression.Substring(i, expression.Length - i)[0] == '\\')
                    {
                        i++;

                        if (stringEscapedCharDict.TryGetValue(expression.Substring(i, expression.Length - i)[0], out var escapedString))
                        {
                            resultString.Append(escapedString);
                            i++;
                        }
                        else
                        {
                            throw new SyntaxErrorException("There is no corresponding escaped character for \\" + expression.Substring(i, 1));
                        }
                    }
                }

                if (!endOfString) throw new SyntaxErrorException(@"缺少一个[ \ ]字符");

                return true;
            }

            return false;
        }

        #endregion

        #region 进程堆栈

        public object ProcessStack(Stack<object> stack)
        {
            //如果堆栈为空，则抛出异常
            if (stack.Count == 0) throw new SyntaxErrorException("空表达式或找不到标记");

            //将栈中的值类型,异常,空值进行处理
            var list = stack.Select(e => e is ValueTypeNestingTrace valueTypeNestingTrace ? valueTypeNestingTrace.Value : e) //处理值类型
                .Select(e => e is SubExpression subExpression ? Evaluate(subExpression.Expression) : e) //处理子表达式
                .Select(e => e is NullConditionalNullValue ? null : e).ToList(); //处理空值

            // 遍历所有的操作符
            foreach (var _operatorMsg in OperatorsEvaluation)
            {
                // 从后往前遍历
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    // 如果当前的操作符不是当前的操作符,则跳过
                    if (list[i] as ExpressionOperator != _operatorMsg.Key) continue;

                    // 如果当前的操作符 同时也是 是右操作符,则
                    if (UnaryPostfixOperators.Contains(_operatorMsg.Key))
                    {
                        try
                        {
                            //定义一个方法,用于递归处理前一个操作符
                            void EvaluateFirstNextUnaryOp(int j, ref int parentIndex)
                            {
                                if (j > 0 && list[j] is ExpressionOperator nextOp && UnaryPostfixOperators.Contains(nextOp))
                                {
                                    EvaluateFirstNextUnaryOp(j - 1, ref j);

                                    //处理前一个操作符
                                    //list[j] = OperatorsEvaluation[nextOp](null, (dynamic)list[j - 1]);
                                    list[j] = OperatorsEvaluation[nextOp](null, list[j - 1]);

                                    //移除前一个操作符
                                    list.RemoveAt(j - 1);
                                    parentIndex = j;
                                }
                            }

                            EvaluateFirstNextUnaryOp(i - 1, ref i);
                            //list[i] = _operatorFunc(null, (dynamic)list[i - 1]);
                            list[i] = _operatorMsg.Value(null, list[i - 1]);
                        }
                        catch (Exception ex)
                        {
                            var right = list[i - 1];
                            //                Bubble up the causing error       //Transport the processing error
                            list[i] = right is BubbleExceptionContainer ? right : new BubbleExceptionContainer(ex);
                        }

                        list.RemoveAt(i - 1);
                        break;
                    }

                    // 如果当前的操作符 同时也是 是左操作符,则(目前没有左操作符)
                    if (UnaryPrefixOperators.Contains(_operatorMsg.Key))
                    {
                        try
                        {
                            void EvaluateFirstPreviousUnaryOp(int j)
                            {
                                if (j < list.Count - 1 && list[j] is ExpressionOperator previousOp && UnaryPrefixOperators.Contains(previousOp))
                                {
                                    EvaluateFirstPreviousUnaryOp(j + 1);

                                    //list[j] = OperatorsEvaluation?[previousOp]((dynamic)list[j + 1], null);
                                    list[j] = OperatorsEvaluation?[previousOp](list[j + 1], null);


                                    list.RemoveAt(j + 1);
                                }
                            }

                            EvaluateFirstPreviousUnaryOp(i + 1);

                            // list[i] = _operatorFunc((dynamic)list[i + 1], null);
                            list[i] = _operatorMsg.Value(list[i + 1], null);
                        }
                        catch (Exception ex)
                        {
                            // var left = (dynamic)list[i + 1];
                            var left = list[i + 1];
                            //                Bubble up the causing error       //Transport the processing error
                            list[i] = left is BubbleExceptionContainer ? left : new BubbleExceptionContainer(ex);
                        }

                        list.RemoveAt(i + 1);
                        break;
                    }

                    // 剩下的为左右双目操作符
                    {
                        // var left = (dynamic)list[i + 1];
                        var left = list[i + 1];
                        // var right = (dynamic)list[i - 1];
                        var right = list[i - 1];

                        try
                        {
                            list[i] = _operatorMsg.Value(left, right);

                            if (left is BubbleExceptionContainer && right is string)
                            {
                                list[i] = left; //Bubble up the causing error
                            }
                            else if (right is BubbleExceptionContainer && left is string)
                            {
                                list[i] = right; //Bubble up the causing error
                            }
                        }
                        catch (Exception ex)
                        {
                            if (left is BubbleExceptionContainer)
                            {
                                list[i] = left; //Bubble up the causing error
                            }
                            else if (right is BubbleExceptionContainer)
                            {
                                list[i] = right; //Bubble up the causing error
                            }
                            else
                            {
                                list[i] = new BubbleExceptionContainer(ex); //Transport the processing error
                            }
                        }

                        list.RemoveAt(i + 1);
                        list.RemoveAt(i - 1);
                        break;
                    }
                }
            }


            stack.Clear();
            //将处理后的结果压入堆栈
            for (var i = 0; i < list.Count; i++)
            {
                stack.Push(list[i]);
            }

            if (stack.Count > 1)
            {
                foreach (var item in stack)
                {
                    if (item is BubbleExceptionContainer bubbleExceptionContainer)
                    {
                        bubbleExceptionContainer.Throw(); //Throw the first occuring error
                    }
                }

                throw new SyntaxErrorException("语法错误.检查没有操作符丢失");
            }
            else if (evaluationStackCount == 1 && stack.Peek() is BubbleExceptionContainer bubbleExceptionContainer)
            {
                //We reached the top level of the evaluation. So we want to throw the resulting exception.
                bubbleExceptionContainer.Throw();
            }

            return stack.Pop();
        }

        #endregion

        #region 用于解析和解释代码的工具方法

        /// <summary>用于解析方法的委托</summary>
        private delegate bool ParsingMethodDelegate(string expression, Stack<object> stack, ref int i);


        /// <summary>用于解释方法的委托</summary>
        private delegate object InternalDelegate(params object[] args);

        /// <summary>管理分配类型</summary>
        private object ManageKindOfAssignation(string expression, ref int index, Match match, Func<object> getCurrentValue, Stack<object> stack = null)
        {
            if (stack?.Count > 1) throw new SyntaxErrorException($"{expression}赋值的左边部分必须是变量,属性或索引器");

            object result;
            var rightExpression = expression.Substring(index);
            index = expression.Length;

            if (rightExpression.Trim().Equals(string.Empty)) throw new SyntaxErrorException("分配中缺少右部分");

            if (match.Groups["assignmentPrefix"].Success)
            {
                var prefixOp = operatorsDictionary[match.Groups["assignmentPrefix"].Value];

                result = OperatorsEvaluation[prefixOp](getCurrentValue(), Evaluate(rightExpression));
            }
            else
            {
                result = Evaluate(rightExpression);
            }

            if (result is BubbleExceptionContainer exceptionContainer)
                exceptionContainer.Throw();

            if (stack != null)
            {
                stack.Clear();
                stack.Push(result);
            }

            return result;
        }

        private void AssignVariable(string varName, object value)
        {
            if (Variables.ContainsKey(varName) && Variables[varName] is StronglyTypedVariable stronglyTypedVariable)
            {
                if (value == null && stronglyTypedVariable.Type.IsValueType && Nullable.GetUnderlyingType(stronglyTypedVariable.Type) == null)
                {
                    throw new SyntaxErrorException($"Can not cast null to {stronglyTypedVariable.Type} because it's not a nullable valueType");
                }

                var typeToAssign = value?.GetType();
                if (typeToAssign == null || stronglyTypedVariable.Type.IsAssignableFrom(typeToAssign))
                {
                    stronglyTypedVariable.Value = value;
                }
                else
                {
                    try
                    {
                        Variables[varName] = Convert.ChangeType(value, stronglyTypedVariable.Type);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidCastException($"A object of type {typeToAssign} can not be cast implicitely in {stronglyTypedVariable.Type}", exception);
                    }
                }
            }
            else
            {
                Variables[varName] = value;
            }
        }

        private static object GetDefaultValueOfType(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>获取Lambda指类型的解释器</summary>
        private bool GetLambdaExpression(string expression, Stack<object> stack)
        {
            var lambdaExpressionMatch = lambdaExpressionRegex.Match(expression);

            if (!lambdaExpressionMatch.Success) return false;

            var argsNames = lambdaArgRegex.Matches(lambdaExpressionMatch.Groups["args"].Value);


            stack.Push(new InternalDelegate((object[] args) =>
            {
                var vars = new Dictionary<string, object>(variables);

                for (var a = 0; a < argsNames.Count || a < args.Length; a++)
                {
                    vars[argsNames[a].Value] = args[a];
                }

                var savedVars = variables;
                Variables = vars;

                var lambdaBody = lambdaExpressionMatch.Groups["expression"].Value.Trim();

                object result = null;

                if (OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate && lambdaBody.StartsWith("{") && lambdaBody.EndsWith("}"))
                {
                    result = ScriptEvaluate(lambdaBody.Substring(1, lambdaBody.Length - 2));
                }
                else
                {
                    result = Evaluate(lambdaExpressionMatch.Groups["expression"].Value);
                }

                variables = savedVars;

                return result;
            }));

            return true;
        }

        /// <summary>获取方法的解释器</summary>
        private MethodInfo GetRealMethod(ref Type type, ref object obj, string func, BindingFlags flag, List<object> args, string genericsTypes, Type[] inferedGenericsTypes, List<ArgKeywordsEncaps> argsWithKeywords, bool testForExtention = false)
        {
            MethodInfo methodInfo = null;
            var modifiedArgs = new List<object>(args);


            bool parameterValidate(ParameterInfo p) => p.Position >= modifiedArgs.Count || (testForExtention && p.Position == 0) || modifiedArgs[p.Position] == null || IsCastable(modifiedArgs[p.Position].GetType(), p.ParameterType) || typeof(Delegate).IsAssignableFrom(p.ParameterType) || p.IsDefined(typeof(ParamArrayAttribute)) || (p.ParameterType.IsByRef && argsWithKeywords.Any(a => a.Index == p.Position + (testForExtention ? 1 : 0)));

            bool methodByNameFilter(MethodInfo m) => m.Name.Equals(func, StringComparisonForCasing) && (m.GetParameters().Length == modifiedArgs.Count || (m.GetParameters().Length > modifiedArgs.Count && m.GetParameters().Take(modifiedArgs.Count).All(p => modifiedArgs[p.Position] == null || IsCastable(modifiedArgs[p.Position].GetType(), p.ParameterType)) && m.GetParameters().Skip(modifiedArgs.Count).All(p => p.HasDefaultValue)) || (m.GetParameters().Length > 0 && m.GetParameters().Last().IsDefined(typeof(ParamArrayAttribute), false) && m.GetParameters().All(parameterValidate)));

            var methodInfos = type.GetMethods(flag).Where(methodByNameFilter).OrderByDescending(m => m.GetParameters().Length).ToList();

            //对于重载并可能实现lambda参数的Linq方法
            try
            {
                if (methodInfos.Count > 1 && type == typeof(Enumerable) && args.Count == 2 && args[1] is InternalDelegate internalDelegate && args[0] is IEnumerable enumerable && enumerable.GetEnumerator() is IEnumerator enumerator && enumerator.MoveNext() && methodInfos.Any(m => m.GetParameters().Any(p => p.ParameterType.Name.StartsWith("Func"))))
                {
                    Type lambdaResultType = internalDelegate.Invoke(enumerator.Current).GetType();

                    methodInfo = methodInfos.Find(m =>
                    {
                        var parameterInfos = m.GetParameters();

                        return parameterInfos.Length == 2 && parameterInfos[1].ParameterType.Name.StartsWith("Func") && parameterInfos[1].ParameterType.GenericTypeArguments is Type[] genericTypesArgs && genericTypesArgs.Length == 2 && genericTypesArgs[1] == lambdaResultType;
                    });

                    if (methodInfo != null)
                    {
                        methodInfo = TryToCastMethodParametersToMakeItCallable(methodInfo, modifiedArgs, genericsTypes, inferedGenericsTypes, obj);
                    }
                }
            }
            catch { }

            for (var m = 0; methodInfo == null && m < methodInfos.Count; m++)
            {
                modifiedArgs = new List<object>(args);

                methodInfo = TryToCastMethodParametersToMakeItCallable(methodInfos[m], modifiedArgs, genericsTypes, inferedGenericsTypes, obj);
            }

            if (methodInfo != null)
            {
                args.Clear();
                args.AddRange(modifiedArgs);
            }

            return methodInfo;
        }

        private bool IsCastable(Type fromType, Type toType)
        {
            return toType.IsAssignableFrom(fromType) || (implicitCastDict.ContainsKey(fromType) && implicitCastDict[fromType].Contains(toType));
        }

        private MethodInfo TryToCastMethodParametersToMakeItCallable(MethodInfo methodInfoToCast, List<object> modifiedArgs, string genericsTypes, Type[] inferedGenericsTypes, object onInstance = null)
        {
            MethodInfo methodInfo = null;

            var oldMethodInfo = methodInfoToCast;

            if (!string.IsNullOrEmpty(genericsTypes))
            {
                methodInfoToCast = MakeConcreteMethodIfGeneric(methodInfoToCast, genericsTypes, inferedGenericsTypes);
            }
            else if (oldMethodInfo.IsGenericMethod && oldMethodInfo.ContainsGenericParameters)
            {
                var genericArgsTypes = oldMethodInfo.GetGenericArguments();
                var inferedTypes = new List<Type>();

                for (var t = 0; t < genericArgsTypes.Length; t++)
                {
                    if (genericArgsTypes[t].IsGenericParameter)
                    {
                        var name = genericArgsTypes[t].Name;
                        var parameterInfos = oldMethodInfo.GetParameters();

                        var paramsForInference = Array.Find(parameterInfos, p => p.ParameterType.IsGenericParameter && p.ParameterType.Name.Equals(name) && modifiedArgs.Count > p.Position && !modifiedArgs[p.Position].GetType().IsGenericParameter);

                        if (paramsForInference != null)
                        {
                            inferedTypes.Add(modifiedArgs[paramsForInference.Position].GetType());
                        }
                        else
                        {
                            paramsForInference = Array.Find(parameterInfos, p => p.ParameterType.IsGenericType && p.ParameterType.ContainsGenericParameters && p.ParameterType.GetGenericArguments().Any(subP => subP.Name.Equals(name)) && modifiedArgs.Count > p.Position && !modifiedArgs[p.Position].GetType().IsGenericType);

                            if (paramsForInference != null)
                            {
                                if (modifiedArgs[paramsForInference.Position] is MethodsGroupEncaps methodsGroupEncaps)
                                {
                                    if (paramsForInference.ParameterType.Name.StartsWith("Converter"))
                                    {
                                        var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                        var paraMethodInfo = Array.Find(methodsGroupEncaps.MethodsGroup, mi => mi.GetParameters().Length == 1);
                                        if (specificType?.GenericParameterPosition == 0)
                                        {
                                            inferedTypes.Add(paraMethodInfo.GetParameters()[0].ParameterType);
                                        }
                                        else if (specificType?.GenericParameterPosition == 1)
                                        {
                                            inferedTypes.Add(paraMethodInfo.ReturnType);
                                        }
                                    }
                                    else if (paramsForInference.ParameterType.Name.StartsWith("Action"))
                                    {
                                        var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                        var paraMethodInfo = Array.Find(methodsGroupEncaps.MethodsGroup, mi => mi.GetParameters().Length == paramsForInference.ParameterType.GetGenericArguments().Length);
                                        if (specificType != null)
                                        {
                                            inferedTypes.Add(paraMethodInfo.GetParameters()[specificType.GenericParameterPosition].ParameterType);
                                        }
                                    }
                                    else if (paramsForInference.ParameterType.Name.StartsWith("Func"))
                                    {
                                        var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                        var paraMethodInfo = Array.Find(methodsGroupEncaps.MethodsGroup, mi => mi.GetParameters().Length == paramsForInference.ParameterType.GetGenericArguments().Length - 1);
                                        if (specificType?.GenericParameterPosition == paraMethodInfo.GetParameters().Length)
                                        {
                                            inferedTypes.Add(paraMethodInfo.ReturnType);
                                        }
                                        else
                                        {
                                            inferedTypes.Add(paraMethodInfo.GetParameters()[specificType.GenericParameterPosition].ParameterType);
                                        }
                                    }
                                }
                                else if (modifiedArgs[paramsForInference.Position].GetType().HasElementType)
                                {
                                    inferedTypes.Add(modifiedArgs[paramsForInference.Position].GetType().GetElementType());
                                }
                            }
                        }
                    }
                    else
                    {
                        inferedTypes.Add(genericArgsTypes[t]);
                    }
                }

                if (inferedTypes.Count > 0 && inferedTypes.Count == genericArgsTypes.Length)
                    methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, inferedTypes.ToArray());
                else
                    methodInfoToCast = MakeConcreteMethodIfGeneric(methodInfoToCast, genericsTypes, inferedGenericsTypes);
            }

            var parametersCastOK = true;

            // To manage empty params argument
            if ((methodInfoToCast.GetParameters().LastOrDefault()?.IsDefined(typeof(ParamArrayAttribute), false) ?? false) && methodInfoToCast.GetParameters().Length == modifiedArgs.Count + 1)
            {
                modifiedArgs.Add(Activator.CreateInstance(methodInfoToCast.GetParameters().Last().ParameterType, new object[] { 0 }));
            }
            else if (methodInfoToCast.GetParameters().Length > modifiedArgs.Count)
            {
                modifiedArgs.AddRange(methodInfoToCast.GetParameters().Skip(modifiedArgs.Count).Select(p => p.DefaultValue));
            }

            for (var a = 0; a < modifiedArgs.Count && parametersCastOK; a++)
            {
                var parameterType = methodInfoToCast.GetParameters()[a].ParameterType;
                var paramTypeName = parameterType.Name;

                if (modifiedArgs[a] is InternalDelegate internalDelegate)
                {
                    if (paramTypeName.StartsWith("Predicate"))
                    {
                        var de = new DelegateEncaps(internalDelegate);
                        var encapsMethod = de.GetType().GetMethod("Predicate").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, encapsMethod);
                    }
                    else if (paramTypeName.StartsWith("Func"))
                    {
                        var de = new DelegateEncaps(internalDelegate);
                        var encapsMethod = de.GetType().GetMethod($"Func{parameterType.GetGenericArguments().Length - 1}").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, encapsMethod);
                    }
                    else if (paramTypeName.StartsWith("Action"))
                    {
                        var de = new DelegateEncaps(internalDelegate);
                        var encapsMethod = de.GetType().GetMethod($"Action{parameterType.GetGenericArguments().Length}").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, encapsMethod);
                    }
                    else if (paramTypeName.StartsWith("Converter"))
                    {
                        var de = new DelegateEncaps(internalDelegate);
                        var encapsMethod = de.GetType().GetMethod("Func1").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, encapsMethod);
                    }
                }
                else if (typeof(Delegate).IsAssignableFrom(parameterType) && modifiedArgs[a] is MethodsGroupEncaps methodsGroupEncaps)
                {
                    var invokeMethod = parameterType.GetMethod("Invoke");
                    var methodForDelegate = Array.Find(methodsGroupEncaps.MethodsGroup, m => invokeMethod.GetParameters().Length == m.GetParameters().Length && invokeMethod.ReturnType.IsAssignableFrom(m.ReturnType));
                    if (methodForDelegate != null)
                    {
                        var parametersTypes = methodForDelegate.GetParameters().Select(p => p.ParameterType).ToArray();
                        Type delegateType;

                        if (methodForDelegate.ReturnType == typeof(void))
                        {
                            delegateType = Type.GetType($"System.Action`{parametersTypes.Length}");
                        }
                        else if (paramTypeName.StartsWith("Predicate"))
                        {
                            delegateType = typeof(Predicate<>);
                            methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, parametersTypes);
                        }
                        else if (paramTypeName.StartsWith("Converter"))
                        {
                            delegateType = typeof(Converter<,>);
                            parametersTypes = parametersTypes.Concat(new Type[] { methodForDelegate.ReturnType }).ToArray();
                        }
                        else
                        {
                            delegateType = Type.GetType($"System.Func`{parametersTypes.Length + 1}");
                            parametersTypes = parametersTypes.Concat(new Type[] { methodForDelegate.ReturnType }).ToArray();
                        }

                        delegateType = delegateType.MakeGenericType(parametersTypes);

                        modifiedArgs[a] = Delegate.CreateDelegate(delegateType, methodsGroupEncaps.ContainerObject, methodForDelegate);

                        if (oldMethodInfo.IsGenericMethod && methodInfoToCast.GetGenericArguments().Length == parametersTypes.Length && !methodInfoToCast.GetGenericArguments().SequenceEqual(parametersTypes) && string.IsNullOrWhiteSpace(genericsTypes))
                        {
                            methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, parametersTypes);
                        }
                    }
                }
                else
                {
                    try
                    {
                        // To manage params argument
                        if (methodInfoToCast.GetParameters().Length == a + 1 && methodInfoToCast.GetParameters()[a].IsDefined(typeof(ParamArrayAttribute), false) && parameterType != modifiedArgs[a]?.GetType() && parameterType.GetElementType() is Type elementType && modifiedArgs.Skip(a).All(arg => arg == null || elementType.IsAssignableFrom(arg.GetType())))
                        {
                            var numberOfElements = modifiedArgs.Count - a;
                            var paramsArray = Array.CreateInstance(elementType, numberOfElements);
                            modifiedArgs.Skip(a).ToArray().CopyTo(paramsArray, 0);
                            modifiedArgs.RemoveRange(a, numberOfElements);
                            modifiedArgs.Add(paramsArray);
                        }
                        else if (modifiedArgs[a] != null && !parameterType.IsAssignableFrom(modifiedArgs[a].GetType()))
                        {
                            if (parameterType.IsByRef)
                            {
                                if (!parameterType.GetElementType().IsAssignableFrom(modifiedArgs[a].GetType()))
                                    modifiedArgs[a] = Convert.ChangeType(modifiedArgs[a], parameterType.GetElementType());
                            }
                            else if (modifiedArgs[a].GetType().IsArray && typeof(IEnumerable).IsAssignableFrom(parameterType) && oldMethodInfo.IsGenericMethod && string.IsNullOrWhiteSpace(genericsTypes) && methodInfoToCast.GetGenericArguments().Length == 1 && !methodInfoToCast.GetGenericArguments()[0].Equals(modifiedArgs[a].GetType().GetElementType()))
                            {
                                methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, new Type[] { modifiedArgs[a].GetType().GetElementType() });
                            }
                            else
                            {
                                if (parameterType.IsArray && modifiedArgs[a] is Array sourceArray)
                                {
                                    modifiedArgs[a] = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(parameterType.GetElementType()).Invoke(null, new object[] { modifiedArgs[a] });

                                    modifiedArgs[a] = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(parameterType.GetElementType()).Invoke(null, new object[] { modifiedArgs[a] });
                                }
                                else if (IsCastable(modifiedArgs[a].GetType(), parameterType))
                                {
                                    modifiedArgs[a] = Convert.ChangeType(modifiedArgs[a], parameterType);
                                }
                                else
                                {
                                    var converter = parameterType.GetMethod("op_Implicit", new[] { modifiedArgs[a].GetType() });
                                    if (converter != null)
                                    {
                                        modifiedArgs[a] = converter.Invoke(null, new[] { modifiedArgs[a] });
                                    }
                                    else
                                    {
                                        parametersCastOK = false;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        parametersCastOK = false;
                    }

                    if (!parametersCastOK)
                    {
                        try
                        {
                            var parameterCastEvaluationEventArg = new ParameterCastEvaluationEventArg(methodInfoToCast, parameterType, modifiedArgs[a], a, this, onInstance);

                            EvaluateParameterCast?.Invoke(this, parameterCastEvaluationEventArg);

                            if (parameterCastEvaluationEventArg.FunctionModifiedArgument)
                            {
                                modifiedArgs[a] = parameterCastEvaluationEventArg.Argument;
                                parametersCastOK = true;
                            }
                        }
                        catch { }
                    }
                }
            }

            if (parametersCastOK)
                methodInfo = methodInfoToCast;

            return methodInfo;
        }

        private MethodInfo MakeConcreteMethodIfGeneric(MethodInfo methodInfo, string genericsTypes, Type[] inferedGenericsTypes)
        {
            if (methodInfo.IsGenericMethod)
            {
                if (genericsTypes.Equals(string.Empty))
                {
                    if (inferedGenericsTypes != null && inferedGenericsTypes.Length == methodInfo.GetGenericArguments().Length)
                    {
                        return methodInfo.MakeGenericMethod(inferedGenericsTypes);
                    }
                    else
                    {
                        return methodInfo.MakeGenericMethod(Enumerable.Repeat(typeof(object), methodInfo.GetGenericArguments().Length).ToArray());
                    }
                }
                else
                {
                    return methodInfo.MakeGenericMethod(GetConcreteTypes(genericsTypes));
                }
            }

            return methodInfo;
        }

        private Type[] GetConcreteTypes(string genericsTypes)
        {
            return genericsDecodeRegex.Matches(genericsEndOnlyOneTrim.Replace(genericsTypes.TrimStart(' ', '<'), "")).Cast<Match>().Select(match => GetTypeByFriendlyName(match.Groups["name"].Value, match.Groups["isgeneric"].Value, true)).ToArray();
        }

        private BindingFlags DetermineInstanceOrStatic(ref Type objType, ref object obj, ref ValueTypeNestingTrace valueTypeNestingTrace)
        {
            valueTypeNestingTrace = obj as ValueTypeNestingTrace;

            if (valueTypeNestingTrace != null)
            {
                obj = valueTypeNestingTrace.Value;
            }

            if (obj is ClassOrEnumType classOrTypeName)
            {
                objType = classOrTypeName.Type;
                obj = null;
                return StaticBindingFlag;
            }
            else
            {
                objType = obj.GetType();
                return InstanceBindingFlag;
            }
        }

        private string GetScriptBetweenCurlyBrackets(string parentScript, ref int index)
        {
            var currentScript = string.Empty;
            var bracketCount = 1;
            for (; index < parentScript.Length; index++)
            {
                var internalStringMatch = stringBeginningRegex.Match(parentScript.Substring(index));
                var internalCharMatch = internalCharRegex.Match(parentScript.Substring(index));

                if (internalStringMatch.Success)
                {
                    var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(parentScript.Substring(index + internalStringMatch.Length), internalStringMatch);
                    currentScript += innerString;
                    index += innerString.Length - 1;
                }
                else if (internalCharMatch.Success)
                {
                    currentScript += internalCharMatch.Value;
                    index += internalCharMatch.Length - 1;
                }
                else
                {
                    var s = parentScript.Substring(index, 1);

                    if (s.Equals("{")) bracketCount++;

                    if (s.Equals("}"))
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                            break;
                    }

                    currentScript += s;
                }
            }

            if (bracketCount > 0)
                throw new Exception($"脚本中在 [{index}] 位置中 缺少{bracketCount} 个 '}}' 字符");


            return currentScript;
        }

        /// <summary>获取圆括号或其他不可混淆的括号之间的表达式列表</summary>
        private List<string> GetExpressionsParenthesized(string expression, ref int i, bool checkSeparator, string separator = ",", string startChar = "(", string endChar = ")")
        {
            var expressionsList = new List<string>();

            var currentExpression = string.Empty;
            var bracketCount = 1;
            for (; i < expression.Length; i++)
            {
                var subExpr = expression.Substring(i);
                var internalStringMatch = stringBeginningRegex.Match(subExpr);
                var internalCharMatch = internalCharRegex.Match(subExpr);

                if (OptionStringEvaluationActive && internalStringMatch.Success)
                {
                    var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expression.Substring(i + internalStringMatch.Length), internalStringMatch);
                    currentExpression += innerString;
                    i += innerString.Length - 1;
                }
                else if (OptionCharEvaluationActive && internalCharMatch.Success)
                {
                    currentExpression += internalCharMatch.Value;
                    i += internalCharMatch.Length - 1;
                }
                else
                {
                    var s = expression.Substring(i, 1);

                    if (s.Equals(startChar))
                    {
                        bracketCount++;
                    }
                    else if (s.Equals("("))
                    {
                        i++;
                        currentExpression += "(" + GetExpressionsParenthesized(expression, ref i, false, ",", "(", ")").SingleOrDefault() + ")";
                        continue;
                    }
                    else if (s.Equals("{"))
                    {
                        i++;
                        currentExpression += "{" + GetExpressionsParenthesized(expression, ref i, false, ",", "{", "}").SingleOrDefault() + "}";
                        continue;
                    }
                    else if (s.Equals("["))
                    {
                        i++;
                        currentExpression += "[" + GetExpressionsParenthesized(expression, ref i, false, ",", "[", "]").SingleOrDefault() + "]";
                        continue;
                    }

                    if (s.Equals(endChar))
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            if (!currentExpression.Trim().Equals(string.Empty))
                                expressionsList.Add(currentExpression);
                            break;
                        }
                    }

                    if (checkSeparator && s.Equals(separator) && bracketCount == 1)
                    {
                        expressionsList.Add(currentExpression);
                        currentExpression = string.Empty;
                    }
                    else
                    {
                        currentExpression += s;
                    }
                }
            }

            if (bracketCount > 0)
                throw new Exception($"[{expression}] 中缺少 [{bracketCount}] 个 字符 ['{endChar}'] ");


            return expressionsList;
        }

        /// <summary>去默认函数列表内寻找并执行对应的函数</summary>
        /// <returns>函数是否存在</returns>
        /// <param name="result">返回函数的结果</param>
        private bool DefaultFunctions(string name, List<string> args, out object result)
        {
            var functionExists = true;

            if (simpleDoubleMathFuncDictionary.TryGetValue(name, out var func))
            {
                result = func(Convert.ToDouble(Evaluate(args[0])));
            }
            else if (doubleDoubleMathFuncDictionary.TryGetValue(name, out var func2))
            {
                result = func2(Convert.ToDouble(Evaluate(args[0])), Convert.ToDouble(Evaluate(args[1])));
            }
            else if (complexStandardFuncDictionary.TryGetValue(name, out var complexFunc))
            {
                result = complexFunc(this, args);
            }
            else if (OptionEvaluateFunctionActive && name.Equals("Evaluate", StringComparisonForCasing))
            {
                result = Evaluate((string)Evaluate(args[0]));
            }
            else if (OptionScriptEvaluateFunctionActive && name.Equals("ScriptEvaluate", StringComparisonForCasing))
            {
                result = ScriptEvaluate((string)Evaluate(args[0]));
            }
            else
            {
                result = null;
                functionExists = false;
            }

            return functionExists;
        }

        private Type GetTypeByFriendlyName(string typeName, string genericTypes = "", bool throwExceptionIfNotFound = false)
        {
            Type result = null;
            // var formatedGenericTypes = string.Empty;
            // var isCached = false;
            // try
            // {
            //     typeName = typeName.Trim();
            //     genericTypes = genericTypes.Trim();
            //
            //     if (primaryTypesDict.ContainsKey(OptionCaseSensitiveEvaluationActive ? typeName : typeName.ToLower()))
            //     {
            //         result = primaryTypesDict[OptionCaseSensitiveEvaluationActive ? typeName : typeName.ToLower()];
            //     }
            //
            //     if (CacheTypesResolutions && (TypesResolutionCaching?.ContainsKey(typeName + genericTypes) ?? false))
            //     {
            //         result = TypesResolutionCaching[typeName + genericTypes];
            //         isCached = true;
            //     }
            //
            //     if (result == null)
            //     {
            //         if (!genericTypes.Equals(string.Empty))
            //         {
            //             var types = GetConcreteTypes(genericTypes);
            //             formatedGenericTypes = $"`{types.Length}[{string.Join(", ", types.Select(type => "[" + type.AssemblyQualifiedName + "]"))}]";
            //         }
            //
            //         result = Type.GetType(typeName + formatedGenericTypes, false, !OptionCaseSensitiveEvaluationActive);
            //     }
            //
            //     if (result == null)
            //     {
            //         result = Types.ToList().Find(type => type.Name.Equals(typeName, StringComparisonForCasing) || type.FullName.StartsWith(typeName + ","));
            //     }
            //
            //     for (var a = 0; a < Assemblies.Count && result == null; a++)
            //     {
            //         if (typeName.Contains("."))
            //         {
            //             result = Type.GetType($"{typeName}{formatedGenericTypes},{Assemblies[a].FullName}", false, !OptionCaseSensitiveEvaluationActive);
            //         }
            //         else
            //         {
            //             for (var i = 0; i < Namespaces.Count && result == null; i++)
            //             {
            //                 result = Type.GetType($"{Namespaces[i]}.{typeName}{formatedGenericTypes},{Assemblies[a].FullName}", false, !OptionCaseSensitiveEvaluationActive);
            //             }
            //         }
            //     }
            // }
            // catch (EESyntaxErrorException exception)
            // {
            //     ExceptionDispatchInfo.Capture(exception).Throw();
            //     // Will not go here but need to return something to avoid compilation errors.
            //     return null;
            // }
            // catch
            // {
            //     /*ignored*/
            // }
            //
            // if (result != null && TypesToBlock.Contains(result))
            //     result = null;
            //
            // if (result == null && throwExceptionIfNotFound)
            //     throw new EESyntaxErrorException($"未知的类型或类 : {typeName}{genericTypes}");
            //
            // if (CacheTypesResolutions && (result != null) && !isCached)
            //     TypesResolutionCaching[typeName + genericTypes] = result;

            return result;
        }

        /// <summary>改变类型</summary>
        private static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            // 泛型类型 且 定义的泛型类型可以为 null
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return null;

                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (conversionType.IsEnum)
            {
                return Enum.ToObject(conversionType, value);
            }

            if (value.GetType().IsPrimitive && conversionType.IsPrimitive)
            {
                return primitiveExplicitCastMethodInfo.MakeGenericMethod(conversionType).Invoke(null, new object[] { value });
            }

            if (DynamicCast(value, conversionType, out var ret))
            {
                return ret;
            }

            return Convert.ChangeType(value, conversionType);
        }


        private static readonly MethodInfo primitiveExplicitCastMethodInfo = typeof(ExpressionEvaluator).GetMethod(nameof(PrimitiveExplicitCast), BindingFlags.Static | BindingFlags.NonPublic);

        private static object PrimitiveExplicitCast<T>(object value) => (T)value;

        private static bool DynamicCast(object source, Type destType, out object result)
        {
            var srcType = source.GetType();
            if (srcType == destType)
            {
                result = source;
                return true;
            }

            result = null;

            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var castOperator = destType.GetMethods(bindingFlags).Union(srcType.GetMethods(bindingFlags)).Where(methodInfo => methodInfo.Name == "op_Explicit" || methodInfo.Name == "op_Implicit").Where(methodInfo =>
            {
                var pars = methodInfo.GetParameters();
                return pars.Length == 1 && pars[0].ParameterType == srcType;
            }).FirstOrDefault(mi => mi.ReturnType == destType);

            if (castOperator != null)
                result = castOperator.Invoke(null, new object[] { source });
            else
                return false;

            return true;
        }

        private string GetCodeUntilEndOfString(string subExpr, Match stringBeginningMatch)
        {
            var stringBuilder = new StringBuilder();

            GetCodeUntilEndOfString(subExpr, stringBeginningMatch, ref stringBuilder);

            return stringBuilder.ToString();
        }

        private void GetCodeUntilEndOfString(string subExpr, Match stringBeginningMatch, ref StringBuilder stringBuilder)
        {
            var codeUntilEndOfStringMatch = stringBeginningMatch.Value.Contains("$") ? (stringBeginningMatch.Value.Contains("@") ? endOfStringWithDollarWithAt.Match(subExpr) : endOfStringWithDollar.Match(subExpr)) : (stringBeginningMatch.Value.Contains("@") ? endOfStringWithoutDollarWithAt.Match(subExpr) : endOfStringWithoutDollar.Match(subExpr));

            if (codeUntilEndOfStringMatch.Success)
            {
                if (codeUntilEndOfStringMatch.Value.EndsWith("\""))
                {
                    stringBuilder.Append(codeUntilEndOfStringMatch.Value);
                }
                else if (codeUntilEndOfStringMatch.Value.EndsWith("{") && codeUntilEndOfStringMatch.Length < subExpr.Length)
                {
                    if (subExpr[codeUntilEndOfStringMatch.Length] == '{')
                    {
                        stringBuilder.Append(codeUntilEndOfStringMatch.Value);
                        stringBuilder.Append("{");
                        GetCodeUntilEndOfString(subExpr.Substring(codeUntilEndOfStringMatch.Length + 1), stringBeginningMatch, ref stringBuilder);
                    }
                    else
                    {
                        var interpolation = GetCodeUntilEndOfStringInterpolation(subExpr.Substring(codeUntilEndOfStringMatch.Length));
                        stringBuilder.Append(codeUntilEndOfStringMatch.Value);
                        stringBuilder.Append(interpolation);
                        GetCodeUntilEndOfString(subExpr.Substring(codeUntilEndOfStringMatch.Length + interpolation.Length), stringBeginningMatch, ref stringBuilder);
                    }
                }
                else
                {
                    stringBuilder.Append(subExpr);
                }
            }
            else
            {
                stringBuilder.Append(subExpr);
            }
        }

        private string GetCodeUntilEndOfStringInterpolation(string subExpr)
        {
            var endOfStringInterpolationMatch = endOfStringInterpolationRegex.Match(subExpr);
            var result = subExpr;

            if (endOfStringInterpolationMatch.Success)
            {
                if (endOfStringInterpolationMatch.Value.EndsWith("}"))
                {
                    result = endOfStringInterpolationMatch.Value;
                }
                else
                {
                    var stringBeginningForEndBlockMatch = stringBeginningForEndBlockRegex.Match(endOfStringInterpolationMatch.Value);

                    var subString = GetCodeUntilEndOfString(subExpr.Substring(endOfStringInterpolationMatch.Length), stringBeginningForEndBlockMatch);

                    result = endOfStringInterpolationMatch.Value + subString + GetCodeUntilEndOfStringInterpolation(subExpr.Substring(endOfStringInterpolationMatch.Length + subString.Length));
                }
            }

            return result;
        }

        #endregion

        #region 用于解析和解释的受保护的工具子类

        /// <summary> 值类型嵌套跟踪 </summary>
        private class ValueTypeNestingTrace
        {
            public object Container { get; set; }

            public MemberInfo Member { get; set; }

            public object Value { get; set; }

            /// <summary> 赋值 </summary>
            public void AssignValue()
            {
                if (Container is ValueTypeNestingTrace valueTypeNestingTrace)
                {
                    var propertyInfo = Member as PropertyInfo;
                    propertyInfo?.SetValue(valueTypeNestingTrace.Value, Value);
                    valueTypeNestingTrace.AssignValue();
                }
                else
                {
                    var propertyInfo = Member as PropertyInfo;
                    propertyInfo?.SetValue(Container, Value);
                }
            }
        }

        /// <summary> 值类型嵌套跟踪 </summary>
        private class ValueTypeNestingTrace<TContainer, TValue>
        {
            public TContainer Container { get; set; }

            public MemberInfo Member { get; set; }

            public TValue Value { get; set; }

            /// <summary> 赋值 </summary>
            public void AssignValue()
            {
                if (Container is ValueTypeNestingTrace<TValue, TValue> valueTypeNestingTrace)
                {
                    var propertyInfo = Member as PropertyInfo;
                    propertyInfo?.SetValue(valueTypeNestingTrace.Value, Value);
                    valueTypeNestingTrace.AssignValue();
                }
                else
                {
                    var propertyInfo = Member as PropertyInfo;
                    propertyInfo?.SetValue(Container, Value);
                }
            }
        }

        /// <summary> 用于?语法糖的容器 </summary>
        private class NullConditionalNullValue { }

        private class ArgKeywordsEncaps
        {
            public int Index { get; set; }
            public string Keyword { get; set; }
            public string VariableName { get; set; }
        }

        private class DelegateEncaps
        {
            private readonly InternalDelegate lambda;

            private readonly MethodInfo methodInfo;

            private readonly object target;

            public DelegateEncaps(InternalDelegate lambda)
            {
                this.lambda = lambda;
            }

            public DelegateEncaps(object target, MethodInfo methodInfo)
            {
                this.target = target;
                this.methodInfo = methodInfo;
            }


            public object CallFluidMethod(params object[] args)
            {
                methodInfo.Invoke(target, args);
                return target;
            }

            public bool Predicate<T1>(T1 arg1)
            {
                return (bool)lambda(arg1);
            }

            public void Action0()
            {
                lambda();
            }

            public void Action1<T1>(T1 arg1)
            {
                lambda(arg1);
            }

            public void Action2<T1, T2>(T1 arg1, T2 arg2)
            {
                lambda(arg1, arg2);
            }

            public void Action3<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
            {
                lambda(arg1, arg2, arg3);
            }

            public void Action4<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                lambda(arg1, arg2, arg3, arg4);
            }

            public void Action5<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                lambda(arg1, arg2, arg3, arg4, arg5);
            }

            public void Action6<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6);
            }

            public void Action7<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            public void Action8<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            public void Action9<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }

            public void Action10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }

            public void Action11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }

            public void Action12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }

            public void Action13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }

            public void Action14<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }

            public void Action15<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            }

            public void Action16<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
            {
                lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
            }

            public TResult Func0<TResult>() => (TResult)lambda();

            public TResult Func1<T, TResult>(T arg) => (TResult)lambda(arg);

            public TResult Func2<T1, T2, TResult>(T1 arg1, T2 arg2)
            {
                return (TResult)lambda(arg1, arg2);
            }

            public TResult Func3<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3)
            {
                return (TResult)lambda(arg1, arg2, arg3);
            }

            public TResult Func4<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4);
            }

            public TResult Func5<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5);
            }

            public TResult Func6<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6);
            }

            public TResult Func7<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            public TResult Func8<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            public TResult Func9<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }

            public TResult Func10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }

            public TResult Func11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }

            public TResult Func12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }

            public TResult Func13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }

            public TResult Func14<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }

            public TResult Func15<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            }

            public TResult Func16<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
            {
                return (TResult)lambda(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
            }
        }

        #endregion
    }


    public class ClassOrEnumType
    {
        public Type Type { get; set; }
    }

    public class StronglyTypedVariable
    {
        public Type Type { get; set; }

        public object Value { get; set; }
    }

    public class SubExpression
    {
        public string Expression { get; set; }

        public SubExpression(string expression)
        {
            Expression = expression;
        }
    }

    /// <summary>表示一组方法，其中要调用的重载方法尚未确定。该类可以被用来模拟委托。</summary>
    public class MethodsGroupEncaps
    {
        /// <summary>定义该方法组的对象实例。</summary>
        public object ContainerObject { get; set; }

        /// <summary>一个方法信息（MethodInfo）数组，其中包含可能用于调用该方法组的重载方法</summary>
        public MethodInfo[] MethodsGroup { get; set; }
    }


    #region Operators

    /// <summary>用于解释的操作符</summary>
    public class ExpressionOperator : IEquatable<ExpressionOperator>
    {
        protected static uint indexer;
        protected uint OperatorValue { get; }

        protected ExpressionOperator()
        {
            indexer++;
            OperatorValue = indexer;
        }


        public static readonly ExpressionOperator Plus = new();
        public static readonly ExpressionOperator Minus = new();
        public static readonly ExpressionOperator UnaryPlus = new();
        public static readonly ExpressionOperator UnaryMinus = new();
        public static readonly ExpressionOperator Multiply = new();
        public static readonly ExpressionOperator Divide = new();
        public static readonly ExpressionOperator Modulo = new();
        public static readonly ExpressionOperator Lower = new();
        public static readonly ExpressionOperator Greater = new();
        public static readonly ExpressionOperator Equal = new();
        public static readonly ExpressionOperator LowerOrEqual = new();
        public static readonly ExpressionOperator GreaterOrEqual = new();
        public static readonly ExpressionOperator Is = new();
        public static readonly ExpressionOperator NotEqual = new();
        public static readonly ExpressionOperator LogicalNegation = new();
        public static readonly ExpressionOperator BitwiseComplement = new();
        public static readonly ExpressionOperator ConditionalAnd = new();
        public static readonly ExpressionOperator ConditionalOr = new();
        public static readonly ExpressionOperator LogicalAnd = new();
        public static readonly ExpressionOperator LogicalOr = new();
        public static readonly ExpressionOperator LogicalXor = new();
        public static readonly ExpressionOperator ShiftBitsLeft = new();
        public static readonly ExpressionOperator ShiftBitsRight = new();
        public static readonly ExpressionOperator NullCoalescing = new();
        public static readonly ExpressionOperator Cast = new();

        public override bool Equals(object obj)
        {
            return obj is ExpressionOperator otherOperator ? Equals(otherOperator) : OperatorValue.Equals(obj);
        }

        public bool Equals(ExpressionOperator otherOperator)
        {
            return otherOperator != null && OperatorValue == otherOperator.OperatorValue;
        }

        public override int GetHashCode() => OperatorValue.GetHashCode();
    }

    #endregion

    #region Enums

    /// <summary>用于定义当<see cref="ExpressionEvaluator.ScriptEvaluate"/>没有找到<c>return</c>关键字时的行为</summary>
    /// <remarks>用于<see cref="ExpressionEvaluator.NoReturnKeywordMode"/></remarks>
    public enum EOnNoReturnKeywordMode
    {
        /// <summary>自动返回最后一个计算的表达式</summary>
        ReturnLastResult,

        /// <summary>返回<c>null</c></summary>
        ReturnNull,

        /// <summary>抛出SyntaxException异常</summary>
        ThrowSyntaxException
    }

    /// <summary>内联命名空间的解释规则</summary>
    /// <remarks>用于<see cref="ExpressionEvaluator.OptionEInlineNamespacesRule"/></remarks>
    public enum EInlineNamespacesRule
    {
        /// <summary>允许使用内存中的任何内联命名空间</summary>
        AllowAll,

        /// <summary>只允许使用在<see cref="ExpressionEvaluator.InlineNamespacesList"/>中定义的内联命名空间</summary>
        AllowOnlyInlineNamespacesList,

        /// <summary>禁止使用任何内联命名空间</summary>
        BlockAll
    }

    #endregion

    #region Exceptions

    /// <summary>用于封装在表达式子部分中发生的异常，以便在表达式求值需要继续执行时，将异常传递到更高层次的调用栈中。</summary>
    public class BubbleExceptionContainer
    {
        //private readonly ExceptionDispatchInfo _dispatchInfo;

        private readonly Exception _exception;

        /// <summary>构造器</summary>
        /// <param name="exception">需要封装的异常</param>
        public BubbleExceptionContainer(Exception exception)
        {
            _exception = exception;
            //_dispatchInfo = ExceptionDispatchInfo.Capture(exception);
        }

        /// <summary>重新抛出已捕获的异常</summary>
        public void Throw() => throw _exception;
    }

    /// <summary>解释器执行时语法错误的异常</summary>
    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message) : base(message) { }

        public SyntaxErrorException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>解释器执行时安全性的异常</summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }

        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion

    #region EventArgs

    /// <summary>关于已经/将要被求值的表达式的信息</summary>
    /// <remarks>用于<see cref="ExpressionEvaluator.ExpressionEvaluating"/>事件</remarks>
    public class ExpressionEvaluationEventArg : EventArgs
    {
        /// <summary>将用于计算表达式的求值器</summary>
        public ExpressionEvaluator Evaluator { get; set; }

        /// <summary>被解释的表达式,可以被修改</summary>
        public string Expression { get; set; }

        /// <summary>设置求值的返回值</summary>
        /// 用于 <see cref="ExpressionEvaluator.ExpressionEvaluated"/> 事件, 存储计算结果
        public object Value
        {
            get => value;
            set
            {
                this.value = value;
                HasValue = true;
            }
        }

        /// <summary>真: 表达式已经被求值, 假: 表达式还未被求值</summary>
        public bool HasValue { get; set; }

        private object value;

        /// <summary>构造器</summary>
        /// <param name="expression">要求值的表达式</param>
        /// <param name="evaluator">将用于计算表达式的求值器</param>
        /// <param name="value"></param>
        public ExpressionEvaluationEventArg(string expression, ExpressionEvaluator evaluator, object value = null)
        {
            Refresh(expression, evaluator, value);
        }

        public ExpressionEvaluationEventArg Refresh(string expression, ExpressionEvaluator evaluator, object _value = null)
        {
            Expression = expression;
            Evaluator = evaluator;
            value = _value;
            return this;
        }
    }

    /// <summary>当前解释器的变量、特性或属性的信息</summary>
    public class VariableEvaluationEventArg : EventArgs
    {
        /// <summary>被解释的变量名</summary>
        public string Name { get; set; }

        /// <summary>为该变量设置一个值</summary>
        public object Value
        {
            get => varValue;
            set
            {
                varValue = value;
                HasValue = true;
            }
        }

        /// <summary>变量是否有值</summary>
        public bool HasValue { get; set; }

        /// <summary>在动态实例属性定义的情况下，调用此属性的对象的实例。<para/>否则设置为null。</summary>
        public object This { get; set; }

        /// <summary>当前解释器的引用</summary>
        public ExpressionEvaluator Evaluator { get; set; }

        /// <summary>是否是泛型类型</summary>
        public bool HasGenericTypes => !string.IsNullOrEmpty(genericTypes);

        /// <summary>在指定了泛型类型的情况下，计算所有类型并返回类型数组</summary>
        public Type[] EvaluateGenericTypes() => evaluateGenericTypes?.Invoke(genericTypes) ?? Type.EmptyTypes;


        private Func<string, Type[]> evaluateGenericTypes;
        private string genericTypes;
        private object varValue;

        /// <summary>构造器</summary>
        /// <param name="name">被解释的变量名</param>
        /// <param name="evaluator">被查找的解释器</param>
        /// <param name="onInstance">要在其上计算字段或属性的对象实例(赋值给<see cref="This"/>)</param>
        /// <param name="genericTypes">在属性访问时指定的泛型类型</param>
        /// <param name="evaluateGenericTypes">用于解释A func to evaluate the list of specific types given between &lt; and &gt;</param>
        public VariableEvaluationEventArg(string name, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null)
        {
            Initialization(name, evaluator, onInstance, genericTypes, evaluateGenericTypes);
        }

        public VariableEvaluationEventArg Initialization(string name, ExpressionEvaluator evaluator = null, object onInstance = null, string _genericTypes = null, Func<string, Type[]> _evaluateGenericTypes = null)
        {
            Name = name;
            This = onInstance;
            Evaluator = evaluator;
            genericTypes = _genericTypes;
            evaluateGenericTypes = _evaluateGenericTypes;
            return this;
        }
    }

    /// <summary>关于当前求值的函数或方法的信息</summary>
    public class FunctionEvaluationEventArg : EventArgs
    {
        /// <summary>构造器</summary>
        /// <param name="name">函数或者方法的名字</param>
        /// <param name="args">传递给函数或方法的参数</param>
        /// <param name="evaluator"><see cref="ExpressionEvaluator"/>检测要求值的函数或方法</param>
        /// <param name="onInstance">要对方法求值的对象实例 (赋值给 <see cref="This"/>)</param>
        /// <param name="genericTypes">调用函数时指定的泛型类型</param>
        /// <param name="evaluateGenericTypes">用于解释泛型类型的函数</param>
        public FunctionEvaluationEventArg(string name, List<string> args = null, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null)
        {
            Initialization(name, args, evaluator, onInstance, genericTypes, evaluateGenericTypes);
        }

        public FunctionEvaluationEventArg Initialization(string name, List<string> args = null, ExpressionEvaluator evaluator = null, object onInstance = null, string _genericTypes = null, Func<string, Type[]> _evaluateGenericTypes = null)
        {
            Name = name;
            Args = args ?? new List<string>();
            This = onInstance;
            Evaluator = evaluator;
            genericTypes = _genericTypes;
            evaluateGenericTypes = _evaluateGenericTypes;
            return this;
        }

        /// <summary>未被解释的函数或方法的参数</summary>
        public List<string> Args { get; set; }

        /// <summary>解释的函数或方法的名字</summary>
        public string Name { get; set; }

        /// <summary>获取函数的参数值</summary>
        public object[] EvaluateArgs() => Args.ConvertAll(arg => Evaluator.Evaluate(arg)).ToArray();

        /// <summary>在指定的索引处获取函数的参数值</summary>
        /// <param name="index">要求值的函数参数的索引</param>
        /// <returns>求值的参数</returns>
        public object EvaluateArg(int index) => Evaluator.Evaluate(Args[index]);

        /// <summary>获取指定索引处的函数参数值</summary>
        /// <typeparam name="T">想要得到结果的类型(强转)</typeparam>
        /// <param name="index">要计算的函数参数的索引</param>
        /// <returns>在指定类型中转换的计算参数</returns>
        public T EvaluateArg<T>(int index) => Evaluator.Evaluate<T>(Args[index]);


        /// <summary>用于设置函数的返回值</summary>
        public object Value
        {
            get => returnValue;
            set
            {
                returnValue = value;
                FunctionReturnedValue = true;
            }
        }

        /// <summary>函数是否返回了值</summary>
        public bool FunctionReturnedValue { get; set; }

        /// <summary>在动态实例方法定义的情况下，调用该方法(函数)的对象的实例。<para/>否则设置为空。</summary>
        public object This { get; set; }

        /// <summary>当前解释器的引用</summary>
        public ExpressionEvaluator Evaluator { get; set; }

        /// <summary>是否为泛型类型</summary>
        public bool HasGenericTypes => !string.IsNullOrEmpty(genericTypes);

        /// <summary>求出所有的泛型类型并返回类型数组</summary>
        public Type[] EvaluateGenericTypes() => evaluateGenericTypes?.Invoke(genericTypes) ?? Type.EmptyTypes;


        private Func<string, Type[]> evaluateGenericTypes;
        private string genericTypes;
        private object returnValue;
    }

    /// <summary>当前计算的索引的信息</summary>
    public class IndexingPreEvaluationEventArg : EventArgs
    {
        /// <summary>构造器</summary>
        /// <param name="args">索引的未计算参数</param>
        /// <param name="evaluator">当前解释器的应用</param>
        /// <param name="onInstance">调用索引的对象的实例。<para/>会变为<see cref="This"/> 属性</param>
        public IndexingPreEvaluationEventArg(List<string> args, ExpressionEvaluator evaluator, object onInstance)
        {
            Initialization(args, evaluator, onInstance);
        }

        public IndexingPreEvaluationEventArg Initialization(List<string> args, ExpressionEvaluator evaluator, object onInstance)
        {
            Args = args;
            This = onInstance;
            Evaluator = evaluator;
            return this;
        }

        /// <summary>未被解释的索引参数</summary>
        public List<string> Args { get; set; }

        /// <summary>调用索引的对象的实例</summary>
        public object This { get; set; }

        private object returnValue;

        /// <summary>要设置索引的结果值</summary>
        public object Value
        {
            get => returnValue;
            set
            {
                returnValue = value;
                HasValue = true;
            }
        }

        /// <summary>
        /// <c>true </c> 已经完成索引求值<para/>
        /// <c>false</c> 索引不存在     <para/>
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>对当前表达式求值器的引用</summary>
        public ExpressionEvaluator Evaluator { get; set; }

        /// <summary>获取所有参数的值</summary>
        public object[] EvaluateArgs() => Args.ConvertAll(arg => Evaluator.Evaluate(arg)).ToArray();

        /// <summary>获取指定索引处索引参数的值</summary>
        public object EvaluateArg(int index) => Evaluator.Evaluate(Args[index]);

        /// <summary>获取指定索引处索引参数的值</summary>
        /// <param name="index">要求值的索引参数的索引</param>
        /// <typeparam name="T">要获得的结果的类型. (进行一次强转)</typeparam>
        /// <returns>在指定类型中强制转换的求值参数</returns>
        public T EvaluateArg<T>(int index) => Evaluator.Evaluate<T>(Args[index]);

        /// <summary>设置 <c>true</c> 取消当前函数或方法的求值，并抛出函数不存在的异常</summary>
        public bool CancelEvaluation { get; set; }
    }

    /// <summary>参数转换求值事件参数的类 </summary>
    public class ParameterCastEvaluationEventArg : EventArgs
    {
        /// <summary>尝试调用的方法数据 </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// 在动态实例方法定义的情况下，调用该方法(函数)的对象的实例。<para/>
        /// 否则设置为空。<para/>
        /// </summary>
        public object This { get; set; }

        /// <summary>指向当前表达式计算器的引用 </summary>
        public ExpressionEvaluator Evaluator { get; set; }

        /// <summary> 参数的必需类型 </summary>
        public Type ParameterType { get; set; }

        /// <summary> 修改前的原始参数 </summary>
        public object OriginalArg { get; set; }

        /// <summary> 参数的位置(从0开始的索引) </summary>
        public int ArgPosition { get; set; }

        /// <summary>构造器</summary>
        /// <param name="methodInfo"> 尝试调用的方法数据 </param>
        /// <param name="parameterType"> 参数的必需类型 </param>
        /// <param name="originalArg"> 修改前的原始参数 </param>
        /// <param name="argPosition"> 参数的位置(从0开始的索引) </param>
        /// <param name="evaluator"> 当前表达式计算器的引用</param>
        /// <param name="this">此方法被动态对象调用的实例,等价于This关键字</param>
        public ParameterCastEvaluationEventArg(MethodInfo methodInfo, Type parameterType, object originalArg, int argPosition, ExpressionEvaluator evaluator = null, object @this = null)
        {
            Initialization(methodInfo, parameterType, originalArg, argPosition, evaluator, @this);
        }

        public ParameterCastEvaluationEventArg Initialization(MethodInfo methodInfo, Type parameterType, object originalArg, int argPosition, ExpressionEvaluator evaluator = null, object @this = null)
        {
            MethodInfo = methodInfo;
            ParameterType = parameterType;
            OriginalArg = originalArg;
            Evaluator = evaluator;
            This = @this;
            ArgPosition = argPosition;

            return this;
        }


        /// <summary>设置修改后的参数</summary>
        public object Argument
        {
            get => modifiedArgument;
            set
            {
                modifiedArgument = value;
                FunctionModifiedArgument = true; //只要被设置后，就会变成true，即使他是null
            }
        }

        /// <summary>
        /// 真:参数已被修改<para/>
        /// 假:这意味着参数不能是给定的类型.<para/>
        /// </summary>
        public bool FunctionModifiedArgument { get; set; }

        private object modifiedArgument;
    }

    /// <summary>当前计算的变量、属性或属性的信息</summary>
    public class VariablePreEvaluationEventArg : VariableEvaluationEventArg
    {
        public VariablePreEvaluationEventArg(string name, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null) : base(name, evaluator, onInstance, genericTypes, evaluateGenericTypes) { }

        public new VariablePreEvaluationEventArg Initialization(string name, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null)
        {
            base.Initialization(name, evaluator, onInstance, genericTypes, evaluateGenericTypes);
            CancelEvaluation = false;
            return this;
        }

        /// <summary>如果设置为true，则取消当前变量、字段或属性的求值，并抛出不存在的异常</summary>
        public bool CancelEvaluation { get; set; }
    }

    /// <summary>当前计算的函数或方法的信息</summary>
    public class FunctionPreEvaluationEventArg : FunctionEvaluationEventArg
    {
        public FunctionPreEvaluationEventArg(string name, List<string> args = null, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null) : base(name, args, evaluator, onInstance, genericTypes, evaluateGenericTypes) { }

        public new FunctionPreEvaluationEventArg Initialization(string name, List<string> args = null, ExpressionEvaluator evaluator = null, object onInstance = null, string genericTypes = null, Func<string, Type[]> evaluateGenericTypes = null)
        {
            base.Initialization(name, args, evaluator, onInstance, genericTypes, evaluateGenericTypes);
            CancelEvaluation = false;
            return this;
        }

        /// <summary>如果设置为true，则取消当前函数或方法的求值，并抛出该函数不存在的异常</summary>
        public bool CancelEvaluation { get; set; }
    }

    #endregion

    #region Helper

    public static class ExpressionEvaluatorHelper
    {
        #region Remove comments

        private static readonly Regex removeCommentsRegex = new($"{blockComments}|{lineComments}|{stringsIgnore}|{verbatimStringsIgnore}", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex newLineCharsRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);

        private const string verbatimStringsIgnore = @"@(""[^""]*"")+"; //language=regex
        private const string stringsIgnore = @"""((\\[^\n]|[^""\n])*)"""; //language=regex
        private const string blockComments = @"/\*(.*?)\*/"; //language=regex
        private const string lineComments = @"//[^\r\n]*"; //language=regex


        /// <summary>移除指定C#脚本的所有行和块注释</summary>
        /// <param name="scriptWithComments">含有注释的C#代码</param>
        /// <remarks>基于 : https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689</remarks>
        /// <returns> 移除掉注释的C#代码 </returns>
        public static string RemoveComments(string scriptWithComments)
        {
            return removeCommentsRegex.Replace(scriptWithComments, Evaluator);

            string Evaluator(Match match)
            {
                if (match.Value.StartsWith("/"))
                {
                    var newLineCharsMatch = newLineCharsRegex.Match(match.Value);

                    return match.Value.StartsWith("/*") && newLineCharsMatch.Success ? newLineCharsMatch.Value : " ";
                }

                return match.Value;
            }
        }

        #endregion
    }

    #endregion
}