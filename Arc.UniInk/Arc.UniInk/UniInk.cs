/************************************************************************************************************************
 *  📰 Title    : UniInk (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                          *
 *  🔖 Version  : 1.0.0                                                                                                 *
 *  👩‍💻 Author   : Arc (https://github.com/Arc-huangjingtong)                                                            *
 *  🔑 Licence  : MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)           *
 *  🔍 Origin   : ExpressionEvaluator (https://github.com/codingseb/ExpressionEvaluator)                                *
 *  🤝 Support  : [.NET Framework 4+] [C# 8.0+] [Support IL2CPP]                                                        *
 *  📝 Desc     : High performance & Easy-use C# Simple Interpreter                                                     *
 *  🆘 Helper   : RegexStudy      : (https://regex101.com/r/0PN0yS/1)                                                   *
 *  🆘 Helper   : Reflection      : (https://mattwarren.org/2016/12/14/Why-is-Reflection-slow/)                         *
/************************************************************************************************************************/

namespace Arc.UniInk
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Reflection;
    using System.Linq;


    public class UniInk
    {
        /// <summary> Constructor </summary>
        /// <param name="context"  > Set context use as "This" or use internal member variables directly </param>
        /// <param name="variables"> Set variables can replace a key string with value object            </param>
        public UniInk(object context = null, Dictionary<string, object> variables = null)
        {
            Context = context;
            Variables = variables ?? new Dictionary<string, object>();

            regex_Operator = new Regex($"^({string.Join("|", dic_Operators.Keys.Select(Regex.Escape))})", RegexOptions.Compiled);
            ParsingMethods = new List<ParsingMethodDelegate>
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
        }

        protected readonly Regex regex_Operator;


        /// <summary><b>Match variable or function</b><list type="table">
        /// <item><term>sign             </term><description> : the [+]  or [-]  in front of variable or function names </description></item>
        /// <item><term>prefixOperator   </term><description> : the [++] or [--] in front of variable or function names </description></item>
        /// <item><term>varKeyword       </term><description> : the keywords : [var]                eg:var a = 1;       </description></item>
        /// <item><term>nullConditional  </term><description> : the nullConditional keywords [?]    eg:object?.GetType()</description></item>
        /// <item><term>inObject         </term><description> : the dot operator keywords [.]       eg:object.GetType() </description></item>
        /// <item><term>name             </term><description> : the variable or function`s name.                        </description></item>
        /// <item><term>assignOperator   </term><description> : the assignOperator like assignmentPrefix add [=]        </description></item>
        /// <item><term>assignmentPrefix </term><description> : has [+] [-] [*] [/] [%] [&amp;] [|] [^] [??]            </description></item>
        /// <item><term>postfixOperator  </term><description> : [++] or [--] at the back of variable or function names  </description></item>
        /// <item><term>isGeneric        </term><description> : is Generic?                                             </description></item>
        /// <item><term>genTag           </term><description> : the [&lt;] [&gt;] in Generic type                       </description></item>
        /// <item><term>isFunction       </term><description> : the [(] in function                                     </description></item> 
        /// </list></summary>
        protected static readonly Regex regex_VarOrFunction = new(@"^((?<prefixOperator>[+][+]|--)|(?<varKeyword>var)\s+|((?<nullConditional>[?])?(?<inObject>\.))?)(?<name>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)((?<assignOperator>(?<assignmentPrefix>[+\-*/%&|^]|\?\?)?=(?![=>]))|(?<postfixOperator>([+][+]|--)(?![\p{L}_0-9]))|((?<isgeneric>[<](?>([\p{L}_](?>[\p{L}_0-9]*)|(?>\s+)|[,\.])+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?(?<isfunction>[(])?))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary><b>Match functionArgKeywords</b><list type="table">
        /// <item><term>keyword          </term><description> : the keywords : [out] [ref] [in]                         </description></item>
        /// <item><term>typeName         </term><description> : made up of : letter[a-z] [.] [[]] [?]                   <para/>
        ///                                                     you can Declare variables in function args              </description></item>
        /// <item><term>toEval           </term><description> : the string will to Evaluate to object                   </description></item>
        /// <item><term>varName          </term><description> : the string of arg name                                  </description></item>
        /// </list></summary>
        protected static readonly Regex regex_funcArg = new(@"^\s*(?<keyword>out|ref|in)\s+((?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)\s+(?=[\p{L}_]))?(?<toEval>(?<varName>[\p{L}_](?>[\p{L}_0-9]*))\s*(=.*)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary><b>Match number</b><list type="table">
        /// <item><term>sign             </term><description> : the [+]  or [-]  in front of number                     </description></item>
        /// <item><term>hasDecimal       </term><description> : has [.] in number?                                      </description></item>
        /// <item><term>type             </term><description> : the keywords :[u] [l] [d] [f] [m] [ul]                  </description></item>
        /// </list>⚠️Don't support: underline:[33_000], scientific format[34.e+23] and hexadecimal format[0x34]</summary>
        protected static readonly Regex regex_Number = new(@"^(?<sign>[+-])?([\d]+)(?<hasdecimal>[\.]?([\d]+))?(?<type>ul|[fdulm])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary><b>Match string's Begin ["]</b><list type="table">
        /// <item><term>interpolated     </term><description> : the [$] in front of string                              </description></item>
        /// <item><term>escaped          </term><description> : the [@] in front of string                              </description></item>
        /// </list>⚠️Although we recognize [$] [@], we don‘t support that and throw an exception</summary>
        protected static readonly Regex regex_StringBegin = new("^(?<interpolated>[$])?(?<escaped>[@])?[\"]", RegexOptions.Compiled);

        /// <summary><b> Match and decode Generics </b><list type="table">
        /// <item><term>name             </term><description> : the type name                                           </description></item>
        /// <item><term>isGeneric        </term><description> : is Generic if group match success                       </description></item>
        /// </list>⚠️the group genTag is use for balance two side</summary>
        protected static readonly Regex regex_Generics = new("(?<name>[^,<>]+)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?", RegexOptions.Compiled);

        /// <summary><b> Match string (excluded [\"] ) </b><list type="table">
        /// <item><term>interpolated     </term><description> : match the [$]                                           </description></item>
        /// <item><term>escaped          </term><description> : match the [@]                                           </description></item>
        /// <item><term>string           </term><description> : match the string                                        </description></item>
        /// </list>⚠️(excluded [\"] )</summary>
        protected static readonly Regex regex_String = new("^(?<interpolated>[$])?(?<escaped>[@])?(?<string>[\"](?>([^\"])*)[\"])", RegexOptions.Compiled);


        /// <summary><b> Match char and Escaped char ['\\'] ['\''] [\0] [\a] [\b] [\f] [\n] [\r] [\t] [\v] </b></summary>
        protected static readonly Regex regex_Char = new(@"^['](?<char>([\\][\\'0abfnrtv]|[^']))[']", RegexOptions.Compiled);

        /// <summary><b> Match (two-dimensional) array type </b></summary>
        protected static readonly Regex regex_Array = new(@"^(\s*(\[(?>(?>\s+)|[,])*)\])+", RegexOptions.Compiled);

        /// <summary><b> Match Lambda Expression </b><list type="table">
        /// <item><term>args             </term><description> : the Lambda Expression's all args                        </description></item>
        /// <item><term>expression       </term><description> : the Lambda Expression                                   </description></item>
        /// </list></summary>
        protected static readonly Regex regex_LambdaExpression = new(@"^(?>\s*)(?<args>((?>\s*)[(](?>\s*)([\p{L}_](?>[\p{L}_0-9]*)(?>\s*)([,](?>\s*)[\p{L}_][\p{L}_0-9]*(?>\s*))*)?[)])|[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)=>(?<expression>.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary><b> Match Lambda an arg </b></summary>
        protected static readonly Regex regex_LambdaArg = new(@"[\p{L}_](?>[\p{L}_0-9]*)", RegexOptions.Compiled);

        /// <summary><b> Match cast grammar </b><list type="table">
        /// <item><term>typeName         </term><description> : the cast type name                                      </description></item>
        /// </list></summary>
        protected static readonly Regex regex_Cast = new(@"^\((?>\s*)(?<typeName>[\p{L}_][\p{L}_0-9\.\[\]<>]*[?]?)(?>\s*)\)", RegexOptions.Compiled);

        /// <summary><b> Match Parentheses  block Keyword </b><list type="table">
        /// <item><term>keyword          </term><description> : match keyword : while||for||foreach||if||else||catch    </description></item>
        /// </list></summary>
        protected static readonly Regex regex_BlockKeywordBegin = new(@"^(?>\s*)(?<keyword>while|for|foreach|if|else(?>\s*)if|catch)(?>\s*)[(]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary><b> Match NoParentheses  block Keyword </b><list type="table">
        /// <item><term>keyword          </term><description> : match keyword : else||do||try||finally                  </description></item>
        /// </list></summary>
        protected static readonly Regex regex_BlockKeywordBegin_NoParentheses = new(@"^(?>\s*)(?<keyword>else|do|try|finally)(?![\p{L}_0-9])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary><b> Match block Begin [{] </b></summary>
        protected static readonly Regex regex_BlockBegin = new(@"^(?>\s*)[{]", RegexOptions.Compiled);

        /// <summary><b> Match xx in xx  [foreach] </b></summary>
        protected static readonly Regex regex_ForeachParenThisEvaluation = new(@"^(?>\s*)(?<variableName>[\p{L}_](?>[\p{L}_0-9]*))(?>\s*)(?<in>in)(?>\s*)(?<collection>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary><b> Match keyword [return] </b></summary>
        protected static readonly Regex regex_Return = new(@"^return((?>\s*)|\()", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary><b> Match end [;] </b></summary>
        protected static readonly Regex regex_ExpressionEnd = new(@"^(?>\s*)[;]", RegexOptions.Compiled);


        /// <summary> If  Statement in Script Only</summary>
        protected enum EBlockState_If { NoBlock, If, ElseIf }


        /// <summary> Catch Types in Method <see cref="GetTypeByName"/> </summary>
        protected static readonly Dictionary<string, Type> dic_CachedTypes = new();

        /// <summary> Some Primary Types in Method <see cref="GetTypeByName"/></summary>
        protected static readonly Dictionary<string, Type> dic_PrimaryTypes = new()
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

        /// <summary> Some custom default object in Method </summary>
        protected static readonly Dictionary<string, object> dic_DefaultVariables = new(StringComparer.OrdinalIgnoreCase)
        {
            { "false", false },
            { "true", true },
            { "this", null },
            { "Pi", Math.PI },
            { "E", Math.E },
            { "null", null },
        };

        /// <summary> Some number Suffix Parse Method  </summary>
        protected static readonly Dictionary<string, Func<string, object>> dic_numberParseFunc = new()
        {
            { "f", number => float.Parse(number) },
            { "d", number => double.Parse(number) },
            { "u", number => uint.Parse(number) },
            { "l", number => long.Parse(number) },
            { "ul", number => ulong.Parse(number) },
            { "m", number => decimal.Parse(number) }
        };

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

        /// <summary> Some Operators string mapping   </summary>
        public static readonly Dictionary<string, ExpressionOperator> dic_Operators = new(StringComparer.Ordinal)
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

        /// <summary> Some UnaryPostfix Operators mark</summary>
        protected static readonly List<ExpressionOperator> Operators_UnaryPostfix = new()
        {
            ExpressionOperator.LogicalNegation, // !a 逻辑取反
            ExpressionOperator.BitwiseComplement, // ~a 位运算取反
            ExpressionOperator.UnaryPlus, // +a 一元加号,表示正数符号
            ExpressionOperator.UnaryMinus // -a 一元减号,表示负数符号
        };

        /// <summary> Some UnaryPostfix Operators mark</summary>
        protected static readonly Dictionary<ExpressionOperator, Func<object, object, object>> dic_OperatorsFunc = new()
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
            { ExpressionOperator.Is, (left, right) => left != null && ((Type)right).IsInstanceOfType(left) }, // 类型判断
            { ExpressionOperator.Equal, (left, right) => left == right }, // 等于
            { ExpressionOperator.NotEqual, (left, right) => left != right }, // 不等于
            { ExpressionOperator.LogicalAnd, (left, right) => (int)left & (int)right }, // 逻辑与
            { ExpressionOperator.LogicalXor, (left, right) => (int)left ^ (int)right }, // 逻辑异或
            { ExpressionOperator.LogicalOr, (left, right) => (int)left | (int)right }, // 逻辑或
            {
                ExpressionOperator.ConditionalAnd, (left, right) =>
                {
                    if (!(bool)left) return false;
                    return (bool)left && (bool)right; // 条件与
                }
            },
            {
                ExpressionOperator.ConditionalOr, (left, right) =>
                {
                    if ((bool)left) return true;
                    return (bool)left || (bool)right; // 条件或
                }
            },
            { ExpressionOperator.NullCoalescing, (left, right) => left ?? right }, // 空合并
        };

        /// <summary> Some simple Double MathFunc </summary>
        protected static readonly Dictionary<string, Func<double, double>> dic_simpleDoubleMathFunc = new()
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

        /// <summary> Some complex StandardFunc </summary>
        protected static readonly Dictionary<string, Func<UniInk, List<string>, object>> dic_complexStandardFunc = new()
        {
            { "Avg", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Sum() / args.Count },
            { "AList", (self, args) => args.ConvertAll(arg => self.Evaluate(arg)) },
            { "Max", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Max() },
            { "Min", (self, args) => args.ConvertAll(arg => Convert.ToDouble(self.Evaluate(arg))).Min() },
            {
                "new", (self, args) =>
                {
                    var cArgs = args.ConvertAll(arg => self.Evaluate(arg));
                    return cArgs[0] is Type type ? Activator.CreateInstance(type, cArgs.Skip(1).ToArray()) : null;
                }
            },
            { "Sign", (self, args) => Math.Sign(Convert.ToDouble(self.Evaluate(args[0]))) }
        };

        /// <summary>Custom Variables for Evaluate</summary>
        public Dictionary<string, object> Variables { get; set; }

        /// <summary> Custom Assembly List </summary>
        public IList<Assembly> Assemblies
        {
            get => assemblies ??= currentAssemblies;
            set => assemblies = value;
        }

        /// <summary> Custom Namespaces same as <c>using Namespace;</c> </summary>
        public List<string> Namespaces { get; set; } = new()
        {
            "System",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Collections",
            "System.Collections.Generic"
        };

        /// <summary> Custom types in UniInk </summary>
        public List<Type> Types { get; } = new();

        /// <summary> Custom static types looking for extension methods in UniInk </summary>
        public List<Type> StaticTypesForExtensionsMethods { get; } = new()
        {
            typeof(Enumerable) // Linq Extension Methods
        };

        /// <summary>the Context is same as can be omitted [this]</summary>
        public object Context
        {
            get => dic_DefaultVariables["this"];
            set
            {
                dic_DefaultVariables["this"] = value;
                if (value != null)
                {
                    ContextMethods.Clear();
                    ContextMembers.Clear();
                    ContextMethods.AddRange(value.GetType().GetMethods(InstanceBindingFlag));
                    ContextMembers.AddRange(value.GetType().GetMembers(BindingFlag));
                }
            }
        }

        public List<MethodInfo> ContextMethods { get; } = new();
        public List<MemberInfo> ContextMembers { get; } = new();


        /// <summary> Current appDomain all assemblies </summary>
        protected static readonly IList<Assembly> currentAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

        protected IList<Assembly> assemblies;


        private static BindingFlags BindingFlag => BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField;
        private static BindingFlags InstanceBindingFlag => BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.GetProperty;
        private static BindingFlags StaticBindingFlag => BindingFlags.Public | BindingFlags.Static;


        /// <summary>Evaluate a Script,support [if] State and so on</summary>
        /// <typeparam name="T">cast Type</typeparam>
        /// <param name="script">the script string (Separator is [;])</param>
        /// <returns>the last expression return</returns>
        public T ScriptEvaluate<T>(string script) => (T)ScriptEvaluate(script);

        /// <summary>Evaluate a Script,support [if] State and so on</summary>
        /// <param name="script">the script string (Separator is [;])</param>
        /// <returns>the last expression return object</returns>
        public object ScriptEvaluate(string script)
        {
            var isReturn = false;
            var isBreak = false;
            var isContinue = false;

            var result = ScriptEvaluate(script, ref isReturn, ref isBreak, ref isContinue);

            if (isBreak) throw new SyntaxException("Invalid keyword :[break]");
            if (isContinue) throw new SyntaxException("Invalid keyword :[continue]");

            return result;
        }


        private object ScriptEvaluate(string script, ref bool valueReturned, ref bool breakCalled, ref bool continueCalled)
        {
            object lastResult = null;
            var isReturn = valueReturned;
            var isBreak = breakCalled;
            var isContinue = continueCalled;
            var BlockState_If = EBlockState_If.NoBlock;
            var ifElseStatementsList = new List<List<string>>();

            script = script.Trim();

            var result = (object)null;

            var scriptLength = script.Length;
            var startIndex = 0;
            var endIndex = 0;


            while (!isReturn && !isBreak && !isContinue && endIndex < scriptLength)
            {
                var blockKeywordsBeginMatch_NoParentheses = regex_BlockKeywordBegin_NoParentheses.Match(script, endIndex, scriptLength - endIndex);
                var blockKeywordsBeginMatch = regex_BlockKeywordBegin.Match(script, endIndex, scriptLength - endIndex);

                var bkbnpMatchSuss = blockKeywordsBeginMatch_NoParentheses.Success;
                var bkbMatchSuss = blockKeywordsBeginMatch.Success;

                if (bkbnpMatchSuss || bkbMatchSuss)
                {
                    endIndex += bkbMatchSuss ? blockKeywordsBeginMatch.Length : blockKeywordsBeginMatch_NoParentheses.Length;

                    var keyword = bkbMatchSuss ? blockKeywordsBeginMatch.Groups["keyword"].Value : blockKeywordsBeginMatch_NoParentheses?.Groups["keyword"].Value ?? string.Empty;
                    var keywordAttributes = bkbMatchSuss ? GetExpressionsParenthesized(script, ref endIndex, true, ';') : null;

                    if (bkbMatchSuss) endIndex++; // skip the [)]

                    var blockBeginningMatch = regex_BlockBegin.Match(script, endIndex, scriptLength - endIndex);

                    var subScript = string.Empty;

                    if (blockBeginningMatch.Success)
                    {
                        endIndex += blockBeginningMatch.Length;

                        subScript = GetExpressionsParenthesized(script, ref endIndex, false, ';', '{', '}').FirstOrDefault();

                        endIndex++;
                    }
                    else
                    {
                        var continueExpressionParsing = true;
                        startIndex = endIndex;

                        while (endIndex < scriptLength && continueExpressionParsing)
                        {
                            var parseResult = TryParseStringAndParenthisAndCurlyBrackets(script, ref endIndex);

                            if (!parseResult && script[endIndex] == ';')
                            {
                                subScript = script.Substring(startIndex, endIndex + 1 - startIndex);
                                continueExpressionParsing = false;
                            }

                            endIndex++;
                        }

                        if (string.IsNullOrWhiteSpace(subScript))
                        {
                            throw new SyntaxException($" no instruction after the [{keyword}]");
                        }
                    }

                    if (keyword.Equals("elseif"))
                    {
                        if (BlockState_If == EBlockState_If.NoBlock) throw new SyntaxException("no [if] with [else if] ");


                        ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                        BlockState_If = EBlockState_If.ElseIf;
                    }
                    else if (keyword.Equals("else"))
                    {
                        if (BlockState_If == EBlockState_If.NoBlock) throw new SyntaxException("no [if] with [else] ");

                        ifElseStatementsList.Add(new List<string> { "true", subScript });
                        BlockState_If = EBlockState_If.NoBlock;
                    }
                    else
                    {
                        ExecuteIfList();

                        if (keyword.Equals("if"))
                        {
                            ifElseStatementsList.Add(new List<string> { keywordAttributes[0], subScript });
                            BlockState_If = EBlockState_If.If;
                        }
                        else if (keyword.Equals("do"))
                        {
                            if ((blockKeywordsBeginMatch = regex_BlockKeywordBegin.Match(script.Substring(endIndex))).Success && blockKeywordsBeginMatch.Groups["keyword"].Value.Equals("while"))
                            {
                                endIndex += blockKeywordsBeginMatch.Length;
                                keywordAttributes = GetExpressionsParenthesized(script, ref endIndex, true, ';');

                                endIndex++;

                                Match nextIsEndOfExpressionMatch;

                                if ((nextIsEndOfExpressionMatch = regex_ExpressionEnd.Match(script.Substring(endIndex))).Success)
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
                            var foreachParenthisEvaluationMatch = regex_ForeachParenThisEvaluation.Match(keywordAttributes[0]);

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
                    ExecuteIfList();

                    var parseResult = TryParseStringAndParenthisAndCurlyBrackets(script, ref endIndex);

                    if (!parseResult && script[endIndex] == ';')
                    {
                        lastResult = ScriptExpressionEvaluate(ref endIndex);
                    }

                    BlockState_If = EBlockState_If.NoBlock;

                    endIndex++;
                }
            }

            if (!string.IsNullOrWhiteSpace(script.Substring(startIndex)) && !isReturn && !isBreak && !isContinue)
                throw new SyntaxException($"{script} missing [;] !");

            ExecuteIfList();

            valueReturned = isReturn;
            breakCalled = isBreak;
            continueCalled = isContinue;

            return lastResult;

            void ExecuteIfList()
            {
                if (ifElseStatementsList.Count == 0) return;
                var ifScript = ifElseStatementsList.Find(statement => (bool)ManageJumpStatementsOrExpressionEval(statement[0]))?[1];

                if (!string.IsNullOrEmpty(ifScript))
                    lastResult = ScriptEvaluate(ifScript, ref isReturn, ref isBreak, ref isContinue);

                ifElseStatementsList.Clear();
            }

            //依次解释指定段落的脚本
            object ScriptExpressionEvaluate(ref int index)
            {
                var expression = script.Substring(startIndex, index - startIndex);

                startIndex = index + 1;

                return ManageJumpStatementsOrExpressionEval(expression);
            }

            //管理跳转语句或解析表达式
            object ManageJumpStatementsOrExpressionEval(string expression)
            {
                expression = expression.Trim();

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
                    if (Evaluate(expression.Substring(6)) is Exception exception)
                    {
                        throw exception;
                    }

                    throw new SyntaxException("[throw] missing [Exception] instance");
                }

                expression = regex_Return.Replace(expression, match =>
                {
                    isReturn = true;
                    return match.Value.Contains("(") ? "(" : string.Empty;
                });

                return Evaluate(expression);
            }
        }

        /// Parse [string] and [( )] and [{ }] with start index
        private bool TryParseStringAndParenthisAndCurlyBrackets(string script, ref int index)
        {
            var parsed = true;
            var scriptLength = script.Length;
            var internalStringMatch = regex_String.Match(script, index, scriptLength - index);

            if (internalStringMatch.Success)
            {
                var innerString = internalStringMatch.Groups["string"].Value;
                index += innerString.Length - 1;
            }
            else
                switch (script[index])
                {
                    case '(':
                        index++;
                        GetExpressionsParenthesized(script, ref index, false);
                        break;
                    case '{':
                        index++;
                        GetExpressionsParenthesized(script, ref index, false, ';', '{', '}');
                        break;
                    default:
                    {
                        var charMatch = regex_Char.Match(script, index, scriptLength - index);

                        if (charMatch.Success)
                        {
                            index += charMatch.Length - 1;
                        }

                        parsed = false;
                        break;
                    }
                }

            return parsed;
        }


        /// <summary> Evaluate a expression and cast type             </summary>
        /// <returns> return the result cast target type if success   </returns>
        public T Evaluate<T>(string expression, int startIndex = 0) => (T)Evaluate(expression, startIndex);

        /// <summary> Evaluate a expression      </summary>
        /// <returns> return the result object   </returns>
        public object Evaluate(string expression, int startIndex = 0)
        {
            var stack = new Stack<object>();

            if (GetLambdaExpression(expression, stack, startIndex))
            {
                return stack.Pop(); //然后出栈
            }

            try
            {
                for (var i = startIndex; i < expression.Length; i++)
                {
                    if (ParsingMethods.Any(parsingMethod => parsingMethod(expression, stack, ref i))) continue;
                    if (char.IsWhiteSpace(expression[i])) continue;

                    throw new SyntaxException($"Invalid character : [{(int)expression[i]}:{expression[i]}] at [{i}  {expression}] ");
                }

                return ProcessStack(stack);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                while (exception.InnerException is TargetInvocationException innerException)
                {
                    exception = innerException;
                }

                throw;
            }
        }

        private readonly List<ParsingMethodDelegate> ParsingMethods;


        /// <summary>Evaluate Cast _eg:(int)object</summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns>Evaluate is successful?</returns>
        private bool EvaluateCast(string expression, Stack<object> stack, ref int i)
        {
            var castMatch = regex_Cast.Match(expression, i, expression.Length - i);

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

        /// <summary>Evaluate Number _eg: -3.64f </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns>Evaluate is successful?</returns>
        private bool EvaluateNumber(string expression, Stack<object> stack, ref int i)
        {
            var numberMatch = regex_Number.Match(expression, i, expression.Length - i);
            //make sure match number sign is not a operator
            if (numberMatch.Success && (!numberMatch.Groups["sign"].Success || stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                i += numberMatch.Length - 1;

                if (numberMatch.Groups["type"].Success)
                {
                    var type = numberMatch.Groups["type"].Value;
                    var numberNoType = numberMatch.Value.Replace(type, string.Empty);

                    if (dic_numberParseFunc.TryGetValue(type, out var parseFunc))
                    {
                        stack.Push(parseFunc(numberNoType));
                    }
                }
                else if (numberMatch.Groups["hasdecimal"].Success) //without the type suffix as double
                {
                    stack.Push(double.Parse(numberMatch.Value));
                }
                else
                {
                    stack.Push(int.Parse(numberMatch.Value));
                }

                return true;
            }

            return false;
        }

        /// <summary> Evaluate Function or declaration of variable _eg: int a (= 0) </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns>
        /// <exception cref="SyntaxException">some syntax error,those make evaluate fail</exception>
        private bool EvaluateVarOrFunc(string expression, Stack<object> stack, ref int i)
        {
            var varFuncMatch = regex_VarOrFunction.Match(expression, i, expression.Length - i);

            if (!varFuncMatch.Success) return false;

            var hasVar = varFuncMatch.Groups["varKeyword"].Success;
            var hasAssign = varFuncMatch.Groups["assignOperator"].Success;
            var hasPostfix = varFuncMatch.Groups["postfixOperator"].Success;
            var hasPrefix = varFuncMatch.Groups["prefixOperator"].Success;
            var hasNullConditional = varFuncMatch.Groups["nullConditional"].Success;

            if (hasVar && !hasAssign) throw new SyntaxException($"The implicit variable is not initialized! [var {varFuncMatch.Groups["name"].Value}]");


            var isInObject = varFuncMatch.Groups["inObject"].Success;
            var isFunction = varFuncMatch.Groups["isfunction"].Success;

            var varFuncName = varFuncMatch.Groups["name"].Value;

            if (!isInObject && dic_Operators.ContainsKey(varFuncName)) return false;

            i += varFuncMatch.Length;

            if (isFunction)
            {
                var funcArgs = GetExpressionsParenthesized(expression, ref i, true);

                //如果是对象的方法,或者是this的方法
                if (isInObject || ContextMethods.Any(methodInfo => methodInfo.Name.Equals(varFuncName)))
                {
                    var hasPush = HandleInObjectMember(isInObject, hasNullConditional, stack, varFuncMatch, out var obj);

                    if (!hasPush)
                    {
                        var argIndex = 0;
                        var funArgWrappers = new List<FunArgWrapper>();

                        var oArgs = funcArgs.ConvertAll(arg =>
                        {
                            var funcArgMatch = regex_funcArg.Match(arg);
                            var toEval = arg;

                            if (funcArgMatch.Success)
                            {
                                var keyword = funcArgMatch.Groups["keyword"].Value;
                                var varName = funcArgMatch.Groups["varName"].Value;

                                var funArgWrapper = new FunArgWrapper(argIndex, keyword, varName);

                                funArgWrappers.Add(funArgWrapper);

                                if (funcArgMatch.Groups["typeName"].Success)
                                {
                                    var typeName = funcArgMatch.Groups["typeName"].Value;
                                    var fixedType = (Type)Evaluate(typeName);
                                    Variables[funArgWrapper.VariableName] = new StronglyTypedVariable(fixedType, GetDefaultValueOfType(fixedType));
                                }

                                toEval = funcArgMatch.Groups["toEval"].Value;
                            }

                            argIndex++;

                            return Evaluate(toEval);
                        });

                        HandleTypeObject(ref obj, out var objType, out _);

                        // 寻找标准实例或公共方法
                        var methodInfo = GetMethod(objType, varFuncName, oArgs, string.Empty, Type.EmptyTypes);


                        var isExtension = false;


                        if (methodInfo == null)
                        {
                            oArgs.Insert(0, obj);
                            objType = obj.GetType();

                            foreach (var type in StaticTypesForExtensionsMethods)
                            {
                                methodInfo = GetMethod(type, varFuncName, oArgs, string.Empty, Type.EmptyTypes);
                                if (methodInfo != null)
                                {
                                    isExtension = true;
                                    break;
                                }
                            }
                        }

                        if (methodInfo != null) //如果找到了方法，则更具是否是扩展方法来传参调用
                        {
                            stack.Push(methodInfo.Invoke(isExtension ? null : obj, oArgs.ToArray()));
                            var argsKeyword = funArgWrappers.FindAll(argWithKeyword => argWithKeyword.Keyword.Equals("out") || argWithKeyword.Keyword.Equals("ref"));
                            argsKeyword.ForEach(outOrRefArg => AssignVariable(outOrRefArg.VariableName, oArgs[outOrRefArg.Index + (isExtension ? 1 : 0)]));
                        }
                        else
                        {
                            throw new SyntaxException($"[{objType}] don‘t find  [{varFuncName}] ");
                        }
                    }
                }
                else
                {
                    if (TryGetFunctions(varFuncName, funcArgs, out var funcResult))
                    {
                        stack.Push(funcResult);
                    }
                    else
                    {
                        throw new SyntaxException($"unknown function : [{varFuncName}] in [{i}] [{expression}]");
                    }
                }
            }
            else //是变量，对象的情况
            {
                if (isInObject || ContextMembers.Any(memberInfo => memberInfo.Name.Equals(varFuncName)))
                {
                    var hasPush = HandleInObjectMember(isInObject, hasNullConditional, stack, varFuncMatch, out var obj);
                    if (!hasPush)
                    {
                        HandleTypeObject(ref obj, out var objType, out var valueTypeNestingTrace);

                        MemberInfo member = objType?.GetProperty(varFuncName, BindingFlag);
                        member ??= objType?.GetField(varFuncName, BindingFlag);
                        object varValue = null; //TODO:
                        var assign = true;


                        if (member == null)
                        {
                            var methodsGroup = objType?.GetMethods(BindingFlag).Where(methodInfo => methodInfo.Name.Equals(varFuncName)).ToArray();
                            if (methodsGroup is { Length: > 0 })
                            {
                                varValue = new MethodsGroupWrapper { ContainerObject = obj, MethodsGroup = methodsGroup };
                            }
                        }

                        var pushVarValue = true;

                        //Var去设置值  且 值为null 且 pushVarValue为true
                        if (varValue == null)
                        {
                            varValue = (member as PropertyInfo)?.GetValue(obj);
                            varValue ??= (member as FieldInfo)?.GetValue(obj);

                            //TODO: 这里有问题
                            if (varValue?.GetType().IsPrimitive ?? false)
                            {
                                stack.Push(valueTypeNestingTrace = new ValueTypeWrapper { Container = valueTypeNestingTrace ?? obj, Member = member, Value = varValue });

                                pushVarValue = false;
                            }
                        }

                        if (pushVarValue) stack.Push(varValue);


                        if (hasAssign)
                        {
                            varValue = ManageKindOfAssignation(expression, ref i, varFuncMatch, varValue, stack);
                        }
                        else if (hasPostfix)
                        {
                            //不是++就是--;
                            if (varValue != null)
                            {
                                varValue = varFuncMatch.Groups["postfixOperator"].Value.Equals("++") ? (int)varValue + 1 : (int)varValue - 1;
                            }
                        }
                        else
                        {
                            assign = false;
                        }

                        if (assign)
                        {
                            if (valueTypeNestingTrace != null)
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
                else
                {
                    if (TryGetVariable(varFuncName, out var varValue))
                    {
                        stack.Push(varValue);
                    }
                    else if (Variables.TryGetValue(varFuncName, out var cusVarValueToPush) || hasAssign || (stack.Count == 1 && stack.Peek() is Type))
                    {
                        if (stack.Count == 1 && stack.Peek() is Type type)
                        {
                            stack.Pop();

                            Variables[varFuncName] = new StronglyTypedVariable(type, GetDefaultValueOfType(type));
                        }

                        if (cusVarValueToPush is StronglyTypedVariable typedVariable)
                        {
                            cusVarValueToPush = typedVariable.Value;
                        }

                        stack.Push(cusVarValueToPush);

                        if (hasAssign)
                        {
                            cusVarValueToPush = ManageKindOfAssignation(expression, ref i, varFuncMatch, cusVarValueToPush, stack);
                            AssignVariable(varFuncName, cusVarValueToPush);
                        }
                        else if (hasPostfix)
                        {
                            cusVarValueToPush = varFuncMatch.Groups["postfixOperator"].Value.Equals("++") ? (int)cusVarValueToPush + 1 : (int)cusVarValueToPush - 1;
                            AssignVariable(varFuncName, cusVarValueToPush);
                        }
                        else if (hasPrefix)
                        {
                            stack.Pop();
                            cusVarValueToPush = varFuncMatch.Groups["prefixOperator"].Value.Equals("++") ? (int)cusVarValueToPush + 1 : (int)cusVarValueToPush - 1;
                            stack.Push(cusVarValueToPush);
                            AssignVariable(varFuncName, cusVarValueToPush);
                        }
                    }
                    else
                    {
                        var genericTypes = varFuncMatch.Groups["isgeneric"].Value;
                        TryGetStaticType(expression, ref i, varFuncName, stack, genericTypes);
                    }
                }

                i--;
            }

            return true;
        }

        private void TryGetStaticType(string expression, ref int i, string varFuncName, Stack<object> stack, string genericTypes = "")
        {
            var staticType = EvaluateType(expression, ref i, varFuncName, genericTypes);

            if (staticType != null)
            {
                stack.Push(staticType);
            }
            else
            {
                throw new SyntaxException($"变量 [{varFuncName}] 在脚本中未知 : [{expression}]");
            }
        }


        private bool HandleInObjectMember(bool isInObject, bool hasNullConditional, Stack<object> stack, Match varFuncMatch, out object obj)
        {
            if (isInObject && (stack.Count == 0 || stack.Peek() is ExpressionOperator))
            {
                throw new SyntaxException($"[{varFuncMatch.Value})] must follow a object");
            }

            obj = isInObject ? stack.Pop() : Context;


            if (obj is null)
            {
                if (hasNullConditional)
                {
                    stack.Push(null);
                    return true;
                }
                else
                {
                    throw new SyntaxException($"[{varFuncMatch.Value}] is null!");
                }
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
            if (expression[i].Equals('\''))
            {
                i++;
                if (expression[i].Equals('\\'))
                {
                    i++;

                    if (dic_EscapedChar.TryGetValue(expression[i], out var value))
                    {
                        stack.Push(value);
                        i++;
                    }
                    else
                    {
                        throw new SyntaxException($"Unknown escape character[{expression[i]}] : You can customize them in [dic_EscapedChar]");
                    }
                }
                else if (expression[i].Equals('\''))
                {
                    throw new SyntaxException($"Illegal character[{i}] : ['']");
                }
                else
                {
                    stack.Push(expression[i]);
                    i++;
                }

                if (expression[i].Equals('\'')) return true;


                throw new SyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            return false;
        }

        /// <summary>Evaluate Operators in <see cref="dic_Operators"/></summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private bool EvaluateOperators(string expression, Stack<object> stack, ref int i)
        {
            var operatorMatch = regex_Operator.Match(expression, i, expression.Length - i);

            if (operatorMatch.Success)
            {
                var op = operatorMatch.Value;

                switch (op)
                {
                    //排除一元运算符的可能性
                    case "+" or "-" when stack.Count == 0 || stack.Peek() is ExpressionOperator:
                        stack.Push(op == "+" ? ExpressionOperator.UnaryPlus : ExpressionOperator.UnaryMinus);
                        break;
                    default:
                        stack.Push(dic_Operators[op]);
                        break;
                }

                i += op.Length - 1;
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

                    if (stack.Pop() is InternalDelegate lambdaDelegate)
                        stack.Push(lambdaDelegate(expressionsInParenthis.ConvertAll(str => Evaluate(str))));
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

        /// <summary>Evaluate TernaryConditionalOperator _eg:a>b?c:d </summary>
        /// <remarks>the match will recursive execute <see cref="Evaluate"/> </remarks>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="stack"> the object stack to push or pop     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <returns> the evaluate is success or not </returns> 
        private bool EvaluateTernaryConditionalOperator(string expression, Stack<object> stack, ref int i)
        {
            if (expression[i].Equals('?'))
            {
                var condition = (bool)ProcessStack(stack);

                for (var j = i + 1; j < expression.Length; j++)
                {
                    var s2 = expression[j];

                    var internalStringMatch = regex_String.Match(expression, j, expression.Length - j);

                    if (internalStringMatch.Success)
                    {
                        var innerString = internalStringMatch.Groups["string"].Value; //TODO:当字符串没有另一边的引号时，错误不会在此处抛出
                        j += innerString.Length - 1;
                    }
                    else if (s2.Equals('('))
                    {
                        j++;
                        GetExpressionsParenthesized(expression, ref j, false);
                    }
                    else if (s2.Equals(':'))
                    {
                        stack.Clear();

                        stack.Push(condition ? Evaluate(expression.Substring(i + 1, j - i - 1)) : Evaluate(expression, j + 1));

                        i = expression.Length;

                        return true;
                    }
                }
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
            var stringBeginningMatch = regex_StringBegin.Match(expression, i, expression.Length - i);

            if (!stringBeginningMatch.Success) return false;

            var isEscaped = stringBeginningMatch.Groups["escaped"].Success;
            var isInterpolated = stringBeginningMatch.Groups["interpolated"].Success;

            if (isEscaped || isInterpolated) throw new SyntaxException("don't support [@] [$]");

            i += stringBeginningMatch.Length;

            var stringRegexPattern = new Regex("^[^\\\"]*");

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

                        if (dic_EscapedChar.TryGetValue(expression[i], out var escapedString))
                        {
                            resultString.Append(escapedString);
                            i++;
                        }
                        else
                        {
                            throw new SyntaxException($"unknown escaped char \\{expression[i]} please add it to dic_EscapedChar");
                        }

                        break;
                    }
                }
            }

            if (!endOfString) throw new SyntaxException("missing an [ \\ ] character");

            return true;
        }


        /// <summary>Evaluate Type _eg: Int32 </summary>
        /// <param name="expression"> the expression to Evaluate     </param>
        /// <param name="i">the <see cref="expression"/> start index </param>
        /// <param name="currentName"></param>
        /// <param name="genericsTypes"></param>
        /// <returns> the evaluate result Type </returns> 
        private Type EvaluateType(string expression, ref int i, string currentName = "", string genericsTypes = "")
        {
            var typeName = $"{currentName}{(i < expression.Length && expression[i] == '?' ? '?' : string.Empty)}";
            var staticType = GetTypeByName(typeName, genericsTypes);

            if (staticType == null)
            {
                var subIndex = 0;
                var typeMatch = regex_VarOrFunction.Match(expression, i, expression.Length - i);

                if (typeMatch.Success // 
                    && !typeMatch.Groups["assignOperator"].Success //
                    && !typeMatch.Groups["postfixOperator"].Success // 
                    && !typeMatch.Groups["isfunction"].Success // 
                    && !typeMatch.Groups["inObject"].Success // 
                    && !typeName.EndsWith("?") //
                    && i < expression.Length) //
                {
                    subIndex += typeMatch.Length;
                    typeName += $"{typeMatch.Groups["name"].Value}{(i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?' ? "?" : "")}";

                    staticType = GetTypeByName(typeName, typeMatch.Groups["isgeneric"].Value);

                    if (staticType != null)
                        i += subIndex;
                }
            }

            if (typeName.EndsWith("?") && staticType != null) i++;

            // For nested type parsing
            if (staticType != null)
            {
                var nestedTypeMatch = regex_VarOrFunction.Match(expression.Substring(i));
                while (nestedTypeMatch.Success && !nestedTypeMatch.Groups["assignOperator"].Success && !nestedTypeMatch.Groups["postfixOperator"].Success && !nestedTypeMatch.Groups["isfunction"].Success)
                {
                    var subIndex = nestedTypeMatch.Length;
                    typeName += $"+{nestedTypeMatch.Groups["name"].Value}{(i + subIndex < expression.Length && expression.Substring(i + subIndex)[0] == '?' ? "?" : "")}";

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

                    nestedTypeMatch = regex_VarOrFunction.Match(expression.Substring(i));
                }
            }

            Match arrayTypeMatch;

            if (i < expression.Length && (arrayTypeMatch = regex_Array.Match(expression, i, expression.Length - i)).Success)
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

        public object ProcessStack(Stack<object> stack)
        {
            if (stack.Count == 0) throw new SyntaxException("Empty expression and Empty stack");

            var list = stack.Select(e => e is ValueTypeWrapper valueTypeNestingTrace ? valueTypeNestingTrace.Value : e); //处理值类型

            stack = new Stack<object>(list);

            object stackCache = null;
            while (stack.Count > 0)
            {
                var pop = stack.Pop();

                if (pop is not ExpressionOperator)
                {
                    stackCache = pop;
                }

                else if (pop is ExpressionOperator @operator)
                {
                    if (Operators_UnaryPostfix.Contains(@operator))
                    {
                        var right = stack.Pop();
                        stackCache = dic_OperatorsFunc[@operator](null, right);
                    }
                    else
                    {
                        var left = stackCache;
                        var right = stack.Pop();

                        stackCache = dic_OperatorsFunc[@operator](left, right);
                    }
                }
            }

            return stackCache;
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


        /// <summary>用于解析方法的委托</summary>
        private delegate bool ParsingMethodDelegate(string expression, Stack<object> stack, ref int i);


        /// <summary>用于解释方法的委托</summary>
        private delegate object InternalDelegate(params object[] args);

        /// <summary>管理分配类型</summary>
        private object ManageKindOfAssignation(string expression, ref int index, Match match, object getCurrentValue, Stack<object> stack)
        {
            if (stack?.Count > 1) throw new SyntaxException($"{expression}赋值的左边部分必须是变量,属性或索引器");

            var rightExpression = expression.Substring(index);

            if (string.IsNullOrWhiteSpace(rightExpression)) throw new SyntaxException("分配中缺少右部分");

            index = expression.Length;
            object result;
            if (match.Groups["assignmentPrefix"].Success)
            {
                var prefixOp = dic_Operators[match.Groups["assignmentPrefix"].Value];

                result = dic_OperatorsFunc[prefixOp](getCurrentValue, Evaluate(rightExpression));
            }
            else
            {
                result = Evaluate(rightExpression);
            }

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
                if (value == null && Nullable.GetUnderlyingType(stronglyTypedVariable.Type) == null)
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
                        throw new InvalidCastException($"{typeToAssign} can't ChangeType: {stronglyTypedVariable.Type}", exception);
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
        private bool GetLambdaExpression(string expression, Stack<object> stack, int startIndex)
        {
            var lambdaExpressionMatch = regex_LambdaExpression.Match(expression, startIndex, expression.Length - startIndex);

            if (!lambdaExpressionMatch.Success) return false;

            var argsNames = regex_LambdaArg.Matches(lambdaExpressionMatch.Groups["args"].Value);


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
        private MethodInfo GetMethod(Type type, string funcName, List<object> args, string genericsTypes, Type[] inferredGenericsTypes)
        {
            MethodInfo methodInfo = null;
            var modifiedArgsCache = new List<object>(args);
            var methodsInfo = ContextMethods.Where(MethodFilter);

            foreach (var info in methodsInfo)
            {
                modifiedArgsCache.Clear();
                modifiedArgsCache.AddRange(args);

                methodInfo = TryToCastMethodParametersToMakeItCallable(info, modifiedArgsCache, genericsTypes, inferredGenericsTypes);

                if (methodInfo != null) break;
            }

            if (methodInfo == null)
            {
                methodsInfo = type.GetMethods(InstanceBindingFlag).Where(MethodFilter);

                foreach (var info in methodsInfo)
                {
                    modifiedArgsCache.Clear();
                    modifiedArgsCache.AddRange(args);

                    methodInfo = TryToCastMethodParametersToMakeItCallable(info, modifiedArgsCache, genericsTypes, inferredGenericsTypes);

                    if (methodInfo != null) break;
                }
            }

            if (methodInfo == null) return null;
            args.Clear();
            args.AddRange(modifiedArgsCache);

            return methodInfo;

            bool MethodFilter(MethodInfo m)
            {
                if (!m.Name.Equals(funcName)) return false;
                var parameterInfos = m.GetParameters();
                if (parameterInfos.Length == args.Count) return true;
                if (parameterInfos.Length > 0 && parameterInfos.Last().IsDefined(typeof(ParamArrayAttribute), false)) return true;
                return parameterInfos.Length > args.Count && parameterInfos.Take(args.Count).All(p => args[p.Position] == null || p.ParameterType.IsInstanceOfType(args[p.Position])) && parameterInfos.Skip(args.Count).All(p => p.HasDefaultValue);
            }
        }


        private MethodInfo TryToCastMethodParametersToMakeItCallable(MethodInfo methodInfoToCast, List<object> modifiedArgs, string genericsTypes, Type[] inferredGenericsTypes)
        {
            MethodInfo result = null;

            var oldMethodInfo = methodInfoToCast;

            if (!string.IsNullOrWhiteSpace(genericsTypes))
            {
                methodInfoToCast = MakeConcreteMethodIfGeneric(methodInfoToCast, string.Empty, inferredGenericsTypes);
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
                            paramsForInference = Array.Find(parameterInfos, p => p.ParameterType is { IsGenericType: true, ContainsGenericParameters: true } // 
                                                                                 && p.ParameterType.GetGenericArguments().Any(subP => subP.Name.Equals(name)) //
                                                                                 && modifiedArgs.Count > p.Position && !modifiedArgs[p.Position].GetType().IsGenericType); //

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
            var parameters = methodInfoToCast.GetParameters();

            // To manage empty params argument
            if ((parameters.LastOrDefault()?.IsDefined(typeof(ParamArrayAttribute), false) ?? false) && parameters.Length == modifiedArgs.Count + 1)
            {
                modifiedArgs.Add(Activator.CreateInstance(parameters.Last().ParameterType, 0));
            }
            else if (parameters.Length > modifiedArgs.Count)
            {
                modifiedArgs.AddRange(parameters.Skip(modifiedArgs.Count).Select(p => p.DefaultValue));
            }

            for (var a = 0; a < modifiedArgs.Count && parametersCastOK; a++)
            {
                var parameterType = parameters[a].ParameterType;
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
                        if (methodInfoToCast.GetParameters().Length == a + 1 && methodInfoToCast.GetParameters()[a].IsDefined(typeof(ParamArrayAttribute), false) //
                                                                             && parameterType != modifiedArgs[a]?.GetType() && parameterType.GetElementType() is { } elementType // 
                                                                             && modifiedArgs.Skip(a).All(arg => arg == null || elementType.IsInstanceOfType(arg))) //
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
                            else if (modifiedArgs[a].GetType().IsArray //
                                     && typeof(IEnumerable).IsAssignableFrom(parameterType) // 
                                     && oldMethodInfo.IsGenericMethod //
                                     && string.IsNullOrWhiteSpace(genericsTypes) //
                                     && methodInfoToCast.GetGenericArguments().Length == 1 //
                                     && methodInfoToCast.GetGenericArguments()[0] != modifiedArgs[a].GetType().GetElementType()) //
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
                result = methodInfoToCast;

            return result;
        }

        /// 根据泛型参数类型信息生成具体的方法
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
            var matches = regex_Generics.Matches(matchStr); //再次检查是否有嵌套泛型
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

        private static void HandleTypeObject(ref object obj, out Type objType, out ValueTypeWrapper valueTypeWrapper)
        {
            valueTypeWrapper = obj as ValueTypeWrapper;

            if (valueTypeWrapper != null)
            {
                obj = valueTypeWrapper.Value;
            }

            if (obj is Type classOrTypeName)
            {
                objType = classOrTypeName;
                obj = null;
                return;
            }

            objType = obj.GetType();
        }

        /// <summary>Get a expression list between [startChar] and [endChar]</summary>
        /// <remarks>⚠️The startChar , endChar and separator must be different</remarks>
        private List<string> GetExpressionsParenthesized(string expression, ref int i, bool checkSeparator, char separator = ',', char startChar = '(', char endChar = ')')
        {
            var expressionsList = new List<string>();

            var currentExpression = new StringBuilder();
            var bracketCount = 1;

            for (; i < expression.Length; i++)
            {
                var internalStringMatch = regex_String.Match(expression, i, expression.Length - i);
                if (internalStringMatch.Success)
                {
                    currentExpression.Append(internalStringMatch.Value);
                    i += internalStringMatch.Length - 1;
                    continue;
                }

                var internalCharMatch = regex_Char.Match(expression, i, expression.Length - i);
                if (internalCharMatch.Success)
                {
                    currentExpression.Append(internalCharMatch.Value);
                    i += internalCharMatch.Length - 1;
                    continue;
                }

                var s = expression[i];

                if (s.Equals(startChar)) bracketCount++;
                if (s.Equals(endChar)) bracketCount--;

                if (bracketCount == 0)
                {
                    var currentExpressionStr = currentExpression.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(currentExpressionStr))
                        expressionsList.Add(currentExpressionStr);
                    break;
                }

                if (bracketCount == 1 && checkSeparator && s.Equals(separator))
                {
                    var currentExpressionStr = currentExpression.ToString().Trim();
                    expressionsList.Add(currentExpressionStr);
                    currentExpression.Clear();
                }
                else
                {
                    currentExpression.Append(s);
                }
            }

            if (bracketCount > 0)
            {
                throw new SyntaxException($"[{expression}] is missing characters ['{endChar}'] ");
            }

            return expressionsList;
        }

        /// <summary>去默认函数列表内寻找并执行对应的函数</summary>
        /// <returns>函数是否存在</returns>
        /// <param name="result">返回函数的结果</param>
        private bool TryGetFunctions(string name, List<string> args, out object result)
        {
            var functionExists = true;

            if (dic_simpleDoubleMathFunc.TryGetValue(name, out var func))
            {
                result = func(Convert.ToDouble(Evaluate(args[0])));
            }
            else if (dic_complexStandardFunc.TryGetValue(name, out var complexFunc))
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


        private bool TryGetVariable(string name, out object result) //,string expression=null,ref int i)
        {
            if (dic_DefaultVariables.TryGetValue(name, out result))
            {
                return true;
            }
            // else if
            // {
            //     
            // }

            return false;
        }


        /// <summary>Get type by the type name</summary>
        /// <remarks>support (nested) generic type </remarks>
        private Type GetTypeByName(string typeName, string genericTypes = "", bool throwExceptionIfNotFound = false)
        {
            typeName = typeName.Trim();
            genericTypes = genericTypes.Trim();
            var fullName = typeName + genericTypes;

            var formattedGenericTypes = string.Empty;

            if (dic_PrimaryTypes.TryGetValue(fullName, out var result)) return result;

            if (dic_CachedTypes.TryGetValue(fullName, out result)) return result;

            result = Types.Find(type => type.Name.Equals(fullName));

            try
            {
                if (result == null)
                {
                    if (!string.IsNullOrWhiteSpace(genericTypes))
                    {
                        var types = GetConcreteTypes(genericTypes);
                        formattedGenericTypes = $"`{types.Length}[{string.Join(", ", types.Select(type => $"[{type.AssemblyQualifiedName}]"))}]";
                    }

                    result = Type.GetType(typeName + formattedGenericTypes, false, false);
                }
                //再从当前程序集中查找


                for (var a = 0; a < Assemblies.Count && result == null; a++)
                {
                    if (typeName.Contains('.'))
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
                throw new SyntaxException($"Failed to get type or class : {typeName}{genericTypes}");
            }

            if (result == null && throwExceptionIfNotFound)
                throw new SyntaxException($"Failed to get type or class : {typeName}{genericTypes}");

            if (result != null) dic_CachedTypes[fullName] = result;

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

            return conversionType != null ? Convert.ChangeType(value, conversionType) : null;
        }


        private static readonly MethodInfo primitiveExplicitCastMethodInfo = typeof(UniInk).GetMethod(nameof(PrimitiveExplicitCast), BindingFlags.Static | BindingFlags.NonPublic);

        private static object PrimitiveExplicitCast<T>(object value) => (T)value;


        #region 用于解析和解释的受保护的工具子类

        /// <summary> 值类型嵌套跟踪 </summary>
        private class ValueTypeWrapper
        {
            public object Container { get; set; }

            public MemberInfo Member { get; set; }

            public object Value { get; set; }

            /// <summary> 赋值 </summary>
            public void AssignValue()
            {
                if (Container is ValueTypeWrapper valueTypeNestingTrace)
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

        private struct FunArgWrapper
        {
            public int Index { get; set; }
            public string Keyword { get; set; }
            public string VariableName { get; set; }

            public FunArgWrapper(int index, string keyword, string variableName)
            {
                Index = index;
                Keyword = keyword;
                VariableName = variableName;
            }
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

    public class StronglyTypedVariable
    {
        public Type Type { get; set; }

        public object Value { get; set; }

        public StronglyTypedVariable(Type type, object value)
        {
            Type = type;
            Value = value;
        }
    }

    /// <summary>表示一组方法，其中要调用的重载方法尚未确定。该类可以被用来模拟委托。</summary>
    public class MethodsGroupWrapper
    {
        /// <summary>定义该方法组的对象实例。</summary>
        public object ContainerObject { get; set; }

        /// <summary>一个方法信息（MethodInfo）数组，其中包含可能用于调用该方法组的重载方法</summary>
        public MethodInfo[] MethodsGroup { get; set; }
    }


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

        public override int GetHashCode()
        {
            return OperatorValue.GetHashCode();
        }
    }


    #region Exceptions

    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base(message) { }
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
}