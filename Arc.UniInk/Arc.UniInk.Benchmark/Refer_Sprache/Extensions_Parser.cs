namespace Sprache
{

    using System;


    /// <summary> 表示一个解析器。 </summary>
    /// <typeparam name="T">结果的类型。</typeparam>
    /// <param name="input">要解析的输入。</param>
    /// <returns>解析器的结果。</returns>
    public delegate IResult<T> Parser <out T>(IInput input);


    /// <summary> 包含一些 <see cref="Parser&lt;T&gt;" /> 的扩展方法.</summary> 
    public static class ParserExtensions
    {
        /// <summary>解析输入而不抛出异常</summary>
        public static IResult<T> TryParse <T>(this Parser<T> parser, string input)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (input  == null) throw new ArgumentNullException(nameof(input));

            return parser(new Input(input));
        }

        /// <summary>解析指定的输入字符串</summary>
        /// <exception cref="Sprache.ParseException">包含解析错误的详细信息</exception>
        public static T Parse <T>(this Parser<T> parser, string input)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (input  == null) throw new ArgumentNullException(nameof(input));

            var result = parser.TryParse(input);

            if (result.WasSuccessful)
            {
                return result.Value;
            }

            throw new ParseException(result.ToString(), Position.FromInput(result.Remainder));
        }
    }

}