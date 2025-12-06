namespace ParsecSharp.Examples
{
    using System;
    using static ParsecSharp.Parser;
    using static ParsecSharp.Text;

    public interface INumber<TNumber> where TNumber : INumber<TNumber>
    {
        TNumber Add(TNumber value);

        TNumber Sub(TNumber value);

        TNumber Mul(TNumber value);

        TNumber Div(TNumber value);
    }


    public class ExpressionParser<TNumber> where TNumber : INumber<TNumber>
    {
        public Parser<char, TNumber> Parser { get; }

        public ExpressionParser(Parser<char, TNumber> number)
        {
            var addsub = Op('+', (x, y) => x.Add(y)) | Op('-', (x, y) => x.Sub(y));
            var muldiv = Op('*', (x, y) => x.Mul(y)) | Op('/', (x, y) => x.Div(y));

            var open = Char('(').Between(Spaces());
            var close = Char(')').Between(Spaces());

            var expr = Fix<char, TNumber>(expr =>
            {
                var factor = number | expr.Between(open, close);
                var term = factor.ChainLeft(muldiv);
                return term.ChainLeft(addsub);
            });

            this.Parser = expr.Between(Spaces()).End();
        }

        private static Parser<char, Func<TNumber, TNumber, TNumber>> Op(char symbol,
            Func<TNumber, TNumber, TNumber> function) => Char(symbol).Between(Spaces()).Map(_ => function);

        public Result<char, TNumber> Parse(string source) => this.Parser.Parse(source);
    }


    public class Integer : INumber<Integer>
    {
        private static readonly Parser<char, Integer> number =
            Many1(DecDigit()).ToInt().Map(x => new Integer(x));

        public static ExpressionParser<Integer> Parser { get; } = new(number);

        public int Value { get; }

        private Integer(int value)
        {
            this.Value = value;
        }

        public Integer Add(Integer value) => new(this.Value + value.Value);

        public Integer Sub(Integer value) => new(this.Value - value.Value);

        public Integer Mul(Integer value) => new(this.Value * value.Value);

        public Integer Div(Integer value) => new(this.Value / value.Value);
    }
}