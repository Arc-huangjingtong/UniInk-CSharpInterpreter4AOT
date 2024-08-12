namespace Arc.UniInk
{

    using System;
    using System.Text.RegularExpressions;


    /*******************************************************************************************************************
    *📰 Title    :  UniInk_Note (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                  *
    *🔖 Version  :  1.0.0                                                                                              *
    *😀 Authors  :  Arc (https://github.com/Arc-huangjingtong)                                                         *
    *📝 Desc     :  Provide commonly used string-related processing tools                                              *
    /*******************************************************************************************************************/


    /// <summary> Provide commonly tools in <see cref="UniInk_Speed"/> </summary>
    /// <remarks> not assurance zero GC </remarks>
    public static class UniInk_Extensions
    {
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


    public partial class UniInk_Speed
    {
        /// <summary> Process the script with the if statement(or not) </summary>
        /// <remarks> Not support Nested if statement </remarks>
        protected static object ProcessList_ScriptsWithIfStatement(InkSyntaxList keys)
        {
            var    start = 0;
            var    end   = keys.Count - 1;
            object res;

            while (true)
            {
                //找到第一个[if]
                var (success_if, index_if) = FindOperator(keys, InkOperator.KeyIf, start, end);
                //找到第一个[ ;]
                var (success_semi, index_semi) = FindOperator(keys, InkOperator.Semicolon, start, end);


                if (success_semi && index_semi < index_if)
                {
                    ProcessList(keys, start, index_semi - 1);
                    keys.SetDirty(index_semi);
                    start = index_semi + 1;
                    continue;
                }

                if (success_if && index_if < index_semi)
                {
                    ProcessList_IfStatement(keys, index_if);
                    continue;
                }


                res = ProcessList(keys, start, index_semi); //index is the last index

                break;
            }

            return res;
        }

        protected static int ProcessList_IfStatement(InkSyntaxList keys, int start)
        {
            // Begin : keyword [if] (condition) { operation }      Open : [if] flag
            // Then  : keyword [else if] (condition) { operation } need : [if] flag
            // End   : keyword [else] { operation }                need : [if] flag and close [if] flag

            // 这个函数会被检测函数调用,那么一开始一定有一个 If 关键字
            var (conditionStart, conditionEnd)
                = GetMatchOperator(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, start, keys.Count - 1);

            // 检测条件是否正确,如果不正确则直接抛出异常
            if (conditionStart == -1 || start + 1 != conditionStart)
            {
                throw new InkSyntaxException("The if statement is wronging");
            }


            // 定位操作的开始和结束
            var (operationStart, operationEnd)
                = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, conditionEnd + 1, keys.Count - 1);

            if (operationStart == -1)
            {
                throw new InkSyntaxException("The if statement is wronging");
            }

            var flag_if     = true;
            var flag_result = false;
            var ifCondition = false;
            var currentEnd  = operationEnd;


            // 计算条件
            var condition = ProcessList(keys, conditionStart + 1, conditionEnd - 1);

            if (condition is InkValue conditionValue)
            {
                if (conditionValue)
                {
                    ifCondition = true;
                    ProcessList(keys, operationStart + 1, operationEnd - 1);
                }
                else
                {
                    ifCondition = false;
                }
            }


            while (true)
            {
                // Check the position relationship between the next else and if, and then enumerate all cases based on their relationship (very elegant)
                var (success_else, index_else) = FindOperator(keys, InkOperator.KeyElse, operationEnd + 1, keys.Count - 1);
                var (success_if, index_if)     = FindOperator(keys, InkOperator.KeyIf,   operationEnd + 1, keys.Count - 1);

                // 1. 如果没有找到 else 和 if , 直接退出If 结算           , 说明后面再也没有 If 语句了 , 交给下一个循环即可
                // 2. 如果找到了 if   , 没找到 else                     , 说明这是一个 if 语句      , 交给下一个循环即可
                // 3. 如果找到了 else , 没找到 if                       , 说明这是一个 else 语句
                // 4. 如果找到了 else , 找到 if 且 if 在 else 之后(大于1) , 说明这是一个 else 语句 , 虽然后面还有 IF, 但是交给下一次循环即可
                // 5. 如果找到了 else , 找到 if 且 if 在 else 之前(等于1) , 说明这是一个 else if 语句

                switch (success_else, success_if, index_if - index_else)
                {
                    case (false, false, _) : return operationEnd;
                    case (false, true, _) :  return operationEnd;
                    case (true, false, _) :
                    case (true, true, > 1) :
                    {
                        var (opElseStart, opElseEnd)
                            = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, index_else + 1, keys.Count - 1);

                        if (!ifCondition)
                        {
                            ProcessList(keys, opElseStart + 1, opElseEnd - 1);
                        }

                        return opElseEnd;
                    }
                    case (true, true, 1) : // else if
                    {
                        //if (1 == 1)
                        //{
                        //  1 + 1;
                        //}
                        //else if (1 == 1)
                        //{
                        //  1 - 1;
                        //}
                        //else 
                        //{
                        //  1 - 1;
                        //}
                        //FuncA();
                        //FuncB();

                        // 匹配 else if 后的条件
                        var (cdsStart, cdsEnd)
                            = GetMatchOperator(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, index_if + 1, keys.Count - 1);

                        // 计算 else if 条件后的操作
                        var (opElseStart, opElseEnd)
                            = GetMatchOperator(keys, InkOperator.BraceLeft, InkOperator.BraceRight, cdsEnd + 1, keys.Count - 1);

                        if (!ifCondition) //如果还没有找到正确的条件
                        {
                            // 计算 else if 后的条件
                            var condition_elseif = ProcessList(keys, cdsStart + 1, cdsEnd - 1);

                            if (condition_elseif is InkValue conditionValue_elseif)
                            {
                                ifCondition = conditionValue_elseif;
                            }

                            if (!ifCondition)
                            {
                                ProcessList(keys, opElseStart + 1, opElseEnd - 1);
                            }
                        }


                        break;
                    }
                }
            }
        }



        // the sign of an if statement:
        // Begin : keyword [if] (condition) { operation } 
        // Then  : keyword [else if] (condition) { operation } need : [if] 
        // End   : keyword [else] { operation } need : [if] and close [if]



        // if (Has State Keywords)
        // {
        //
        // 
        // 
        //      While (true)
        //      {
        //         var (hasState , ConditionPartStart , ConditionPartEnd , OperationStart , OperationEnd) = GetStatementPart();
        //         var result = ProcessList(keys, OperationStart, OperationEnd);
        //         if (hasState == false)
        //         {
        //            break;
        //         }
        //         else if (result == true)
        //         {
        //
        //             ProcessList(keys, OperationStart, OperationEnd); (recursion)
        //             var end = GetStatementEnd(); // get the end of the statement , and jump to ;
        //             SetDirty(..end);
        //         }
        //         else
        //         {
        //             continue;
        //         }
        //      }
        // }
    }


    #endregion

}