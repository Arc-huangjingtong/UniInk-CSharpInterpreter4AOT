namespace Arc.UniInk.BestPractices
{

    using System;
    using NUnit.Framework;


    [TestFixture]
    public class Tests
    {
        public readonly ScriptAction Action = new ScriptAction();

        [OneTimeSetUp]
        public void Setup() { }

        [Test]
        public void Test_Getter()
        {
            var test = ScriptAction.Evaluator.Evaluate("grower");
            if (test is InkValue value)
            {
                Console.WriteLine(value.Value_int);
            }

            var test2 = ScriptAction.Evaluator.Evaluate("grower");
            if (test2 is InkValue value2)
            {
                Console.WriteLine(value2.Value_int);
            }
        }

        [Test]
        public void Test_01()
        {
            var script = "grower";
        }
    }

}