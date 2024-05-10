namespace Sprache.Tests.Scenarios
{

    using System;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class StarDateTest
    {
        private static readonly Parser<DateTime> StarTrek2009_StarDate = from year in Parse.Digit.Many().Text() from delimiter in Parse.Char('.') from dayOfYear in Parse.Digit.Repeat(1, 3).Text().End() select new DateTime(int.Parse(year), 1, 1).AddDays(int.Parse(dayOfYear) - 1);

        [Test]
        public void ItIsPossibleToParseAStarDate()
        {
            ClassicAssert.Equals(new DateTime(2259, 2, 24), StarTrek2009_StarDate.Parse("2259.55"));
        }

        [Test]
        public void InvalidStarDatesAreNotParsed()
        {
            Assert.Throws<ParseException>(() =>
            {
                var date = StarTrek2009_StarDate.Parse("2259.4000");
            });
        }
    }

}