namespace Arc.UniInk
{

    /*******************************************************************************************************************
    *📰 Title    :  UniInk_Speed (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                 *
    *🔖 Version  :  1.0.0                                                                                              *
    *😀 Author   :  Arc (https://github.com/Arc-huangjingtong)                                                         *
    *🔑 Licence  :  MIT (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity/blob/main/LICENSE)        *
    *🤝 Support  :  [.NET Framework 4+] [C# 9.0+] [IL2CPP Support]                                                     *
    *📝 Desc     :  [High performance] [zero box & unbox] [zero GC!] [zero reflection runtime] [Easy-use]              *
    /*******************************************************************************************************************/

    // ReSharper disable SpecifyACultureInStringConversionExplicitly
    // ReSharper disable PartialTypeWithSinglePart
    using System;
    using System.Collections.Generic;


    /// <summary> The C# Evaluator easy to use : you can execute simple expression or scripts with a string   </summary>
    /// <remarks> If you want to custom your own rules , you should read the code easily and modify it !      </remarks>
    public partial class UniInk_Speed
    {
        ///////////////////////////////////////////////     Settings    ////////////////////////////////////////////////

        public const float  EPSILON_FLOAT  = 0.000001f;
        public const double EPSILON_DOUBLE = 0.000001d;
        public const int    CAPACITY_LIST  = 30;
        public const int    CAPACITY_DICT  = 80;
        public const int    FUNC_MAX_LEN   = 10; // Max length of function name
        public const int    VARI_MAX_LEN   = 10; // Max length of variable name

        ///////////////////////////////////////////////   Constructor   ////////////////////////////////////////////////

        /// <summary> Default Constructor : Initialize variables and parsing Methods        </summary>
        /// <remarks> the variables are saved the object’s reference, not the value         </remarks>
        /// <param name="variables"> Set variables can replace a key string with value object </param>
        public UniInk_Speed(Dictionary<string, InkValue> variables = null)
        {
            ParsingMethods = new(CAPACITY_LIST)
            {
                EvaluateOperators //
              , EvaluateFunction  //
              , EvaluateNumber    //
              , EvaluateChar      //
              , EvaluateString    //
              , EvaluateBool      //
              , EvaluateVariable  //
            };

            if (variables == null) return;

            foreach (var variable in variables)
            {
                dic_Variables.Add(GetStringSliceHashCode(variable.Key, 0, variable.Key.Length - 1), variable.Value);
            }
        }


        ///////////////////////////////////////////////       APIs      ////////////////////////////////////////////////


        /// <summary> Evaluate a expression or simple scripts                                    </summary>
        /// <returns> return the result object , when valueType , will be replaced to a InkValue </returns>
        public object Evaluate(string expression) => Evaluate(expression, 0, expression.Length - 1);

        /// <summary> Evaluate a expression or simple scripts in string slice                    </summary>
        /// <returns> return the result object , when valueType , will be replaced to a InkValue </returns>
        /// <param name="startIndex"> The start index of the expression(contain the start index)   </param>
        /// <param name= "endIndex" > The  end  index of the expression(contain the  end  index)   </param>
        public object Evaluate(string expression, int startIndex, int endIndex)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            var keys = CompileLexerAndFill(expression, startIndex, endIndex);

            var result = ExecuteProcess(keys);

            RecoverResources(keys);

            return result;
        }


        /// <summary> Register a local function </summary>
        public void RegisterFunction(string fucName, InkFunction inkFunc)
        {
            var hash = GetStringSliceHashCode(fucName, 0, fucName.Length - 1);
            if (!dic_Functions.ContainsKey(hash))
            {
                dic_Functions.Add(hash, inkFunc);
            }
        }

        /// <summary> Register a local variable </summary>
        public void RegisterVariable(string varName, InkValue inkValue)
        {
            var hash = GetStringSliceHashCode(varName, 0, varName.Length - 1);
            InkValue.GetTime--;
            if (!dic_Variables.ContainsKey(hash))
            {
                inkValue.dontRelease = true;
                dic_Variables.Add(hash, inkValue);
            }
        }

        /// <summary> Register a local function </summary>
        public static void RegisterGlobalFunction(string fucName, InkFunction inkFunc)
        {
            var hash = GetStringSliceHashCode(fucName, 0, fucName.Length - 1);
            dic_GlobalFunctions.Add(hash, inkFunc);
        }

        /// <summary> UniInk Lexer  :   Fill the SyntaxList       </summary>
        public InkSyntaxList CompileLexerAndFill(string expression, int startIndex, int endIndex)
        {
            var keys = InkSyntaxList.Get();

            for (var i = startIndex ; i <= endIndex ; i++)
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

                throw new InkSyntaxException($"Invalid character : [{expression[i]}] at [{i}  {expression}] ");
            }

            if (Equals(keys[keys.Count - 1], InkOperator.Semicolon))
            {
                keys.ObjectList.RemoveAt(keys.Count - 1);
            }

            return keys;
        }

        /// <summary> UniInk Parser : Execute the SyntaxList and return the result object </summary>
        public static object ExecuteProcess(InkSyntaxList keys)
        {
            var res = InputIsScript(keys) ? ProcessList_Scripts(keys) : ProcessList(keys, 0, keys.Count - 1);

            return res;
        }

        /// <summary> Release the resources in UniInk </summary>
        public void RecoverResources(InkSyntaxList keys)
        {
            InkSyntaxList.ReleaseAll(keys);
            InkSyntaxList.ReleaseTemp();

            ReleaseTempVariables();
        }


        /// <summary> Clear the cache in UniInk anytime , Internal cache pool will be clear </summary>
        /// <remarks> Most of the time you don't need to call                               </remarks>
        public static void ClearCache()
        {
            InkValue.ReleasePool();
            InkSyntaxList.ReleasePool();
        }

        /////////////////////////////////////////////// Process Methods ////////////////////////////////////////////////

        /// <summary> UniInk SyntaxList : Process the SyntaxList </summary>
        protected static object ProcessList(InkSyntaxList syntaxList, int start, int end)
        {
            if (start > end) return null;

            ProcessList_Lambda(syntaxList, start, end);
            ProcessList_Parenthis(syntaxList, start, end);
            ProcessList_Operators(syntaxList, start, end);

            var resultCache = syntaxList.CastOther[start];

            if (resultCache is InkValue inkValue)
            {
                var copy = InkValue.Get();
                inkValue.CopyTo(copy);
                resultCache = copy;
            }

            return resultCache;
        }

        protected static object ProcessList_Scripts(InkSyntaxList keys)
        {
            var    start = 0;
            object res;

            while (true)
            {
                var (success, index) = FindOperator(keys, InkOperator.Semicolon, start, keys.Count - 1);

                if (success)
                {
                    ProcessList(keys, start, index - 1);
                    keys.SetDirty(index);
                    start = index + 1;
                    continue;
                }

                res = ProcessList(keys, start, index); //index is the last index

                break;
            }

            return res;
        }

        protected static void ProcessList_Parenthis(InkSyntaxList keys, int start, int end)
        {
            var hasParenthis = true;

            while (hasParenthis)
            {
                int startIndex, endIndex;

                (hasParenthis, startIndex, endIndex) = FindSection(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, start, end);

                if (!hasParenthis) continue;

                if (startIndex > 0 && keys[startIndex - 1] is InkFunction)
                {
                    ProcessList_Functions(keys, startIndex, endIndex);
                }
                else
                {
                    ProcessList_Operators(keys, startIndex + 1, endIndex - 1);
                }


                keys.SetDirty(startIndex);
                keys.SetDirty(endIndex);
            }
        }

        protected static void ProcessList_Operators(InkSyntaxList keys, int _startIndex, int _endIndex)
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

                var LNeedRelease = false;
                var RNeedRelease = false;

                var startIndex = index;
                var endIndex   = index;


                for (var i = index - 1 ; i >= _startIndex ; i--) // Left
                {
                    if (keys.IndexDirty[i])
                    {
                        if (keys.CastOther[i] == null) continue;

                        left              = keys.CastOther[i];
                        keys.CastOther[i] = null;

                        LNeedRelease = true;

                        startIndex = i;

                        break;
                    }

                    left = keys[i];

                    startIndex = i;

                    break;
                }

                if (left is InkOperator)
                {
                    left       = null;
                    startIndex = index;
                }

                for (var i = index + 1 ; i <= _endIndex ; i++) // Right
                {
                    if (keys.IndexDirty[i])
                    {
                        if (keys.CastOther[i] == null) continue;

                        right             = keys.CastOther[i];
                        keys.CastOther[i] = null;

                        RNeedRelease = true;

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

                    if (LNeedRelease && left is InkValue inkValueL)
                    {
                        InkValue.Release(inkValueL);
                    }

                    if (RNeedRelease && right is InkValue inkValueR)
                    {
                        InkValue.Release(inkValueR);
                    }

                    keys.SetDirty(result, startIndex, endIndex);
                }
                else
                {
                    throw new InkSyntaxException($"Unknown Operator : {curOperator}");
                }
            }

            for (var i = _startIndex ; i <= _endIndex ; i++)
            {
                if (!keys.IndexDirty[i])
                {
                    if (keys.ObjectList[i] is InkValue inkValue)
                    {
                        if (keys.CastOther[i] == null)
                        {
                            keys.CastOther[i] = inkValue.Clone();
                        }
                        else if (keys.CastOther[i] is InkValue inkValue2)
                        {
                            inkValue.CopyTo(inkValue2);
                        }
                    }
                    else if (keys.ObjectList[i] != null)
                    {
                        keys.CastOther[i] = keys.ObjectList[i];
                    }

                    keys.SetDirty(i);
                }
            }
        }

        protected static void ProcessList_Functions(InkSyntaxList keys, int paramStart, int paramEnd)
        {
            var func         = keys[paramStart - 1] as InkFunction; //😊
            var paramList    = InkSyntaxList.GetTemp();             //😊
            var sectionStart = 0;

            for (var i = paramStart + 1 ; i <= paramEnd - 1 ; i++)
            {
                object current;

                if (keys.IndexDirty[i])
                {
                    if (keys.CastOther[i] == null) continue;

                    current = keys.CastOther[i];

                    keys.CastOther[i] = null;
                }
                else
                {
                    current = keys[i] is InkValue inkValue ? inkValue.Clone() : keys[i];
                }

                if (current == null) continue;


                if (Equals(current, InkOperator.Comma))
                {
                    ProcessList_Operators(paramList, sectionStart, paramList.Count - 1);
                    sectionStart = paramList.Count;
                }
                else
                {
                    paramList.Add(current);
                }
            }

            ProcessList_Operators(paramList, sectionStart, paramList.Count - 1);

            ObjectRemoveNull(paramList.CastOther);
            var result = func?.FuncDelegate2.Invoke(paramList.CastOther);

            if (result is InkValue) { }
            else if (result is not null)
            {
                result = InkValue.GetObjectValue(result);
            }

            keys.SetDirty(result, paramStart - 1, paramEnd);
        }

        protected static void ProcessList_Lambda(InkSyntaxList keys, int paramStart, int paramEnd)
        {
            while (true)
            {
                var (hasArrow, arrowIndex) = FindOperator(keys, InkOperator.Lambda, paramStart, paramEnd);

                if (!hasArrow) break;

                var (hasBalance, endIndex) = FindNoBalanceOperator(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, arrowIndex, paramEnd);

                if (!hasBalance) throw new InkSyntaxException("Parenthis is not balance!");


                //var c => GET(c, Rarity) == 2)
                var startIndex = arrowIndex - 1;

                var lambdaData = InkSyntaxList.GetTemp();


                for (var i = arrowIndex + 1 ; i <= endIndex - 1 ; i++)
                {
                    object current;

                    if (keys.IndexDirty[i])
                    {
                        if (keys.CastOther[i] == null) continue;

                        current = keys.CastOther[i];

                        keys.CastOther[i] = null;
                    }
                    else
                    {
                        if (keys[i] is InkValue { ValueType: not (TypeCode.Object or TypeCode.DBNull) } inkValue)
                        {
                            current = inkValue.Clone();
                        }
                        else
                        {
                            current = keys[i];
                        }
                    }

                    lambdaData.Add(current);
                }

                var variable = keys[startIndex] as InkValue;

                var lambda = new Predicate<object>(o =>
                {
                    // this section has GC 🙂
                    InkOperator.InkOperator_Assign(variable, o);
                    var result = (bool)(InkValue)ProcessList(lambdaData, 0, lambdaData.Count - 1);
                    InkSyntaxList.Recover(lambdaData);

                    return result;
                });

                var inkObject = InkValue.GetObjectValue(lambda);

                keys.SetDirty(inkObject, startIndex, endIndex - 1);
            }
        }

        protected void ReleaseTempVariables()
        {
            foreach (var variable in dic_Variables_Temp)
            {
                variable.Value.dontRelease = false;
                InkValue.Release(variable.Value);
            }

            dic_Variables_Temp.Clear();
        }

        ///////////////////////////////////////////////  Mapping  Data  ////////////////////////////////////////////////


        /// <summary> Some UnaryPostfix operators func mapping </summary>
        protected static readonly Dictionary<InkOperator, Func<object, object, object>> dic_OperatorsFunc = new(CAPACITY_DICT)
        {
            { InkOperator.Plus, InkOperator.InkOperator_Plus }                     //
          , { InkOperator.Minus, InkOperator.InkOperator_Minus }                   //
          , { InkOperator.Multiply, InkOperator.InkOperator_Multiply }             //
          , { InkOperator.Divide, InkOperator.InkOperator_Divide }                 //
          , { InkOperator.Modulo, InkOperator.InkOperator_Modulo }                 //
          , { InkOperator.Lower, InkOperator.InkOperator_Lower }                   //
          , { InkOperator.Greater, InkOperator.InkOperator_Greater }               //
          , { InkOperator.Equal, InkOperator.InkOperator_Equal }                   //
          , { InkOperator.LowerOrEqual, InkOperator.InkOperator_LowerOrEqual }     //
          , { InkOperator.GreaterOrEqual, InkOperator.InkOperator_GreaterOrEqual } //
          , { InkOperator.NotEqual, InkOperator.InkOperator_NotEqual }             //
          , { InkOperator.LogicalNOT, InkOperator.InkOperator_LogicalNOT }         //
          , { InkOperator.ConditionalAnd, InkOperator.InkOperator_ConditionalAnd } //
          , { InkOperator.ConditionalOr, InkOperator.InkOperator_ConditionalOr }   //
          , { InkOperator.Assign, InkOperator.InkOperator_Assign }                 //
          , { InkOperator.KeyReturn, InkOperator.InkOperator_Return }              //
        };

        /// <summary> Some Escaped Char mapping </summary>
        protected static readonly Dictionary<char, char> dic_EscapedChar = new(CAPACITY_DICT)
        {
            { '\\', '\\' }, { '\'', '\'' } //
          , { '0', '\0' }, { 'a', '\a' }   //
          , { 'b', '\b' }, { 'f', '\f' }   //
          , { 'n', '\n' }, { 'r', '\r' }   //
          , { 't', '\t' }, { 'v', '\v' }   //
        };

        /// <summary> Some Global Functions mapping </summary>
        public static readonly Dictionary<int, InkFunction> dic_GlobalFunctions = new(CAPACITY_DICT);

        /// <summary> Some local functions mapping </summary>
        public readonly Dictionary<int, InkFunction> dic_Functions = new(CAPACITY_DICT);

        /// <summary> Some local variables mapping </summary>
        public readonly Dictionary<int, InkValue> dic_Variables = new(CAPACITY_DICT);

        /// <summary> Some temp  variables mapping </summary>
        public Dictionary<int, InkValue> dic_Variables_Temp = new(CAPACITY_DICT);



        /////////////////////////////////////////////// Parsing Methods ////////////////////////////////////////////////

        protected delegate bool ParsingMethodDelegate(string expression, InkSyntaxList stack, ref int i);

        /// <summary> The Parsing Methods for <see cref="Evaluate"/> </summary>
        protected readonly List<ParsingMethodDelegate> ParsingMethods;

        /// <summary> Evaluate Operators in<see cref="InkOperator"/> </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns> 
        protected static bool EvaluateOperators(string expression, InkSyntaxList keys, ref int i)
        {
            for (var operatorLen = InkOperator.MaxOperatorLen - 1 ; operatorLen >= 0 ; operatorLen--)
            {
                if (i + operatorLen >= expression.Length) continue; // long=>short, || first than |, so we need to check the length

                var operatorHash = GetStringSliceHashCode(expression, i, i + operatorLen);
                if (InkOperator.Dic_Values.TryGetValue(operatorHash, out var @operator))
                {
                    keys.Add(@operator);
                    i += operatorLen;
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
                numberMatch.Calculate();
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

        /// <summary> Evaluate Function _eg: LOG("Hello World")      </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected bool EvaluateFunction(string expression, InkSyntaxList keys, ref int i)
        {
            for (var len = FUNC_MAX_LEN ; len >= 0 ; len--)
            {
                if (i + len >= expression.Length) continue; // long=>short, || first than |, so we need to check the length

                var varHash = GetStringSliceHashCode(expression, i, i + len);
                if (dic_Functions.TryGetValue(varHash, out var variable))
                {
                    keys.Add(variable);
                    i += len;
                    return true;
                }

                if (dic_GlobalFunctions.TryGetValue(varHash, out var variable2))
                {
                    keys.Add(variable2);
                    i += len;
                    return true;
                }
            }

            return false;
        }

        /// <summary> Evaluate Variable _eg: var a = 3;              </summary>
        /// <param name="expression"> the expression to Evaluate       </param>
        /// <param name="keys"> the object stack to push or pop        </param>
        /// <param name="i"> the <see cref="expression"/> start index  </param>
        /// <returns> the evaluate is success or not                 </returns>
        protected bool EvaluateVariable(string expression, InkSyntaxList keys, ref int i)
        {
            if (keys.Count > 0 && keys[keys.Count - 1] is InkOperator operatorValue && Equals(operatorValue, InkOperator.KeyVar))
            {
                var startIndex = i;

                while (i < expression.Length && expression[i] != ' ' && expression[i] != '=')
                {
                    i++;
                }

                i--;

                var keyHash = GetStringSliceHashCode(expression, startIndex, i);


                if (dic_Variables.TryGetValue(keyHash, out _))
                {
                    throw new InkSyntaxException($"the variable[{expression.Substring(startIndex, i - startIndex)}] is already exist!");
                }

                var inkValue = InkValue.Get();
                inkValue.ValueType   = TypeCode.DBNull;
                inkValue.dontRelease = true;
                dic_Variables_Temp.Add(keyHash, inkValue);


                keys.SetDirty(keys.Count - 1); // the keyVar

                keys.Add(dic_Variables_Temp[keyHash]);

                return true;
            }


            for (var len = VARI_MAX_LEN ; len >= 0 ; len--)
            {
                if (i + len >= expression.Length) continue; // long=>short, || first than |, so we need to check the length

                var varHash = GetStringSliceHashCode(expression, i, i + len);
                if (dic_Variables_Temp.TryGetValue(varHash, out var variable))
                {
                    keys.Add(variable);
                    i += len;
                    return true;
                }

                if (dic_Variables.TryGetValue(varHash, out var variable2))
                {
                    keys.Add(variable2);
                    if (variable2.getter)
                    {
                        variable2.Getter(variable2);
                    }

                    i += len;
                    return true;
                }
            }

            return false;
        }


        /////////////////////////////////////////////// Helping Methods ////////////////////////////////////////////////


        /// <summary> Match <see cref="value"/> from <see cref="input"/> 's  <see cref="startIndex"/>         </summary>
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

        /// <summary> Find <see cref="input"/> is whether start with numbers from <see cref="startIndex"/>    </summary>
        protected static bool StartsWithNumbersFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
            if (!char.IsDigit(input[startIndex]))
            {
                value = null;
                len   = 0;
                return false;
            }

            value           = InkValue.Get();
            value.ValueType = TypeCode.Int32;
            len             = 0;

            var pointNum = 0;

            for (var i = startIndex ; i < input.Length ; i++)
            {
                if (char.IsDigit(input[i]))
                {
                    len++;
                    value.Value_Meta.Add(input[i]);
                }
                else if (input[i] == '.')
                {
                    len++;
                    pointNum++;

                    if (pointNum > 1)
                    {
                        throw new InkSyntaxException("[NotSupport]:Too many decimal points, can't calling method with float or double number.");
                    }

                    value.ValueType = TypeCode.Double;
                    value.Value_Meta.Add(input[i]);
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
                            value.ValueType = TypeCode.Single;
                            len++;
                            break;
                        case 'd' or 'D' :
                            value.ValueType = TypeCode.Double;
                            len++;
                            break;
                    }


                    return true;
                }
            }

            return true;
        }

        /// <summary> Find <see cref="input"/> is whether start with char from <see cref="startIndex"/>       </summary>
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
                        value = InkValue.GetCharValue(EscapedChar);
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
                    value = InkValue.GetCharValue(input[i]);
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

        /// <summary> Find <see cref="input"/> is whether start with string from <see cref="startIndex"/>     </summary>
        protected static bool StartsWithStringFormIndex(string input, int startIndex, out InkValue value, out int len)
        {
            var i = startIndex;


            if (input[i].Equals('\"'))
            {
                i++;
                value           = InkValue.Get();
                value.ValueType = TypeCode.String;


                while (i < input.Length)
                {
                    if (input[i].Equals('\\'))
                    {
                        i++;

                        if (dic_EscapedChar.TryGetValue(input[i], out var EscapedChar))
                        {
                            value.Value_Meta.Add(EscapedChar);
                            i++;
                        }
                        else
                        {
                            throw new InkSyntaxException($"Unknown escape character[{input[i]}] : You can customize them in [dic_EscapedChar]");
                        }
                    }
                    else if (input[i].Equals('\"'))
                    {
                        len = i - startIndex;

                        return true;
                    }
                    else
                    {
                        value.Value_Meta.Add(input[i]);
                        i++;
                    }
                }

                throw new InkSyntaxException($"Illegal character[{i}] : too many characters in a character literal");
            }

            len   = 0;
            value = null;
            return false;
        }

        /// <summary> Find <see cref="input"/> is whether start with bool from <see cref="startIndex"/>       </summary>
        protected static bool StartsWithBoolFromIndex(string input, int startIndex, out InkValue value, out int len)
        {
            const string trueStr  = "true";
            const string falseStr = "false";

            if (StartsWithInputStrFromIndex(input, falseStr, startIndex))
            {
                len   = falseStr.Length;
                value = InkValue.GetBoolValue(false);
                return true;
            }

            if (StartsWithInputStrFromIndex(input, trueStr, startIndex))
            {
                len   = trueStr.Length;
                value = InkValue.GetBoolValue(true);
                return true;
            }

            len   = 0;
            value = null;
            return false;
        }


        /// <summary> Find the innermost section between <see cref="sct_start"/> and <see cref="sct_end"/>    </summary>
        /// <param name="keys"     > the keys to find section in                                                </param>
        /// <param name="sct_start"> the start section key : the last  find before <see cref="sct_end"/>        </param>
        /// <param name="sct_end"  > the end   section key : the first find after  <see cref="sct_start"/>      </param>
        /// <returns> the result is success or not , the start index and end index of the section             </returns>
        protected static (bool result, int startIndex, int endIndex) FindSection(InkSyntaxList keys, InkOperator sct_start, InkOperator sct_end, int start, int end)
        {
            var startIndex = -1;
            var endIndex   = -1;

            for (var i = start ; i <= end ; i++)
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
                throw new InkSyntaxException($"Missing match {sct_end}");
            }

            return (startIndex != -1 && endIndex != -1, startIndex, endIndex);
        }

        /// <summary> Find the outermost operator range in the specified left and right </summary>
        protected static (int StartIndex, int EndIndex) GetMatchOperator(InkSyntaxList keys, InkOperator opLeft, InkOperator opRight, int start, int end)
        {
            int startIndex = -1, balance = -1;

            for (var i = start ; i <= end ; i++)
            {
                if (keys[i] is InkOperator op && Equals(op, opLeft))
                {
                    startIndex = i;
                    balance    = 1;
                    break;
                }
            }

            for (var i = startIndex + 1 ; i <= end ; i++)
            {
                if (keys[i] is InkOperator opL && Equals(opL, opLeft))
                {
                    balance++;
                }
                else if (keys[i] is InkOperator opR && Equals(opR, opRight))
                {
                    balance--;

                    if (balance == 0)
                    {
                        return (startIndex, i);
                    }
                }
            }


            return (-1, -1);
        }



        protected static (bool result, int index) FindOperator(InkSyntaxList keys, InkOperator @operator, int start, int end)
        {
            for (var i = start ; i <= end ; i++)
            {
                if (keys.IndexDirty[i])
                {
                    continue;
                }

                if (Equals(keys[i], @operator))
                {
                    return (true, i);
                }
            }

            return (false, end);
        }

        protected static (bool result, int index) FindNoBalanceOperator(InkSyntaxList keys, InkOperator operatorLeft, InkOperator operatorRight, int start, int end)
        {
            var balance = 0;

            for (var i = start ; i <= end ; i++)
            {
                var current = keys[i];

                if (current is InkOperator operatorValue)
                {
                    if (Equals(operatorValue, operatorLeft))
                    {
                        balance++;
                    }
                    else if (Equals(operatorValue, operatorRight))
                    {
                        balance--;
                    }
                }

                if (balance == -1)
                {
                    return (true, i);
                }
            }

            return (false, -1);
        }

        /// <summary>Get the highest priority operator in the <see cref="keys"/>                              </summary>
        /// <param name="keys"> the keys to find the highest priority operator in                               </param>
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

                if (keys[i] is InkOperator @operator
                 && @operator.PriorityIndex < priorityOperator.PriorityIndex)
                {
                    index            = i;
                    priorityOperator = @operator;
                }
            }

            return (priorityOperator, index);
        }

        /// <summary> Judge the input String is a Script or not (depend on the operator:[;] [{])              </summary>
        protected static bool InputIsScript(InkSyntaxList keys)
        {
            foreach (var obj in keys.ObjectList)
            {
                //match [;]
                if (obj is InkOperator @operator && Equals(@operator, InkOperator.Semicolon))
                {
                    return true;
                }
            }


            return false;
        }

        protected static void ObjectRemoveNull(List<object> objectList)
        {
            for (var i = 0 ; i < objectList.Count ; i++)
            {
                if (objectList[i] == null)
                {
                    objectList.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary> Get the hash code of the string from <see cref="startIndex"/> to <see cref="endIndex"/> </summary>
        public static int GetStringSliceHashCode(string str, int startIndex, int endIndex)
        {
            var hash = 0;
            for (var i = startIndex ; i <= endIndex ; i++)
            {
                hash = hash * 31 + str[i];
            }

            return hash;
        }

        public static int GetStringSliceHashCode(string str) => GetStringSliceHashCode(str, 0, str.Length - 1);
    }


    ////////////////////////////////////////////////// Helping Class ///////////////////////////////////////////////////


    /// <summary> UniInk Operator : Custom your own Operator! </summary>
    public partial class InkOperator
    {
        public static readonly Dictionary<int, InkOperator> Dic_Values = new(UniInk_Speed.CAPACITY_DICT);

        //priority refer to : https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/
        //keyword  refer to : https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/
        public static readonly InkOperator ParenthisLeft  = new("(", 1);
        public static readonly InkOperator ParenthisRight = new(")", 1);
        public static readonly InkOperator Dot            = new(".", 2);
        public static readonly InkOperator BracketStart   = new("[", 2);
        public static readonly InkOperator BracketEnd     = new("]", 2);
        public static readonly InkOperator Increment      = new("++", 2);
        public static readonly InkOperator Decrement      = new("--", 2);
        public static readonly InkOperator LogicalNOT     = new("!", 3);
        public static readonly InkOperator BitNot         = new("~", 3);
        public static readonly InkOperator Cast           = new("😊()", 4);
        public static readonly InkOperator Multiply       = new("*", 5);
        public static readonly InkOperator Divide         = new("/", 5);
        public static readonly InkOperator Modulo         = new("%", 5);
        public static readonly InkOperator Plus           = new("+", 6);
        public static readonly InkOperator Minus          = new("-", 6);
        public static readonly InkOperator LeftShift      = new("<<", 7);
        public static readonly InkOperator RightShift     = new(">>", 7);
        public static readonly InkOperator Lower          = new("<", 8);
        public static readonly InkOperator Greater        = new(">", 8);
        public static readonly InkOperator LowerOrEqual   = new("<=", 8);
        public static readonly InkOperator GreaterOrEqual = new(">=", 8);
        public static readonly InkOperator Equal          = new("==", 9);
        public static readonly InkOperator NotEqual       = new("!=", 9);
        public static readonly InkOperator BitwiseAnd     = new("&", 10);
        public static readonly InkOperator BitwiseXor     = new("^", 11);
        public static readonly InkOperator BitwiseOr      = new("|", 12);
        public static readonly InkOperator ConditionalAnd = new("&&", 13);
        public static readonly InkOperator ConditionalOr  = new("||", 14);
        public static readonly InkOperator Conditional    = new("?:", 15);
        public static readonly InkOperator Assign         = new("=", 16);
        public static readonly InkOperator Comma          = new(",", 16);
        public static readonly InkOperator Lambda         = new("=>", 17);
        public static readonly InkOperator BraceLeft      = new("{", 20);
        public static readonly InkOperator BraceRight     = new("}", 20);
        public static readonly InkOperator Semicolon      = new(";", 20);
        public static readonly InkOperator Colon          = new(":", -1);
        public static readonly InkOperator QuestionMark   = new("?", -1);
        public static readonly InkOperator At             = new("@\"", -1);
        public static readonly InkOperator Dollar         = new("$\"", -1);
        public static readonly InkOperator Hash           = new("#", -1);


        public static readonly InkOperator KeyIf       = new("if", 20);
        public static readonly InkOperator KeyVar      = new("var", 20);
        public static readonly InkOperator KeyElse     = new("else", 20);
        public static readonly InkOperator KeyReturn   = new("return", 20);
        public static readonly InkOperator KeySwitch   = new("switch", 20);   // don't support
        public static readonly InkOperator KeyWhile    = new("while", 20);    // don't support
        public static readonly InkOperator KeyFor      = new("for", 20);      // don't support
        public static readonly InkOperator KeyForeach  = new("foreach", 20);  // don't support
        public static readonly InkOperator KeyIn       = new("in", 20);       // don't support
        public static readonly InkOperator KeyBreak    = new("break", 20);    // don't support
        public static readonly InkOperator KeyContinue = new("continue", 20); // don't support
        public static readonly InkOperator Empty       = new("😊", short.MaxValue);


        public static int MaxOperatorLen;

        /// <summary> the lower the value, the higher the priority </summary>
        public readonly short PriorityIndex;

        public readonly string Name;

        /// <summary> the indexer of the operator    </summary>
        protected static short indexer;

        /// <summary> the only value of the operator </summary>
        protected readonly short OperatorValue = indexer++;



        protected InkOperator(string name, short priorityIndex)
        {
            PriorityIndex  = priorityIndex;
            MaxOperatorLen = Math.Max(MaxOperatorLen, name.Length);
            Name           = name;
            var hash = UniInk_Speed.GetStringSliceHashCode(name, 0, name.Length - 1);
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

                    var rightValueCopy = InkValue.Get();

                    rightValue.CopyTo(rightValueCopy);
                    return rightValueCopy;
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
                case null when right is InkValue rightValue :
                {
                    var inkDefult = InkValue.GetIntValue(0);
                    var result    = inkDefult - rightValue;
                    InkValue.Release(inkDefult);
                    return result;
                }
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

        public static object InkOperator_Modulo(object left, object right)
        {
            switch (left)
            {
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue % rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_Lower(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue < rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_Greater(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue > rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_Equal(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue == rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_LowerOrEqual(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue <= rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_GreaterOrEqual(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue >= rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_NotEqual(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return leftValue != rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_LogicalNOT(object left, object right)
        {
            switch (right)
            {
                case InkValue rightValue :
                {
                    return !rightValue;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_ConditionalAnd(object left, object right)
        {
            switch (left)
            {
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return InkValue.GetBoolValue(leftValue.Value_bool && rightValue.Value_bool);
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_ConditionalOr(object left, object right)
        {
            switch (left)
            {
                case null : return right;
                case InkValue leftValue when right is InkValue rightValue :
                {
                    return InkValue.GetBoolValue(leftValue.Value_bool || rightValue.Value_bool);
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }

        public static object InkOperator_Assign(object left, object right)
        {
            if (left is InkValue leftValue)
            {
                switch (right)
                {
                    case InkValue rightValue :
                    {
                        leftValue.ValueType = rightValue.ValueType;
                        rightValue.Calculate();
                        rightValue.CopyTo(leftValue);

                        return null;
                    }
                    case not null :
                    {
                        leftValue.ValueType    = TypeCode.Object;
                        leftValue.Value_Object = right;
                        leftValue.isCalculate  = true;

                        return null;
                    }

                    default : throw new InkSyntaxException($"worrying operator using!");
                }
            }

            throw new InkSyntaxException($"the left value is not a variable!");
        }

        public static object InkOperator_Return(object left, object right)
        {
            switch (right)
            {
                case InkValue rightValue :
                {
                    rightValue.Calculate();
                    var result = rightValue.Clone();
                    result.returner = true;
                    return result;
                }
                default : throw new InkSyntaxException($"unknown type{left}--{right}");
            }
        }
    }


    /// <summary> UniInk Function : Custom your own Function! </summary>
    public partial class InkFunction
    {
        public InkFunction(Func<List<object>, object> func)
        {
            FuncDelegate2 = func;
        }

        public InkFunction(Func<List<object>, object> func, Type[] paramTypes, Type returnType)
        {
            FuncDelegate2 = func;
            ParamTypes    = paramTypes;
            ReturnType    = returnType;
        }

        public readonly Type[] ParamTypes;

        public readonly Type ReturnType;


        public readonly Func<List<object>, object> FuncDelegate2;
    }


    /// <summary> In UniInk , every valueType is Object , No Boxing! </summary>
    public partial class InkValue
    {
        public static readonly Queue<InkValue> pool = new(UniInk_Speed.CAPACITY_LIST);

        public static InkValue Get()
        {
            GetTime++;
            return pool.Count > 0 ? pool.Dequeue() : new();
        }

        public static void ReleasePool() => pool.Clear();

        public static int GetTime;
        public static int ReleaseTime;

        public static InkValue GetCharValue(char c)
        {
            var value = Get();
            value.ValueType   = TypeCode.Char;
            value.Value_char  = c;
            value.isCalculate = true;
            return value;
        }

        public static InkValue GetBoolValue(bool b)
        {
            var value = Get();
            value.ValueType   = TypeCode.Boolean;
            value.Value_bool  = b;
            value.isCalculate = true;
            return value;
        }

        public static InkValue GetIntValue(int i)
        {
            var value = Get();
            value.ValueType   = TypeCode.Int32;
            value.Value_int   = i;
            value.isCalculate = true;
            return value;
        }

        public static InkValue GetFloatValue(float f)
        {
            var value = Get();
            value.ValueType   = TypeCode.Single;
            value.Value_float = f;
            value.isCalculate = true;
            return value;
        }

        public static InkValue GetDoubleValue(double d)
        {
            var value = Get();
            value.ValueType    = TypeCode.Double;
            value.Value_double = d;
            value.isCalculate  = true;
            return value;
        }

        public static InkValue GetString(string str)
        {
            var value = Get();
            value.ValueType = TypeCode.String;

            foreach (var c in str)
            {
                value.Value_Meta.Add(c);
            }

            value.isCalculate = true;
            return value;
        }

        public static InkValue GetObjectValue(object obj)
        {
            var value = Get();
            value.ValueType    = TypeCode.Object;
            value.Value_Object = obj;
            value.isCalculate  = true;
            return value;
        }


        public static InkValue SetGetter(Action<InkValue> getter)
        {
            var value = Get();
            value.getter = true;
            value.Getter = getter;
            return value;
        }


        public static void Release(InkValue value)
        {
            if (value.dontRelease) return;


            ReleaseTime++;

            value.Value_Meta.Clear();
            value.isCalculate  = false;
            value.setter       = false;
            value.getter       = false;
            value.returner     = false;
            value.Value_int    = 0;
            value.Value_bool   = false;
            value.Value_char   = default;
            value.Value_float  = 0;
            value.Value_double = 0;
            value.Value_Object = null;

            pool.Enqueue(value);
        }

        public int    Value_int;
        public bool   Value_bool;
        public char   Value_char;
        public float  Value_float;
        public double Value_double;
        public object Value_Object;
        public string Value_String => string.Concat(Value_Meta);


        public TypeCode ValueType;

        public readonly List<char> Value_Meta = new(UniInk_Speed.CAPACITY_LIST);

        public bool isCalculate;

        public bool dontRelease;

        public bool setter;
        public bool getter;
        public bool returner;

        public Action<InkValue> Setter;
        public Action<InkValue> Getter;


        /// <summary> Calculate the value </summary>
        public void Calculate()
        {
            if (isCalculate) return;

            switch (ValueType)
            {
                case TypeCode.Int32 :
                {
                    Value_int = 0;

                    foreach (var t in Value_Meta)
                    {
                        Value_int = Value_int * 10 + (t - '0');
                    }

                    break;
                }
                case TypeCode.Single :
                {
                    Value_float = 0;
                    var floatPart = 0.0f;
                    var numCount  = 0;

                    foreach (var c in Value_Meta) //123
                    {
                        if (c == '.') break;

                        numCount++;
                        Value_float = Value_float * 10 + (c - '0');
                    }

                    for (var i = Value_Meta.Count - 1 ; i >= numCount ; i--)
                    {
                        if (Value_Meta[i] == '.') break;

                        floatPart = floatPart / 10 + (Value_Meta[i] - '0') * 0.1f;
                    }

                    Value_float += floatPart;

                    break;
                }
                case TypeCode.Double :
                {
                    Value_double = 0;
                    var floatPart = 0.0d;
                    var numCount  = 0;

                    foreach (var c in Value_Meta)
                    {
                        if (c == '.') break;

                        Value_double = Value_double * 10 + (c - '0');
                        numCount++;
                    }

                    for (var i = Value_Meta.Count - 1 ; i >= numCount ; i--)
                    {
                        if (Value_Meta[i] == '.') break;

                        floatPart = floatPart / 10 + (Value_Meta[i] - '0') * 0.1d;
                    }

                    Value_double += floatPart;

                    break;
                }
                case TypeCode.Object : break;
                case TypeCode.String : break;
                default :              throw new InkSyntaxException("Unknown ValueType");
            }

            isCalculate = true;
        }

        public void CopyTo(InkValue value)
        {
            value.ValueType   = ValueType;
            value.isCalculate = isCalculate;

            value.Value_int    = Value_int;
            value.Value_bool   = Value_bool;
            value.Value_char   = Value_char;
            value.Value_float  = Value_float;
            value.Value_double = Value_double;
            value.Value_Object = Value_Object;
            value.Value_Meta.Clear();

            foreach (var meta in Value_Meta)
            {
                value.Value_Meta.Add(meta);
            }
        }

        public InkValue Clone()
        {
            var value = Get();
            CopyTo(value);
            return value;
        }


        public override string ToString()
        {
            if (!isCalculate) return Value_String;

            switch (ValueType)
            {
                case TypeCode.Int32 :   return Value_int.ToString();
                case TypeCode.Boolean : return Value_bool.ToString();
                case TypeCode.Char :    return Value_char.ToString();
                case TypeCode.Single :  return Value_float.ToString();
                case TypeCode.Double :  return Value_double.ToString();
                case TypeCode.String :  return Value_String;
                case TypeCode.Object :  return Value_Object.ToString();
                default :               throw new InkSyntaxException("Unknown ValueType");
            }
        }

        public override int  GetHashCode()      => Value_Meta.GetHashCode();
        public override bool Equals(object obj) => obj is InkValue value && Value_Meta == value.Value_Meta;


        public static InkValue operator +(InkValue left, InkValue right)
        {
            var answer = Get();

            answer.ValueType = left.ValueType;

            left.Calculate();
            right.Calculate();

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.String, TypeCode.String) :
                    foreach (var c in left.Value_Meta) answer.Value_Meta.Add(c);
                    foreach (var c in right.Value_Meta) answer.Value_Meta.Add(c);
                    break;
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_int = left.Value_int + right.Value_int;
                    break;
                case (TypeCode.Single, TypeCode.Single) :
                    answer.Value_float = left.Value_float + right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_double = left.Value_double + right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }

            answer.isCalculate = true;

            return answer;
        }

        public static InkValue operator -(InkValue left, InkValue right)
        {
            var answer = Get();

            answer.ValueType = left!.ValueType;

            left.Calculate();
            right!.Calculate();

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_int = left.Value_int - right.Value_int;
                    break;
                case (TypeCode.Single, TypeCode.Single) :
                    answer.Value_float = left.Value_float - right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_double = left.Value_double - right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }

            answer.isCalculate = true;

            return answer;
        }

        public static InkValue operator *(InkValue left, InkValue right)
        {
            var answer = Get();

            answer.ValueType = left!.ValueType;

            left.Calculate();
            right!.Calculate();

            switch (answer.ValueType)
            {
                case TypeCode.Int32 :
                    answer.Value_int = left.Value_int * right.Value_int;
                    break;
                case TypeCode.Single :
                    answer.Value_float = left.Value_float * right.Value_float;
                    break;
                case TypeCode.Double :
                    answer.Value_double = left.Value_double * right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }

            answer.isCalculate = true;


            return answer;
        }

        public static InkValue operator /(InkValue left, InkValue right)
        {
            var answer = Get();

            left.Calculate();
            right.Calculate();

            answer.ValueType = left.ValueType;

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_int = left.Value_int / right.Value_int;
                    break;
                case (TypeCode.Single, TypeCode.Single) :
                    answer.Value_float = left.Value_float / right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_double = left.Value_double / right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }

            answer.isCalculate = true;

            return answer;
        }

        public static InkValue operator %(InkValue left, InkValue right)
        {
            var answer = Get();

            answer.ValueType = left.ValueType;

            left.Calculate();
            right.Calculate();

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_int = left.Value_int % right.Value_int;
                    break;
                case (TypeCode.Single, TypeCode.Single) :
                    answer.Value_float = left.Value_float % right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_double = left.Value_double % right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }

            answer.isCalculate = true;

            return answer;
        }

        public static InkValue operator >(InkValue left, InkValue right)
        {
            var answer = GetBoolValue(false);

            left.Calculate();
            right.Calculate();

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_bool = left.Value_int > right.Value_int;
                    break;
                case (TypeCode.Boolean, TypeCode.Boolean) :
                    answer.Value_bool = left.Value_float > right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_bool = left.Value_double > right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }


            return answer;
        }

        public static InkValue operator <(InkValue left, InkValue right)
        {
            var answer = GetBoolValue(false);

            left.Calculate();
            right.Calculate();

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_bool = left.Value_int < right.Value_int;
                    break;
                case (TypeCode.Boolean, TypeCode.Boolean) :
                    answer.Value_bool = left.Value_float < right.Value_float;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_bool = left.Value_double < right.Value_double;
                    break;
                default : throw new InkSyntaxException("worrying operator using!");
            }


            return answer;
        }

        public static InkValue operator ==(InkValue left, InkValue right)
        {
            left!.Calculate();
            right!.Calculate();

            var answer = GetBoolValue(false);

            switch (left.ValueType, right.ValueType)
            {
                case (TypeCode.Int32, TypeCode.Int32) :
                    answer.Value_bool = left.Value_int == right.Value_int;
                    break;
                case (TypeCode.Boolean, TypeCode.Boolean) :
                    answer.Value_bool = Math.Abs(left.Value_float - right.Value_float) < UniInk_Speed.EPSILON_FLOAT;
                    break;
                case (TypeCode.Double, TypeCode.Double) :
                    answer.Value_bool = Math.Abs(left.Value_double - right.Value_double) < UniInk_Speed.EPSILON_DOUBLE;
                    break;
                case (TypeCode.String, TypeCode.String) :
                {
                    if (left.Value_Meta.Count != right.Value_Meta.Count) break;

                    answer.Value_bool = true;

                    for (var i = 0 ; i < left.Value_Meta.Count ; i++)
                    {
                        if (left.Value_Meta[i] != right.Value_Meta[i])
                        {
                            answer.Value_bool = false;
                            break;
                        }
                    }

                    break;
                }
                default :
                {
                    answer.Value_bool = false;
                    break;
                }
            }


            return answer;
        }

        public static InkValue operator !=(InkValue left, InkValue right) => (left  == right).Negate();
        public static InkValue operator >=(InkValue left, InkValue right) => (left  < right).Negate();
        public static InkValue operator <=(InkValue left, InkValue right) => (right > left).Negate();
        public static InkValue operator !(InkValue  left) => left.Clone().Negate();


        public static implicit operator int(InkValue    st) => st.Value_int;
        public static implicit operator float(InkValue  st) => st.Value_float;
        public static implicit operator double(InkValue st) => st.Value_double;
        public static implicit operator bool(InkValue   st) => st.Value_bool;
        public static implicit operator char(InkValue   st) => st.Value_char;
        public static implicit operator string(InkValue st) => st.Value_String;


        protected InkValue Negate()
        {
            if (ValueType == TypeCode.Boolean)
            {
                Value_bool = !Value_bool;
            }

            return this;
        }
    }


    /// <summary> InkSyntaxList is a list of object, it can be used to store the syntax object </summary>
    public partial class InkSyntaxList
    {
        public static readonly Queue<InkSyntaxList> pool        = new(UniInk_Speed.CAPACITY_LIST);
        public static readonly List<InkSyntaxList>  LambdaCache = new(UniInk_Speed.CAPACITY_LIST);
        public static          InkSyntaxList        Get() => pool.Count > 0 ? pool.Dequeue() : new();

        public static InkSyntaxList GetTemp()
        {
            var get = Get();
            LambdaCache.Add(get);
            return get;
        }

        public static void ReleasePool() => pool.Clear();

        public static void ReleaseAll(InkSyntaxList value)
        {
            foreach (var obj in value.ObjectList)
            {
                if (obj is InkValue inkValue)
                {
                    InkValue.Release(inkValue);
                }
            }

            foreach (var obj in value.CastOther)
            {
                if (obj is InkValue inkValue)
                {
                    InkValue.Release(inkValue);
                }
            }

            value.ObjectList.Clear();
            value.CastOther.Clear();
            value.IndexDirty.Clear();

            pool.Enqueue(value);
        }

        public static void ReleaseTemp()
        {
            foreach (var inkSyntaxList in LambdaCache)
            {
                ReleaseAll(inkSyntaxList);
            }

            LambdaCache.Clear();
        }

        public static void Recover(InkSyntaxList value)
        {
            for (var index = 0 ; index < value.CastOther.Count ; index++)
            {
                var obj = value.CastOther[index];
                if (obj is InkValue inkValue)
                {
                    InkValue.Release(inkValue);
                }

                value.CastOther[index] = null;
            }

            for (var index = 0 ; index < value.IndexDirty.Count ; index++)
            {
                value.IndexDirty[index] = false;
            }
        }

        public readonly List<object> ObjectList = new(UniInk_Speed.CAPACITY_LIST);
        public readonly List<object> CastOther  = new(UniInk_Speed.CAPACITY_LIST);
        public readonly List<bool>   IndexDirty = new(UniInk_Speed.CAPACITY_LIST);



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


    /// <summary> InkSyntaxException throw when the syntax is wrong  </summary>
    public partial class InkSyntaxException : Exception
    {
        public InkSyntaxException(string message) : base(message) { }
    }

}
//2058 lines of code
//3000 lines of code [MAX]