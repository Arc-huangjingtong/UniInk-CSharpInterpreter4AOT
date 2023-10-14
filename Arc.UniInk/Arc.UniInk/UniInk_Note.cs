/************************************************************************************************************************
 * UniInk_NoteHub : use for note code and test code                                                                     *
 * In reality, it is difficult to satisfy everyone's script writing requirements through a unified abstract design.     *
 * Therefore, we must strike a balance between feature support and interpreter performance.                             *
/************************************************************************************************************************/


namespace Arc.UniInk
{
    public class UniInk_Note
    {
        // /// <summary>get scripts between [{] and [}]</summary>
        // /// <remarks>exclude [{] and [}] in string or char</remarks>
        // /// <param name="parentScript">the parent scripts </param>
        // /// <param name="index">the index start with [{] </param>
        // /// <returns>the substring end with [}]</returns>
        // /// <exception cref="Exception">missing [}]</exception>
        // private string GetScriptBetweenCurlyBrackets(string parentScript, ref int index)
        // {
        //     var currentScript = new StringBuilder();
        //     var bracketCount = 1;
        //     for (; index < parentScript.Length; index++)
        //     {
        //         var internalStringMatch = regex_String.Match(parentScript, index, parentScript.Length - index);
        //         if (internalStringMatch.Success)
        //         {
        //             var innerString = internalStringMatch.Value; //TODO:当字符串没有另一边的引号时，错误不会在此处抛出
        //             currentScript.Append(innerString);
        //             index += innerString.Length - 1;
        //             continue;
        //         }
        //
        //         var internalCharMatch = regex_Char.Match(parentScript, index, parentScript.Length - index);
        //         if (internalCharMatch.Success)
        //         {
        //             currentScript.Append(internalCharMatch.Value);
        //             index += internalCharMatch.Length - 1;
        //             continue;
        //         }
        //
        //
        //         var s = parentScript[index];
        //
        //         if (s.Equals('{')) bracketCount++;
        //         if (s.Equals('}')) bracketCount--;
        //
        //         if (bracketCount == 0) break;
        //
        //         currentScript.Append(s);
        //     }
        //
        //     if (bracketCount > 0) throw new Exception($"[{index}]:{bracketCount} '}}' is missing ！");
        //
        //
        //     return currentScript.ToString();
        // }
        
        
        //      /// <summary>Get a expression list between [startChar] and [endChar]</summary>
        // /// <remarks>⚠️The startChar , endChar and separator must be different</remarks>
        // private List<string> GetExpressionsParenthesized(string expression, ref int i, bool checkSeparator, char separator = ',', char startChar = '(', char endChar = ')')
        // {
        //     var expressionsList = new List<string>();
        //
        //     var currentExpression = string.Empty;
        //     var bracketCount = 1;
        //
        //     /// We must prevent the string having separator or startend char that we define
        //     for (; i < expression.Length; i++)
        //     {
        //         var internalStringMatch = regex_String.Match(expression, i, expression.Length - i);
        //         var internalCharMatch = regex_Char.Match(expression, i, expression.Length - i);
        //
        //         if (internalStringMatch.Success)
        //         {
        //             currentExpression += internalStringMatch.Value;
        //             i += internalStringMatch.Length - 1;
        //             continue;
        //         }
        //
        //         if (internalCharMatch.Success)
        //         {
        //             currentExpression += internalCharMatch.Value;
        //             i += internalCharMatch.Length - 1;
        //             continue;
        //         }
        //
        //         var s = expression[i];
        //
        //         if (s.Equals(startChar)) bracketCount++;
        //         if (s.Equals(endChar)) bracketCount--;
        //
        //         if (bracketCount == 0)
        //         {
        //             if (!string.IsNullOrWhiteSpace(currentExpression))
        //                 expressionsList.Add(currentExpression);
        //             break;
        //         }
        //
        //         if (checkSeparator && s.Equals(separator) && bracketCount == 1)
        //         {
        //             expressionsList.Add(currentExpression);
        //             currentExpression = string.Empty;
        //         }
        //         else
        //         {
        //             currentExpression += s;
        //         }
        //     }
        //
        //     if (bracketCount > 0)
        //     {
        //         throw new SyntaxException($"[{expression}] is missing characters ['{endChar}'] ");
        //     }
        //
        //     return expressionsList;
        // }
        
    }
}