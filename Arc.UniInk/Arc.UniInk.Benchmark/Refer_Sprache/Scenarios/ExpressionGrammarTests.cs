using System;
using System.Linq.Expressions;


namespace Sprache.Tests.Scenarios
{

    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class ExpressionGrammarTests
    {
        [Test]
        public void DroppedClosingParenthesisProducesMeaningfulError()
        {
            const string input = "1 + (2 * 3";
            var          x     = Assert.Throws<ParseException>(() => ExpressionParser.ParseExpression(input));
            //ClassicAssert.Contains("expected )", x.Message.ToCharArray());
            ClassicAssert.AreEqual(1,  x.Position.Line);
            ClassicAssert.AreEqual(11, x.Position.Column);
        }

        [Test]
        public void MissingOperandProducesMeaningfulError()
        {
            const string input = "1 + * 3";
            var          x     = Assert.Throws<ParseException>(() => ExpressionParser.ParseExpression(input));
            //ClassicAssert.DoesNotContain("expected end of input", x.Message);
            ClassicAssert.AreEqual(1, x.Position.Line);
            ClassicAssert.AreEqual(5, x.Position.Column);
        }
    }


    internal static class ExpressionParser
    {
        public static Expression<Func<double>> ParseExpression(string text)
        {
            return Lambda.Parse(text);
        }

        private static Parser<ExpressionType> Operator(string op, ExpressionType opType)
        {
            return Parse.MatchString(op).Token().Return(opType);
        }

        private static readonly Parser<ExpressionType> Add      = Operator("+", ExpressionType.AddChecked);
        private static readonly Parser<ExpressionType> Subtract = Operator("-", ExpressionType.SubtractChecked);
        private static readonly Parser<ExpressionType> Multiply = Operator("*", ExpressionType.MultiplyChecked);
        private static readonly Parser<ExpressionType> Divide   = Operator("/", ExpressionType.Divide);

        private static readonly Parser<Expression> Constant = Parse.Decimal.Select(x => Expression.Constant(double.Parse(x))).Named("number");

        private static readonly Parser<Expression> Factor = (from lparen in Parse.MatchChar('(') from expr in Parse.Ref(() => Expr) from rparen in Parse.MatchChar(')') select expr).Named("expression").XOr(Constant);

        private static readonly Parser<Expression> Operand = ((from sign in Parse.MatchChar('-') from factor in Factor select Expression.Negate(factor)).XOr(Factor)).Token();

        private static readonly Parser<Expression> Term = Parse.XChainOperator(Multiply.XOr(Divide), Operand, Expression.MakeBinary);

        private static readonly Parser<Expression> Expr = Parse.XChainOperator(Add.XOr(Subtract), Term, Expression.MakeBinary);

        private static readonly Parser<Expression<Func<double>>> Lambda = Expr.End().Select(body => Expression.Lambda<Func<double>>(body));
    }

}