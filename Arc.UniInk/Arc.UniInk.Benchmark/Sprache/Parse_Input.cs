namespace Sprache
{

    using System;
    using System.Collections.Generic;


    /// <summary>Represents an input for parsing.</summary>
    public class Input : IInput
    {
        /// <summary>获取分配给 <see cref="Input" /> 实例的备忘录列表</summary>
        public IDictionary<object, object> Memos { get; }

        /// <summary>Initializes a new instance of the <see cref="Input" /> class.</summary>
        /// <param name="source">The source.</param>
        public Input(string source) : this(source, 0) { }

        internal Input(string source, int position, int line = 1, int column = 1)
        {
            Source   = source;
            Position = position;
            Line     = line;
            Column   = column;

            Memos = new Dictionary<object, object>();
        }

        /// <summary>推进输入</summary>
        /// <returns>A new <see cref="IInput" /> that is advanced.</returns>
        /// <exception cref="System.InvalidOperationException">The input is already at the end of the source.</exception>
        public IInput Advance()
        {
            if (AtEnd) throw new InvalidOperationException("The input is already at the end of the source.");

            return new Input(Source, Position + 1, Current == '\n' ? Line + 1 : Line, Current == '\n' ? 1 : Column + 1);
        }

        /// <summary>源字符串</summary>
        public string Source { get; }

        /// <summary>当前字符</summary>
        public char Current => Source[Position];

        /// <summary>获取一个值，该值指示是否到达了源的末尾。</summary>
        public bool AtEnd => Position == Source.Length;

        /// <summary>获取当前位置</summary>
        public int Position { get; }

        /// <summary>当前行号</summary>
        public int Line { get; }

        /// <summary>当前列号</summary>
        public int Column { get; }



        /// <summary>Indicates whether the current <see cref="Input" /> is equal to another object of the same type.</summary>
        /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IInput other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Source, other.Source) && Position == other.Position;
        }

        public override string ToString() => $"Line {Line}, Column {Column}";

        public override int GetHashCode()
        {
            unchecked { return ((Source != null ? Source.GetHashCode() : 0) * 397) ^ Position; }
        }

        public override bool Equals(object obj) => Equals(obj as IInput);

        public static bool operator ==(Input left, Input right) => Equals(left, right);
        public static bool operator !=(Input left, Input right) => !Equals(left, right);
    }


    /// <summary> 表示用于解析的输入 </summary>
    public interface IInput : IEquatable<IInput>
    {
        /// <summary> Advances the input. </summary>
        /// <returns>A new <see cref="IInput" /> that is advanced.</returns>
        /// <exception cref="System.InvalidOperationException">The input is already at the end of the source.</exception>
        IInput Advance();

        /// <summary> 获取整个源 </summary>
        string Source { get; }

        /// <summary> 获取当前的字符 <see cref="System.Char" />.</summary>
        char Current { get; }

        /// <summary> 是否已到达源的末尾 </summary>
        bool AtEnd { get; }

        /// <summary> 获取当前位置 </summary>
        int Position { get; }

        /// <summary> 获取当前行号 </summary>
        int Line { get; }

        /// <summary> 获取当前列号 </summary>
        int Column { get; }

        /// <summary>备忘录</summary>
        IDictionary<object, object> Memos { get; }
    }

}