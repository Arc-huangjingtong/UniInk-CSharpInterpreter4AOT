namespace Sprache.Tests.Scenarios
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public static class AssemblerParser
    {
        public static Parser<string> Identifier = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit);

        public static Parser<T> AsmToken <T>(Parser<T> parser)
        {
            var whitespace = Parse.WhiteSpace.Except(Parse.LineEnd);
            return from leading in whitespace.Many() from item in parser from trailing in whitespace.Many() select item;
        }

        public static Parser<Tuple<string, string[]>> Instruction = from instructionName in AsmToken(Parse.LetterOrDigit.Many().Text()) from operands in AsmToken(Identifier).XDelimitedBy(Parse.MatchChar(',')) select Tuple.Create(instructionName, operands.ToArray());

        public static CommentParser Comment = new CommentParser { Single = ";", NewLine = Environment.NewLine };

        public static Parser<string> LabelId = Parse.Identifier(Parse.Letter.Or(Parse.MatchChars("._?")), Parse.LetterOrDigit.Or(Parse.MatchChars("_@#$~.?")));

        public static Parser<string> Label = from labelName in AsmToken(LabelId) from colon in AsmToken(Parse.MatchChar(':')) select labelName;

        public static readonly Parser<IEnumerable<AssemblerLine>> Assembler = (from label in Label.Optional() from instruction in Instruction.Optional() from comment in AsmToken(Comment.SingleLineComment).Optional() from lineTerminator in Parse.LineTerminator select new AssemblerLine(label.GetOrDefault(), instruction.IsEmpty ? null : instruction.Get().Item1, instruction.IsEmpty ? null : instruction.Get().Item2, comment.GetOrDefault())).XMany().End();
    }


    public class AssemblerTests
    {
        [Test]
        public void CanParseEmpty()
        {
            //ClassicAssert.AreEqual
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "", ClassicAssert.IsEmpty);
        }

        [Test]
        public void CanParseComment()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, ";comment", lines => ClassicAssert.AreEqual(new AssemblerLine(null, null, null, "comment"), lines.Single()));
        }

        [Test]
        public void CanParseCommentWithSpaces()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "  ; comment ", lines => ClassicAssert.AreEqual(new AssemblerLine(null, null, null, " comment "), lines.Single()));
        }

        [Test]
        public void CanParseLabel()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "label:", lines => ClassicAssert.AreEqual(new AssemblerLine("label", null, null, null), lines.Single()));
        }

        [Test]
        public void CanParseLabelWithSpaces()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "  label :  ", lines => ClassicAssert.AreEqual(new AssemblerLine("label", null, null, null), lines.Single()));
        }

        [Test]
        public void CanParseIntruction()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "mov a,b", lines => ClassicAssert.AreEqual(new AssemblerLine(null, "mov", new[] { "a", "b" }, null), lines.Single()));
        }

        [Test]
        public void CanParseIntructionWithSpaces()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, " mov   a , b ", lines => ClassicAssert.AreEqual(new AssemblerLine(null, "mov", new[] { "a", "b" }, null), lines.Single()));
        }

        [Test]
        public void CanParseAllTogether()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, "  label:  mov   a  ,  b  ;  comment  ", lines => ClassicAssert.AreEqual(new AssemblerLine("label", "mov", new[] { "a", "b" }, "  comment  "), lines.Single()));
        }

        [Test]
        public void CanParseSeceralLines()
        {
            AssertParser.SucceedsWith(AssemblerParser.Assembler, @"      ;multiline sample
                        label:  
                        mov   a  ,  b;", lines => ClassicAssert.AreEqual(new[] { new AssemblerLine(null, null, null, "multiline sample"), new AssemblerLine("label", null, null, null), new AssemblerLine(null, "mov", new[] { "a", "b" }, "") }, lines));
        }
    }


    public class AssemblerLine
    {
        public readonly string   Label;
        public readonly string   InstructionName;
        public readonly string[] Operands;
        public readonly string   Comment;

        public AssemblerLine(string label, string instructionName, string[] operands, string comment)
        {
            Label           = label;
            InstructionName = instructionName;
            Operands        = operands;
            Comment         = comment;
        }

        protected bool Equals(AssemblerLine other)
        {
            return ToString() == other.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((AssemblerLine)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Label != null ? Label.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InstructionName != null ? InstructionName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Operands        != null ? Operands.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Comment         != null ? Comment.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Join(" ", Label == null ? "" : (Label + ":"), InstructionName == null ? "" : InstructionName + string.Join(", ", Operands), Comment == null ? "" : ";" + Comment);
        }
    }

}