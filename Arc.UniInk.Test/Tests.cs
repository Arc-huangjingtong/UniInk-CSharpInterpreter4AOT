namespace Arc.UniInk.Test
{
    using Arc.UniInk;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        [SetUp] //[Ignore("SetUpTest")]
        public void SetUpTest()
        {
            TestContext.Progress.WriteLine("simple math operation");
        }


        [Test]
        [TestCase("2+4")]
        [TestCase("4-2")]
        [TestCase("4*2")]
        [TestCase("4/2")]
        public void Test(string script)
        {
            var test = new TestClass();
            var Ink = new UniInk(test);
            var ans = Ink.Evaluate($"{script}");

            Assert.True(true);

            Assert.NotNull(ans);
            TestContext.Progress.WriteLine($"✅:{script}={ans}");
        }
    }
    //✅❌

    #region HelperClass

    public class TestClass
    {
        public int Id = 233;

        public TestClass() { }
    }

    #endregion
}