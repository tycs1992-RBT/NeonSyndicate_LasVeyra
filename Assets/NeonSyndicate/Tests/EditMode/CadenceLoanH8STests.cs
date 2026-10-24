using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Ownership order of the animator's AlwaysAnimate flag between the Dante living cast review (CadenceLoanH8S in the
    /// adapter) and the quiet listening loop (which ends its previous loan and takes again when it begins, exactly like
    /// HumanoidAnimator.BeginQuietReviewH8S). Every start order and end order must give the original flag back.
    /// </summary>
    public sealed class CadenceLoanH8STests
    {
        private sealed class Actor
        {
            public bool Flag;
            public CadenceLoanH8S Review, Quiet;
            public void QuietBegin() { Flag = Quiet.Release(Flag); Flag = Quiet.Take(Flag); }
            public void QuietEnd() { Flag = Quiet.Release(Flag); }
            /// <summary>BeginNpcReviewH8S: end any quiet loop, take the flag, then the living cast review starts its own loop.</summary>
            public void ReviewBegin() { QuietEnd(); Flag = Review.Take(Flag); QuietBegin(); }
            /// <summary>ReleaseNpcH8S: the loop that joined last leaves first.</summary>
            public void ReviewEnd() { QuietEnd(); Flag = Review.Release(Flag); }
        }

        [TestCase(false)] [TestCase(true)]
        public void ReviewFirstUnwindsInEitherEndOrder(bool original)
        {
            Actor a = new Actor { Flag = original };
            a.ReviewBegin();
            Assert.IsTrue(a.Flag);
            a.ReviewEnd();
            Assert.AreEqual(original, a.Flag, "review end");
            a.ReviewBegin();
            a.QuietEnd();
            Assert.IsTrue(a.Flag, "A window revert while the visual shows must keep full cadence.");
            a.ReviewEnd();
            Assert.AreEqual(original, a.Flag, "revert, then review end");
        }

        [TestCase(false)] [TestCase(true)]
        public void QuietFirstIsEndedBeforeTheReviewTakesTheFlag(bool original)
        {
            Actor a = new Actor { Flag = original };
            a.QuietBegin();
            a.ReviewBegin();
            Assert.IsTrue(a.Flag);
            a.ReviewEnd();
            Assert.AreEqual(original, a.Flag, "window loop, review, review end");
            a.QuietBegin();
            a.ReviewBegin();
            a.QuietEnd();
            Assert.IsTrue(a.Flag);
            a.ReviewEnd();
            Assert.AreEqual(original, a.Flag, "window loop, review, revert, review end");
        }

        [Test]
        public void TakingUnderAnotherOwnerWithoutUnwindingLeavesTheFlagOn()
        {
            // The order the review now prevents: the review records the quiet owner's "on" as its prior.
            Actor a = new Actor { Flag = false };
            a.QuietBegin();
            a.Flag = a.Review.Take(a.Flag);
            a.QuietBegin();
            a.ReviewEnd();
            Assert.IsTrue(a.Flag, "Without unwinding, the flag is stuck on after both owners left.");
        }

        [Test]
        public void ReleaseWithoutALoanAndASecondTakeChangeNothing()
        {
            CadenceLoanH8S loan = new CadenceLoanH8S();
            Assert.IsFalse(loan.Release(false));
            Assert.IsTrue(loan.Release(true));
            Assert.IsTrue(loan.Take(false));
            Assert.IsTrue(loan.Take(true), "A second Take keeps the first record.");
            Assert.IsFalse(loan.Release(true));
            Assert.IsFalse(loan.Owned);
        }
    }
}
