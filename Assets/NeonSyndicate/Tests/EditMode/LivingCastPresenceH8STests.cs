using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>The living cast presence policy: hysteresis, debounce, immediate fallbacks and fail-closed input. Engine-free.</summary>
    public sealed class LivingCastPresenceH8STests
    {
        private static readonly Vector3 Post = new Vector3(10f, 2f, -4f);

        /// <summary>One step with the actor at the post offset and the player (and camera) at a distance from the actor.</summary>
        private static bool Step(LivingCastPresenceH8S p, float postOffset, float viewer, float dt = 0.1f, float rise = 0f,
            bool live = true, bool owner = true, bool bound = true)
        {
            Vector3 actor = Post + new Vector3(postOffset, rise, 0f);
            Vector3 eye = actor + new Vector3(0f, 0f, viewer);
            return p.Step(live, owner, bound, Post, actor, true, eye, true, eye, dt);
        }

        private static bool Run(LivingCastPresenceH8S p, float seconds, float postOffset, float viewer)
        {
            bool shown = p.Showing;
            for (float t = 0f; t < seconds - 1e-4f; t += 0.1f) shown = Step(p, postOffset, viewer);
            return shown;
        }

        [Test]
        public void ConstantsStayInsideTheCastOwnersReturnLimits()
        {
            Assert.Less(LivingCastPresenceH8S.PostEnter, LivingCastPresenceH8S.PostExit);
            Assert.Less(LivingCastPresenceH8S.RiseEnter, LivingCastPresenceH8S.RiseExit);
            // Read the cast owner's own return limits, so a change there cannot silently overtake the presence exits.
            string cast = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Guidance04E/CampaignDirector.Cast.cs"));
            Match limits = Regex.Match(cast, @"MathUtil\.FlatDistance\(p, s\.Post\) > ([0-9.]+)f \|\| Mathf\.Abs\(p\.y - s\.Post\.y\) > ([0-9.]+)f");
            Assert.IsTrue(limits.Success, "CampaignDirector.Maintain changed its return test; re-check the presence exits.");
            Assert.Less(LivingCastPresenceH8S.PostExit, float.Parse(limits.Groups[1].Value, CultureInfo.InvariantCulture), "Hand back before the cast owner's flat return limit.");
            Assert.Less(LivingCastPresenceH8S.RiseExit, float.Parse(limits.Groups[2].Value, CultureInfo.InvariantCulture), "Hand back before the cast owner's height return limit.");
            Assert.Less(LivingCastPresenceH8S.ViewEnter, LivingCastPresenceH8S.ViewExit);
            Assert.Greater(LivingCastPresenceH8S.MinHold, LivingCastPresenceH8S.MaxStep);
        }

        [Test]
        public void ShowsOnlyAfterTheEnterConditionsHoldForTheDebounce()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(false);
            Assert.IsFalse(Run(p, 0.4f, 0.5f, 5f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.Holding, p.Reason);
            Assert.IsTrue(Run(p, 0.2f, 0.5f, 5f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.Shown, p.Reason);
        }

        [Test]
        public void TheRangeBandNeverFlickers()
        {
            LivingCastPresenceH8S shown = new LivingCastPresenceH8S(), hidden = new LivingCastPresenceH8S();
            shown.Reset(true);
            hidden.Reset(false);
            for (int i = 0; i < 200; i++)
            {
                float viewer = i % 2 == 0 ? 25f : 29.5f;
                Assert.IsTrue(Step(shown, 0.5f, viewer), "shown, step " + i);
                Assert.IsFalse(Step(hidden, 0.5f, viewer), "hidden, step " + i);
            }
        }

        [Test]
        public void LeavingRangeNeedsTheDebounceAndOneFrameCutsDoNotFlip()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(true);
            Assert.IsTrue(Step(p, 0.5f, 60f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.Holding, p.Reason);
            Assert.IsTrue(Step(p, 0.5f, 3f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.Shown, p.Reason);
            Assert.IsTrue(Run(p, 0.4f, 0.5f, 31f));
            Assert.IsFalse(Run(p, 0.2f, 0.5f, 31f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.OutOfRange, p.Reason);
        }

        [Test]
        public void LeavingThePostFallsBackAtOnceAndReturnNeedsTheInnerRadius()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(true);
            Assert.IsTrue(Step(p, 2.5f, 4f));
            Assert.IsFalse(Step(p, 2.7f, 4f));
            Assert.AreEqual(LivingCastPresenceReasonH8S.AwayFromPost, p.Reason);
            Assert.IsFalse(Run(p, 2f, 2f, 4f), "Between the radii the fallback stays.");
            Assert.IsFalse(Run(p, 0.4f, 1.5f, 4f));
            Assert.IsTrue(Run(p, 0.2f, 1.5f, 4f));
        }

        [Test]
        public void VerticalDistanceFromThePostCountsToo()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(true);
            Assert.IsTrue(Step(p, 0f, 4f, 0.1f, 1.1f));
            Assert.IsFalse(Step(p, 0f, 4f, 0.1f, 1.3f));
            for (int i = 0; i < 10; i++) Assert.IsFalse(Step(p, 0f, 4f, 0.1f, 1f));
            for (int i = 0; i < 10; i++) Step(p, 0f, 4f, 0.1f, 0.5f);
            Assert.IsTrue(p.Showing);
        }

        [Test]
        public void LostActorUnboundPostOrHiddenBodyFallBackImmediately()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(true);
            Assert.IsFalse(Step(p, 0f, 4f, 0.1f, 0f, false));
            Assert.AreEqual(LivingCastPresenceReasonH8S.NoActor, p.Reason);
            p.Reset(true);
            Assert.IsFalse(Step(p, 0f, 4f, 0.1f, 0f, true, false));
            Assert.AreEqual(LivingCastPresenceReasonH8S.HiddenByOwner, p.Reason);
            p.Reset(true);
            Assert.IsFalse(Step(p, 0f, 4f, 0.1f, 0f, true, true, false));
            Assert.AreEqual(LivingCastPresenceReasonH8S.PostUnbound, p.Reason);
        }

        [Test]
        public void TheNearerViewerCountsAndNoViewerNeverShows()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(false);
            Vector3 actor = Post;
            for (int i = 0; i < 10; i++) p.Step(true, true, true, Post, actor, true, actor + Vector3.forward * 80f, true, actor + Vector3.right * 3f, 0.1f);
            Assert.IsTrue(p.Showing, "Player near, camera far.");
            p.Reset(false);
            for (int i = 0; i < 10; i++) p.Step(true, true, true, Post, actor, true, actor + Vector3.forward * 3f, false, Vector3.zero, 0.1f);
            Assert.IsTrue(p.Showing, "Camera near, no player.");
            p.Reset(false);
            for (int i = 0; i < 10; i++) p.Step(true, true, true, Post, actor, false, Vector3.zero, false, Vector3.zero, 0.1f);
            Assert.IsFalse(p.Showing);
        }

        [Test]
        public void NonFiniteInputFailsClosedAndALongHitchCannotSatisfyTheDebounce()
        {
            LivingCastPresenceH8S p = new LivingCastPresenceH8S();
            p.Reset(true);
            Vector3 bad = new Vector3(float.NaN, 0f, 0f);
            Assert.IsFalse(p.Step(true, true, true, Post, bad, true, Post, true, Post, 0.1f));
            p.Reset(false);
            Assert.IsFalse(Step(p, 0f, 4f, float.NaN));
            Assert.IsFalse(Step(p, 0f, 4f, 30f), "One 30 s hitch counts as one capped step.");
            Assert.IsFalse(Step(p, 0f, 4f, -1f));
            Assert.IsTrue(Step(p, 0f, 4f, 0.25f));
        }

        [Test]
        public void TheStartCheckUsesTheEnterLimits()
        {
            Vector3 actor = Post + new Vector3(1f, 0f, 0f);
            Assert.IsTrue(LivingCastPresenceH8S.CanEnter(true, Post, actor, true, actor + Vector3.forward * 5f, true, actor + Vector3.forward * 5f));
            Assert.IsFalse(LivingCastPresenceH8S.CanEnter(false, Post, actor, true, actor, true, actor));
            Assert.IsFalse(LivingCastPresenceH8S.CanEnter(true, Post, Post + new Vector3(2f, 0f, 0f), true, actor, true, actor));
            Assert.IsFalse(LivingCastPresenceH8S.CanEnter(true, Post, actor, true, actor + Vector3.forward * 26f, true, actor + Vector3.forward * 26f));
            Assert.IsFalse(LivingCastPresenceH8S.CanEnter(true, Post, actor, false, Vector3.zero, false, Vector3.zero));
        }
    }
}
