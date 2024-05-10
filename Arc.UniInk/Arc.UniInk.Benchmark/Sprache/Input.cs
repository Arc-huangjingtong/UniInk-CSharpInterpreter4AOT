namespace Sprache
{

    using System;
    using System.Collections.Generic;


    /// <summary>Represents an input for parsing.</summary>
    public class Input : IInput
    {
        private readonly string _source;
        private readonly int    _position;
        private readonly int    _line;
        private readonly int    _column;

        /// <summary>
        /// Gets the list of memos assigned to the <see cref="Input" /> instance.
        /// </summary>
        public IDictionary<object, object> Memos { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Input" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public Input(string source) : this(source, 0) { }

        internal Input(string source, int position, int line = 1, int column = 1)
        {
            _source   = source;
            _position = position;
            _line     = line;
            _column   = column;

            Memos = new Dictionary<object, object>();
        }

        /// <summary>
        /// Advances the input.
        /// </summary>
        /// <returns>A new <see cref="IInput" /> that is advanced.</returns>
        /// <exception cref="System.InvalidOperationException">The input is already at the end of the source.</exception>
        public IInput Advance()
        {
            if (AtEnd) throw new InvalidOperationException("The input is already at the end of the source.");

            return new Input(_source, _position + 1, Current == '\n' ? _line + 1 : _line, Current == '\n' ? 1 : _column + 1);
        }

        /// <summary>
        /// Gets the whole source.
        /// </summary>
        public string Source => _source;

        /// <summary>
        /// Gets the current <see cref="System.Char" />.
        /// </summary>
        public char Current => _source[_position];

        /// <summary>
        /// Gets a value indicating whether the end of the source is reached.
        /// </summary>
        public bool AtEnd => _position == _source.Length;

        /// <summary>
        /// Gets the current positon.
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Gets the current line number.
        /// </summary>
        public int Line => _line;

        /// <summary>
        /// Gets the current column.
        /// </summary>
        public int Column => _column;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString() => $"Line {_line}, Column {_column}";

        /// <summary>Serves as a hash function for a particular type.</summary>
        /// <returns>A hash code for the current <see cref="Input" />.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((_source != null ? _source.GetHashCode() : 0) * 397) ^ _position;
            }
        }



        /// <summary>
        /// Indicates whether the current <see cref="Input" /> is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IInput other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(_source, other.Source) && _position == other.Position;
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

        /// <summary> 获取一个值，该值指示是否已到达源的末尾 </summary>
        bool AtEnd { get; }

        /// <summary> 获取当前位置 </summary>
        int Position { get; }

        /// <summary> 获取当前行号 </summary>
        int Line { get; }

        /// <summary> 获取当前列号 </summary>
        int Column { get; }

        /// <summary> Memos used by this input</summary>
        IDictionary<object, object> Memos { get; }
    }
}