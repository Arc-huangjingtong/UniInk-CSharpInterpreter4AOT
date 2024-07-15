namespace Sprache
{

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NUnit.Framework;


    /// <summary>解析器和组合器</summary>
    public static partial class Parse
    {
        /// <summary>当检测到左递归时, 失败结果的消息</summary> 
        public const string LeftRecursionErrorMessage = "Left recursion in the grammar.";

        /// <summary> 匹配任意字符.</summary>
        public static readonly Parser<char> AnyChar = MatchChar(_ => true, "any character");

        /// <summary> 匹配一个空白字符.</summary>
        public static readonly Parser<char> WhiteSpace = MatchChar(char.IsWhiteSpace, "whitespace");

        /// <summary> 匹配一个十进制数字.</summary>
        public static readonly Parser<char> Digit = MatchChar(char.IsDigit, "digit");

        /// <summary> 匹配一个字母.</summary>
        public static readonly Parser<char> Letter = MatchChar(char.IsLetter, "letter");

        /// <summary> 匹配一个字母或数字.</summary>
        public static readonly Parser<char> LetterOrDigit = MatchChar(char.IsLetterOrDigit, "letter or digit");

        /// <summary> 匹配一个小写字母.</summary>
        public static readonly Parser<char> Lower = MatchChar(char.IsLower, "lowercase letter");

        /// <summary> 匹配一个大写字母.</summary>
        public static readonly Parser<char> Upper = MatchChar(char.IsUpper, "uppercase letter");

        /// <summary> 匹配一个数字字符.</summary>
        public static readonly Parser<char> Numeric = MatchChar(char.IsNumber, "numeric character");



        /// <summary>尝试解析一个匹配 'predicate' 的字符.</summary>
        public static Parser<char> MatchChar(Predicate<char> predicate, string description)
        {
            if (predicate   == null) throw new ArgumentNullException(nameof(predicate));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return Parser;

            IResult<char> Parser(IInput i)
            {
                if (!i.AtEnd)
                {
                    if (predicate(i.Current))
                    {
                        return ResultHelper.Success(i.Current, i.Advance());
                    }

                    return ResultHelper.Failure<char>(i, $"unexpected '{i.Current}'", new[] { description });
                }

                return ResultHelper.Failure<char>(i, "Unexpected end of input reached", new[] { description });
            }
        }

        public static Parser<char> MatchChar(char c) => MatchChar(ch => c == ch, char.ToString(c));


        /// <summary> 匹配一个字符, 除了匹配 <paramref name="predicate"/> 的字符.</summary>
        public static Parser<char> MatchCharExcept(Predicate<char> predicate, string description)
        {
            return MatchChar(c => !predicate(c), $"any character except {description}");
        }

        public static Parser<char> MatchCharExcept(char c) => MatchCharExcept(ch => c == ch, char.ToString(c));


        /// <summary> 匹配一个字符, 匹配其中任意一个在 chars  中的字符</summary>
        public static Parser<char> MatchChars(params char[] chars) => MatchChar(chars.Contains, string.Join("|", chars));

        /// <summary> 匹配一个字符, 匹配其中任意一个在 string 中的字符</summary>
        public static Parser<char> MatchChars(string str) => MatchChar(str.Contains, string.Join("|", str));


        /// <summary> 匹配一个字符, 除了匹配 <paramref name="chars"/> 中的字符.</summary>
        public static Parser<char> MatchCharExcept(IEnumerable<char> chars)
        {
            var _chars = chars as char[] ?? chars.ToArray();
            return MatchCharExcept(_chars.Contains, string.Join("|", _chars));
        }

        /// <summary> 匹配一个字符, 除了匹配 <paramref name="str"/> 中的字符.</summary>
        public static Parser<char> MatchCharExcept(string str) => MatchCharExcept(str.Contains, string.Join("|", str));

        /// <summary> 匹配一个字符, 不分大小写的匹配一个字符</summary>
        public static Parser<char> MatchCharIgnoreCase(char c)
        {
            return MatchChar(ch => char.ToLower(c) == char.ToLower(ch), char.ToString(c));
        }

        /// <summary> 匹配一个字符, 不分大小写的匹配 <paramref name="str"/> 中的字符 </summary>
        public static Parser<IEnumerable<char>> MatchCharIgnoreCase(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            return str.Select(MatchCharIgnoreCase).Aggregate(Return(Enumerable.Empty<char>()), (a, p) => a.Concat(p.Once())).Named(str);
        }


        /// <summary> 匹配一个字符串, 是否等于<see cref="str"/></summary>
        public static Parser<IEnumerable<char>> MatchString(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            return str.Select(MatchChar).Aggregate(Return(Enumerable.Empty<char>()), (a, p) => a.Concat(p.Once())).Named(str);
        }



        /// <summary> 构造一个解析器, 如果给定的解析器成功, 则失败, 如果给定的解析器失败, 则成功. 在任何情况下, 它都不会消耗任何输入. 就像正则表达式中的负向前瞻.</summary>
        /// <param name="parser">要包装的解析器</param>
        /// <returns>一个与给定解析器相反的解析器.</returns>
        public static Parser<object> Not <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return Parser;

            IResult<object> Parser(IInput i)
            {
                var result = parser(i);

                if (result.WasSuccessful)
                {
                    var msg = $"`{string.Join(", ", result.Expectations)}' was not expected";
                    return ResultHelper.Failure<object>(i, msg, Array.Empty<string>());
                }

                return ResultHelper.Success<object>(null, i);
            }
        }

        /// <summary>首先解析第一个, 如果成功, 则解析第二个.</summary>
        public static Parser<U> Then <T, U>(this Parser<T> first, Func<T, Parser<U>> second)
        {
            if (first  == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i => first(i).IfSuccess(s => second(s.Value)(s.Remainder));
        }

        /// <summary>多次匹配一系列元素,以命令式实现以减少堆栈使用.</summary> 
        public static Parser<IEnumerable<T>> Many <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return i =>
            {
                var remainder = i;
                var result    = new List<T>();
                var r         = parser(i);

                while (r.WasSuccessful)
                {
                    if (remainder.Equals(r.Remainder)) break;

                    result.Add(r.Value);
                    remainder = r.Remainder;
                    r         = parser(remainder);
                }

                return ResultHelper.Success<IEnumerable<T>>(result, remainder);
            };
        }

        /// <summary> 解析一系列元素, 如果任何元素只解析了一部分, 则失败.</summary>
        /// <remarks>
        /// <para>
        /// Using <seealso cref="XMany{T}(Parser{T})"/> may be preferable to <seealso cref="Many{T}(Parser{T})"/>
        /// where the first character of each match identified by <paramref name="parser"/>
        /// is sufficient to determine whether the entire match should succeed. The X*
        /// methods typically give more helpful errors and are easier to debug than their
        /// unqualified counterparts.
        /// </para>
        /// </remarks>
        /// <seealso cref="XOr{T}"/>
        public static Parser<IEnumerable<T>> XMany <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Many().Then(m => parser.Once().XOr(Return(m)));
        }

        /// <summary>尝试解析至少一个元素的流.</summary>
        public static Parser<IEnumerable<T>> AtLeastOnce <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Once().Then(t1 => parser.Many().Select(t1.Concat));
        }

        /// <summary>
        /// TryParse a stream of elements with at least one item. Except the first
        /// item, all other items will be matched with the <code>XMany</code> operator.
        /// </summary>
        public static Parser<IEnumerable<T>> XAtLeastOnce <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Once().Then(t1 => parser.XMany().Select(t1.Concat));
        }

        /// <summary>
        /// Parse end-of-input.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static Parser<T> End <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return i => parser(i).IfSuccess(s => s.Remainder.AtEnd ? s : ResultHelper.Failure<T>(s.Remainder, $"unexpected '{s.Remainder.Current}'", new[] { "end of input" }));
        }

        /// <summary>
        /// Take the result of parsing, and project it onto a different domain.
        /// </summary>
        public static Parser<U> Select <T, U>(this Parser<T> parser, Func<T, U> convert)
        {
            if (parser  == null) throw new ArgumentNullException(nameof(parser));
            if (convert == null) throw new ArgumentNullException(nameof(convert));

            return parser.Then(t => Return(convert(t)));
        }

        /// <summary> 解析令牌, 嵌入在任意数量的空白字符中.</summary>
        public static Parser<T> Token <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return WhiteSpace.Many().SelectMany(_ => parser, (leading, item) => new { leading, item }).SelectMany(_ => WhiteSpace.Many(), (@t, _) => @t.item);
        }

        /// <summary>
        /// Refer to another parser indirectly. This allows circular compile-time dependency between parsers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Parser<T> Ref <T>(Func<Parser<T>> reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            Parser<T> p = null;

            return i =>
            {
                p ??= reference();

                if (i.Memos.TryGetValue(p, out var memo))
                {
                    var pResult = (IResult<T>)memo;
                    if (pResult.WasSuccessful)
                    {
                        return pResult;
                    }

                    if (!pResult.WasSuccessful && pResult.Message == LeftRecursionErrorMessage)
                    {
                        throw new ParseException(pResult.ToString());
                    }
                }

                i.Memos[p] = ResultHelper.Failure<T>(i, LeftRecursionErrorMessage, Array.Empty<string>());
                var result = p(i);
                i.Memos[p] = result;
                return result;
            };
        }

        /// <summary> 将字符流转换为字符串 </summary>
        public static Parser<string> Text(this Parser<IEnumerable<char>> characters)
        {
            return characters.Select(chs => new string(chs.ToArray()));
        }

        /// <summary>
        /// Parse first, if it succeeds, return first, otherwise try second.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<T> Or <T>(this Parser<T> first, Parser<T> second)
        {
            if (first  == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i =>
            {
                var fr = first(i);
                if (!fr.WasSuccessful)
                {
                    return second(i).IfFailure(sf => DetermineBestError(fr, sf));
                }

                if (fr.Remainder.Equals(i)) return second(i).IfFailure(_ => fr);

                return fr;
            };
        }

        /// <summary>
        /// Names part of the grammar for help with error messages.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Parser<T> Named <T>(this Parser<T> parser, string name)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (name   == null) throw new ArgumentNullException(nameof(name));

            return i => parser(i).IfFailure(f => f.Remainder.Equals(i) ? ResultHelper.Failure<T>(f.Remainder, f.Message, new[] { name }) : f);
        }

        /// <summary>
        /// Parse first, if it succeeds, return first, otherwise try second.
        /// Assumes that the first parsed character will determine the parser chosen (see Try).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<T> XOr <T>(this Parser<T> first, Parser<T> second)
        {
            if (first  == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return i =>
            {
                var fr = first(i);
                if (!fr.WasSuccessful)
                {
                    // The 'X' part
                    if (!fr.Remainder.Equals(i)) return fr;

                    return second(i).IfFailure(sf => DetermineBestError(fr, sf));
                }

                // This handles a zero-length successful application of first.
                if (fr.Remainder.Equals(i)) return second(i).IfFailure(_ => fr);

                return fr;
            };
        }

        // Examines two results presumably obtained at an "Or" junction; returns the result with
        // the most information, or if they apply at the same input position, a union of the results.
        private static IResult<T> DetermineBestError <T>(IResult<T> firstFailure, IResult<T> secondFailure)
        {
            if (secondFailure.Remainder.Position > firstFailure.Remainder.Position) return secondFailure;

            if (secondFailure.Remainder.Position == firstFailure.Remainder.Position) return ResultHelper.Failure<T>(firstFailure.Remainder, firstFailure.Message, firstFailure.Expectations.Union(secondFailure.Expectations));

            return firstFailure;
        }

        /// <summary>
        /// Parse a stream of elements containing only one item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> Once <T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Select(r => (IEnumerable<T>)new[] { r });
        }

        /// <summary>
        /// Concatenate two streams of elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> Concat <T>(this Parser<IEnumerable<T>> first, Parser<IEnumerable<T>> second)
        {
            if (first  == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return first.Then(f => second.Select(f.Concat));
        }

        /// <summary> 立即成功并返回值. </summary>
        public static Parser<T> Return <T>(T value)
        {
            return i => ResultHelper.Success(value, i);
        }

        /// <summary>
        /// Version of Return with simpler inline syntax.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Parser<U> Return <T, U>(this Parser<T> parser, U value)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return parser.Select(_ => value);
        }

        /// <summary>
        /// Attempt parsing only if the <paramref name="except"/> parser fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        public static Parser<T> Except <T, U>(this Parser<T> parser, Parser<U> except)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (except == null) throw new ArgumentNullException(nameof(except));

            // Could be more like: except.Then(s => s.Fail("..")).XOr(parser)
            return i =>
            {
                var r = except(i);
                if (r.WasSuccessful) return ResultHelper.Failure<T>(i, "Excepted parser succeeded.", new[] { "other than the excepted input" });

                return parser(i);
            };
        }

        /// <summary>
        /// Parse a sequence of items until a terminator is reached.
        /// Returns the sequence, discarding the terminator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> Until <T, U>(this Parser<T> parser, Parser<U> until) => parser.Except(until).Many().Then(until.Return);

        /// <summary>
        /// Succeed if the parsed value matches predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Parser<T> Where <T>(this Parser<T> parser, Func<T, bool> predicate)
        {
            if (parser    == null) throw new ArgumentNullException(nameof(parser));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return i => parser(i).IfSuccess(s => predicate(s.Value) ? s : ResultHelper.Failure<T>(i, $"Unexpected {s.Value}.", Array.Empty<string>()));
        }

        /// <summary>适用于 Linq 语法的 Then 单子组合器。</summary>
        public static Parser<V> SelectMany <T, U, V>(this Parser<T> parser, Func<T, Parser<U>> selector, Func<T, U, V> projector)
        {
            if (parser    == null) throw new ArgumentNullException(nameof(parser));
            if (selector  == null) throw new ArgumentNullException(nameof(selector));
            if (projector == null) throw new ArgumentNullException(nameof(projector));

            return parser.Then(t => selector(t).Select(u => projector(t, u)));
        }

        /// <summary>从左到右连接一个左结合的操作符</summary>
        public static Parser<T> ChainOperator <T, TOp>(Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return operand.Then(first => ChainOperatorRest(first, op, operand, apply, Or));
        }

        /// <summary> 从左到右连接一个左结合的操作符</summary>
        public static Parser<T> XChainOperator <T, TOp>(Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return operand.Then(first => ChainOperatorRest(first, op, operand, apply, XOr));
        }

        /// <summary> 从左到右连接一个左结合的操作符</summary>
        private static Parser<T> ChainOperatorRest <T, TOp>(T firstOperand, Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply, Func<Parser<T>, Parser<T>, Parser<T>> or)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return or(op.Then(opValue => operand.Then(operandValue => ChainOperatorRest(apply(opValue, firstOperand, operandValue), op, operand, apply, or))), Return(firstOperand));
        }

        /// <summary> 链一个右结合的操作符 </summary>
        public static Parser<T> ChainRightOperator <T, TOp>(Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply, Or));
        }

        /// <summary>
        /// Chain a right-associative operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOp"></typeparam>
        /// <param name="op"></param>
        /// <param name="operand"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static Parser<T> XChainRightOperator <T, TOp>(Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply, XOr));
        }

        private static Parser<T> ChainRightOperatorRest <T, TOp>(T lastOperand, Parser<TOp> op, Parser<T> operand, Func<TOp, T, T, T> apply, Func<Parser<T>, Parser<T>, Parser<T>> or)
        {
            if (op      == null) throw new ArgumentNullException(nameof(op));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply   == null) throw new ArgumentNullException(nameof(apply));

            return or(op.Then(opValue => operand.Then(operandValue => ChainRightOperatorRest(operandValue, op, operand, apply, or)).Then(r => Return(apply(opValue, lastOperand, r)))), Return(lastOperand));
        }

        /// <summary>解析一个数字</summary>
        public static readonly Parser<string> Number = Numeric.AtLeastOnce().Text();

        private static Parser<string> DecimalWithoutLeadingDigits(CultureInfo ci = null)
        {
            return Return("").SelectMany(_ => MatchString((ci ?? CultureInfo.CurrentCulture).NumberFormat.NumberDecimalSeparator).Text(), (nothing, dot) 
                                             => new { nothing, dot }).SelectMany(@t => Number, (@t, fraction) => @t.dot + fraction);
        }

        private static Parser<string> DecimalWithLeadingDigits(CultureInfo ci = null)
        {
            return Number.Then(n => DecimalWithoutLeadingDigits(ci).XOr(Return("")).Select(f => n + f));
        }

        /// <summary>
        /// Parse a decimal number using the current culture's separator character.
        /// </summary>
        public static readonly Parser<string> Decimal = DecimalWithLeadingDigits().XOr(DecimalWithoutLeadingDigits());

        /// <summary>
        /// Parse a decimal number with separator '.'.
        /// </summary>
        public static readonly Parser<string> DecimalInvariant = DecimalWithLeadingDigits(CultureInfo.InvariantCulture).XOr(DecimalWithoutLeadingDigits(CultureInfo.InvariantCulture));
    }

}