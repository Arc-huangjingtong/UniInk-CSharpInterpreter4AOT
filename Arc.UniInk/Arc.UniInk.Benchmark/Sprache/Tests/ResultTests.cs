using System.Collections.Generic;


namespace Sprache.Tests
{

    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class ResultTests
    {
        public void FailureContainingBracketFormattedSuccessfully()
        {
            var p = Parse.String("xy").Text().XMany().End();
            var r = (Result<IEnumerable<string>>)p.TryParse("x{");
            //Assert.Contains("unexpected '{'", r.Message);
        }

        [Test]
        public void FailureShowsNearbyParseResults()
        {
            var p = from a in Parse.Char('x') from b in Parse.Char('y') select $"{a},{b}";

            var r = (Result<string>)p.TryParse("x{");

            const string expectedMessage = @"Parsing failure: unexpected '{'; expected y (Line 1, Column 2); recently consumed: x";

            ClassicAssert.AreEqual(expectedMessage, r.ToString());
        }
    }

}