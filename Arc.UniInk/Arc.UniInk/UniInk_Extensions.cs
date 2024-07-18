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
        protected static object ProcessList_ScriptsWithIfStatement(InkSyntaxList keys)
        {
            var    start = 0;
            object res;

            while (true)
            {
                var (success_if, index_if) = FindOperator(keys, InkOperator.KeyIf,     start, keys.Count - 1);
                var (success, index)       = FindOperator(keys, InkOperator.Semicolon, start, keys.Count - 1);

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

        protected static void ProcessList_IfStatement(InkSyntaxList keys, int start, int end)
        {
            var              hasIfState = true;
            Span<EStatement> spanKeys   = stackalloc EStatement[end - start];

            while (hasIfState)
            {
                int startIndex, endIndex;

                (hasIfState, startIndex, endIndex) = FindSection(keys, InkOperator.ParenthisLeft, InkOperator.ParenthisRight, start, end);

                if (!hasIfState) continue;

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


        public enum EStatement { IF, ELSE, ELSE_IF }

        // the sign of an if statement, begin at the first [if] , end at the last [}] meanwhile the next object is not [else if] or [else]

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