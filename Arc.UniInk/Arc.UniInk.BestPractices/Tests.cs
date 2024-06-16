using System;
using NUnit.Framework;


namespace Arc.UniInk.BestPractices
{

    [TestFixture]
    public class Tests
    {
        public readonly DuelAction Action = new DuelAction();

        [OneTimeSetUp]
        public void Setup()
        {
            Action.Initialization(null);
        }



        [Test]
        public void Test1()
        {
            var test = DuelAction.Evaluator.Evaluate("grower");

            var test2 = DuelAction.Evaluator.Evaluate("grower");
            if (test is InkValue value)
            {
                Console.WriteLine(value.Value_int);
            }

            if (test2 is InkValue value2)
            {
                Console.WriteLine(value2.Value_int);
            }
        }
    }

}