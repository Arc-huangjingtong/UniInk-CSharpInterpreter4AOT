namespace Sprache
{

    using System;


    /// <summary>表示输入中的位置</summary>
    public class Position : IEquatable<Position>
    {
        public static Position FromInput(IInput input) => new(input.Position, input.Line, input.Column);

        public Position(int pos, int line, int column) => (Pos, Line, Column) = (pos, line, column);



        /// <summary> 当前位置 </summary>
        public int Pos { get; }

        /// <summary> 当前行 </summary>
        public int Line { get; }

        /// <summary> 当前列 </summary>
        public int Column { get; }



        public bool Equals(Position other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Pos == other.Pos && Line == other.Line && Column == other.Column;
        }

        public override int GetHashCode()
        {
            var h = 31;
            h = h * 13 + Pos;
            h = h * 13 + Line;
            h = h * 13 + Column;
            return h;
        }

        public override string ToString() => $"Line {Line}, Column {Column}";

        public override bool Equals(object obj) => Equals(obj as Position);

        public static bool operator ==(Position left, Position right) => Equals(left, right);
        public static bool operator !=(Position left, Position right) => !Equals(left, right);
    }

}