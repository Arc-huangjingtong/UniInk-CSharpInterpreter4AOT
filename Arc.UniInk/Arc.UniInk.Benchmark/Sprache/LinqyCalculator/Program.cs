namespace LinqyCalculator
{

    using System;
    using Sprache;
    using NUnit.Framework;


    [TestFixture]
    public class Program
    {
        [TestCase("9*((1+2*3)/2)")]
        public static void Main2(string line)
        {
            try
            {
                var parsed = ExpressionParser.ParseExpression(line);
                Console.WriteLine($"Parsed as {parsed}", parsed);
                Console.WriteLine($"Value is {parsed.Compile().Invoke()}");
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"There was a problem with your input: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

}