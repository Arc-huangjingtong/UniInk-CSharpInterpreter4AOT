namespace Sprache.Tests
{

    using System;
    using System.Globalization;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class DecimalTests : IDisposable
    {
        private static readonly Parser<string> DecimalParser          = Parse.Decimal.End();
        private static readonly Parser<string> DecimalInvariantParser = Parse.DecimalInvariant.End();

        private CultureInfo _previousCulture;

        public DecimalTests()
        {
            _previousCulture           = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
        }

        [Test]
        public void LeadingDigits()
        {
            ClassicAssert.AreEqual("12.23", DecimalParser.Parse("12.23"));
        }

        [Test]
        public void NoLeadingDigits()
        {
            ClassicAssert.AreEqual(".23", DecimalParser.Parse(".23"));
        }

        [Test]
        public void TwoPeriods()
        {
            Assert.Throws<ParseException>(() => DecimalParser.Parse("1.2.23"));
        }

        [Test]
        public void Letters()
        {
            Assert.Throws<ParseException>(() => DecimalParser.Parse("1A.5"));
        }

        [Test]
        public void LeadingDigitsInvariant()
        {
            ClassicAssert.AreEqual("12.23", DecimalInvariantParser.Parse("12.23"));
        }
    }

}