namespace Arc.UniInk.Note
{

    /*******************************************************************************************************************
    *📰 Title    :  UniInk_Note (https://github.com/Arc-huangjingtong/UniInk-CSharpInterpreter4Unity)                  *
    *🔖 Version  :  1.0.0                                                                                              *
    *😀 Authors  :  Arc (https://github.com/Arc-huangjingtong)                                                         *
    *📝 Desc     :  Provide commonly used string-related processing tools                                              *
    /*******************************************************************************************************************/

    using System.Text.RegularExpressions;



    public static class UniInkHelper
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

}