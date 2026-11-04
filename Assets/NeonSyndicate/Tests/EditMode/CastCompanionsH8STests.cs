using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S cast and companions: the companion bark rate limit and lines, follow spacing (camera line, the player's lane,
    /// stepping aside, the sheltered slot, tight hover, regroup), the target preference and the double tap call, the
    /// companion card rules, the guidance words (distance, direction, place labels), the Hearth Witness phase hints
    /// against the shipped quest, the cast idle beats and conversation engagement, the venue hand-in rule and the
    /// accessibility maths. All engine-free.
    /// </summary>
    public sealed class CastCompanionsH8STests
    {
        private const float Eps = 1e-3f;
        private static readonly char EnDash = (char)0x2013;
        private static readonly char EmDash = (char)0x2014;

        private static void NoDashes(string text, string what)
        {
            Assert.That(text.IndexOf(EnDash) < 0 && text.IndexOf(EmDash) < 0, Is.True, what + " holds a dash character: " + text);
        }

        // ================================================================== barks

        [Test]
        public void BarkGateHonoursCooldownsGapAndPriority()
        {
            CompanionBarkGateH8S g = new CompanionBarkGateH8S();
            int v;
            Assert.That(g.TryFire(CompanionBarkH8S.AbilityUsed, 0f, out v), Is.True);
            Assert.That(v, Is.EqualTo(0));
            Assert.That(g.TryFire(CompanionBarkH8S.Found, 1f, out v), Is.False, "equal urgency waits out the global gap");
            Assert.That(g.TryFire(CompanionBarkH8S.LowHealth, 1f, out v), Is.True, "a more urgent bark speaks inside the gap");
            Assert.That(g.TryFire(CompanionBarkH8S.LowHealth, 10f, out v), Is.False, "low health has its own long cooldown");
            Assert.That(g.TryFire(CompanionBarkH8S.AbilityUsed, 6f, out v), Is.False, "ability bark still cooling down");
            Assert.That(g.TryFire(CompanionBarkH8S.AbilityUsed, 13f, out v), Is.True);
            Assert.That(v, Is.EqualTo(1), "variants rotate");
            Assert.That(g.TryFire(CompanionBarkH8S.Downed, 13.5f, out v), Is.True, "downed outranks everything");
            Assert.That(g.TryFire(CompanionBarkH8S.None, 100f, out v), Is.False);
            Assert.That(g.TryFire((CompanionBarkH8S)99, 100f, out v), Is.False);
            g.Reset();
            Assert.That(g.TryFire(CompanionBarkH8S.LowHealth, 0f, out v), Is.True, "reset forgets cooldowns");
            Assert.That(v, Is.EqualTo(0), "reset restarts the rotation");
        }

        [Test]
        public void BarkVariantsCycleThroughAllLines()
        {
            CompanionBarkGateH8S g = new CompanionBarkGateH8S();
            HashSet<int> seen = new HashSet<int>();
            float t = 0f;
            for (int i = 0; i < CompanionBarkGateH8S.VariantCount * 2; i++)
            {
                int v;
                Assert.That(g.TryFire(CompanionBarkH8S.Regrouping, t, out v), Is.True);
                seen.Add(v);
                t += CompanionBarkGateH8S.Cooldown(CompanionBarkH8S.Regrouping) + 0.01f;
            }
            Assert.That(seen.Count, Is.EqualTo(CompanionBarkGateH8S.VariantCount));
        }

        [Test]
        public void EveryArchetypeHasShortDistinctBarkLines()
        {
            string[] kinds = { BondIds.QuartzGuide, BondIds.AshWarden, BondIds.StormSkimmer };
            foreach (string a in kinds)
            {
                HashSet<string> lines = new HashSet<string>();
                for (int k = 1; k <= (int)CompanionBarkH8S.Found; k++)
                    for (int v = 0; v < CompanionBarkGateH8S.VariantCount; v++)
                    {
                        string line = CompanionBarkLinesH8S.Line(a, (CompanionBarkH8S)k, v);
                        Assert.That(line.Length, Is.InRange(8, 44), a + " " + (CompanionBarkH8S)k);
                        Assert.That(line.StartsWith("[") && line.EndsWith("]"), Is.True, "barks are subtitles of calls: " + line);
                        NoDashes(line, "bark");
                        Assert.That(lines.Add(line), Is.True, "repeated line " + line);
                    }
            }
            Assert.That(CompanionBarkLinesH8S.Line(BondIds.QuartzGuide, CompanionBarkH8S.None, 0), Is.EqualTo(""));
            Assert.That(CompanionBarkLinesH8S.Line(BondIds.AshWarden, CompanionBarkH8S.Downed, -1), Is.Not.Empty, "negative variants wrap");
            Assert.That(CompanionBarkLinesH8S.Line(BondIds.AshWarden, CompanionBarkH8S.Downed, 0), Is.Not.EqualTo(CompanionBarkLinesH8S.Line(BondIds.StormSkimmer, CompanionBarkH8S.Downed, 0)));
        }

        // ================================================================== spacing

        [Test]
        public void CameraLineOcclusionOnlyBetweenCameraAndPlayer()
        {
            Vector3 cam = new Vector3(0f, 2.2f, -4f), chest = new Vector3(0f, 1.3f, 0f);
            Assert.That(CompanionSpacingRulesH8S.OccludesView(cam, chest, new Vector3(0f, 1.7f, -2f), 0.4f), Is.True, "a hovering body behind the player sits in the view");
            Assert.That(CompanionSpacingRulesH8S.OccludesView(cam, chest, new Vector3(0f, 0.3f, -1.5f), 0.25f), Is.False, "a small walker stays under the line");
            Assert.That(CompanionSpacingRulesH8S.OccludesView(cam, chest, new Vector3(1.8f, 1.7f, -2f), 0.4f), Is.False, "beside the line is clear");
            Assert.That(CompanionSpacingRulesH8S.OccludesView(cam, chest, new Vector3(0f, 2.3f, -5f), 0.4f), Is.False, "behind the camera never blocks");
            Assert.That(CompanionSpacingRulesH8S.OccludesView(cam, chest, new Vector3(0f, 1.2f, 1f), 0.4f), Is.False, "beyond the player never blocks");
            float t;
            Assert.That(CompanionSpacingRulesH8S.SegmentDistance(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(3f, 0f, 5f), out t), Is.EqualTo(3f).Within(Eps));
            Assert.That(t, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(CompanionSpacingRulesH8S.SegmentDistance(Vector3.zero, Vector3.zero, new Vector3(0f, 4f, 0f), out t), Is.EqualTo(4f).Within(Eps), "a zero segment is a point");
        }

        [Test]
        public void PlayerLaneAndStepAside()
        {
            Vector3 p = Vector3.zero, v = new Vector3(0f, 0f, 2f);
            Assert.That(CompanionSpacingRulesH8S.InPlayerLane(p, v, new Vector3(0.2f, 0f, 2f), 0.8f, 3.5f), Is.True);
            Assert.That(CompanionSpacingRulesH8S.InPlayerLane(p, v, new Vector3(1.4f, 0f, 2f), 0.8f, 3.5f), Is.False, "beside the lane");
            Assert.That(CompanionSpacingRulesH8S.InPlayerLane(p, v, new Vector3(0f, 0f, -1.5f), 0.8f, 3.5f), Is.False, "behind the player");
            Assert.That(CompanionSpacingRulesH8S.InPlayerLane(p, v, new Vector3(0f, 0f, 5f), 0.8f, 3.5f), Is.False, "too far ahead to matter");
            Assert.That(CompanionSpacingRulesH8S.InPlayerLane(p, new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 1f), 0.8f, 3.5f), Is.False, "a standing player needs no room");
            Vector3 right = CompanionSpacingRulesH8S.SidestepPoint(p, v, new Vector3(0.2f, 0f, 1.5f), -1, 0.8f, false);
            Assert.That(right.x, Is.EqualTo(0.8f + CompanionSpacingRulesH8S.SidestepMargin).Within(Eps), "steps to the side it leans to");
            Assert.That(right.z, Is.EqualTo(1.5f).Within(Eps));
            Vector3 left = CompanionSpacingRulesH8S.SidestepPoint(p, v, new Vector3(0.2f, 0f, 1.5f), -1, 0.8f, true);
            Assert.That(left.x, Is.LessThan(0f), "the other side when asked");
            Vector3 tie = CompanionSpacingRulesH8S.SidestepPoint(p, v, new Vector3(0f, 0f, 1.5f), -1, 0.8f, false);
            Assert.That(tie.x, Is.LessThan(0f), "a tie uses the preferred side");
            Vector3 clamped = CompanionSpacingRulesH8S.SidestepPoint(p, v, new Vector3(0.1f, 0f, 0.05f), 1, 0.8f, false);
            Assert.That(clamped.z, Is.EqualTo(0.4f).Within(Eps), "never level with the player's body");
            Vector3 lead = CompanionSpacingRulesH8S.LeadPoint(p, v, CompanionSpacingRulesH8S.LeadDistance);
            Assert.That(lead.z, Is.EqualTo(CompanionSpacingRulesH8S.LeadDistance).Within(Eps));
            foreach (CompanionProfile prof in new[] { CompanionArchetypes.Get(BondIds.QuartzGuide), CompanionArchetypes.Get(BondIds.AshWarden), CompanionArchetypes.Get(BondIds.StormSkimmer) })
            {
                float l = CompanionSpacingRulesH8S.LeadFor(prof.FollowDistance);
                Assert.That(l, Is.LessThan(prof.FollowDistance + 0.6f), prof.Title + " settles inside its idle radius when the player stops");
                Assert.That(l, Is.LessThanOrEqualTo(CompanionSpacingRulesH8S.LeadDistance));
            }
            Assert.That(CompanionSpacingRulesH8S.LeadPoint(p, Vector3.zero, 2f).z, Is.EqualTo(2f).Within(Eps), "no direction falls back to forward");
        }

        [Test]
        public void ShelterSlotPutsThePlayerBetweenThreatAndCompanion()
        {
            Vector3 p = new Vector3(10f, 0f, 10f), threat = new Vector3(20f, 0f, 10f);
            Vector3 slot = CompanionSpacingRulesH8S.ShelterSlot(p, threat, Vector3.forward, 1, 2f);
            Assert.That(slot.x, Is.LessThan(p.x), "on the far side from the threat");
            Assert.That(Vector3.Distance(slot, threat), Is.GreaterThan(Vector3.Distance(p, threat)));
            Vector3 none = CompanionSpacingRulesH8S.ShelterSlot(p, p, Vector3.forward, 1, 2f);
            Assert.That(none.z, Is.LessThan(p.z), "no threat direction: straight behind");
            Assert.That(CompanionSpacingRulesH8S.ShouldShelter(0.2f, true), Is.True);
            Assert.That(CompanionSpacingRulesH8S.ShouldShelter(0.2f, false), Is.False, "out of a fight it simply mends");
            Assert.That(CompanionSpacingRulesH8S.ShouldShelter(0f, true), Is.False, "a downed companion is not following");
            Assert.That(CompanionSpacingRulesH8S.ShouldShelter(0.5f, true), Is.False);
            Assert.That(CompanionSpacingRulesH8S.CrossedLow(0.35f, 0.25f), Is.True);
            Assert.That(CompanionSpacingRulesH8S.CrossedLow(0.25f, 0.2f), Is.False, "only the crossing counts");
            Assert.That(CompanionSpacingRulesH8S.CrossedLow(0.5f, 0f), Is.False, "going down is the downed bark");
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(5f, 4f), Is.False, "foes on both sides do not swap the slot");
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(5f, 2.5f), Is.True, "a clearly nearer foe does");
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(20f, 15f), Is.False, "far away it needs 30 percent");
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(20f, 13f), Is.True);
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(float.PositiveInfinity, 9f), Is.True, "no current threat: take it");
            Assert.That(CompanionSpacingRulesH8S.SwitchThreat(5f, float.NaN), Is.False);
            Assert.That(CompanionSpacingRulesH8S.ShelterStopRadius, Is.GreaterThan(1f), "a wide stop margin on the slot");
        }

        [Test]
        public void TightHoverAndRegroup()
        {
            Assert.That(CompanionSpacingRulesH8S.HoverFor(1.7f, false, 0.6f), Is.EqualTo(1.7f).Within(Eps));
            Assert.That(CompanionSpacingRulesH8S.HoverFor(1.7f, true, 0.6f), Is.EqualTo(CompanionSpacingRulesH8S.TightHover).Within(Eps));
            Assert.That(CompanionSpacingRulesH8S.HoverFor(1.7f, true, 2f), Is.EqualTo(1.2f).Within(Eps), "never lower than the body needs");
            Assert.That(CompanionSpacingRulesH8S.HoverFor(0.5f, true, 0.4f), Is.EqualTo(0.5f).Within(Eps), "never higher than asked");
            RegroupTrackerH8S r = new RegroupTrackerH8S();
            Assert.That(r.Update(3f, 3f), Is.False, "close from the start is no regroup");
            Assert.That(r.Update(CompanionSpacingRulesH8S.RegroupFar + 5f, 3f), Is.False);
            Assert.That(r.Far, Is.True);
            Assert.That(r.Update(10f, 3f), Is.False, "still catching up");
            Assert.That(r.Update(2.5f, 3f), Is.True, "back within reach");
            Assert.That(r.Update(2.5f, 3f), Is.False, "only once");
            Assert.That(r.Update(float.NaN, 3f), Is.False);
        }

        // ================================================================== commands

        [Test]
        public void TargetPreferenceIsLockThenAimThenLastHit()
        {
            Assert.That(CompanionTargetRulesH8S.Pick(true, true, true), Is.EqualTo(CompanionTargetSourceH8S.Locked));
            Assert.That(CompanionTargetRulesH8S.Pick(false, true, true), Is.EqualTo(CompanionTargetSourceH8S.Aimed));
            Assert.That(CompanionTargetRulesH8S.Pick(false, false, true), Is.EqualTo(CompanionTargetSourceH8S.Engaged));
            Assert.That(CompanionTargetRulesH8S.Pick(false, false, false), Is.EqualTo(CompanionTargetSourceH8S.None));
            Assert.That(CompanionTargetRulesH8S.Pick(false, false, true, true), Is.EqualTo(CompanionTargetSourceH8S.Boss), "an aimed boss outranks the add you hit last");
            Assert.That(CompanionTargetRulesH8S.Pick(false, true, true, true), Is.EqualTo(CompanionTargetSourceH8S.Aimed));
            Assert.That(CompanionTargetRulesH8S.Pick(true, false, true, false), Is.EqualTo(CompanionTargetSourceH8S.Locked));
            Assert.That(CompanionTargetRulesH8S.Pick(false, false, false, true), Is.EqualTo(CompanionTargetSourceH8S.Engaged));
            Assert.That(CompanionTargetRulesH8S.Engaged(10f, 12f, 8f, 25f), Is.True);
            Assert.That(CompanionTargetRulesH8S.Engaged(10f, 10f + CompanionTargetRulesH8S.EngagedWindow + 0.1f, 8f, 25f), Is.False, "too long ago");
            Assert.That(CompanionTargetRulesH8S.Engaged(10f, 12f, 30f, 25f), Is.False, "out of assist range");
            Assert.That(CompanionTargetRulesH8S.Engaged(15f, 12f, 8f, 25f), Is.False, "a hit in the future is bad data");
            Assert.That(CompanionTargetRulesH8S.Engaged(float.NaN, 12f, 8f, 25f), Is.False);
            Assert.That(CompanionTargetRulesH8S.Better(11f, 20f, 10f, 2f), Is.True, "the more recent hit wins");
            Assert.That(CompanionTargetRulesH8S.Better(10.05f, 20f, 10f, 2f), Is.False, "near ties go to the closer foe");
        }

        [Test]
        public void CallGestureTapHoldAndOptionalDoubleTap()
        {
            CompanionCallGestureH8S g = new CompanionCallGestureH8S();
            Assert.That(g.Update(HoldGesture.Result.Tap, false, false, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.Toggle), "default: a tap toggles at once");
            Assert.That(g.Update(HoldGesture.Result.Hold, false, false, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.Recall));
            Assert.That(g.Update(HoldGesture.Result.None, true, false, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None), "default ignores the press itself");
            Assert.That(g.Update(HoldGesture.Result.Tap, false, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None), "first tap waits");
            Assert.That(g.Pending, Is.True);
            Assert.That(g.Update(HoldGesture.Result.None, false, true, 0.1f), Is.EqualTo(CompanionCallGestureH8S.Command.None));
            Assert.That(g.Update(HoldGesture.Result.None, true, true, 0.1f), Is.EqualTo(CompanionCallGestureH8S.Command.Recall), "decided on the second press");
            Assert.That(g.Pending, Is.False);
            Assert.That(g.Update(HoldGesture.Result.Tap, false, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None), "its release adds nothing");
            Assert.That(g.Update(HoldGesture.Result.Tap, false, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None));
            CompanionCallGestureH8S.Command c = CompanionCallGestureH8S.Command.None;
            for (int i = 0; i < 40 && c == CompanionCallGestureH8S.Command.None; i++) c = g.Update(HoldGesture.Result.None, false, true, 0.02f);
            Assert.That(c, Is.EqualTo(CompanionCallGestureH8S.Command.Toggle), "one tap toggles once the window passes");
            Assert.That(g.Update(HoldGesture.Result.Tap, false, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None));
            Assert.That(g.Update(HoldGesture.Result.Hold, false, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.Recall), "a hold still calls it");
            Assert.That(g.Pending, Is.False, "and drops the waiting tap");
            g.Update(HoldGesture.Result.Tap, false, true, 0.016f);
            g.Reset();
            Assert.That(g.Pending, Is.False);
            Assert.That(g.Update(HoldGesture.Result.None, true, true, 0.016f), Is.EqualTo(CompanionCallGestureH8S.Command.None), "a reset tap never pairs with a later press");
        }

        /// <summary>Drives the real HoldGesture frame by frame and returns every command the companion would get.</summary>
        private static List<CompanionCallGestureH8S.Command> RunGesture(bool[] held, float frame)
        {
            HoldGesture hold = new HoldGesture(0.6f);
            CompanionCallGestureH8S call = new CompanionCallGestureH8S();
            List<CompanionCallGestureH8S.Command> got = new List<CompanionCallGestureH8S.Command>();
            for (int i = 0; i < held.Length; i++)
            {
                bool pressed = held[i] && (i == 0 || !held[i - 1]);
                CompanionCallGestureH8S.Command c = call.Update(hold.Update(pressed, held[i], frame), pressed, true, frame);
                if (c != CompanionCallGestureH8S.Command.None) got.Add(c);
            }
            for (int i = 0; i < 30; i++)
            {
                CompanionCallGestureH8S.Command c = call.Update(hold.Update(false, false, frame), false, true, frame);
                if (c != CompanionCallGestureH8S.Command.None) got.Add(c);
            }
            return got;
        }

        [Test]
        public void DoubleTapThroughTheRealHoldGesture()
        {
            // Two quick presses of the companion button, as the hold gesture sees them frame by frame (30 ms frames).
            bool[] quick = { true, true, false, false, false, true, true, false };
            Assert.That(RunGesture(quick, 0.03f), Is.EqualTo(new List<CompanionCallGestureH8S.Command> { CompanionCallGestureH8S.Command.Recall }));
            // A slow double tap: the second press comes inside the window but is held past it (0.3 s) before release.
            bool[] slow = { true, true, false, false, false, false, true, true, true, true, true, true, true, true, true, true, false };
            Assert.That(RunGesture(slow, 0.03f), Is.EqualTo(new List<CompanionCallGestureH8S.Command> { CompanionCallGestureH8S.Command.Recall }), "one recall, never two toggles");
            // Two taps further apart than the window are two toggles.
            bool[] apart = new bool[40];
            apart[0] = apart[1] = true;
            apart[25] = apart[26] = true;
            Assert.That(RunGesture(apart, 0.03f), Is.EqualTo(new List<CompanionCallGestureH8S.Command> { CompanionCallGestureH8S.Command.Toggle, CompanionCallGestureH8S.Command.Toggle }));
        }

        // ================================================================== card

        [Test]
        public void CardModesLabelsFillsAndPulse()
        {
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Downed, true, true, true, true), Is.EqualTo(CompanionModeH8S.Downed), "the record state wins");
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Waiting, false, false, true, false), Is.EqualTo(CompanionModeH8S.Waiting));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Fused, false, false, false, false), Is.EqualTo(CompanionModeH8S.Fused));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Resting, false, false, false, false), Is.EqualTo(CompanionModeH8S.Resting));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Assisting, false, false, true, false), Is.EqualTo(CompanionModeH8S.Assisting));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Following, true, true, true, false), Is.EqualTo(CompanionModeH8S.Separated));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Following, true, false, false, false), Is.EqualTo(CompanionModeH8S.Riding));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Following, false, false, false, false), Is.EqualTo(CompanionModeH8S.Arriving));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Following, false, false, true, true), Is.EqualTo(CompanionModeH8S.Sheltering));
            Assert.That(CompanionCardRulesH8S.ModeOf((int)CompanionState.Following, false, false, true, false), Is.EqualTo(CompanionModeH8S.Following));
            HashSet<string> labels = new HashSet<string>();
            for (int m = 0; m <= (int)CompanionModeH8S.Separated; m++)
            {
                string l = CompanionCardRulesH8S.Label((CompanionModeH8S)m);
                Assert.That(l.Length, Is.InRange(3, 13), "fits the chip");
                Assert.That(labels.Add(l), Is.True, "every mode has its own word");
            }
            Assert.That(CompanionCardRulesH8S.CooldownFill(0f, 14f), Is.EqualTo(1f));
            Assert.That(CompanionCardRulesH8S.CooldownFill(7f, 14f), Is.EqualTo(0.5f).Within(Eps));
            Assert.That(CompanionCardRulesH8S.CooldownFill(20f, 14f), Is.EqualTo(0f).Within(Eps));
            Assert.That(CompanionCardRulesH8S.CooldownFill(5f, 0f), Is.EqualTo(0f));
            Assert.That(CompanionCardRulesH8S.CooldownFill(float.NaN, 10f), Is.EqualTo(1f));
            Assert.That(CompanionCardRulesH8S.PulseAlpha(0f, false), Is.EqualTo(1f).Within(Eps));
            Assert.That(CompanionCardRulesH8S.PulseAlpha(CompanionCardRulesH8S.PulseSeconds * 0.5f, false), Is.EqualTo(0.5f).Within(Eps));
            Assert.That(CompanionCardRulesH8S.PulseAlpha(CompanionCardRulesH8S.PulseSeconds * 0.5f, true), Is.EqualTo(0.45f).Within(Eps), "reduced effects: steady glow");
            Assert.That(CompanionCardRulesH8S.PulseAlpha(5f, false), Is.EqualTo(0f));
            Assert.That(CompanionCardRulesH8S.PulseAlpha(-1f, false), Is.EqualTo(0f));
            Assert.That(CompanionCardRulesH8S.HealthPoints(0.5f, 70f), Is.EqualTo(35));
            Assert.That(CompanionCardRulesH8S.HealthPoints(1f, 70f), Is.EqualTo(70));
            Assert.That(CompanionCardRulesH8S.HealthPoints(0.001f, 70f), Is.EqualTo(1), "a sliver shows as 1, not 0");
            Assert.That(CompanionCardRulesH8S.HealthPoints(-1f, 70f), Is.EqualTo(0));
            Assert.That(CompanionCardRulesH8S.HealthPoints(2f, 70f), Is.EqualTo(70));
            Assert.That(CompanionCardRulesH8S.LowHealth(0.2f), Is.True);
            Assert.That(CompanionCardRulesH8S.LowHealth(0f), Is.False);
            Assert.That(CompanionCardRulesH8S.LowHealth(0.6f), Is.False);
        }

        // ================================================================== guidance words

        [Test]
        public void DistancesRoundInReadableSteps()
        {
            Assert.That(GuideTextRulesH8S.DistanceBucket(2f), Is.EqualTo(0));
            Assert.That(GuideTextRulesH8S.DistanceBucket(4f), Is.EqualTo(5));
            Assert.That(GuideTextRulesH8S.DistanceBucket(23f), Is.EqualTo(25));
            Assert.That(GuideTextRulesH8S.DistanceBucket(51f), Is.EqualTo(50));
            Assert.That(GuideTextRulesH8S.DistanceBucket(149f), Is.EqualTo(150));
            Assert.That(GuideTextRulesH8S.DistanceBucket(260f), Is.EqualTo(250));
            Assert.That(GuideTextRulesH8S.DistanceBucket(1234f), Is.EqualTo(1200));
            Assert.That(GuideTextRulesH8S.DistanceBucket(float.NaN), Is.EqualTo(0));
            Assert.That(GuideTextRulesH8S.DistanceBucket(float.PositiveInfinity), Is.GreaterThan(1000));
            Assert.That(GuideTextRulesH8S.DistanceText(0), Is.EqualTo("here"));
            Assert.That(GuideTextRulesH8S.DistanceText(85), Is.EqualTo("85 m"));
            Assert.That(GuideTextRulesH8S.DistanceText(1200), Is.EqualTo("1.2 km"));
        }

        [Test]
        public void DirectionsAreRelativeToTheView()
        {
            Vector3 f = Vector3.forward;
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(0f, 0f, 5f)), Is.EqualTo(0));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(5f, 0f, 5f)), Is.EqualTo(1));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(5f, 0f, 0f)), Is.EqualTo(2));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(5f, 0f, -5f)), Is.EqualTo(3));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(0f, 0f, -5f)), Is.EqualTo(4));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(-5f, 0f, -5f)), Is.EqualTo(5));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(-5f, 0f, 0f)), Is.EqualTo(6));
            Assert.That(GuideTextRulesH8S.Sector(f, new Vector3(-5f, 0f, 5f)), Is.EqualTo(7));
            Assert.That(GuideTextRulesH8S.Sector(Vector3.right, new Vector3(0f, 0f, -5f)), Is.EqualTo(2), "turning the view turns the words");
            Assert.That(GuideTextRulesH8S.Sector(Vector3.up, new Vector3(0f, 0f, 5f)), Is.EqualTo(-1), "a view straight up has no direction");
            Assert.That(GuideTextRulesH8S.SectorWord(-1), Is.EqualTo(""));
            Assert.That(GuideTextRulesH8S.Vertical(5f), Is.EqualTo(1));
            Assert.That(GuideTextRulesH8S.Vertical(-5f), Is.EqualTo(-1));
            Assert.That(GuideTextRulesH8S.Vertical(2f), Is.EqualTo(0));
            Assert.That(GuideTextRulesH8S.Describe(85, 7, 1), Is.EqualTo("85 m ahead left, above"));
            Assert.That(GuideTextRulesH8S.Describe(0, 3, 0), Is.EqualTo("here"));
            Assert.That(GuideTextRulesH8S.Describe(0, 3, -1), Is.EqualTo("here, below"));
            Assert.That(GuideTextRulesH8S.Describe(1200, 4, 0), Is.EqualTo("1.2 km behind"));
            HashSet<int> keys = new HashSet<int>();
            foreach (int b in new[] { 0, 5, 10, 85, 1200 })
                for (int s = -1; s < 8; s++)
                    for (int v = -1; v <= 1; v++) Assert.That(keys.Add(GuideTextRulesH8S.Key(b, s, v)), Is.True, "keys never collide");
        }

        [Test]
        public void GuidanceWordsAreStickyAndNeverLongerThanTheFittedWording()
        {
            Assert.That(GuideTextRulesH8S.StickySector(24f, 0), Is.EqualTo(0), "just past the edge keeps the word");
            Assert.That(GuideTextRulesH8S.StickySector(30f, 0), Is.EqualTo(1), "clearly past it changes");
            Assert.That(GuideTextRulesH8S.StickySector(24f, -1), Is.EqualTo(1), "no previous word: the plain sector");
            Assert.That(GuideTextRulesH8S.StickySector(-170f, 4), Is.EqualTo(4));
            Assert.That(GuideTextRulesH8S.StickySector(float.NaN, 3), Is.EqualTo(-1));
            Assert.That(GuideTextRulesH8S.ViewAngle(Vector3.forward, Vector3.right), Is.EqualTo(90f).Within(Eps));
            Assert.That(float.IsNaN(GuideTextRulesH8S.ViewAngle(Vector3.up, Vector3.right)), Is.True);
            Assert.That(GuideTextRulesH8S.StickyBucket(23f, 20), Is.EqualTo(20), "a metre or two of sway keeps the number");
            Assert.That(GuideTextRulesH8S.StickyBucket(26.5f, 20), Is.EqualTo(25));
            Assert.That(GuideTextRulesH8S.StickyBucket(4.5f, 0), Is.EqualTo(0), "here stays here a little longer");
            Assert.That(GuideTextRulesH8S.StickyBucket(6f, 0), Is.EqualTo(5));
            Assert.That(GuideTextRulesH8S.StickyBucket(140f, -1), Is.EqualTo(140));
            Assert.That(GuideTextRulesH8S.StickyVertical(2.5f, 1), Is.EqualTo(1));
            Assert.That(GuideTextRulesH8S.StickyVertical(1.5f, 1), Is.EqualTo(0));
            Assert.That(GuideTextRulesH8S.StickyVertical(-2.5f, -1), Is.EqualTo(-1));
            Assert.That(GuideTextRulesH8S.StickyVertical(2.5f, 0), Is.EqualTo(0));
            int longest = GuideTextRulesH8S.LongestWording.Length;
            foreach (int b in new[] { 0, 5, 45, 50, 190, 975, 1000, 9900, 99900 })
                for (int s = -1; s < 8; s++)
                    for (int v = -1; v <= 1; v++)
                        Assert.That(GuideTextRulesH8S.Describe(b, s, v).Length, Is.LessThanOrEqualTo(longest), b + " " + s + " " + v);
            Assert.That(GuideTextRulesH8S.Describe(GuideTextRulesH8S.DistanceBucket(float.PositiveInfinity), 3, 1), Is.EqualTo(GuideTextRulesH8S.LongestWording));
        }

        [Test]
        public void PlaceWordsGetOneLabelRulePerKind()
        {
            Assert.That(GuideTextRulesH8S.KindOf("Realm:hearth-lens"), Is.EqualTo(PlaceKindH8S.Realm));
            Assert.That(GuideTextRulesH8S.KindOf("Venue:forge.exit"), Is.EqualTo(PlaceKindH8S.Venue));
            Assert.That(GuideTextRulesH8S.KindOf("Under:refuge.caretaker"), Is.EqualTo(PlaceKindH8S.Under));
            Assert.That(GuideTextRulesH8S.KindOf("Cast:dante"), Is.EqualTo(PlaceKindH8S.Cast));
            Assert.That(GuideTextRulesH8S.KindOf("Veyra Lofts"), Is.EqualTo(PlaceKindH8S.Named));
            Assert.That(GuideTextRulesH8S.KindOf("Giver"), Is.EqualTo(PlaceKindH8S.None));
            Assert.That(GuideTextRulesH8S.KindOf(""), Is.EqualTo(PlaceKindH8S.None));
            Assert.That(GuideTextRulesH8S.Label("Venue:forge.exit", v => v == "forge" ? "Dante Rook's Crystal Forge" : "", null, null), Is.EqualTo("Dante Rook's Crystal Forge"));
            Assert.That(GuideTextRulesH8S.Label("Venue:forge.exit", null, null, null), Is.EqualTo("forge"), "no title function: the venue id");
            Assert.That(GuideTextRulesH8S.Label("Cast:dante", null, null, id => CastIds04E.Name(id)), Is.EqualTo("Dante Rook"));
            Assert.That(GuideTextRulesH8S.Label("Under:refuge.caretaker", null, r => "Lantern Refuge", null), Is.EqualTo("Lantern Refuge"));
            Assert.That(GuideTextRulesH8S.Label("District:OldSilver", null, null, null), Is.EqualTo("OldSilver"));
            Assert.That(GuideTextRulesH8S.Label("Realm:storm-spring", null, null, null), Is.EqualTo("Root spring"));
            Assert.That(GuideTextRulesH8S.RealmTitle("Realm:some-new-anchor"), Is.EqualTo("Some New Anchor"), "an unlisted anchor still reads well");
            Assert.That(GuideTextRulesH8S.RealmTitle("Venue:forge.exit"), Is.EqualTo(""));
            HashSet<PlaceKindH8S> tagged = new HashSet<PlaceKindH8S> { PlaceKindH8S.Realm, PlaceKindH8S.Venue, PlaceKindH8S.Under, PlaceKindH8S.Cast };
            HashSet<string> tags = new HashSet<string>();
            foreach (PlaceKindH8S k in tagged) Assert.That(tags.Add(GuideTextRulesH8S.KindTag(k)), Is.True);
            Assert.That(GuideTextRulesH8S.KindTag(PlaceKindH8S.None), Is.EqualTo(""));
        }

        [Test]
        public void EveryKnownRealmAnchorHasATitle()
        {
            string[] ids = { "hearth-lens", "oath-seal", "oath-witness", "storm-crossing", "storm-spring", "storm-vein", "oath-terrace", "storm-cavern", "storm-ledge",
                "castellan", "sovereign", "gallery", "storm-shrine", "threshold", "grotto", "observation", "forge-scar", "azure" };
            HashSet<string> titles = new HashSet<string>();
            foreach (string id in ids)
            {
                string token = "Realm:" + id;
                Assert.That(QuestPlaces.IsKnownPlace(null, token), Is.True, token + " is a shipped realm word");
                string t = GuideTextRulesH8S.RealmTitle(token);
                Assert.That(t.Length, Is.InRange(4, 24), token);
                NoDashes(t, token);
                Assert.That(titles.Add(t), Is.True, "two realm anchors share the title " + t);
            }
        }

        // ================================================================== Hearth Witness hints

        private static QuestDef HearthWitness()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/Quests/ascension_h3_hearth_witness.json");
            List<string> errors = new List<string>();
            QuestFile file = QuestJson.Read<QuestFile>(File.ReadAllText(path), path, errors, null);
            Assert.That(errors, Is.Empty);
            foreach (QuestDef q in file.Quests) if (q.Id == QuestHintsH8S.HearthWitness) return q;
            Assert.Fail("the shipped Hearth Witness quest is missing");
            return null;
        }

        [Test]
        public void HearthWitnessHintsCoverEveryShippedPhase()
        {
            QuestDef q = HearthWitness();
            Assert.That(q.Phases.Count, Is.EqualTo(QuestHintsH8S.HearthWitnessPhases.Length), "the hint table lists the quest's phases");
            for (int i = 0; i < q.Phases.Count; i++)
            {
                Assert.That(q.Phases[i].Id, Is.EqualTo(QuestHintsH8S.HearthWitnessPhases[i]));
                string hint = QuestHintsH8S.For(q.Id, q.Phases[i].Id);
                Assert.That(hint.Length, Is.InRange(20, 200), q.Phases[i].Id);
                Assert.That(hint.EndsWith("."), Is.True);
                NoDashes(hint, "hint " + q.Phases[i].Id);
            }
            Assert.That(QuestHintsH8S.Has(q.Id), Is.True);
            Assert.That(QuestHintsH8S.For(q.Id, "no_such_phase"), Is.EqualTo(""));
            Assert.That(QuestHintsH8S.For("fg_04oh_h2_living_lens", "briefing"), Is.EqualTo(""), "other quests get nothing invented");
            Assert.That(QuestHintsH8S.For(null, null), Is.EqualTo(""));
        }

        [Test]
        public void HearthWitnessHandInIsTheWalkBackToTheForge()
        {
            QuestDef q = HearthWitness();
            for (int p = 0; p < q.Phases.Count; p++)
            {
                QuestPhase ph = q.Phases[p];
                bool last = p == q.Phases.Count - 1;
                for (int step = 0; step < ph.Steps.Count; step++)
                {
                    bool handIn = VenueQuestRulesH8S.HandIn(ph, last, step, "forge", "Dante Rook");
                    Assert.That(handIn, Is.EqualTo(ph.Id == "return"), "phase " + ph.Id + " step " + step);
                }
            }
            QuestPhase ret = q.Phases[q.Phases.Count - 1];
            Assert.That(VenueQuestRulesH8S.StepAtVenue(ret.Steps[0], "forge", "Dante Rook"), Is.True, "Venue:forge.exit is at the Forge");
            Assert.That(VenueQuestRulesH8S.StepAtVenue(ret.Steps[1], "forge", "Dante Rook"), Is.True, "Dante's closing dialogue");
            Assert.That(VenueQuestRulesH8S.StepAtVenue(ret.Steps[1], "forge", "Someone Else"), Is.False, "another presenter's dialogue is not");
            Assert.That(VenueQuestRulesH8S.StepAtVenue(q.Phases[1].Steps[0], "forge", "Dante Rook"), Is.False, "the stair porch is elsewhere");
            QuestPhase mixed = new QuestPhase { Id = "m", Steps = new List<QuestStep> { new QuestStep { Type = "GoTo", At = "Realm:storm-spring" }, new QuestStep { Type = "GoTo", At = "Venue:forge.exit" }, new QuestStep { Type = "Give" } } };
            Assert.That(VenueQuestRulesH8S.HandIn(mixed, true, 0, "forge", "Dante Rook"), Is.False, "a step elsewhere first");
            Assert.That(VenueQuestRulesH8S.HandIn(mixed, true, 1, "forge", "Dante Rook"), Is.True, "instant steps may follow");
            Assert.That(VenueQuestRulesH8S.HandIn(mixed, true, 2, "forge", "Dante Rook"), Is.False, "only instant steps left is no hand-in");
            Assert.That(VenueQuestRulesH8S.HandIn(mixed, false, 1, "forge", "Dante Rook"), Is.False, "not the last phase");
            Assert.That(VenueQuestRulesH8S.HandIn(mixed, true, 5, "forge", "Dante Rook"), Is.False, "step out of range");
            Assert.That(VenueQuestRulesH8S.HandIn(null, true, 0, "forge", "Dante Rook"), Is.False);
            foreach (string t in VenueQuestRulesH8S.InstantSteps) Assert.That(VenueQuestRulesH8S.IsInstant(t), Is.True);
            Assert.That(VenueQuestRulesH8S.IsInstant("Talk") || VenueQuestRulesH8S.IsInstant("GoTo") || VenueQuestRulesH8S.IsInstant(null), Is.False);
        }

        [Test]
        public void VenueOfferFollowsTheVenueRuleAndOnlyThePresenterClaimsIt()
        {
            QuestDef assistant = new QuestDef { Id = "a_assistant", VenueGiver = "forge", Giver = "Dante Rook", Presenter = "Workshop assistant" };
            QuestDef first = new QuestDef { Id = "b_dante", VenueGiver = "forge", Giver = "Dante Rook", Presenter = "Dante Rook" };
            QuestDef second = new QuestDef { Id = "c_dante", VenueGiver = "forge", Giver = "Dante Rook", Presenter = "Dante Rook" };
            QuestDef lofts = new QuestDef { Id = "d_lofts", VenueGiver = "lofts", Giver = "Veyra Lofts", Presenter = "Nessa Vale" };
            List<QuestDef> book = new List<QuestDef> { lofts, assistant, first, second };
            List<QuestDef> dantes = new List<QuestDef>();
            foreach (QuestDef q in book) if (VenueQuestRulesH8S.Presents(q, "forge", "Dante Rook")) dantes.Add(q);
            Assert.That(dantes, Is.EqualTo(new List<QuestDef> { first, second }), "the presenter names the person; the assistant's request is not Dante's");
            HashSet<string> done = new HashSet<string>();
            HashSet<string> locked = new HashSet<string> { "c_dante" };
            string active = "";
            // The same predicates QuestBook.NextVenueOffer applies: not completed, and accepted or not locked.
            System.Func<QuestDef, bool> isDone = q => done.Contains(q.Id);
            System.Func<QuestDef, bool> eligible = q => q.Id == active || !locked.Contains(q.Id);
            QuestDef offer = VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible);
            Assert.That(offer, Is.SameAs(assistant), "quest order: the assistant's request comes first");
            Assert.That(VenueQuestRulesH8S.PresenterOffer(offer, dantes, null), Is.Null, "so Dante offers nothing yet");
            done.Add("a_assistant");
            offer = VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible);
            Assert.That(VenueQuestRulesH8S.PresenterOffer(offer, dantes, null), Is.SameAs(first));
            Assert.That(VenueQuestRulesH8S.PresenterOffer(offer, dantes, first), Is.Null, "a running quest is not offered again");
            done.Add("b_dante");
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.Null, "the locked one waits");
            active = "c_dante";
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.SameAs(second), "an accepted quest is offered to resume");
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "lofts", 10, isDone, eligible), Is.SameAs(lofts));
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(null, "forge", 10, isDone, eligible), Is.Null);
        }

        // ================================================================== cast idle and staging

        [Test]
        public void ToolsGiveFittingIdleStyles()
        {
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Hammer), Is.EqualTo(CastIdleStyleH8S.Work), "the forgewright works");
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Wrench), Is.EqualTo(CastIdleStyleH8S.Work));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Ledger), Is.EqualTo(CastIdleStyleH8S.Read), "the archivist reads");
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Clipboard), Is.EqualTo(CastIdleStyleH8S.Read));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Tablet), Is.EqualTo(CastIdleStyleH8S.Read));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.DataSlate), Is.EqualTo(CastIdleStyleH8S.Read));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Medkit), Is.EqualTo(CastIdleStyleH8S.Tend));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Lantern), Is.EqualTo(CastIdleStyleH8S.Watch), "the refuge keeper stands watch");
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.Staff), Is.EqualTo(CastIdleStyleH8S.Watch));
            Assert.That(CastIdleRulesH8S.StyleFor(HeldTool.None), Is.EqualTo(CastIdleStyleH8S.Watch));
            CastDef04E dante = CastRoster04E.Find(CastIds04E.Dante);
            Assert.That(CastIdleRulesH8S.StyleFor(dante.Costume.Tool), Is.EqualTo(CastIdleStyleH8S.Work), "Dante works at his forge");
        }

        private static float BusyShare(CastIdleStyleH8S style, ResidentShift04M shift, int seed)
        {
            int busy = 0, n = 0;
            for (float t = 0f; t < 120f; t += 0.1f, n++) if (CastIdleRulesH8S.Beat(style, shift, t, seed).Busy) busy++;
            return busy / (float)n;
        }

        [Test]
        public void IdleBeatsFollowRoleAndShift()
        {
            Assert.That(BusyShare(CastIdleStyleH8S.Work, ResidentShift04M.Work, 3), Is.InRange(0.5f, 0.8f), "mostly working, glancing up now and then");
            Assert.That(BusyShare(CastIdleStyleH8S.Read, ResidentShift04M.Work, 3), Is.InRange(0.6f, 0.85f));
            Assert.That(BusyShare(CastIdleStyleH8S.Watch, ResidentShift04M.Work, 3), Is.EqualTo(0f), "a watch never has busy hands");
            Assert.That(BusyShare(CastIdleStyleH8S.Work, ResidentShift04M.Shelter, 3), Is.EqualTo(0f), "nobody keeps working in a storm");
            Assert.That(BusyShare(CastIdleStyleH8S.Work, ResidentShift04M.Break, 3), Is.LessThan(BusyShare(CastIdleStyleH8S.Work, ResidentShift04M.Work, 3)), "calmer on a break");
            bool glance = false, left = false, right = false, differs = false;
            for (float t = 0f; t < 60f; t += 0.1f)
            {
                CastIdleBeatH8S a = CastIdleRulesH8S.Beat(CastIdleStyleH8S.Watch, ResidentShift04M.Work, t, 1);
                CastIdleBeatH8S b = CastIdleRulesH8S.Beat(CastIdleStyleH8S.Watch, ResidentShift04M.Work, t, 6);
                if (a.Glance)
                {
                    glance = true;
                    Assert.That(Mathf.Abs(a.GlanceYaw), Is.InRange(20f, 45f), "a natural glance");
                    if (a.GlanceYaw < 0f) left = true; else right = true;
                }
                if (a.Glance != b.Glance) differs = true;
            }
            Assert.That(glance && left && right, Is.True, "a watch looks both ways");
            Assert.That(differs, Is.True, "two people never move in step");
            CastIdleBeatH8S none = CastIdleRulesH8S.Beat(CastIdleStyleH8S.Work, ResidentShift04M.Work, float.NaN, 1);
            Assert.That(none.Busy || none.Glance, Is.False);
        }

        [Test]
        public void ConversationEngagementHasHysteresis()
        {
            Assert.That(CastIdleRulesH8S.Engaged(false, 4f, true, false), Is.True);
            Assert.That(CastIdleRulesH8S.Engaged(false, 6f, true, false), Is.False, "too far to start");
            Assert.That(CastIdleRulesH8S.Engaged(true, 6f, true, false), Is.True, "close enough to keep going");
            Assert.That(CastIdleRulesH8S.Engaged(true, 8f, true, false), Is.False, "walked away");
            Assert.That(CastIdleRulesH8S.Engaged(true, 2f, false, false), Is.False, "out of sight");
            Assert.That(CastIdleRulesH8S.Engaged(false, 11f, false, true), Is.True, "a scene with them always counts");
            Assert.That(CastIdleRulesH8S.Engaged(false, float.NaN, true, false), Is.False);
        }

        // ================================================================== accessibility

        [Test]
        public void AccessibilityTextScaleAndFonts()
        {
            Assert.That(AccessibilityRulesH8S.ClampTextScale(float.NaN), Is.EqualTo(1f));
            Assert.That(AccessibilityRulesH8S.ClampTextScale(2f), Is.EqualTo(AccessibilityRulesH8S.MaxTextScale));
            Assert.That(AccessibilityRulesH8S.ClampTextScale(0.5f), Is.EqualTo(AccessibilityRulesH8S.MinTextScale));
            Assert.That(AccessibilityRulesH8S.Font(16, 1.25f, 22), Is.EqualTo(20));
            Assert.That(AccessibilityRulesH8S.Font(16, 1.5f, 20), Is.EqualTo(20), "capped at what the row holds");
            Assert.That(AccessibilityRulesH8S.Font(16, 0.5f, 22), Is.EqualTo(16), "never smaller than the base");
            Assert.That(AccessibilityRulesH8S.Font(19, 1f, 10), Is.EqualTo(19), "a cap below the base keeps the base");
            Assert.That(AccessibilityRulesH8S.MarkerScale(true), Is.EqualTo(AccessibilityRulesH8S.HighContrastSize));
            Assert.That(AccessibilityRulesH8S.MarkerScale(false), Is.EqualTo(1f));
        }

        [Test]
        public void HighContrastKeepsHueAndBoostsStrength()
        {
            Color gold = new Color(1f, 0.8f, 0.22f, 0.6f);
            Assert.That(AccessibilityRulesH8S.MarkerColor(gold, false), Is.EqualTo(gold), "off: unchanged");
            Color g = AccessibilityRulesH8S.MarkerColor(gold, true);
            Assert.That(Mathf.Max(g.r, Mathf.Max(g.g, g.b)), Is.EqualTo(1f).Within(Eps), "full brightness");
            Assert.That(Mathf.Min(g.r, Mathf.Min(g.g, g.b)), Is.LessThanOrEqualTo(0.15f + Eps), "strong saturation");
            Assert.That(g.a, Is.EqualTo(1f));
            Assert.That(g.r >= g.g && g.g >= g.b, Is.True, "the hue order is kept");
            Color dim = AccessibilityRulesH8S.MarkerColor(new Color(0.2f, 0.4f, 0.5f), true);
            Assert.That(dim.b, Is.EqualTo(1f).Within(Eps), "a dim blue becomes a bright blue");
            Assert.That(dim.b >= dim.g && dim.g >= dim.r, Is.True);
            Color grey = AccessibilityRulesH8S.MarkerColor(new Color(0.6f, 0.6f, 0.62f), true);
            Assert.That(grey, Is.EqualTo(new Color(1f, 1f, 1f, 1f)), "greys become white");
            Assert.That(AccessibilityRulesH8S.MarkerColor(Color.black, true), Is.EqualTo(new Color(1f, 1f, 1f, 1f)));
            Color vivid = AccessibilityRulesH8S.MarkerColor(new Color(0.42f, 1f, 0.48f), true);
            Assert.That(vivid.g, Is.EqualTo(1f).Within(Eps));
            Assert.That(AccessibilityRulesH8S.OutlineColor(1f).a, Is.InRange(0.8f, 1f));
            Assert.That(AccessibilityRulesH8S.OutlineColor(-1f).a, Is.EqualTo(0f));
        }

        [Test]
        public void GiverStateGlyphsStayDistinctInHighContrast()
        {
            GiverState[] states = { GiverState.AvailableMain, GiverState.AvailableSide, GiverState.ActiveTarget, GiverState.TurnIn, GiverState.Unavailable };
            HashSet<MarkerShape04E> shapes = new HashSet<MarkerShape04E>();
            List<Color> colors = new List<Color>();
            foreach (GiverState s in states)
            {
                Assert.That(shapes.Add(GuidanceRules04E.ShapeOf(s)), Is.True, "shapes still differ, not only colours");
                colors.Add(AccessibilityRulesH8S.MarkerColor(GuidanceRules04E.ColorOf(s), true));
            }
            for (int i = 0; i < colors.Count; i++)
                for (int j = i + 1; j < colors.Count; j++)
                {
                    Color a = colors[i], b = colors[j];
                    float d = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                    Assert.That(d, Is.GreaterThan(0.2f), states[i] + " and " + states[j] + " look alike in high contrast");
                }
        }
    }
}
