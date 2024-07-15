namespace Sprache.Tests
{

    using System;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;


    public class InputTests
    {
        [Test]
        public void InputsOnTheSameString_AtTheSamePosition_AreEqual()
        {
            var s  = "Nada";
            var p  = 2;
            var i1 = new Input(s, p);
            var i2 = new Input(s, p);
            ClassicAssert.AreEqual(i1, i2);
            ClassicAssert.True(i1 == i2);
        }

        [Test]
        public void InputsOnTheSameString_AtDifferentPositions_AreNotEqual()
        {
            var s  = "Nada";
            var i1 = new Input(s, 1);
            var i2 = new Input(s, 2);
            ClassicAssert.AreNotEqual(i1, i2);
            ClassicAssert.True(i1 != i2);
        }

        [Test]
        public void InputsOnDifferentStrings_AtTheSamePosition_AreNotEqual()
        {
            var p  = 2;
            var i1 = new Input("Algo", p);
            var i2 = new Input("Nada", p);
            ClassicAssert.AreNotEqual(i1, i2);
        }

        [Test]
        public void InputsAtEnd_CannotAdvance()
        {
            var i = new Input("", 0);
            ClassicAssert.True(i.AtEnd);
            Assert.Throws<InvalidOperationException>(() => i.Advance());
        }

        [Test]
        public void AdvancingInput_MovesForwardOneCharacter()
        {
            var i = new Input("abc", 1);
            var j = i.Advance();
            ClassicAssert.AreEqual(2, j.Position);
        }

        [Test]
        public void CurrentCharacter_ReflectsPosition()
        {
            var i = new Input("abc", 1);
            ClassicAssert.AreEqual('b', i.Current);
        }

        [Test]
        public void ANewInput_WillBeAtFirstCharacter()
        {
            var i = new Input("abc");
            ClassicAssert.AreEqual(0, i.Position);
        }

        [Test]
        public void AdvancingInput_IncreasesColumnNumber()
        {
            var i = new Input("abc", 1);
            var j = i.Advance();
            ClassicAssert.AreEqual(2, j.Column);
        }

        [Test]
        public void AdvancingInputAtEOL_IncreasesLineNumber()
        {
            var i = new Input("\nabc");
            var j = i.Advance();
            ClassicAssert.AreEqual(2, j.Line);
        }

        [Test]
        public void AdvancingInputAtEOL_ResetsColumnNumber()
        {
            var i = new Input("\nabc");
            var j = i.Advance();
            ClassicAssert.AreEqual(2, j.Line);
            ClassicAssert.AreEqual(1, j.Column);
        }

        [Test]
        public void LineCountingSmokeTest()
        {
            IInput i = new Input("abc\ndef");
            ClassicAssert.AreEqual(0, i.Position);
            ClassicAssert.AreEqual(1, i.Line);
            ClassicAssert.AreEqual(1, i.Column);

            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(1, b.Position);
                ClassicAssert.AreEqual(1, b.Line);
                ClassicAssert.AreEqual(2, b.Column);
            });
            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(2, b.Position);
                ClassicAssert.AreEqual(1, b.Line);
                ClassicAssert.AreEqual(3, b.Column);
            });
            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(3, b.Position);
                ClassicAssert.AreEqual(1, b.Line);
                ClassicAssert.AreEqual(4, b.Column);
            });
            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(4, b.Position);
                ClassicAssert.AreEqual(2, b.Line);
                ClassicAssert.AreEqual(1, b.Column);
            });
            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(5, b.Position);
                ClassicAssert.AreEqual(2, b.Line);
                ClassicAssert.AreEqual(2, b.Column);
            });
            i = i.AdvanceAssert((_, b) =>
            {
                ClassicAssert.AreEqual(6, b.Position);
                ClassicAssert.AreEqual(2, b.Line);
                ClassicAssert.AreEqual(3, b.Column);
            });
        }
    }

}