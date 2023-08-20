using System;
using System.Text.RegularExpressions;
using NUnit.Framework;


namespace Arc.UniInk.Test
{
    [TestFixture]
    public class Tests
    {
        
        protected static readonly Regex regex1 =new(@"(?<name>[^,<>]+)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])|(?<-gentag>[>]))*(?(gentag)(?!))[>])?", RegexOptions.Compiled);
        protected static readonly Regex regex2 =new(@"(?<name>[^,<>]+)(?<isgeneric>[<](?>[^<>]+|(?<gentag>[<])(?!\k<gentag>)[>])*)?", RegexOptions.Compiled);
        [Test]
        public void Test1()
        {
            
            Assert.True(true);
        }
    }
}