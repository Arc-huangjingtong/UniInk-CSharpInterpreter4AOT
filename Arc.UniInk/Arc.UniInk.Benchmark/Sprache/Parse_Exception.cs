namespace Sprache
{

    using System;


    /// <summary> 表示解析过程中发生的错误 </summary>
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
        public ParseException(string message, Position position) : base(message)
        {
            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            Position = position;
        }


        /// <summary>如果可用，则获取解析失败的位置；否则为 null </summary>
        public Position Position { get; }
    }

}