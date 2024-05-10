namespace LinqyCalculator
{

    using System;
    using System.Reflection;
    using System.Linq.Expressions;
    using System.Linq;
    using Sprache;


    public static class ExpressionParser
    {
        public static Expression<Func<double>> ParseExpression(string text) => Lambda.Parse(text);


        private static Parser<ExpressionType> Operator(string op, ExpressionType opType) => Parse.String(op).Token().Return(opType);

        private static readonly Parser<ExpressionType> ADD = Operator("+", ExpressionType.AddChecked);
        private static readonly Parser<ExpressionType> SUB = Operator("-", ExpressionType.SubtractChecked);
        private static readonly Parser<ExpressionType> MUL = Operator("*", ExpressionType.MultiplyChecked);
        private static readonly Parser<ExpressionType> DIV = Operator("/", ExpressionType.Divide);
        private static readonly Parser<ExpressionType> MOD = Operator("%", ExpressionType.Modulo);
        private static readonly Parser<ExpressionType> POW = Operator("^", ExpressionType.Power);

        private static readonly Parser<Expression> Function = Parse.Letter.AtLeastOnce().Text().SelectMany(name => Parse.MatchChar('('), (name, lparen) => new { name, lparen }).SelectMany(@t => Parse.Ref(() => Expr).DelimitedBy(Parse.MatchChar(',').Token()), (@t, expr) => new { @t, expr }).SelectMany(@t => Parse.MatchChar(')'), (@t, rparen) => CallFunction(@t.@t.name, @t.expr.ToArray()));

        private static Expression CallFunction(string name, Expression[] parameters)
        {
            var methodInfo = typeof(Math).GetTypeInfo().GetMethod(name, parameters.Select(e => e.Type).ToArray());
            if (methodInfo == null) throw new ParseException($"Function '{name}({string.Join(",", parameters.Select(e => e.Type.Name))})' does not exist.");

            return Expression.Call(methodInfo, parameters);
        }

        private static readonly Parser<Expression> Constant = Parse.Decimal.Select(x => Expression.Constant(double.Parse(x))).Named("number");

        private static readonly Parser<Expression> Factor = Parse.MatchChar('(')                                                                      //
                                                                 .SelectMany(_ => Parse.Ref(() => Expr), (lparen, expr) => new { lparen, expr }) //
                                                                 .SelectMany(_ => Parse.MatchChar(')'),       (@t,     _) => @t.expr)                 //
                                                                 .Named("expression")                                                            //
                                                                 .XOr(Constant).XOr(Function);

        private static readonly Parser<Expression> Operand = Parse.MatchChar('-').SelectMany(_ => Factor, (_, factor) => Expression.Negate(factor)).XOr(Factor).Token();

        private static readonly Parser<Expression> InnerTerm = Parse.ChainRightOperator(POW, Operand, Expression.MakeBinary);

        private static readonly Parser<Expression> Term = Parse.ChainOperator(MUL.Or(DIV).Or(MOD), InnerTerm, Expression.MakeBinary);

        private static readonly Parser<Expression> Expr = Parse.ChainOperator(ADD.Or(SUB), Term, Expression.MakeBinary);

        private static readonly Parser<Expression<Func<double>>> Lambda = Expr.End().Select(body => Expression.Lambda<Func<double>>(body));
    }

}