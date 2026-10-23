using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S tell language (CombatTellsH8S, CreatureAttackDb.TimingH8S): one colour and mark per answer, ground marks by
    /// body shape, wind-up floors by class and difficulty that never shorten an original wind-up, time to leave every
    /// area blow, marks drawn at the hit tests' true reach, honest lanes and the aim lock.
    /// </summary>
    public sealed class CombatTellsH8STests
    {
        private static readonly DifficultyLevel[] Difficulties = { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran };

        private static float ScaleOf(DifficultyLevel d)
        {
            return CombatPressureRules.WindupScale(d);
        }

        /// <summary>Every creature attack the game can play: the species tables and the regional role overrides.</summary>
        private static List<KeyValuePair<string, CreatureAttack>> AllAttacks()
        {
            List<KeyValuePair<string, CreatureAttack>> list = new List<KeyValuePair<string, CreatureAttack>>();
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
                foreach (CreatureAttack a in s.Attacks)
                    list.Add(new KeyValuePair<string, CreatureAttack>(s.Id + " " + a.Name, a));
            foreach (CreatureAttack a in FracturedWildsCombat04OC.AzureAttacks()) list.Add(new KeyValuePair<string, CreatureAttack>("azure " + a.Name, a));
            for (int slot = 0; slot < 3; slot++)
                foreach (CreatureAttack a in FrontierCombat04OG.AzureRoles(slot)) list.Add(new KeyValuePair<string, CreatureAttack>("frontier " + slot + " " + a.Name, a));
            return list;
        }

        [Test]
        public void OneColourAndMarkPerAnswer()
        {
            Assert.That(CombatTellsH8S.SeverityOf(DamageFlags.Unblockable), Is.EqualTo(TellSeverityH8S.Dodge));
            Assert.That(CombatTellsH8S.SeverityOf(DamageFlags.Unblockable | DamageFlags.Knockdown | DamageFlags.Heavy), Is.EqualTo(TellSeverityH8S.Dodge));
            Assert.That(CombatTellsH8S.SeverityOf(DamageFlags.Parryable | DamageFlags.Knockdown), Is.EqualTo(TellSeverityH8S.Guard), "a knockdown can still be guarded");
            Assert.That(CombatTellsH8S.SeverityOf(DamageFlags.None), Is.EqualTo(TellSeverityH8S.Guard));
            Assert.That(CombatTellsH8S.MarkOf(TellSeverityH8S.Dodge), Is.EqualTo("!!"));
            Assert.That(CombatTellsH8S.MarkOf(TellSeverityH8S.Guard), Is.EqualTo("!"));
            Color dodge = CombatTellsH8S.ColorOf(TellSeverityH8S.Dodge);
            Color guard = CombatTellsH8S.ColorOf(TellSeverityH8S.Guard);
            // The documented language (CREATURES.md, the 04D boss HUD): orange "!!" means dodge, red "!" means guard.
            Assert.That(dodge.r, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(dodge.g, Is.InRange(0.5f, 0.7f), "dodge is orange");
            Assert.That(guard.g, Is.LessThan(0.35f), "guard is red");
            Assert.That(dodge.g - guard.g, Is.GreaterThan(0.25f), "the two answers are clearly different hues");
            Assert.That(CombatTellsH8S.DodgeFlashColor.g, Is.GreaterThan(guard.g), "the pre-impact flash of an unblockable blow is not the guard red");
            MeleeMove heavy = new MeleeMove { Unblockable = true };
            Assert.That(CombatTellsH8S.SeverityOf(heavy), Is.EqualTo(TellSeverityH8S.Dodge));
            Assert.That(CombatTellsH8S.SeverityOf(new MeleeMove()), Is.EqualTo(TellSeverityH8S.Guard));
            Assert.That(CombatTellsH8S.SeverityOf((MeleeMove)null), Is.EqualTo(TellSeverityH8S.Guard));
        }

        [Test]
        public void ShapesPickTheRightGroundMark()
        {
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Arc, 90f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Strike));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Arc, 320f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Sweep));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Leap, 0f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Lunge));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Line, 0f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Charge));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Line, 0f, AttackUse.Air), Is.EqualTo(TellShapeH8S.Dive));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Ring, 0f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Area));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Under, 0f, AttackUse.Under), Is.EqualTo(TellShapeH8S.Area));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Cone, 40f, AttackUse.Air), Is.EqualTo(TellShapeH8S.Breath));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Projectile, 0f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.Shot));
            Assert.That(CombatTellsH8S.ShapeOf(AttackShape.Call, 0f, AttackUse.Ground), Is.EqualTo(TellShapeH8S.None));
            Assert.That(CombatTellsH8S.ShapeOf((CreatureAttack)null), Is.EqualTo(TellShapeH8S.None));
            Assert.That(CombatTellsH8S.MarkFor(TellShapeH8S.Sweep), Is.EqualTo(TellMarkH8S.Ring));
            Assert.That(CombatTellsH8S.MarkFor(TellShapeH8S.Area), Is.EqualTo(TellMarkH8S.Ring));
            Assert.That(CombatTellsH8S.MarkFor(TellShapeH8S.Lunge), Is.EqualTo(TellMarkH8S.Lane));
            Assert.That(CombatTellsH8S.MarkFor(TellShapeH8S.Charge), Is.EqualTo(TellMarkH8S.Lane));
            foreach (TellShapeH8S s in new[] { TellShapeH8S.Strike, TellShapeH8S.Dive, TellShapeH8S.Breath, TellShapeH8S.Shot, TellShapeH8S.None })
                Assert.That(CombatTellsH8S.MarkFor(s), Is.EqualTo(TellMarkH8S.None), s + " draws nothing on the ground");
            Assert.That(CombatTellsH8S.ShapeOf(new MeleeMove { Arc = 360f }), Is.EqualTo(TellShapeH8S.Sweep));
            Assert.That(CombatTellsH8S.ShapeOf(new MeleeMove { Arc = 120f }), Is.EqualTo(TellShapeH8S.Strike));
        }

        [Test]
        public void TheShippedTablesGetTheMarksTheyNeed()
        {
            // The all-round sweeps had no ground mark at all; now each gets a ring, the pounces and charges a lane.
            string[] sweeps = { "crystal_basilisk Tail sweep", "violet_wyvern Blade tail", "quartz_drake Tail crush", "dune_wyrm Tail lash", "ash_burrower Flail sweep" };
            string[] lanes = { "neon_jackal Pounce", "frost_stalker Pounce", "crystal_basilisk Charge", "ember_scavenger Darting slash", "ash_burrower Ash charge" };
            Dictionary<string, CreatureAttack> byName = new Dictionary<string, CreatureAttack>();
            foreach (KeyValuePair<string, CreatureAttack> kv in AllAttacks()) byName[kv.Key] = kv.Value;
            foreach (string s in sweeps)
            {
                Assert.That(byName.ContainsKey(s), Is.True, s + " exists");
                Assert.That(CombatTellsH8S.MarkFor(CombatTellsH8S.ShapeOf(byName[s])), Is.EqualTo(TellMarkH8S.Ring), s);
            }
            foreach (string s in lanes)
            {
                Assert.That(byName.ContainsKey(s), Is.True, s + " exists");
                Assert.That(CombatTellsH8S.MarkFor(CombatTellsH8S.ShapeOf(byName[s])), Is.EqualTo(TellMarkH8S.Lane), s);
            }
            Assert.That(CombatTellsH8S.ShapeOf(byName["violet_wyvern Dive"]), Is.EqualTo(TellShapeH8S.Dive), "air dives draw no ground lane");
        }

        [Test]
        public void VeteranKeepsTheOriginalFloorsAndEasierDifficultiesGetMore()
        {
            Assert.That(CombatTellsH8S.MinWindup(TellShapeH8S.Strike, false, DifficultyLevel.Veteran), Is.EqualTo(CreatureAttackDb.MinWindup).Within(1e-5f));
            Assert.That(CombatTellsH8S.MinWindup(TellShapeH8S.Strike, true, DifficultyLevel.Veteran), Is.EqualTo(CreatureAttackDb.MinHeavyWindup).Within(1e-5f));
            TellShapeH8S[] shapes = { TellShapeH8S.Strike, TellShapeH8S.Sweep, TellShapeH8S.Lunge, TellShapeH8S.Charge, TellShapeH8S.Dive, TellShapeH8S.Area, TellShapeH8S.Breath, TellShapeH8S.Shot };
            foreach (TellShapeH8S s in shapes)
            {
                for (int h = 0; h < 2; h++)
                {
                    bool heavy = h == 1;
                    float story = CombatTellsH8S.MinWindup(s, heavy, DifficultyLevel.Story);
                    float balanced = CombatTellsH8S.MinWindup(s, heavy, DifficultyLevel.Balanced);
                    float veteran = CombatTellsH8S.MinWindup(s, heavy, DifficultyLevel.Veteran);
                    Assert.That(story, Is.GreaterThanOrEqualTo(balanced), s + " story gives at least balanced's time");
                    Assert.That(balanced, Is.GreaterThanOrEqualTo(veteran), s + " balanced gives at least veteran's time");
                    float original = heavy ? CreatureAttackDb.MinHeavyWindup : CreatureAttackDb.MinWindup;
                    Assert.That(veteran, Is.GreaterThanOrEqualTo(original - 1e-5f), s + " never below the original floor");
                }
            }
            Assert.That(CombatTellsH8S.MinWindup(TellShapeH8S.None, false, DifficultyLevel.Story), Is.EqualTo(0f));
        }

        [Test]
        public void TellTimingNeverShortensAndOnlyTouchesTheWindup()
        {
            foreach (KeyValuePair<string, CreatureAttack> kv in AllAttacks())
            {
                CreatureAttack a = kv.Value;
                foreach (DifficultyLevel d in Difficulties)
                {
                    foreach (float size in new[] { 0.4f, 1.5f, 6f })
                    {
                        AttackTiming before = CreatureAttackDb.Timing(a, ScaleOf(d), size);
                        AttackTiming after = CreatureAttackDb.TimingH8S(a, ScaleOf(d), size, d, 0.8f, 1f);
                        string w = kv.Key + " " + d + " size " + size;
                        Assert.That(after.Windup, Is.GreaterThanOrEqualTo(before.Windup - 1e-5f), w + " wind-up never shortened");
                        Assert.That(after.Active, Is.EqualTo(before.Active).Within(1e-5f), w + " active window unchanged");
                        Assert.That(after.Recovery, Is.EqualTo(before.Recovery).Within(1e-5f), w + " recovery unchanged");
                        bool heavy = (a.Flags & (DamageFlags.Knockdown | DamageFlags.Unblockable)) != 0;
                        Assert.That(after.Windup, Is.GreaterThanOrEqualTo(CombatTellsH8S.MinWindup(CombatTellsH8S.ShapeOf(a), heavy, d) - 1e-5f), w + " class floor");
                        Assert.That(after.WindupFrac, Is.InRange(0f, 1f), w);
                    }
                }
            }
        }

        [Test]
        public void EveryAreaBlowLeavesTimeToGetOut()
        {
            int areas = 0;
            foreach (KeyValuePair<string, CreatureAttack> kv in AllAttacks())
            {
                CreatureAttack a = kv.Value;
                TellShapeH8S s = CombatTellsH8S.ShapeOf(a);
                if (s != TellShapeH8S.Area && s != TellShapeH8S.Sweep) continue;
                areas++;
                foreach (DifficultyLevel d in Difficulties)
                {
                    float body = 1.2f;
                    AttackTiming t = CreatureAttackDb.TimingH8S(a, ScaleOf(d), 1.5f, d, body, 1f);
                    float escape = CombatTellsH8S.EscapeDistance(a, body, 1f);
                    Assert.That(escape, Is.GreaterThan(0f), kv.Key + " has a depth to escape from");
                    Assert.That(t.Windup, Is.GreaterThanOrEqualTo(CombatTellsH8S.ReactionSeconds(d) + CombatTellsH8S.EscapeSeconds(escape) - 1e-4f), kv.Key + " " + d);
                }
            }
            Assert.That(areas, Is.GreaterThanOrEqualTo(10), "the tables hold many area blows");
        }

        [Test]
        public void TheLongTailLashGetsMoreTimeThanBefore()
        {
            CreatureAttack lash = null;
            foreach (CreatureAttack a in CreatureSpeciesDb.Get("dune_wyrm").Attacks) if (a.Name == "Tail lash") lash = a;
            Assert.That(lash, Is.Not.Null);
            AttackTiming before = CreatureAttackDb.Timing(lash, ScaleOf(DifficultyLevel.Veteran), 5f);
            AttackTiming after = CreatureAttackDb.TimingH8S(lash, ScaleOf(DifficultyLevel.Veteran), 5f, DifficultyLevel.Veteran, 2.5f, 1f);
            // A 7 m all-round knockdown: one dodge clears 4.3 m, the rest is a jog.
            Assert.That(after.Windup, Is.GreaterThan(before.Windup + 0.1f), "the wyrm's 7 m tail lash had too little time to clear on Veteran");
            Assert.That(after.Windup, Is.LessThan(2f), "but stays a quick, dangerous blow");
        }

        [Test]
        public void EscapeTimeIsOneDodgeThenAJog()
        {
            Assert.That(CombatTellsH8S.EscapeSeconds(0f), Is.EqualTo(0f));
            Assert.That(CombatTellsH8S.EscapeSeconds(-3f), Is.EqualTo(0f));
            Assert.That(CombatTellsH8S.EscapeSeconds(float.NaN), Is.EqualTo(0f));
            Assert.That(CombatTellsH8S.EscapeSeconds(CombatTellsH8S.DodgeDistance), Is.EqualTo(CombatTellsH8S.DodgeSeconds).Within(1e-5f));
            Assert.That(CombatTellsH8S.EscapeSeconds(CombatTellsH8S.DodgeDistance + CombatTellsH8S.JogSpeed), Is.EqualTo(CombatTellsH8S.DodgeSeconds + 1f).Within(1e-4f));
            float last = 0f;
            for (float d = 0.25f; d < 12f; d += 0.25f)
            {
                float e = CombatTellsH8S.EscapeSeconds(d);
                Assert.That(e, Is.GreaterThan(last), "escape time grows with depth");
                last = e;
            }
            Assert.That(CombatTellsH8S.MinAreaWindup(3f, DifficultyLevel.Story), Is.GreaterThan(CombatTellsH8S.MinAreaWindup(3f, DifficultyLevel.Veteran)));
        }

        [Test]
        public void MarksAreDrawnAtTheTrueReach()
        {
            // UIKit.Ring: alpha = clamp01((0.12 - |d - 0.85|) * 30) over the quad's half size. If that texture changes,
            // RingVisibleEdge must follow it.
            float edge = CombatTellsH8S.RingVisibleEdge;
            Assert.That(Mathf.Clamp01((0.12f - Mathf.Abs(edge - 0.85f)) * 30f), Is.GreaterThanOrEqualTo(0.5f), "the outline is still clearly drawn at the edge");
            Assert.That(Mathf.Clamp01((0.12f - Mathf.Abs(edge + 0.02f - 0.85f)) * 30f), Is.EqualTo(0f).Within(1e-4f), "and gone just past it");
            foreach (float hit in new[] { 0.9f, 2.6f, 4.9f, 7.6f })
                Assert.That(CombatTellsH8S.RingQuadRadius(hit) * edge, Is.EqualTo(hit).Within(1e-4f), "outline ends at the hit radius " + hit);
            Assert.That(CombatTellsH8S.RingQuadRadius(0.01f), Is.GreaterThanOrEqualTo(0.3f), "tiny rings stay visible");
            // Creature.InRing: flat distance <= radius + 0.3 + max(0.3, the player's body radius).
            Assert.That(CombatTellsH8S.CreatureRingHitRadius(3f), Is.EqualTo(3.6f).Within(1e-5f));
            // Creature.InArc (sweeps): distance - body radius - 0.3 <= reach.
            Assert.That(CombatTellsH8S.SweepHitRadius(1.2f, 3.4f), Is.EqualTo(4.9f).Within(1e-5f));
            // Creature.Contact (pounces, charges): chest distance - body radius - 0.3 <= reach, to either side.
            Assert.That(CombatTellsH8S.LaneHalfWidth(0.5f, 1.1f), Is.EqualTo(1.9f).Within(1e-5f));
            // The old ring drew its outline about 0.15 r inside the radius it was given, before the slack and the body.
            float oldVisible = 3f * edge;
            Assert.That(CombatTellsH8S.CreatureRingHitRadius(3f) - oldVisible, Is.GreaterThan(0.6f), "the old ring hid more than half a metre of danger");
        }

        [Test]
        public void LanesShowTheRealPath()
        {
            // A charge runs its full speed over the active window, then reaches past the body's front.
            float charge = CombatTellsH8S.LaneLength(TellShapeH8S.Charge, 10f, 0.9f, 3f, 1.2f, 1.6f);
            Assert.That(charge, Is.EqualTo(9f + 1.2f + 1.6f + CombatTellsH8S.PlayerRadius).Within(1e-4f));
            // A pounce covers the gap to its target (Creature.Commit: speed min(a.Speed, gap / active + 1)).
            float near = CombatTellsH8S.LaneLength(TellShapeH8S.Lunge, 13f, 0.42f, 4f, 0.5f, 1.1f);
            float far = CombatTellsH8S.LaneLength(TellShapeH8S.Lunge, 13f, 0.42f, 8f, 0.5f, 1.1f);
            float cap = CombatTellsH8S.LaneLength(TellShapeH8S.Lunge, 13f, 0.42f, 80f, 0.5f, 1.1f);
            Assert.That(far, Is.GreaterThan(near), "a farther target means a longer leap");
            Assert.That(cap, Is.EqualTo(13f * 0.42f + 0.5f + 1.1f + CombatTellsH8S.PlayerRadius).Within(1e-4f), "never longer than its speed allows");
            float gap = 4f - 0.5f - 0.4f;
            Assert.That(near, Is.EqualTo(Mathf.Min(13f * 0.42f, gap + 0.42f) + 0.5f + 1.1f + CombatTellsH8S.PlayerRadius).Within(1e-4f));
        }

        [Test]
        public void TheAimLocksBeforeLaunchAndOnlySnapsWithinFiftyDegrees()
        {
            Assert.That(CombatTellsH8S.AimLockFraction, Is.EqualTo(0.7f).Within(1e-5f), "the facing lock in Creature.AttackMotion");
            Assert.That(CombatTellsH8S.AimLocked(0.69f, 1f), Is.False);
            Assert.That(CombatTellsH8S.AimLocked(0.7f, 1f), Is.True);
            Vector3 f = Vector3.forward;
            Vector3 right30 = new Vector3(Mathf.Sin(30f * Mathf.Deg2Rad), 0f, Mathf.Cos(30f * Mathf.Deg2Rad)) * 6f;
            Vector3 right70 = new Vector3(Mathf.Sin(70f * Mathf.Deg2Rad), 0f, Mathf.Cos(70f * Mathf.Deg2Rad)) * 6f;
            Assert.That(Vector3.Dot(CombatTellsH8S.AimFor(f, right30, 50f), right30.normalized), Is.GreaterThan(0.99999f), "within the snap it aims at the target");
            Assert.That(Vector3.Dot(CombatTellsH8S.AimFor(f, right70, 50f), f), Is.GreaterThan(0.99999f), "beyond the snap it goes straight ahead");
            Assert.That(Vector3.Dot(CombatTellsH8S.AimFor(f, Vector3.zero, 50f), f), Is.GreaterThan(0.99999f));
            Assert.That(Vector3.Dot(CombatTellsH8S.AimFor(Vector3.zero, right30, 50f), right30.normalized), Is.GreaterThan(0.99999f), "a zero facing falls back to +Z");
            Vector3 lifted = CombatTellsH8S.AimFor(new Vector3(0f, 5f, 1f), new Vector3(1f, -9f, 2f), 50f);
            Assert.That(lifted.y, Is.EqualTo(0f), "aims are flat");
            Assert.That(lifted.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void OnlyBlowsThatLandAtOneMomentCountForTheCadence()
        {
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Strike), Is.True);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Sweep), Is.True);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Lunge), Is.True);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Charge), Is.True);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Area), Is.True);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Breath), Is.False, "breath ticks over time");
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Shot), Is.False, "a shot lands after its flight");
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.Dive), Is.False);
            Assert.That(CombatTellsH8S.LandsAtCommit(TellShapeH8S.None), Is.False);
        }
    }
}
