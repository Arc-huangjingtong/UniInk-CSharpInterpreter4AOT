/*****************************************************************************************************************
 *    Title     : UniInk (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)
 *    Version   : 1.0.0
 *    Author    : Arc
 *    Licence   : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)
 *    Origin    : UniInk(https://github.com/codingseb/UniInk)
/*****************************************************************************************************************/

namespace Arc.UniInk
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;


    /// <summary>表达式求值器:简单的解释器，用于解释符串表达式/脚本</summary>
    public class UniInk
    {
        #region Regex

        /// https://regex101.com/r/0PN0yS/1
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
        protected static readonly Regex varOrFunctionRegEx = new(@"^((?<sign>[+-])|(?<prefixOperator>[+][+]|--)|(?<varKeyword>var)\s+|((?<nullConditional>[?])?(?<inObject>\.))?)(?<name>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)((?<assignationOperator>(?<assignmentPrefix>[+\-*/%&|^]|<<|>>|\?\?)?=(?![=>]))|(?<postfixOperator>([+][+]|--)(?![\p{L}_0-9]))|((?<isgeneric>[<](?>([\p{L}_](?>[\p{L}_0-9]*)|(?>\s+)|[,])+)*[>])?(?<isfunction>[(])?))", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        /// 匹配脚本中的数字
        /// sign: 匹配数字字面量前面的符号，可以是加号或减号
        /// hasDecimal: 匹配数字字面量是否包含小数点
        /// type: 匹配数字字面量的类型后缀，可以是u、l、d、f、m、ul 等
        /// 支持[1111.2f],[+123],[-122.2m]等写法，放弃支持 下划线[33_000], 科学计数[34.e+23] 等其他写法
        protected static readonly Regex numberRegex = new(@"^(?<sign>[+-])?([\d]+)(?<hasdecimal>[\.]?([\d]+))?(?<type>ul|[fdulm])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        //匹配 字符串前面的$或者@符号或者“双引号
        protected static readonly Regex stringBeginRegex = new("^(?<interpolated>[$])?(?<escaped>[@])?[\"]", RegexOptions.Compiled);

        //匹配 单字符中的转义和非转移字符
        private static readonly Regex internalCharRegex = new(@"^['](\\[\\'0abfnrtv]|[^'])[']", RegexOptions.Compiled);

        //匹配 数组或者二位数组
        private static readonly Regex arrayTypeDetectionRegex = new(@"^(\s*(\[(?>(?>\s+)|[,])*)\])+", RegexOptions.Compiled);

        //匹配 泛型
        private static readonly Regex genericsDecodeRegex = new("(?<name>[^,<>]+)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?", RegexOptions.Compiled);


        //匹配 $标记的字符串末尾
        protected static readonly Regex endOfStringWithDollar = new("^([^\"{\\\\]|\\\\[\\\\\"0abfnrtv])*[\"{]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithoutDollar = new("^([^\"\\\\]|\\\\[\\\\\"0abfnrtv])*[\"]", RegexOptions.Compiled);
        protected static readonly Regex endOfStringWithDollarWithAt = new("^[^\"{]*[\"{]", RegexOptions.Compiled);

        protected static readonly Regex endOfStringWithoutDollarWithAt = new("^[^\"]*[\"]", RegexOptions.Compiled);

        //
        protected static readonly Regex endOfStringInterpolationRegex = new("^('\"'|[^}\"])*[}\"]", RegexOptions.Compiled);
        protected static readonly Regex stringBeginningForEndBlockRegex = new("[$]?[@]?[\"]$", RegexOptions.Compiled);
        protected static readonly Regex lambdaExpressionRegex = new(@"^(?>\s*)(?<args>((?>\s*)[(](?>\s*)([\p{L}_](?>[\p{L}_0-9]*)(?>\s*)([,](?>\s*)[\p{L}_][\p{L}_0-9]*(?>\s*))*)?[)])|[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)=>(?<expression>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        protected static readonly Regex lambdaArgRegex = new(@"[\p{L}_](?>[\p{L}_0-9]*)", RegexOptions.Compiled);

        protected static readonly Regex functionArgKeywordsRegex = new(@"^\s*(?<keyword>out|ref|in)\s+((?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)\s+(?=[\p{L}_]))?(?<toEval>(?<varName>[\p{L}_](?>[\p{L}_0-9]*))\s*(=.*)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static readonly Regex castRegex = new(@"^\((?>\s*)(?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)(?>\s*)\)", RegexOptions.Compiled);


        // 仅限于脚本模式下
        /// 匹配while||for||foreach||if||else||catch(后面跟括号的)关键字
        private static readonly Regex blockKeywordBeginRegex = new(@"^(?>\s*)(?<keyword>while|for|foreach|if|else(?>\s*)if|catch)(?>\s*)[(]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配else||do||try||finally(后面不更括号的)关键字
        private static readonly Regex blockKeywordBeginRegex_NoParentheses = new(@"^(?>\s*)(?<keyword>else|do|try|finally)(?![\p{L}_0-9])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配花括号的开始
        private static readonly Regex blockBeginRegex = new(@"^(?>\s*)[{]", RegexOptions.Compiled);

        /// 匹配foreach后的括号中的内容 xx in xx
        private static readonly Regex foreachParenThisEvaluationRegex = new(@"^(?>\s*)(?<variableName>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)(?<in>in)(?>\s*)(?<collection>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// 匹配return关键字
        private static readonly Regex returnKeywordRegex = new(@"^return((?>\s*)|\()", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// 匹配 ; 结束符
        private static readonly Regex nextIsEndOfExpressionRegex = new(@"^(?>\s*)[;]", RegexOptions.Compiled);

        #endregion

        #region Enums

        protected enum EBlockState_If { NoBlock, If, ElseIf }

        protected enum EBlockState_Try { NoBlock, Try, Catch }

        #endregion

        #region DictionaryData (Primary types, number suffix, escaped chars, operators management, default vars and functions)

        private static readonly Dictionary<string, Type> primaryTypesDic = new()
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

        /// 数字后缀字典  Always Case insensitive, like in C#
        private static readonly Dictionary<string, Func<string, object>> numberSuffixToParse = new()
        {
            { "f", number => float.Parse(number) },
            { "d", number => double.Parse(number) },
            { "u", number => uint.Parse(number) },
            { "l", number => long.Parse(number) },
            { "ul", number => ulong.Parse(number) },
            { "m", number => decimal.Parse(number) }
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
        public readonly Dictionary<string, ExpressionOperator> operatorsDictionary = new(StringComparer.Ordinal)
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

        /// 一元右操作符
        protected static readonly List<ExpressionOperator> UnaryPostfixOperators = new()
        {
            ExpressionOperator.LogicalNegation, // !a 逻辑取反
            ExpressionOperator.BitwiseComplement, // ~a 位运算取反
            ExpressionOperator.UnaryPlus, // +a 一元加号,表示正数符号
            ExpressionOperator.UnaryMinus // -a 一元减号,表示负数符号
        };

        /// 二元操作符计算逻辑
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
                    if (right is string rightStr && left is string leftStr)
                    {
                        return leftStr + rightStr;
                    }

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
                        return null;
                    }

                    if (!(bool)left) return false;

                    if (right is BubbleExceptionContainer rightExceptionContainer)
                    {
                        rightExceptionContainer.Throw();
                        return null;
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
                        return null;
                    }

                    if ((bool)left) return true;

                    if (right is BubbleExceptionContainer rightExceptionContainer)
                    {
                        rightExceptionContainer.Throw();
                        return null;
                    }

                    return (bool)left || (bool)right; // 条件或
                }
            },
            { ExpressionOperator.NullCoalescing, (left, right) => left ?? right }, // 空合并
        };

        /// <summary>默认变量</summary>
        private readonly Dictionary<string, object> defaultVariables = new()
        {
            { "Pi", Math.PI },
            { "E", Math.E },
            { "null", null },
            { "true", true },
            { "false", false },
            { "this", null }
        };

        ///简单的浮点数计算函数
        private readonly Dictionary<string, Func<double, double>> simpleDoubleMathFuncDictionary = new()
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


        ///复杂的基本函数
        private readonly Dictionary<string, Func<UniInk, List<string>, object>> complexStandardFuncDictionary = new()
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
            { "Avg", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Sum() / args.Count },
            {
                "default", (self, args) =>
                {
                    var argValue = self.Evaluate(args[0]);

                    if (argValue is ClassOrEnumType classOrTypeName)
                        return Activator.CreateInstance(classOrTypeName.Type);

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

                    if (args.Count == 2)
                    {
                        var arg2 = self.Evaluate(args[1]);

                        if (arg2 is MidpointRounding midpointRounding)
                            return Math.Round(Convert.ToDouble(self.Evaluate(args[0])), midpointRounding);

                        return Math.Round(Convert.ToDouble(self.Evaluate(args[0])), Convert.ToInt32(arg2));
                    }

                    if (args.Count == 1) { return Math.Round(Convert.ToDouble(self.Evaluate(args[0]))); }

                    throw new ArgumentException();
                }
            },
            { "Sign", (self, args) => Math.Sign(Convert.ToDouble(self.Evaluate(args[0]))) }
        };

        #endregion

        #region Caches

        /// <summary> 用于类型解析的共享缓存 </summary>
        public static readonly Dictionary<string, Type> CachedTypesDic = new();

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
        public List<string> Namespaces { get; set; } = new()
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Collections",
            "System.Collections.Generic"
        };

        /// <summary>添加或删除要在表达式中管理的特定类型。</summary>
        public List<Type> Types { get; } = new();

        /// <summary>用于查找扩展方法的静态类型列表</summary>
        public IList<Type> StaticTypesForExtensionsMethods { get; } = new List<Type>
        {
            typeof(Enumerable) // 用于Linq扩展方法
        };

        #endregion

        #region Reflection Flags

        private static BindingFlags InstanceBindingFlag => BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Static;
        private static BindingFlags StaticBindingFlag => BindingFlags.Public | BindingFlags.Static;

        #endregion

        #region 自定义和动态求值

        /// <summary>计算堆栈初始化次数，以确定是否到达了表达式入口点。在这种情况下，应该抛出传输的异常。</summary>
        private int evaluationStackCount;

        /// <summary>如果设置了，该对象将使用它的字段、属性和方法作为全局变量和函数</summary>
        public object Context
        {
            get => defaultVariables["this"];
            set => defaultVariables["this"] = value;
        }

        /// <summary>
        /// 的当前实例计算的表达式和脚本中可以使用的变量名/值字典 <see cref="UniInk"/><para/>
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();


        /// <summary>在函数或方法解析之前触发。</summary>
        /// 允许动态定义函数或方法及其对应的值。<para/>
        /// 允许取消对该函数的评估（将其视为不存在）。<para/>
        public event EventHandler<FunctionEvaluationEventArg> PreEvaluateFunction;

        #endregion

        #region 构造函数和可重写的init方法

        /// <summary>默认构造器</summary>
        public UniInk() { }

        /// <summary>带有初始化变量的构造函数</summary>
        /// <param name="variables">解释器中可以使用的变量</param>
        public UniInk(Dictionary<string, object> variables) : this() => Variables = variables;

        /// <summary>具有上下文初始化的构造函数</summary>
        /// <param name="context">提出它的字段、属性和方法的上下文</param>
        public UniInk(object context) : this() => Context = context;

        /// <summary>具有变量和上下文初始化的构造函数</summary>
        /// <param name="context">提出它的字段、属性和方法的上下文</param>
        /// <param name="variables">解释器中可以使用的变量</param>
        public UniInk(object context, Dictionary<string, object> variables) : this()
        {
            Context = context;
            Variables = variables;
        }

        #endregion

        #region 主要的求值方法(Expressions and scripts)

        #region 脚本

        /// <summary>解释脚本(用分号分隔的多个表达式),支持一些条件、循环等c#代码流管理关键字</summary>
        /// <typeparam name="T">要对表达式的结果进行强制转换的类型</typeparam>
        /// <param name="script">求值的脚本</param>
        /// <returns>最后一次求值表达式的结果</returns>
        public T ScriptEvaluate<T>(string script) => (T)ScriptEvaluate(script);

        /// <summary>解释脚本(用分号分隔的多个表达式),支持一些条件、循环等c#代码流管理关键字</summary>
        /// <param name="script">需要求值的脚本字符串</param>
        /// <returns>最后一次求值表达式的结果</returns>
        public object ScriptEvaluate(string script)
        {
            var isReturn = false;
            var isBreak = false;
            var isContinue = false;

            var result = ScriptEvaluate(script, ref isReturn, ref isBreak, ref isContinue);

            if (isBreak) throw new SyntaxException("无效关键字:[break]");
            if (isContinue) throw new SyntaxException("无效关键字:[continue]");
            return result;
        }

        private object ScriptEvaluate(string script, ref bool valueReturned, ref bool breakCalled, ref bool continueCalled)
        {
            object lastResult = null;
            var isReturn = valueReturned;
            var isBreak = breakCalled;
            var isContinue = continueCalled;
            var BlockState_If = EBlockState_If.NoBlock;
            var BlockState_Try = EBlockState_Try.NoBlock;
            var ifElseStatementsList = new List<List<string>>();
            var tryStatementsList = new List<List<string>>();

            script = script.TrimEnd(); //修剪末尾的空格

            var result = (object)null;

            var startIndex = 0;
            var endIndex = 0;

            //处理代码块关键字,直到遇到第一个表达式
            while (!isReturn && !isBreak && !isContinue && endIndex < script.Length)
            {
                var blockKeywordsBeginMatch_NoParentheses = blockKeywordBeginRegex_NoParentheses.Match(script.Substring(endIndex));
                var blockKeywordsBeginMatch = blockKeywordBeginRegex.Match(script.Substring(endIndex));
                var str = script.Substring(startIndex, endIndex - startIndex);


                if (string.IsNullOrWhiteSpace(str) && (blockKeywordsBeginMatch.Success || blockKeywordsBeginMatch_NoParentheses.Success))
                {
                    endIndex += blockKeywordsBeginMatch.Success ? blockKeywordsBeginMatch.Length : blockKeywordsBeginMatch_NoParentheses.Length;
                    var keyword = blockKeywordsBeginMatch.Success ? blockKeywordsBeginMatch.Groups["keyword"].Value.Replace(" ", "").Replace("\t", "") : (blockKeywordsBeginMatch_NoParentheses?.Groups["keyword"].Value ?? string.Empty);
                    var keywordAttributes = blockKeywordsBeginMatch.Success ? GetExpressionsParenthesized(script, ref endIndex, true, ';') : null;

                    if (blockKeywordsBeginMatch.Success) endIndex++;

                    var blockBeginningMatch = blockBeginRegex.Match(script.Substring(endIndex));

                    var subScript = string.Empty;

                    if (blockBeginningMatch.Success)
                    {
                        endIndex += blockBeginningMatch.Length;

                        subScript = GetScriptBetweenCurlyBrackets(script, ref endIndex);

                        endIndex++;
                    }
                    else
                    {
                        var continueExpressionParsing = true;
                        startIndex = endIndex;

                        while (endIndex < script.Length && continueExpressionParsing)
                        {
                            if (TryParseStringAndParenthisAndCurlyBrackets(ref endIndex)) { }
                            else if (script.Length - endIndex > 2 && script.Substring(endIndex, 3).Equals("';'"))
                            {
                                endIndex += 2;
                            }
                            else if (script[endIndex] == ';')
                            {
                                subScript = script.Substring(startIndex, endIndex + 1 - startIndex);
                                continueExpressionParsing = false;
                            }

                            endIndex++;
                        }

                        if (string.IsNullOrWhiteSpace(subScript)) throw new SyntaxException($"[{keyword}] 语句后无指令");
                    }

                    if (keyword.Equals("elseif"))
                    {
                        if (BlockState_If == EBlockState_If.NoBlock)
                            throw new SyntaxException("[else if] 没有对应的 [if]");

                        ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                        BlockState_If = EBlockState_If.ElseIf;
                    }
                    else if (keyword.Equals("else"))
                    {
                        if (BlockState_If == EBlockState_If.NoBlock)
                            throw new SyntaxException("[else] 没有对应的 [if]");


                        ifElseStatementsList.Add(new List<string> { "true", subScript });
                        BlockState_If = EBlockState_If.NoBlock;
                    }
                    else if (keyword.Equals("catch"))
                    {
                        if (BlockState_Try == EBlockState_Try.NoBlock)
                            throw new SyntaxException(" [catch] 没有对应的  [try] ");

                        tryStatementsList.Add(new List<string> { "catch", keywordAttributes.Count > 0 ? keywordAttributes[0] : null, subScript });
                        BlockState_Try = EBlockState_Try.Catch;
                    }
                    else if (keyword.Equals("finally"))
                    {
                        if (BlockState_Try == EBlockState_Try.NoBlock)
                            throw new SyntaxException(" [finally] 没有对应的  [try]");

                        tryStatementsList.Add(new List<string> { "finally", subScript });
                        BlockState_Try = EBlockState_Try.NoBlock;
                    }
                    else
                    {
                        ExecuteTryList();
                        ExecuteIfList();

                        if (keyword.Equals("if"))
                        {
                            ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                            BlockState_If = EBlockState_If.If;
                            BlockState_Try = EBlockState_Try.NoBlock;
                        }
                        else if (keyword.Equals("try"))
                        {
                            tryStatementsList.Add(new List<string> { subScript });
                            BlockState_If = EBlockState_If.NoBlock;
                            BlockState_Try = EBlockState_Try.Try;
                        }
                        else if (keyword.Equals("do"))
                        {
                            if ((blockKeywordsBeginMatch = blockKeywordBeginRegex.Match(script.Substring(endIndex))).Success && blockKeywordsBeginMatch.Groups["keyword"].Value.Equals("while"))
                            {
                                endIndex += blockKeywordsBeginMatch.Length;
                                keywordAttributes = GetExpressionsParenthesized(script, ref endIndex, true, ';');

                                endIndex++;

                                Match nextIsEndOfExpressionMatch;

                                if ((nextIsEndOfExpressionMatch = nextIsEndOfExpressionRegex.Match(script.Substring(endIndex))).Success)
                                {
                                    endIndex += nextIsEndOfExpressionMatch.Length;

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
                                    throw new SyntaxException("A [;] character is missing. (After the do while condition)");
                                }
                            }
                            else
                            {
                                throw new SyntaxException("No [while] keyword after the [do] keyword and block");
                            }
                        }
                        else if (keyword.Equals("while"))
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
                        else if (keyword.Equals("for"))
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
                        else if (keyword.Equals("foreach"))
                        {
                            var foreachParenthisEvaluationMatch = foreachParenThisEvaluationRegex.Match(keywordAttributes[0]);

                            if (!foreachParenthisEvaluationMatch.Success)
                            {
                                throw new SyntaxException("foreach相关语法错误");
                            }

                            if (!foreachParenthisEvaluationMatch.Groups["in"].Value.Equals("in"))
                            {
                                throw new SyntaxException("foreach中未找到 [in] 关键字");
                            }

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

                    startIndex = endIndex;
                }
                else
                {
                    ExecuteTryList();
                    ExecuteIfList();

                    if (TryParseStringAndParenthisAndCurlyBrackets(ref endIndex)) { }
                    else if (script.Length - endIndex > 2 && script.Substring(endIndex, 3).Equals("';'"))
                    {
                        endIndex += 2;
                    }
                    else if (script[endIndex] == ';')
                    {
                        lastResult = ScriptExpressionEvaluate(ref endIndex);
                    }

                    BlockState_If = EBlockState_If.NoBlock;
                    BlockState_Try = EBlockState_Try.NoBlock;

                    endIndex++;
                }
            }

            if (!script.Substring(startIndex).Trim().Equals(string.Empty) && !isReturn && !isBreak && !isContinue)
                throw new SyntaxException($"{script} 中缺少 [;] 字符");

            ExecuteTryList();
            ExecuteIfList();

            valueReturned = isReturn;
            breakCalled = isBreak;
            continueCalled = isContinue;

            return lastResult;

            void ExecuteTryList()
            {
                if (tryStatementsList.Count > 0)
                {
                    if (tryStatementsList.Count == 1)
                    {
                        throw new SyntaxException("try语句至少需要一个catch或一个finally语句。");
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
                                    if (!((ClassOrEnumType)Evaluate(exceptionVariable[0])).Type.IsInstanceOfType(exception)) continue;

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
                var internalStringMatch = stringBeginRegex.Match(script.Substring(index));

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
                var expression = script.Substring(startIndex, index - startIndex);

                startIndex = index + 1;

                return ManageJumpStatementsOrExpressionEval(expression);
            }

            //管理跳转语句或表达式求值
            object ManageJumpStatementsOrExpressionEval(string expression)
            {
                expression = expression.Trim(); //修剪空字符串

                if (expression.Equals("break"))
                {
                    isBreak = true;
                    return result;
                }

                if (expression.Equals("continue"))
                {
                    isContinue = true;
                    return result;
                }

                if (expression.StartsWith("throw "))
                {
                    if (Evaluate(expression.Remove(0, 6)) is Exception exception) //移除throw 关键字
                    {
                        throw exception; //ExceptionDispatchInfo.Capture(exception).Throw();
                    }
                    else
                    {
                        throw new SyntaxException("[throw]后 缺少[Exception]实例");
                    }
                }

                expression = returnKeywordRegex.Replace(expression, match =>
                {
                    if (!match.Value.StartsWith("return"))
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
                EvaluateVarOrFunc,
                EvaluateOperators,
                EvaluateChar,
                EvaluateParenthis,
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
                //如果是lambda表达式，则入栈
                if (GetLambdaExpression(expression, stack))
                    return stack.Pop(); //然后出栈


                for (var i = 0; i < expression.Length; i++)
                {
                    if (!ParsingMethods.Any(parsingMethod => parsingMethod(expression, stack, ref i)))
                    {
                        var s = expression.Substring(i, 1);
                        if (string.IsNullOrWhiteSpace(s)) continue;

                        throw new SyntaxException($"无效的字符 [{(int)s[0]}:{s}]");
                    }
                }

                return ProcessStack(stack);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                var exceptionToThrow = exception.InnerException;

                while (exceptionToThrow is TargetInvocationException && exceptionToThrow.InnerException != null)
                    exceptionToThrow = exceptionToThrow.InnerException;

                throw exceptionToThrow;
            }
            finally
            {
                evaluationStackCount--;
            }
        }

        #endregion

        #endregion

        #region 子部分求值方法(protected virtual)

        /// <summary>解析强转:(int)</summary>
        private bool EvaluateCast(string expression, Stack<object> stack, ref int i)
        {
            var castMatch = castRegex.Match(expression.Substring(i));

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

        /// <summary>解析数字:只能是十进制类型</summary>
        private bool EvaluateNumber(string expression, Stack<object> stack, ref int i)
        {
            var numberMatch = numberRegex.Match(expression.Substring(i));

            //匹配成功 && ( 前面无符号 || 栈为空 || 栈顶为运算符 )
            if (numberMatch.Success && (!numberMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                i += numberMatch.Length - 1;

                if (numberMatch.Groups["type"].Success) //有后缀的情况下,直接解析对应类型
                {
                    var type = numberMatch.Groups["type"].Value;
                    var numberNoType = numberMatch.Value.Replace(type, string.Empty);

                    if (numberSuffixToParse.TryGetValue(type, out var parseFunc))
                    {
                        stack.Push(parseFunc(numberNoType));
                    }
                }
                else if (numberMatch.Groups["hasdecimal"].Success) //无后缀情况下,只要有小数点就被解析为double类型
                {
                    stack.Push(double.Parse(numberMatch.Value));
                }
                else //否则默认为int类型
                {
                    stack.Push(int.Parse(numberMatch.Value));
                }

                return true;
            }

            return false;
        }

        /// <summary>解析变量或函数</summary>
        private bool EvaluateVarOrFunc(string expression, Stack<object> stack, ref int i)
        {
            var varFuncMatch = varOrFunctionRegEx.Match(expression.Substring(i));

            if (!varFuncMatch.Success) return false;

            //有var 关键字 但 没有赋值运算符
            if (varFuncMatch.Groups["varKeyword"].Success && !varFuncMatch.Groups["assignationOperator"].Success)
                throw new SyntaxException($"隐式变量未初始化! [var {varFuncMatch.Groups["name"].Value}]");

            if ((!varFuncMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator) //前缀没有符号 或 栈为空 或 栈顶为运算符 
                && (!operatorsDictionary.ContainsKey(varFuncMatch.Groups["name"].Value) || varFuncMatch.Groups["inObject"].Success)) // 不是已注册的运算符 或 在对象中
            {
                var varFuncName = varFuncMatch.Groups["name"].Value;
                var inObject = varFuncMatch.Groups["inObject"].Success;

                i += varFuncMatch.Length;

                //是方法的情况
                if (varFuncMatch.Groups["isfunction"].Success)
                {
                    //找到括号包裹的部分(参数)
                    var funcArgs = GetExpressionsParenthesized(expression, ref i, true);

                    //如果是对象的方法,或者是this的方法
                    if (inObject || (Context?.GetType().GetMethods(InstanceBindingFlag).Any(methodInfo => methodInfo.Name.Equals(varFuncName)) ?? false))
                    {
                        if (inObject && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
                            throw new SyntaxException($"[{varFuncMatch.Value})] 必须紧跟一个对象"); //只有点

                        var obj = inObject ? stack.Pop() : Context;
                        var objType = obj?.GetType();

                        try
                        {
                            if (obj is NullValue)
                            {
                                stack.Push(obj);
                            }
                            else if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                            {
                                stack.Push(new NullValue());
                            }
                            else if (obj is BubbleExceptionContainer)
                            {
                                stack.Push(obj);
                                return true;
                            }
                            else
                            {
                                var argIndex = 0;
                                var argsWithKeywords = new List<ArgKeywordsWrapper>();

                                var oArgs = funcArgs.ConvertAll(arg =>
                                {
                                    var functionArgKeywordsMatch = functionArgKeywordsRegex.Match(arg);
                                    object argValue;

                                    if (functionArgKeywordsMatch.Success)
                                    {
                                        var argKeywordWrapper = new ArgKeywordsWrapper { Index = argIndex, Keyword = functionArgKeywordsMatch.Groups["keyword"].Value, VariableName = functionArgKeywordsMatch.Groups["varName"].Value };

                                        argsWithKeywords.Add(argKeywordWrapper);

                                        if (functionArgKeywordsMatch.Groups["typeName"].Success)
                                        {
                                            var fixedType = ((ClassOrEnumType)Evaluate(functionArgKeywordsMatch.Groups["typeName"].Value)).Type;

                                            Variables[argKeywordWrapper.VariableName] = new StronglyTypedVariable { Type = fixedType, Value = GetDefaultValueOfType(fixedType) };
                                        }
                                        else if (!Variables.ContainsKey(argKeywordWrapper.VariableName))
                                        {
                                            Variables[argKeywordWrapper.VariableName] = null;
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

                                var flag = DetermineInstanceOrStatic(out objType, ref obj, out _);

                                // 寻找标准实例或公共方法
                                var methodInfo = GetRealMethod(ref objType, varFuncName, flag, oArgs, string.Empty, Type.EmptyTypes, argsWithKeywords);

                                // 如果找不到，检查obj是否是dictionaryObject或类似对象
                                if (obj is IDictionary<string, object> dictionaryObject && (dictionaryObject[varFuncName] is InternalDelegate || dictionaryObject[varFuncName] is Delegate)) //obj is IDynamicMetaObjectProvider &&
                                {
                                    if (dictionaryObject[varFuncName] is InternalDelegate internalDelegate)
                                        stack.Push(internalDelegate(oArgs.ToArray()));
                                    else if (dictionaryObject[varFuncName] is Delegate del)
                                        stack.Push(del.DynamicInvoke(oArgs.ToArray()));
                                }
                                else if (objType.GetProperty(varFuncName, InstanceBindingFlag) is { } instancePropertyInfo && (instancePropertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)) || instancePropertyInfo.PropertyType == typeof(Delegate)) && instancePropertyInfo.GetValue(obj) is Delegate del)
                                {
                                    stack.Push(del.DynamicInvoke(oArgs.ToArray()));
                                }
                                else
                                {
                                    var isExtension = false;

                                    // if not found try to Find extension methods.
                                    if (methodInfo == null && obj != null)
                                    {
                                        oArgs.Insert(0, obj);
                                        objType = obj.GetType();

                                        for (var e = 0; e < StaticTypesForExtensionsMethods.Count && methodInfo == null; e++)
                                        {
                                            var type = StaticTypesForExtensionsMethods[e];
                                            methodInfo = GetRealMethod(ref type, varFuncName, StaticBindingFlag, oArgs, string.Empty, Type.EmptyTypes, argsWithKeywords, true);
                                            isExtension = methodInfo != null;
                                        }
                                    }

                                    if (methodInfo != null)
                                    {
                                        var argsArray = oArgs.ToArray();
                                        stack.Push(methodInfo.Invoke(isExtension ? null : obj, argsArray));
                                        argsWithKeywords.FindAll(argWithKeyword => argWithKeyword.Keyword.Equals("out") || argWithKeyword.Keyword.Equals("ref")).ForEach(outOrRefArg => AssignVariable(outOrRefArg.VariableName, argsArray[outOrRefArg.Index + (isExtension ? 1 : 0)]));
                                    }
                                    else if (objType.GetProperty(varFuncName, StaticBindingFlag) is { } staticPropertyInfo && (staticPropertyInfo.PropertyType.IsSubclassOf(typeof(Delegate)) || staticPropertyInfo.PropertyType == typeof(Delegate)) && staticPropertyInfo.GetValue(obj) is Delegate del2)
                                    {
                                        stack.Push(del2.DynamicInvoke(oArgs.ToArray()));
                                    }
                                    else
                                    {
                                        var query = from type in StaticTypesForExtensionsMethods
                                            where !type.IsGenericType && type.IsSealed && !type.IsNested
                                            from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                            where method.GetParameters()[0].ParameterType == objType // static extMethod(this outType, ...)
                                            select method;

                                        var methodInfos = query as MethodInfo[] ?? query.ToArray();
                                        if (methodInfos.Any())
                                        {
                                            var fnArgsPrint = string.Join(",", funcArgs);
                                            var fnOverloadsPrint = "";

                                            foreach (var mi in methodInfos)
                                            {
                                                var parInfo = mi.GetParameters();
                                                fnOverloadsPrint += string.Join(",", parInfo.Select(x => x.ParameterType.FullName ?? x.ParameterType.Name)) + "\n";
                                            }

                                            throw new SyntaxException($"[{objType}] 的扩展方法 \"{varFuncName}\"没有参数重载: {fnArgsPrint}. 候选: {fnOverloadsPrint}");
                                        }


                                        throw new SyntaxException($"[{objType}] 对象方法  [{varFuncName}] ");
                                    }
                                }
                            }
                        }
                        catch (SyntaxException) { throw; }
                        catch (NullReferenceException nullException)
                        {
                            stack.Push(new BubbleExceptionContainer(nullException));

                            return true;
                        }
                        catch (Exception ex)
                        {
                            //Transport the exception in stack.
                            var nestedException = new SyntaxException($"The call of the method \"{varFuncName}\" on type [{objType}] generate this error : {ex.InnerException?.Message ?? ex.Message}", ex);
                            stack.Push(new BubbleExceptionContainer(nestedException));
                            return true; //Signals an error to the parsing method array call                          
                        }
                    }
                    else
                    {
                        var functionPreEvaluationEventArg = new FunctionEvaluationEventArg(varFuncName, funcArgs, this);

                        PreEvaluateFunction?.Invoke(this, functionPreEvaluationEventArg);

                        if (functionPreEvaluationEventArg.FunctionReturnedValue)
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
                        else if (Variables.TryGetValue(varFuncName, out o) && o is MethodsGroupWrapper methodsGroupWrapper)
                        {
                            var args = funcArgs.ConvertAll(Evaluate);
                            List<object> modifiedArgs = null;
                            MethodInfo methodInfo = null;

                            for (var m = 0; methodInfo == null && m < methodsGroupWrapper.MethodsGroup.Length; m++)
                            {
                                modifiedArgs = new List<object>(args);

                                methodInfo = TryToCastMethodParametersToMakeItCallable(methodsGroupWrapper.MethodsGroup[m], modifiedArgs, string.Empty, Type.EmptyTypes);
                            }

                            if (methodInfo != null)
                                stack.Push(methodInfo.Invoke(methodsGroupWrapper.ContainerObject, modifiedArgs.ToArray()));
                        }
                        else
                        {
                            throw new SyntaxException($"[{varFuncName}]:函数在脚本中未知: [{expression}]");
                        }
                    }
                }
                //是变量，对象的情况
                else
                {
                    if (inObject || Context?.GetType().GetProperties(InstanceBindingFlag).Any(propInfo => propInfo.Name.Equals(varFuncName)) == true || Context?.GetType().GetFields(InstanceBindingFlag).Any(fieldInfo => fieldInfo.Name.Equals(varFuncName)) == true)
                    {
                        if (inObject && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
                            throw new SyntaxException($"[{varFuncMatch.Value}] 后面必须有一个对象");

                        var obj = inObject ? stack.Pop() : Context;
                        var objType = obj?.GetType();

                        try
                        {
                            if (obj is NullValue)
                            {
                                stack.Push(obj);
                            }
                            else if (varFuncMatch.Groups["nullConditional"].Success && obj == null)
                            {
                                stack.Push(new NullValue());
                            }
                            else if (obj is BubbleExceptionContainer)
                            {
                                stack.Push(obj);
                                return true;
                            }
                            else
                            {
                                var flag = DetermineInstanceOrStatic(out objType, ref obj, out var valueTypeNestingTrace);


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

                                    if (methodsGroup is { Length: > 0 })
                                        varValue = new MethodsGroupWrapper { ContainerObject = obj, MethodsGroup = methodsGroup };
                                }

                                var pushVarValue = true;

                                if (isDynamic)
                                {
                                    if (!varFuncMatch.Groups["assignationOperator"].Success || varFuncMatch.Groups["assignmentPrefix"].Success)
                                        varValue = dictionaryObject.TryGetValue(varFuncName, out var value) ? value : null;
                                    else
                                        pushVarValue = false;
                                }

                                //Var去设置值 且 不是动态的 且 值为null 且 pushVarValue为true
                                if (!isDynamic && varValue == null)
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


                                if (varFuncMatch.Groups["assignationOperator"].Success)
                                {
                                    var value = varValue;
                                    varValue = ManageKindOfAssignation(expression, ref i, varFuncMatch, () => value, stack);
                                }
                                else if (varFuncMatch.Groups["postfixOperator"].Success)
                                {
                                    //不是++就是--;
                                    if (varValue != null)
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
                        }
                        catch (SyntaxException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            //Transport the exception in stack.
                            var nestedException = new SyntaxException($"[{objType}] object has no public Property or Member named \"{varFuncName}\".", ex);
                            stack.Push(new BubbleExceptionContainer(nestedException));
                            i--;
                            return true; //Signals an error to the parsing method array call
                        }
                    }
                    else
                    {
                        if (defaultVariables.TryGetValue(varFuncName, out var varValueToPush))
                        {
                            stack.Push(varValueToPush);
                        }
                        else if (Variables.TryGetValue(varFuncName, out var cusVarValueToPush) || varFuncMatch.Groups["assignationOperator"].Success || (stack.Count == 1 && stack.Peek() is ClassOrEnumType && string.IsNullOrWhiteSpace(expression.Substring(i))))
                        {
                            if (stack.Count == 1 && stack.Peek() is ClassOrEnumType classOrEnum)
                            {
                                if (Variables.ContainsKey(varFuncName))
                                    throw new SyntaxException($"变量名已存在：[{varFuncName}]");
                                if (varFuncMatch.Groups["varKeyword"].Success)
                                    throw new SyntaxException("无法使用type和var关键字的变量");

                                stack.Pop();

                                Variables[varFuncName] = new StronglyTypedVariable { Type = classOrEnum.Type, Value = GetDefaultValueOfType(classOrEnum.Type), };
                            }

                            if (cusVarValueToPush is StronglyTypedVariable typedVariable)
                                cusVarValueToPush = typedVariable.Value;

                            stack.Push(cusVarValueToPush);


                            var assign = true;

                            if (varFuncMatch.Groups["assignationOperator"].Success)
                            {
                                var push = cusVarValueToPush;
                                cusVarValueToPush = ManageKindOfAssignation(expression, ref i, varFuncMatch, () => push, stack);
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
                        else
                        {
                            var staticType = EvaluateType(expression, ref i, varFuncName, string.Empty);

                            if (staticType != null)
                            {
                                stack.Push(new ClassOrEnumType(staticType));
                            }
                            else
                            {
                                throw new SyntaxException($"变量 [{varFuncName}] 在脚本中未知 : [{expression}]");
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
            var typeName = $"{currentName}{(i < expression.Length && expression[i] == '?' ? "?" : "")}";
            var staticType = GetTypeByName(typeName, genericsTypes);

            if (staticType == null)
            {
                var subIndex = 0;
                var typeMatch = varOrFunctionRegEx.Match(expression.Substring(i));

                if (typeMatch.Success && !typeMatch.Groups["sign"].Success && !typeMatch.Groups["assignationOperator"].Success && !typeMatch.Groups["postfixOperator"].Success && !typeMatch.Groups["isfunction"].Success && !typeMatch.Groups["inObject"].Success && i < expression.Length && !typeName.EndsWith("?"))
                {
                    subIndex += typeMatch.Length;
                    typeName += $"{typeMatch.Groups["name"].Value}{((i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?') ? "?" : "")}";

                    staticType = GetTypeByName(typeName, typeMatch.Groups["isgeneric"].Value);

                    if (staticType != null)
                        i += subIndex;
                }
            }

            if (typeName.EndsWith("?") && staticType != null) i++;

            // For nested type parsing
            if (staticType != null)
            {
                var nestedTypeMatch = varOrFunctionRegEx.Match(expression.Substring(i));
                while (nestedTypeMatch.Success && !nestedTypeMatch.Groups["sign"].Success && !nestedTypeMatch.Groups["assignationOperator"].Success && !nestedTypeMatch.Groups["postfixOperator"].Success && !nestedTypeMatch.Groups["isfunction"].Success)
                {
                    var subIndex = nestedTypeMatch.Length;
                    typeName += $"+{nestedTypeMatch.Groups["name"].Value}{((i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?') ? "?" : "")}";

                    var nestedType = GetTypeByName(typeName, nestedTypeMatch.Groups["isgeneric"].Value);
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
                var arrayType = GetTypeByName(staticType + arrayTypeMatch.Value);
                if (arrayType != null)
                {
                    i += arrayTypeMatch.Length;
                    staticType = arrayType;
                }
            }

            return staticType;
        }

        /// <summary>解析字符Char</summary>
        private bool EvaluateChar(string expression, Stack<object> stack, ref int i)
        {
            if (expression[i].Equals('\''))
            {
                i++;
                if (expression[i].Equals('\\')) //后一个字符只要是\就被是为转义字符
                {
                    i++; //然后查看再后一个字符
                    var escapedChar = expression[i];

                    if (charEscapedCharDict.TryGetValue(escapedChar, out var value))
                    {
                        stack.Push(value);
                        i++;
                    }
                    else
                    {
                        throw new SyntaxException("未知的转义方式");
                    }
                }
                else if (expression[i].Equals('\''))
                {
                    throw new SyntaxException("空字符''是非法的");
                }
                else
                {
                    stack.Push(expression[i]);
                    i++;
                }

                if (expression[i].Equals('\''))
                {
                    return true;
                }

                throw new SyntaxException("字符不合法,可能包含太多字符");
            }

            return false;
        }

        /// <summary>解析操作符</summary> 
        private bool EvaluateOperators(string expression, Stack<object> stack, ref int i)
        {
            var regexPattern = "^(" + string.Join("|", operatorsDictionary.Keys.OrderByDescending(key => key.Length).Select(Regex.Escape)) + ")";

            var match = Regex.Match(expression.Substring(i), regexPattern, RegexOptions.None);

            if (match.Success)
            {
                var op = match.Value;

                switch (op)
                {
                    //排除一元运算符的可能性
                    case "+" or "-" when stack.Count == 0 || stack.Peek() is ExpressionOperator:
                        stack.Push(op == "+" ? ExpressionOperator.UnaryPlus : ExpressionOperator.UnaryMinus);
                        break;
                    default:
                        stack.Push(operatorsDictionary[op]);
                        break;
                }

                i += op.Length - 1;
                return true;
            }

            return false;
        }

        /// <summary>解析三目运算符</summary>
        private bool EvaluateTernaryConditionalOperator(string expression, Stack<object> stack, ref int i)
        {
            if (expression[i].Equals('?'))
            {
                var condition = (bool)ProcessStack(stack);

                var restOfExpression = expression.Substring(i + 1);

                for (var j = 0; j < restOfExpression.Length; j++)
                {
                    var s2 = restOfExpression[j];

                    var internalStringMatch = stringBeginRegex.Match(restOfExpression.Substring(j));

                    if (internalStringMatch.Success)
                    {
                        var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(restOfExpression.Substring(j + internalStringMatch.Length), internalStringMatch);
                        j += innerString.Length - 1;
                    }
                    else if (s2.Equals('('))
                    {
                        j++;
                        GetExpressionsParenthesized(restOfExpression, ref j, false);
                    }
                    else if (s2.Equals(':'))
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
            var s = expression[i];

            if (s.Equals(')')) throw new SyntaxException($"[)] 找不到对应的匹配 : [{expression}] ");

            if (s.Equals('('))
            {
                i++;

                if (stack.Count > 0 && stack.Peek() is InternalDelegate)
                {
                    var expressionsInParenthis = GetExpressionsParenthesized(expression, ref i, true);

                    if (stack.Pop() is InternalDelegate lambdaDelegate)
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

        /// <summary>解析字符串</summary>
        private bool EvaluateString(string expression, Stack<object> stack, ref int i)
        {
            var stringBeginningMatch = stringBeginRegex.Match(expression.Substring(i));

            if (!stringBeginningMatch.Success) return false;

            var isEscaped = stringBeginningMatch.Groups["escaped"].Success;
            var isInterpolated = stringBeginningMatch.Groups["interpolated"].Success;

            if (isEscaped || isInterpolated) throw new SyntaxException("不支持@和$字符,请删除后重试");

            i += stringBeginningMatch.Length;

            var stringRegexPattern = new Regex("^[^\\\"]*"); //提取一行文本中不包含反斜杠和双引号的部分

            var endOfString = false;
            var resultString = new StringBuilder();
            resultString.Append('\"');
            while (!endOfString && i < expression.Length)
            {
                var stringMatch = stringRegexPattern.Match(expression.Substring(i));

                resultString.Append(stringMatch.Value);
                i += stringMatch.Length;

                switch (expression[i])
                {
                    case '"':
                        endOfString = true;
                        stack.Push(resultString.ToString());
                        break;
                    case '\\':
                    {
                        i++;

                        if (charEscapedCharDict.TryGetValue(expression[i], out var escapedString))
                        {
                            resultString.Append(escapedString);
                            i++;
                        }
                        else
                        {
                            throw new SyntaxException($"未知的转义字符 \\{expression[i]}");
                        }

                        break;
                    }
                }
            }

            if (!endOfString) throw new SyntaxException("缺少一个[ \\ ]字符");

            return true;
        }

        #endregion

        #region 进程堆栈

        public object ProcessStack(Stack<object> stack)
        {
            //如果堆栈为空，则抛出异常
            if (stack.Count == 0) throw new SyntaxException("空表达式或找不到标记");

            //将栈中的值类型,异常,空值进行处理
            var list = stack.Select(e => e is ValueTypeNestingTrace valueTypeNestingTrace ? valueTypeNestingTrace.Value : e) //处理值类型
                .Select(e => e is NullValue ? null : e).ToList(); //处理空值

            // 遍历所有的操作符
            foreach (var _operatorMsg in OperatorsEvaluation)
            {
                // 从后往前遍历
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    // 如果当前的操作符不是当前的操作符,则跳过
                    if (!ReferenceEquals(list[i] as ExpressionOperator, _operatorMsg.Key)) continue;

                    // 如果当前的操作符 同时也是 是右操作符,则
                    if (UnaryPostfixOperators.Contains(_operatorMsg.Key))
                    {
                        try
                        {
                            EvaluateFirstNextUnaryOp(i - 1, ref i);
                            list[i] = _operatorMsg.Value(null, list[i - 1]);

                            //定义一个方法,用于递归处理前一个操作符
                            void EvaluateFirstNextUnaryOp(int j, ref int parentIndex)
                            {
                                if (j > 0 && list[j] is ExpressionOperator nextOp && UnaryPostfixOperators.Contains(nextOp))
                                {
                                    EvaluateFirstNextUnaryOp(j - 1, ref j);

                                    //处理前一个操作符
                                    list[j] = OperatorsEvaluation[nextOp](null, list[j - 1]);

                                    //移除前一个操作符
                                    list.RemoveAt(j - 1);
                                    parentIndex = j;
                                }
                            }
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


                    // 剩下的为左右双目操作符
                    {
                        var left = list[i + 1];
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
                    if (item is BubbleExceptionContainer bubbleExceptionContainer1)
                    {
                        bubbleExceptionContainer1.Throw(); //抛出第一个出现的错误
                    }
                }

                throw new SyntaxException("语法错误.检查没有操作符丢失");
            }

            if (evaluationStackCount == 1 && stack.Peek() is BubbleExceptionContainer bubbleExceptionContainer)
            {
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
            if (stack?.Count > 1) throw new SyntaxException($"{expression}赋值的左边部分必须是变量,属性或索引器");

            object result;
            var rightExpression = expression.Substring(index);
            index = expression.Length;

            if (rightExpression.Trim().Equals(string.Empty)) throw new SyntaxException("分配中缺少右部分");

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

        /// <summary>给变量赋值</summary>
        private void AssignVariable(string varName, object value)
        {
            if (Variables.ContainsKey(varName) && Variables[varName] is StronglyTypedVariable stronglyTypedVariable)
            {
                if (value == null && stronglyTypedVariable.Type.IsValueType && Nullable.GetUnderlyingType(stronglyTypedVariable.Type) == null)
                {
                    throw new SyntaxException($"不可空的类型 : 不能强制转换为 null {stronglyTypedVariable.Type}");
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
                        throw new InvalidCastException($"对象类型:{typeToAssign}无法转换成{stronglyTypedVariable.Type}", exception);
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


            stack.Push(new InternalDelegate(args =>
            {
                var vars = new Dictionary<string, object>(Variables);

                for (var a = 0; a < argsNames.Count || a < args.Length; a++)
                {
                    vars[argsNames[a].Value] = args[a];
                }

                var savedVars = Variables;
                Variables = vars;

                var lambdaBody = lambdaExpressionMatch.Groups["expression"].Value.Trim();

                object result;

                if (lambdaBody.StartsWith("{") && lambdaBody.EndsWith("}"))
                {
                    result = ScriptEvaluate(lambdaBody.Substring(1, lambdaBody.Length - 2));
                }
                else
                {
                    result = Evaluate(lambdaExpressionMatch.Groups["expression"].Value);
                }

                Variables = savedVars;

                return result;
            }));

            return true;
        }

        /// <summary>获取方法的解释器</summary>
        private MethodInfo GetRealMethod(ref Type type, string func, BindingFlags flag, List<object> args, string genericsTypes, Type[] inferredGenericsTypes, List<ArgKeywordsWrapper> argsWithKeywords, bool testForExtension = false)
        {
            MethodInfo methodInfo = null;
            var modifiedArgs = new List<object>(args);


            var methodsInfo = type.GetMethods(flag).Where(methodByNameFilter).OrderByDescending(m => m.GetParameters().Length).ToList();

            //对于重载并可能实现lambda参数的Linq方法
            try
            {
                if (methodsInfo.Count > 1 && type == typeof(Enumerable) && args.Count == 2 && args[1] is InternalDelegate internalDelegate && args[0] is IEnumerable enumerable && enumerable.GetEnumerator() is { } enumerator && enumerator.MoveNext() && methodsInfo.Any(m => m.GetParameters().Any(p => p.ParameterType.Name.StartsWith("Func"))))
                {
                    var lambdaResultType = internalDelegate.Invoke(enumerator.Current).GetType();

                    methodInfo = methodsInfo.Find(m =>
                    {
                        var parameters = m.GetParameters();

                        return parameters.Length == 2 && parameters[1].ParameterType.Name.StartsWith("Func") && parameters[1].ParameterType.GenericTypeArguments is { Length: 2 } genericTypesArgs && genericTypesArgs[1] == lambdaResultType;
                    });

                    if (methodInfo != null)
                    {
                        methodInfo = TryToCastMethodParametersToMakeItCallable(methodInfo, modifiedArgs, genericsTypes, inferredGenericsTypes);
                    }
                }
            }
            catch
            {
                /*ignored*/
            }

            for (var m = 0; methodInfo == null && m < methodsInfo.Count; m++)
            {
                modifiedArgs = new List<object>(args);

                methodInfo = TryToCastMethodParametersToMakeItCallable(methodsInfo[m], modifiedArgs, genericsTypes, inferredGenericsTypes);
            }

            if (methodInfo == null) return null;
            args.Clear();
            args.AddRange(modifiedArgs);

            return methodInfo;

            bool methodByNameFilter(MethodInfo m) => m.Name.Equals(func) && (m.GetParameters().Length == modifiedArgs.Count || (m.GetParameters().Length > modifiedArgs.Count && m.GetParameters().Take(modifiedArgs.Count).All(p => modifiedArgs[p.Position] == null ||  p.ParameterType.IsInstanceOfType(modifiedArgs[p.Position]) ) && m.GetParameters().Skip(modifiedArgs.Count).All(p => p.HasDefaultValue)) || (m.GetParameters().Length > 0 && m.GetParameters().Last().IsDefined(typeof(ParamArrayAttribute), false) && m.GetParameters().All(parameterValidate)));

            bool parameterValidate(ParameterInfo p) => p.Position >= modifiedArgs.Count || (testForExtension && p.Position == 0) || modifiedArgs[p.Position] == null || p.ParameterType.IsInstanceOfType(modifiedArgs[p.Position]) || typeof(Delegate).IsAssignableFrom(p.ParameterType) || p.IsDefined(typeof(ParamArrayAttribute)) || (p.ParameterType.IsByRef && argsWithKeywords.Any(a => a.Index == p.Position + (testForExtension ? 1 : 0)));
        }

        private MethodInfo TryToCastMethodParametersToMakeItCallable(MethodInfo methodInfoToCast, List<object> modifiedArgs, string genericsTypes, Type[] inferredGenericsTypes)
        {
            MethodInfo methodInfo = null;

            var oldMethodInfo = methodInfoToCast;

            if (!string.IsNullOrEmpty(genericsTypes))
            {
                methodInfoToCast = MakeConcreteMethodIfGeneric(methodInfoToCast, genericsTypes, inferredGenericsTypes);
            }
            else if (oldMethodInfo.IsGenericMethod && oldMethodInfo.ContainsGenericParameters)
            {
                var genericArgsTypes = oldMethodInfo.GetGenericArguments();
                var inferredTypes = new List<Type>();

                foreach (var t1 in genericArgsTypes)
                {
                    if (t1.IsGenericParameter)
                    {
                        var name = t1.Name;
                        var parameterInfos = oldMethodInfo.GetParameters();

                        var paramsForInference = Array.Find(parameterInfos, p => p.ParameterType.IsGenericParameter && p.ParameterType.Name.Equals(name) && modifiedArgs.Count > p.Position && !modifiedArgs[p.Position].GetType().IsGenericParameter);

                        if (paramsForInference != null)
                        {
                            inferredTypes.Add(modifiedArgs[paramsForInference.Position].GetType());
                        }
                        else
                        {
                            paramsForInference = Array.Find(parameterInfos, p => p.ParameterType is { IsGenericType: true, ContainsGenericParameters: true } && p.ParameterType.GetGenericArguments().Any(subP => subP.Name.Equals(name)) && modifiedArgs.Count > p.Position && !modifiedArgs[p.Position].GetType().IsGenericType);

                            if (paramsForInference == null) continue;

                            if (modifiedArgs[paramsForInference.Position] is MethodsGroupWrapper methodsGroupWrapper)
                            {
                                if (paramsForInference.ParameterType.Name.StartsWith("Converter"))
                                {
                                    var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                    var paraMethodInfo = Array.Find(methodsGroupWrapper.MethodsGroup, mi => mi.GetParameters().Length == 1);
                                    switch (specificType?.GenericParameterPosition)
                                    {
                                        case 0:
                                            inferredTypes.Add(paraMethodInfo.GetParameters()[0].ParameterType);
                                            break;
                                        case 1:
                                            inferredTypes.Add(paraMethodInfo.ReturnType);
                                            break;
                                    }
                                }
                                else if (paramsForInference.ParameterType.Name.StartsWith("Action"))
                                {
                                    var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                    var paraMethodInfo = Array.Find(methodsGroupWrapper.MethodsGroup, mi => mi.GetParameters().Length == paramsForInference.ParameterType.GetGenericArguments().Length);
                                    if (specificType != null)
                                    {
                                        inferredTypes.Add(paraMethodInfo.GetParameters()[specificType.GenericParameterPosition].ParameterType);
                                    }
                                }
                                else if (paramsForInference.ParameterType.Name.StartsWith("Func"))
                                {
                                    var specificType = Array.Find(paramsForInference.ParameterType.GetGenericArguments(), pType => pType.Name.Equals(name));
                                    var paraMethodInfo = Array.Find(methodsGroupWrapper.MethodsGroup, mi => mi.GetParameters().Length == paramsForInference.ParameterType.GetGenericArguments().Length - 1);
                                    if (specificType?.GenericParameterPosition == paraMethodInfo.GetParameters().Length)
                                    {
                                        inferredTypes.Add(paraMethodInfo.ReturnType);
                                    }
                                    else
                                    {
                                        if (specificType != null)
                                            inferredTypes.Add(paraMethodInfo.GetParameters()[specificType.GenericParameterPosition].ParameterType);
                                    }
                                }
                            }
                            else if (modifiedArgs[paramsForInference.Position].GetType().HasElementType)
                            {
                                inferredTypes.Add(modifiedArgs[paramsForInference.Position].GetType().GetElementType());
                            }
                        }
                    }
                    else
                    {
                        inferredTypes.Add(t1);
                    }
                }

                if (inferredTypes.Count > 0 && inferredTypes.Count == genericArgsTypes.Length)
                    methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, inferredTypes.ToArray());
                else
                    methodInfoToCast = MakeConcreteMethodIfGeneric(methodInfoToCast, genericsTypes, inferredGenericsTypes);
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
                        var de = new DelegateWrapper(internalDelegate);
                        var method = de.GetType().GetMethod("Predicate").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, method);
                    }
                    else if (paramTypeName.StartsWith("Func"))
                    {
                        var de = new DelegateWrapper(internalDelegate);
                        var method = de.GetType().GetMethod($"Func{parameterType.GetGenericArguments().Length - 1}").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, method);
                    }
                    else if (paramTypeName.StartsWith("Action"))
                    {
                        var de = new DelegateWrapper(internalDelegate);
                        var method = de.GetType().GetMethod($"Action{parameterType.GetGenericArguments().Length}").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, method);
                    }
                    else if (paramTypeName.StartsWith("Converter"))
                    {
                        var de = new DelegateWrapper(internalDelegate);
                        var method = de.GetType().GetMethod("Func1").MakeGenericMethod(parameterType.GetGenericArguments());
                        modifiedArgs[a] = Delegate.CreateDelegate(parameterType, de, method);
                    }
                }
                else if (typeof(Delegate).IsAssignableFrom(parameterType) && modifiedArgs[a] is MethodsGroupWrapper methodsGroupWrapper)
                {
                    var invokeMethod = parameterType.GetMethod("Invoke");
                    var methodForDelegate = Array.Find(methodsGroupWrapper.MethodsGroup, m => invokeMethod != null && invokeMethod.GetParameters().Length == m.GetParameters().Length && invokeMethod.ReturnType.IsAssignableFrom(m.ReturnType));
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

                        modifiedArgs[a] = Delegate.CreateDelegate(delegateType, methodsGroupWrapper.ContainerObject, methodForDelegate);

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
                        if (methodInfoToCast.GetParameters().Length == a + 1 && methodInfoToCast.GetParameters()[a].IsDefined(typeof(ParamArrayAttribute), false) && parameterType != modifiedArgs[a]?.GetType() && parameterType.GetElementType() is { } elementType && modifiedArgs.Skip(a).All(arg => arg == null || elementType.IsInstanceOfType(arg)))
                        {
                            var numberOfElements = modifiedArgs.Count - a;
                            var paramsArray = Array.CreateInstance(elementType, numberOfElements);
                            modifiedArgs.Skip(a).ToArray().CopyTo(paramsArray, 0);
                            modifiedArgs.RemoveRange(a, numberOfElements);
                            modifiedArgs.Add(paramsArray);
                        }
                        else if (modifiedArgs[a] != null && !parameterType.IsInstanceOfType(modifiedArgs[a]))
                        {
                            if (parameterType.IsByRef)
                            {
                                if (!parameterType.GetElementType().IsInstanceOfType(modifiedArgs[a]))
                                    modifiedArgs[a] = Convert.ChangeType(modifiedArgs[a], parameterType.GetElementType());
                            }
                            else if (modifiedArgs[a].GetType().IsArray && typeof(IEnumerable).IsAssignableFrom(parameterType) && oldMethodInfo.IsGenericMethod && string.IsNullOrWhiteSpace(genericsTypes) && methodInfoToCast.GetGenericArguments().Length == 1 && methodInfoToCast.GetGenericArguments()[0] != modifiedArgs[a].GetType().GetElementType())
                            {
                                methodInfoToCast = MakeConcreteMethodIfGeneric(oldMethodInfo, genericsTypes, new Type[] { modifiedArgs[a].GetType().GetElementType() });
                            }
                            else
                            {
                                if (parameterType.IsArray && modifiedArgs[a] is Array)
                                {
                                    modifiedArgs[a] = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(parameterType.GetElementType()).Invoke(null, new[] { modifiedArgs[a] });

                                    modifiedArgs[a] = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(parameterType.GetElementType()).Invoke(null, new[] { modifiedArgs[a] });
                                }
                                else if (parameterType.IsInstanceOfType(modifiedArgs[a]))
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
                }
            }

            if (parametersCastOK)
                methodInfo = methodInfoToCast;

            return methodInfo;
        }

        private MethodInfo MakeConcreteMethodIfGeneric(MethodInfo methodInfo, string genericsTypes, Type[] inferredGenericsTypes)
        {
            if (methodInfo.IsGenericMethod)
            {
                if (genericsTypes.Equals(string.Empty))
                {
                    if (inferredGenericsTypes != null && inferredGenericsTypes.Length == methodInfo.GetGenericArguments().Length)
                    {
                        return methodInfo.MakeGenericMethod(inferredGenericsTypes);
                    }

                    return methodInfo.MakeGenericMethod(Enumerable.Repeat(typeof(object), methodInfo.GetGenericArguments().Length).ToArray());
                }

                return methodInfo.MakeGenericMethod(GetConcreteTypes(genericsTypes));
            }

            return methodInfo;
        }

        /// <summary> 获取泛型内部的类型 </summary>
        private Type[] GetConcreteTypes(string genericsTypes)
        {
            var matchStr = genericsTypes.Trim().TrimStart('<').TrimEnd('>');
            var matches = genericsDecodeRegex.Matches(matchStr); //再次检查是否有嵌套泛型
            var concreteTypes = new List<Type>();

            foreach (Match match in matches)
            {
                var typeName = match.Groups["name"].Value;
                var isGeneric = match.Groups["isgeneric"].Value;

                var concreteType = GetTypeByName(typeName, isGeneric, true);
                concreteTypes.Add(concreteType);
            }

            return concreteTypes.ToArray();
        }

        private static BindingFlags DetermineInstanceOrStatic(out Type objType, ref object obj, out ValueTypeNestingTrace valueTypeNestingTrace)
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


            objType = obj.GetType();
            return InstanceBindingFlag;
        }
        
        /// <summary>获取两个花括号之间的脚本</summary>
        private string GetScriptBetweenCurlyBrackets(string parentScript, ref int index)
        {
            var currentScript = string.Empty;
            var bracketCount = 1;
            for (; index < parentScript.Length; index++)
            {
                var internalStringMatch = stringBeginRegex.Match(parentScript.Substring(index));
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

                    if (s.Equals("{"))
                    {
                        bracketCount++;
                    }

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

        /// <summary>获取指定括号之间的表达式列表</summary>
        private List<string> GetExpressionsParenthesized(string expression, ref int i, bool checkSeparator, char separator = ',', char startChar = '(', char endChar = ')')
        {
            var expressionsList = new List<string>();

            var currentExpression = string.Empty;
            var bracketCount = 1;
            for (; i < expression.Length; i++)
            {
                var subExpr = expression.Substring(i);
                var internalStringMatch = stringBeginRegex.Match(subExpr);
                var internalCharMatch = internalCharRegex.Match(subExpr);

                if (internalStringMatch.Success)
                {
                    var innerString = internalStringMatch.Value + GetCodeUntilEndOfString(expression.Substring(i + internalStringMatch.Length), internalStringMatch);
                    currentExpression += innerString;
                    i += innerString.Length - 1;
                }
                else if (internalCharMatch.Success)
                {
                    currentExpression += internalCharMatch.Value;
                    i += internalCharMatch.Length - 1;
                }
                else
                {
                    var s = expression[i];

                    if (s.Equals(startChar))
                    {
                        bracketCount++;
                    }
                    else
                    {
                        if (s == '(')
                        {
                            i++;
                            currentExpression += $"({GetExpressionsParenthesized(expression, ref i, false).SingleOrDefault()})";
                            continue;
                        }

                        if (s == '{')
                        {
                            i++;
                            currentExpression += $"{{{GetExpressionsParenthesized(expression, ref i, false, ',', '{', '}').SingleOrDefault()}}}";
                            continue;
                        }

                        if (s == '[')
                        {
                            i++;
                            currentExpression += $"[{GetExpressionsParenthesized(expression, ref i, false, ',', '[', ']').SingleOrDefault()}]";
                            continue;
                        }
                    }

                    if (s.Equals(endChar))
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(currentExpression))
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
                throw new Exception($"[{expression}] 中缺少字符 ['{endChar}'] ");


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
            else if (complexStandardFuncDictionary.TryGetValue(name, out var complexFunc))
            {
                result = complexFunc(this, args);
            }
            else
            {
                result = null;
                functionExists = false;
            }

            return functionExists;
        }

        /// <summary>通过类型名获取类型</summary>
        private Type GetTypeByName(string typeName, string genericTypes = "", bool throwExceptionIfNotFound = false)
        {
            typeName = typeName.Trim();
            genericTypes = genericTypes.Trim();
            var fullName = typeName + genericTypes;

            Type result;
            var formattedGenericTypes = string.Empty;
            var isCached = false;

            try
            {
                primaryTypesDic.TryGetValue(typeName, out result);
                if (CachedTypesDic.TryGetValue(fullName, out result))
                    isCached = true;


                if (result == null)
                {
                    if (!string.IsNullOrEmpty(genericTypes))
                    {
                        var types = GetConcreteTypes(genericTypes);
                        formattedGenericTypes = $"`{types.Length}[{string.Join(", ", types.Select(type => $"[{type.AssemblyQualifiedName}]"))}]";
                    }

                    result = Type.GetType(typeName + formattedGenericTypes, false, false);
                }


                result ??= Types.Find(_type => _type.Name.Equals(typeName));


                for (var a = 0; a < Assemblies.Count && result == null; a++)
                {
                    if (typeName.Contains("."))
                    {
                        result = Type.GetType($"{typeName}{formattedGenericTypes},{Assemblies[a].FullName}", false, false);
                    }
                    else
                    {
                        for (var i = 0; i < Namespaces.Count && result == null; i++)
                        {
                            result = Type.GetType($"{Namespaces[i]}.{typeName}{formattedGenericTypes},{Assemblies[a].FullName}", false, false);
                        }
                    }
                }
            }
            catch
            {
                throw new SyntaxException($"类型或类获取失败 : {typeName}{genericTypes}");
            }

            if (result == null && throwExceptionIfNotFound)
                throw new SyntaxException($"未知的类型或类 : {typeName}{genericTypes}");

            if (result != null && !isCached)
                CachedTypesDic[fullName] = result;

            return result;
        }

        /// <summary>改变类型</summary>
        private static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == null) throw new ArgumentNullException(nameof(conversionType));

            // 泛型类型 且 定义的泛型类型可以为 null
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return null;

                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (conversionType is { IsEnum: true })
            {
                return Enum.ToObject(conversionType, value);
            }

            if (conversionType != null && value.GetType().IsPrimitive && conversionType.IsPrimitive)
            {
                return primitiveExplicitCastMethodInfo.MakeGenericMethod(conversionType).Invoke(null, new object[] { value });
            }

            if (DynamicCast(value, conversionType, out var ret))
            {
                return ret;
            }

            return conversionType != null ? Convert.ChangeType(value, conversionType) : null;
        }


        private static readonly MethodInfo primitiveExplicitCastMethodInfo = typeof(UniInk).GetMethod(nameof(PrimitiveExplicitCast), BindingFlags.Static | BindingFlags.NonPublic);

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
            var castOperator = destType.GetMethods(bindingFlags).Union(srcType.GetMethods(bindingFlags)).Where(methodInfo => methodInfo.Name is "op_Explicit" or "op_Implicit").Where(methodInfo =>
            {
                var pars = methodInfo.GetParameters();
                return pars.Length == 1 && pars[0].ParameterType == srcType;
            }).FirstOrDefault(mi => mi.ReturnType == destType);

            if (castOperator != null)
                result = castOperator.Invoke(null, new[] { source });
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

        /// <summary> 用于?语法糖的容器 </summary>
        private struct NullValue { }

        private struct ArgKeywordsWrapper
        {
            public int Index { get; set; }
            public string Keyword { get; set; }
            public string VariableName { get; set; }
        }

        private class DelegateWrapper
        {
            private readonly InternalDelegate lambda;

            private readonly MethodInfo methodInfo;

            private readonly object target;

            public DelegateWrapper(InternalDelegate lambda)
            {
                this.lambda = lambda;
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
        }

        #endregion
    }


    public class ClassOrEnumType
    {
        public ClassOrEnumType(Type type) => Type = type;

        public Type Type { get; set; }
    }

    public class StronglyTypedVariable
    {
        public Type Type { get; set; }

        public object Value { get; set; }
    }

    /// <summary>表示一组方法，其中要调用的重载方法尚未确定。该类可以被用来模拟委托。</summary>
    public class MethodsGroupWrapper
    {
        /// <summary>定义该方法组的对象实例。</summary>
        public object ContainerObject { get; set; }

        /// <summary>一个方法信息（MethodInfo）数组，其中包含可能用于调用该方法组的重载方法</summary>
        public MethodInfo[] MethodsGroup { get; set; }
    }


    #region Operators

    /// <summary>用于解释的操作符</summary>
    public class ExpressionOperator
    {
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


        protected static ushort indexer;
        protected ushort OperatorValue { get; }

        protected ExpressionOperator()
        {
            indexer++;
            OperatorValue = indexer;
        }

        public bool Equals(ExpressionOperator otherOperator)
        {
            return otherOperator != null && OperatorValue == otherOperator.OperatorValue;
        }
    }

    #endregion


    #region Exceptions

    /// <summary>用于封装在表达式子部分中发生的异常，以便在表达式求值需要继续执行时，将异常传递到更高层次的调用栈中。</summary>
    public class BubbleExceptionContainer
    {
        private readonly Exception _exception;

        /// <summary>构造器</summary>
        /// <param name="exception">需要封装的异常</param>
        public BubbleExceptionContainer(Exception exception)
        {
            _exception = exception;
        }

        /// <summary>重新抛出已捕获的异常</summary>
        public void Throw() => throw _exception;
    }

    /// <summary>解释器执行时语法错误的异常</summary>
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base(message) { }

        public SyntaxException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion

    #region EventArgs

    /// <summary>关于已经/将要被求值的表达式的信息</summary>
    /// <remarks>用于<see cref="UniInk.ExpressionEvaluating"/>事件</remarks>
    public class ExpressionEvaluationEventArg
    {
        /// <summary>将用于计算表达式的求值器</summary>
        public UniInk Evaluator { get; set; }

        /// <summary>被解释的表达式,可以被修改</summary>
        public string Expression { get; set; }

        /// <summary>设置求值的返回值</summary>
        /// 用于 <see cref="UniInk.ExpressionEvaluated"/> 事件, 存储计算结果
        public object Value { get; set; }

        /// <summary>真: 表达式已经被求值, 假: 表达式还未被求值</summary>
        public bool HasValue => Value != null;

        /// <summary>构造器</summary>
        /// <param name="expression">要求值的表达式</param>
        /// <param name="evaluator">将用于计算表达式的求值器</param>
        /// <param name="value"></param>
        public ExpressionEvaluationEventArg(string expression, UniInk evaluator, object _value = null)
        {
            Expression = expression;
            Evaluator = evaluator;
            Value = _value;
        }
    }

    /// <summary>当前解释器的变量、特性或属性的信息</summary>
    public class VariableEvaluationEventArg
    {
        /// <summary>被解释的变量名</summary>
        public string Name { get; set; }

        /// <summary>为该变量设置一个值</summary>
        public object Value { get; set; }

        /// <summary>变量是否有值</summary>
        public bool HasValue => Value != null;

        /// <summary>在动态实例属性定义的情况下，调用此属性的对象的实例。<para/>否则设置为null。</summary>
        public object This { get; set; }

        /// <summary>当前解释器的引用</summary>
        public UniInk Evaluator { get; set; }

        /// <summary>是否是泛型类型</summary>
        public bool HasGenericTypes => !string.IsNullOrEmpty(genericTypes);

        /// <summary>在指定了泛型类型的情况下，计算所有类型并返回类型数组</summary>
        public Type[] EvaluateGenericTypes() => evaluateGenericTypes?.Invoke(genericTypes) ?? Type.EmptyTypes;


        private readonly Func<string, Type[]> evaluateGenericTypes;
        private readonly string genericTypes;

        /// <summary>构造器</summary>
        /// <param name="name">被解释的变量名</param>
        /// <param name="evaluator">被查找的解释器</param>
        /// <param name="onInstance">要在其上计算字段或属性的对象实例(赋值给<see cref="This"/>)</param>
        /// <param name="genericTypes">在属性访问时指定的泛型类型</param>
        /// <param name="evaluateGenericTypes">用于解释A func to evaluate the list of specific types given between &lt; and &gt;</param>
        public VariableEvaluationEventArg(string name, UniInk evaluator = null, object onInstance = null, string _genericTypes = null, Func<string, Type[]> _evaluateGenericTypes = null)
        {
            Name = name;
            This = onInstance;
            Evaluator = evaluator;
            genericTypes = _genericTypes;
            evaluateGenericTypes = _evaluateGenericTypes;
        }
    }

    /// <summary>关于当前求值的函数或方法的信息</summary>
    public class FunctionEvaluationEventArg
    {
        /// <summary>构造器</summary>
        /// <param name="name">函数或者方法的名字</param>
        /// <param name="args">传递给函数或方法的参数</param>
        /// <param name="evaluator"><see cref="UniInk"/>检测要求值的函数或方法</param>
        /// <param name="onInstance">要对方法求值的对象实例 (赋值给 <see cref="This"/>)</param>
        public FunctionEvaluationEventArg(string name, List<string> args = null, UniInk evaluator = null, object onInstance = null)
        {
            Name = name;
            Args = args ?? new List<string>();
            This = onInstance;
            Evaluator = evaluator;
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
        public object Value { get; set; }

        /// <summary>函数是否返回了值</summary>
        public bool FunctionReturnedValue => Value != null;

        /// <summary>在动态实例方法定义的情况下，调用该方法(函数)的对象的实例。<para/>否则设置为空。</summary>
        public object This { get; set; }

        /// <summary>当前解释器的引用</summary>
        public UniInk Evaluator { get; set; }
    }

    #endregion


    public static class UniInkHelper
    {
        //基于 : https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689

        #region Remove comments

        private static readonly Regex removeCommentsRegex = new($"{blockComments}|{lineComments}|{stringsIgnore}|{verbatimStringsIgnore}", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex newLineCharsRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);

        private const string verbatimStringsIgnore = @"@(""[^""]*"")+"; //language=regex
        private const string stringsIgnore = @"""((\\[^\n]|[^""\n])*)"""; //language=regex
        private const string blockComments = @"/\*(.*?)\*/"; //language=regex
        private const string lineComments = @"//[^\r\n]*"; //language=regex


        /// <summary>移除指定C#脚本的所有行和块注释</summary>
        /// <param name="scriptWithComments">含有注释的C#代码</param>
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
}
//3203行 --删除
//3286行 --删除左操作符
//3669行
//3694行--删除索引器
//3888行
//3984行
//4132行
//4225行
//4404行
//4562行
//优化思路：SubString()方法的使用，尽量不要使用，因为这个方法会创建一个新的字符串，而不是引用原来的字符串，这样会导致内存的浪费
//改进：使用Span<T>结构体，这个结构体可以引用原来的字符串，而不是创建一个新的字符串，这样就不会导致内存的浪费