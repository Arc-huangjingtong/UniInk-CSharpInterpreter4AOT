namespace Arc.UniInk
{

    /******************************************************************************************************************
    * 📰 Title    :  UniInk_Note (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                *
    * 🔖 Version  :  1.0.0                                                                                            *
    * 😀 Authors  :  Arc (https://github.com/Arc-huangjingtong)                                                       *
    * 📝 Desc     :  Provide commonly used string-related processing tools                                            *
    /******************************************************************************************************************/

    using System.Text.RegularExpressions;
    using System;


    /// <summary> Provide commonly tools in <see cref="UniInk"/> </summary>
    /// <remarks> [⚠]not assurance zero GC </remarks>
    public static class UniInk_Extensions
    {
        /// <summary> Get a Bool? Value with UniInk </summary>
        /// <remarks> Automatically recycle the generated InkValue internally, and return null if the operation fails </remarks> 
        public static bool? EvaluateBool(this UniInk ink, string expression)
        {
            var result = ink.Evaluate_IfStatement(expression);

            if (result is InkValue { ValueType: TypeCode.Boolean } inkValue)
            {
                var resultBool = inkValue.Value_bool;

                InkValue.Release(inkValue);

                return resultBool;
            }

            return null;
        }

        /// <summary> Get a Int32? Value with UniInk </summary>
        /// <remarks> Automatically recycle the generated InkValue internally, and return null if the operation fails </remarks>
        public static int? EvaluateInt(this UniInk ink, string expression)
        {
            var result = ink.Evaluate_IfStatement(expression);

            if (result is InkValue { ValueType: TypeCode.Int32 } inkValue)
            {
                var resultInt = inkValue.Value_int;

                InkValue.Release(inkValue);

                return resultInt;
            }

            return null;
        }

        #region Remove comments


        //Base on : https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689
        private static readonly Regex removeCommentsRegex = new($"{blockComments}|{lineComments}|{stringsIgnore}|{verbatimStringsIgnore}", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex newLineCharsRegex   = new(@"\r\n|\r|\n", RegexOptions.Compiled);

        private const string verbatimStringsIgnore = @"@(""[^""]*"")+";           //language=regex
        private const string stringsIgnore         = @"""((\\[^\n]|[^""\n])*)"""; //language=regex
        private const string blockComments         = @"/\*(.*?)\*/";              //language=regex
        private const string lineComments          = @"//[^\r\n]*";               //language=regex


        /// <summary> Remove all line and block comments from the specified C# script </summary> 
        /// <returns> C# script without comments                                      </returns>
        /// <param name="scriptWithComments"> C# script with comments                   </param>
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



    #region Support for UniInk_Speed : If Statement


    /// Part: Support for UniInk_Speed : If Statement
    public partial class UniInk
    {
        /// <summary> Process the script with the if statement(or not) </summary>
        /// <remarks> Not support Nested if statement </remarks>
        protected static object ProcessList_ScriptsWithIfStatement(InkSyntaxList keys)
        {
            var    index_start = 0;
            var    index_end   = keys.Count - 1;
            object res         = null;

            while (true)
            {
                //找到第一个[if]
                var (success_if, index_if) = FindOperator(keys, InkOperator.KeyIf, index_start, index_end);
                //找到第一个[ ;]
                var (success_semi, index_semi) = FindOperator(keys, InkOperator.Semicolon, index_start, index_end);


                if (success_semi && index_semi < index_if)
                {
                    ProcessList(keys, index_start, index_semi - 1);
                    keys.SetDirty(index_semi);
                    index_start = index_semi + 1;
                    continue;
                }

                if (success_if && index_if < index_semi)
                {
                    var current = ProcessList_IfStatement(keys, index_if);

                    keys.SetDirty(null, index_start, current);
                    index_start = current + 1;
                    continue;
                }

                if (index_start < index_end)
                {
                    res = ProcessList(keys, index_start, index_semi); //index is the last index
                }

                break;
            }

            for (var i = 0 ; i < keys.Count ; i++)
            {
                if (keys.CastOther[i] is InkValue { returner: true } value)
                {
                    res = value.Clone();
                }
            }

            return res;
        }

        /// <summary> Process the if statement </summary>
        /// <returns> Return the processed lasted operator [}]  </returns>
        protected static int ProcessList_IfStatement(InkSyntaxList keys, int start)
        {
            // Begin : keyword [if] (condition) { operation }      Open : [if] flag
            // Then  : keyword [else if] (condition) { operation } need : [if] flag
            // End   : keyword [else] { operation }                need : [if] flag and close [if] flag

            // This function will be called by the detection function, so there must be an If keyword at the beginning

            // Locate the [If (condition)]'s condition
            var (conditionStart, conditionEnd)
                = GetMatchOperator(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, start, keys.Count - 1);

            // Check if the condition is correct, if not, throw an exception directly
            if (conditionStart == -1 || start + 1 != conditionStart)
            {
                throw new InkSyntaxException("The if statement is wronging");
            }

            // Locate the [(condition){ operation }] operation
            var (operationStart, operationEnd)
                = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, conditionEnd + 1, keys.Count - 1);

            if (operationStart == -1)
            {
                throw new InkSyntaxException("The if statement is wronging");
            }

            var ifCondition = false;
            var currentEnd  = operationEnd;
            var index_end   = keys.Count - 1;

            var condition = ProcessList(keys, conditionStart + 1, conditionEnd - 1);

            if (condition is InkValue conditionValue)
            {
                if (conditionValue)
                {
                    ifCondition = true;
                    ProcessList(keys, operationStart + 1, operationEnd - 1);
                }

                InkValue.Release(conditionValue);
            }


            while (true)
            {
                // Check the position relationship between the next else and if, and then enumerate all cases based on their relationship (very elegant)
                var (success_else, index_else) = FindOperator(keys, InkOperator.KeyElse, currentEnd + 1, index_end);
                var (success_if, index_if)     = FindOperator(keys, InkOperator.KeyIf,   currentEnd + 1, index_end);

                // 1. 如果没有找到 else 和 if , 直接退出If 结算           , 说明后面再也没有 If 语句了 , 交给下一个循环即可
                // 2. 如果找到了 if   , 没找到 else                     , 说明这是一个 if 语句      , 交给下一个循环即可
                // 3. 如果找到了 else , 没找到 if                       , 说明这是一个 else 语句
                // 4. 如果找到了 else , 找到 if 且 if 在 else 之后(大于1) , 说明这是一个 else 语句 , 虽然后面还有 IF, 但是交给下一次循环即可
                // 5. 如果找到了 else , 找到 if 且 if 在 else 之前(等于1) , 说明这是一个 else if 语句

                switch (success_else, success_if, index_if - index_else)
                {
                    case (false, false, _) : return currentEnd;
                    case (false, true, _) :  return currentEnd;
                    case (true, false, _) :
                    case (true, true, > 1) :
                    {
                        var temp_start = index_else + 1;

                        if (success_if) temp_start++;

                        var (opElseStart, opElseEnd)
                            = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, temp_start, keys.Count - 1);

                        if (!ifCondition)
                        {
                            var result = ProcessList(keys, opElseStart + 1, opElseEnd - 1);

                            if (result is InkValue resultValue)
                            {
                                InkValue.Release(resultValue);
                            }
                        }

                        currentEnd = opElseEnd;

                        return currentEnd;
                    }
                    case (true, true, 1) : // else if
                    {
                        // 匹配 else if (condition) 后的条件
                        var (cdsStart, cdsEnd)
                            = GetMatchOperator(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, index_if + 1, index_end);

                        // 计算 else if 条件后的操作
                        var (opElseStart, opElseEnd)
                            = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, cdsEnd + 1, index_end);


                        // 计算 else if 后的条件
                        var condition_elseif = ProcessList(keys, cdsStart + 1, cdsEnd - 1);

                        if (condition_elseif is InkValue conditionValue_elseif)
                        {
                            if (conditionValue_elseif && ifCondition == false)
                            {
                                var result = ProcessList(keys, opElseStart + 1, opElseEnd - 1);

                                if (result is InkValue resultValue)
                                {
                                    InkValue.Release(resultValue);
                                }
                            }

                            ifCondition = conditionValue_elseif || ifCondition;

                            InkValue.Release(conditionValue_elseif);
                        }

                        currentEnd = opElseEnd;
                        break;
                    }
                }
            }
        }

        /// <summary> Judge the input String is a Script or not (depend on the operator:[;] [{]) </summary>
        protected static bool InputIsScript_IfStatement(InkSyntaxList keys)
        {
            foreach (var obj in keys.ObjectList)
            {
                //match [;] or [{]
                if (obj is InkOperator op && (Equals(op, InkOperator.Semicolon) || Equals(op, InkOperator.BraceLeft)))
                {
                    return true;
                }
            }


            return false;
        }

        public static object ExecuteProcess_IfStatement(InkSyntaxList keys)
        {
            var res = InputIsScript_IfStatement(keys) ? ProcessList_ScriptsWithIfStatement(keys) : ProcessList(keys, 0, keys.Count - 1);

            return res;
        }

        /// <summary> Evaluate the expression with the if statement </summary>
        public object Evaluate_IfStatement(string expression, int startIndex, int endIndex)
        {
            var keys = CompileLexerAndFill(expression, startIndex, endIndex);

            var result = ExecuteProcess_IfStatement(keys);

            RecoverResources(keys);

            return result;
        }

        public object Evaluate_IfStatement(string expression)
        {
            return Evaluate_IfStatement(expression, 0, expression.Length - 1);
        }
    }


    #endregion

}