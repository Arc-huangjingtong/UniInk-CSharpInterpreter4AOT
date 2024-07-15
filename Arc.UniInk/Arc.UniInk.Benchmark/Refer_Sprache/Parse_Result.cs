namespace Sprache
{

    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary> 包含一些帮助函数，用于创建 <see cref="IResult&lt;T&gt;"/> 实例 </summary>
    public static class ResultHelper
    {
        /// <summary> 创建一个成功的 Result </summary>
        /// <param name="value">成功解析的值</param>
        /// <param name="remainder">输入的剩余部分</param>
        public static IResult<T> Success <T>(T value, IInput remainder) => new Result<T>(value, remainder);

        /// <summary> 创建一个失败的 Result </summary>
        /// <param name="remainder">输入的剩余部分</param>
        /// <param name="message">错误消息</param>
        /// <param name="expectations">解析器的期望</param>
        public static IResult<T> Failure <T>(IInput remainder, string message, IEnumerable<string> expectations) => new Result<T>(remainder, message, expectations);
    }


    internal class Result <T> : IResult<T>
    {
        private readonly T _value;

        /// <summary>解析成功的结果</summary>
        public Result(T value, IInput remainder)
        {
            WasSuccessful = true;
            _value        = value;
            Remainder     = remainder;
            Message       = null;
            Expectations  = Enumerable.Empty<string>();
        }

        /// <summary>解析失败的结果</summary>
        public Result(IInput remainder, string message, IEnumerable<string> expectations)
        {
            WasSuccessful = false;
            _value        = default;
            Remainder     = remainder;
            Message       = message;
            Expectations  = expectations;
        }

        public T Value => WasSuccessful ? _value : throw new InvalidOperationException("No value can be computed.");

        public bool WasSuccessful { get; }

        public string Message { get; }

        public IEnumerable<string> Expectations { get; }

        public IInput Remainder { get; }

        public override string ToString()
        {
            if (WasSuccessful)
            {
                return $"Successful parsing of {Value}.";
            }

            var expMsg = "";

            if (Expectations.Any())
            {
                expMsg = " expected " + Expectations.Aggregate((e1, e2) => $"{e1} or {e2}");
            }

            var recentlyConsumed = CalculateRecentlyConsumed();

            return $"Parsing failure: {Message};{expMsg} ({Remainder}); recently consumed: {recentlyConsumed}";
        }

        private string CalculateRecentlyConsumed()
        {
            const int windowSize = 10;

            var totalConsumedChars = Remainder.Position;
            var windowStart        = totalConsumedChars - windowSize;
            windowStart = windowStart < 0 ? 0 : windowStart;

            var numberOfRecentlyConsumedChars = totalConsumedChars - windowStart;

            return Remainder.Source.Substring(windowStart, numberOfRecentlyConsumedChars);
        }
    }



    /// <summary>表示解析结果.</summary>
    /// <typeparam name="T">结果类型</typeparam>
    public interface IResult <out T>
    {
        /// <summary>获取结果值</summary>
        T Value { get; }

        /// <summary>解析是否成功.</summary>
        bool WasSuccessful { get; }

        /// <summary>获取错误消息.</summary>
        string Message { get; }

        /// <summary>获取解析器的期望，以防出现错误</summary>
        IEnumerable<string> Expectations { get; }

        /// <summary>获取输入的剩余部分</summary> 
        IInput Remainder { get; }
    }

}