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
                        Assert.That(after.WindupFrac, Is.GreaterThanOrEqualTo(before.WindupFrac - 1e-5f), w + " the pose spends a larger share of the swing winding up, never a smaller one");
                    }
                }
            }
        }

        [Test]
        public void EveryAreaBlowLeavesTimeToReactAndRoll()
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
                    float roll = CombatTellsH8S.ReactionSeconds(d) + CombatTellsH8S.EscapeSeconds(Mathf.Min(escape, CombatTellsH8S.DodgeDistance));
                    Assert.That(t.Windup, Is.GreaterThanOrEqualTo(roll - 1e-4f), kv.Key + " " + d + ": time to react and roll out (or through)");
                }
            }
            Assert.That(areas, Is.GreaterThanOrEqualTo(10), "the tables hold many area blows");
        }

        [Test]
        public void DeeperRingsWaitLongerButNeverMoreThanOneFullRoll()
        {
            foreach (DifficultyLevel d in Difficulties)
            {
                float last = 0f;
                foreach (float reach in new[] { 0.5f, 1.5f, 2.5f, 3.4f, 6f, 12f })
                {
                    float w = RingWindup(reach, d);
                    Assert.That(w, Is.GreaterThanOrEqualTo(last - 1e-5f), d + " reach " + reach + ": a deeper ring never gives less time");
                    last = w;
                }
                Assert.That(RingWindup(12f, d), Is.GreaterThan(RingWindup(0.5f, d)), d + ": a small ring needs only part of a roll");
                Assert.That(RingWindup(12f, d), Is.EqualTo(RingWindup(6f, d)).Within(1e-4f), d + ": past one roll's depth the roll's invulnerability answers it, not a longer wait");
            }
        }

        /// <summary>The wind-up the real tables give a made-up body-centred ring with a quick base wind-up.</summary>
        private static float RingWindup(float reach, DifficultyLevel d)
        {
            CreatureAttack ring = new CreatureAttack { Name = "test ring", Shape = AttackShape.Ring, Reach = reach, Windup = 0.2f, Active = 0.2f, Recovery = 0.5f };
            return CreatureAttackDb.TimingH8S(ring, ScaleOf(d), 1.5f, d, 1f, 1f).Windup;
        }

        [Test]
        public void ShippedBigSweepsKeepTheirTimingAndAFastWideSweepIsRaised()
        {
            CreatureAttack lash = null;
            foreach (CreatureAttack a in CreatureSpeciesDb.Get("dune_wyrm").Attacks) if (a.Name == "Tail lash") lash = a;
            Assert.That(lash, Is.Not.Null);
            foreach (DifficultyLevel d in Difficulties)
            {
                AttackTiming before = CreatureAttackDb.Timing(lash, ScaleOf(d), 5f);
                AttackTiming after = CreatureAttackDb.TimingH8S(lash, ScaleOf(d), 5f, d, 2.5f, 1f);
                Assert.That(after.Windup, Is.EqualTo(before.Windup).Within(1e-4f), "the wyrm's tail lash already leaves time for a roll on " + d);
            }
            // A made-up wide sweep with a quick wind-up: raised to a reaction and a full roll, never left at 0.4 s.
            CreatureAttack fast = new CreatureAttack { Name = "fast sweep", Shape = AttackShape.Arc, Arc = 320f, Reach = 4f, Windup = 0.5f, Active = 0.2f, Recovery = 0.6f, Flags = DamageFlags.Knockdown };
            AttackTiming raised = CreatureAttackDb.TimingH8S(fast, ScaleOf(DifficultyLevel.Veteran), 1.5f, DifficultyLevel.Veteran, 1f, 1f);
            AttackTiming plain = CreatureAttackDb.Timing(fast, ScaleOf(DifficultyLevel.Veteran), 1.5f);
            Assert.That(raised.Windup, Is.GreaterThan(plain.Windup + 0.1f), "raised well past its quick wind-up");
            CreatureAttack wider = new CreatureAttack { Name = "wider sweep", Shape = AttackShape.Arc, Arc = 320f, Reach = 9f, Windup = 0.5f, Active = 0.2f, Recovery = 0.6f, Flags = DamageFlags.Knockdown };
            AttackTiming widerT = CreatureAttackDb.TimingH8S(wider, ScaleOf(DifficultyLevel.Veteran), 1.5f, DifficultyLevel.Veteran, 1f, 1f);
            Assert.That(widerT.Windup, Is.EqualTo(raised.Windup).Within(1e-4f), "a wider sweep waits no longer: one full roll answers both");
            AttackTiming broken = CreatureAttackDb.TimingH8S(fast, ScaleOf(DifficultyLevel.Veteran), 1.5f, DifficultyLevel.Veteran, 1f, 0.5f);
            Assert.That(broken.Windup, Is.LessThan(raised.Windup), "broken claws (half reach) leave a shallower sweep");
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
            // The ring texture (UIKit.Ring: alpha = clamp01((0.12 - |d - 0.85|) * 30) over the quad's half size) draws its
            // outline up to RingVisibleEdge and nothing past it. If that texture changes, RingVisibleEdge must follow it.
            float visible = CombatTellsH8S.RingVisibleEdge;
            Assert.That(Mathf.Clamp01((0.12f - Mathf.Abs(visible - 0.85f)) * 30f), Is.GreaterThanOrEqualTo(0.5f), "the outline is still clearly drawn at the edge");
            Assert.That(Mathf.Clamp01((0.12f - Mathf.Abs(visible + 0.02f - 0.85f)) * 30f), Is.EqualTo(0f).Within(1e-4f), "and gone just past it");
            Assert.That(CombatTellsH8S.RingQuadRadius(0.01f), Is.GreaterThanOrEqualTo(0.3f), "tiny rings stay visible");
            // Each drawn edge is where the real hit test (the rules Creature.InRing, InArc and Contact call) flips for a
            // player-sized target: a hand's width inside is hit, a hand's width outside is safe.
            const float hand = 0.02f;
            float p = CombatTellsH8S.PlayerRadius;
            foreach (float r in new[] { 0.8f, 2.5f, 6f })
            {
                float edge = CombatTellsH8S.CreatureRingHitRadius(r);
                Assert.That(CombatTellsH8S.RingQuadRadius(edge) * visible, Is.EqualTo(edge).Within(1e-4f), "ring " + r + ": the outline is drawn at the hit edge");
                Assert.That(CombatTellsH8S.RingHits(edge - hand, 0f, r, p), Is.True, "ring " + r + ": just inside the drawn edge is hit");
                Assert.That(CombatTellsH8S.RingHits(edge + hand, 0f, r, p), Is.False, "ring " + r + ": just outside it is safe");
            }
            Assert.That(CombatTellsH8S.RingHits(1f, 3f, 2f, p), Is.False, "a ring never reaches a player well above or below it");
            foreach (float body in new[] { 0.5f, 1.2f, 3f })
            {
                foreach (float reach in new[] { 1.1f, 3.4f })
                {
                    float sweep = CombatTellsH8S.SweepHitRadius(body, reach);
                    Assert.That(CombatTellsH8S.ReachHits(sweep - hand, body, p, reach), Is.True, "sweep: just inside the drawn ring is hit");
                    Assert.That(CombatTellsH8S.ReachHits(sweep + hand, body, p, reach), Is.False, "sweep: just outside it is safe");
                    float half = CombatTellsH8S.LaneHalfWidth(body, reach);
                    Assert.That(CombatTellsH8S.ReachHits(half - hand, body, p, reach), Is.True, "lane: just inside its side is hit");
                    Assert.That(CombatTellsH8S.ReachHits(half + hand, body, p, reach), Is.False, "lane: just beside it is safe");
                }
            }
        }

        [Test]
        public void LanesShowTheRealPath()
        {
            const float speed = 13f, active = 0.42f, body = 0.5f, reach = 1.1f, hand = 0.02f;
            float p = CombatTellsH8S.PlayerRadius;
            // A pounce's lane ends where its contact ends: the leap Creature.Commit launches (LeapGap, LeapSpeed) carries
            // the body over the active window, then a player just short of the lane's end is hit and one just past it is not.
            foreach (float d in new[] { 2f, 4f, 8f, 80f })
            {
                float lane = Pounce(d);
                float travel = CombatTellsH8S.LeapSpeed(speed, CombatTellsH8S.LeapGap(d, body), active) * active;
                Assert.That(CombatTellsH8S.ReachHits(lane - hand - travel, body, p, reach), Is.True, d + " m: just inside the lane's end is hit");
                Assert.That(CombatTellsH8S.ReachHits(lane + hand - travel, body, p, reach), Is.False, d + " m: just past it is safe");
            }
            Assert.That(Pounce(8f), Is.GreaterThan(Pounce(4f)), "a farther target means a longer leap");
            Assert.That(Pounce(80f), Is.EqualTo(Pounce(200f)).Within(1e-4f), "never longer than its speed allows");
            // A charge runs at its own speed over the whole active window (Creature.Commit), then reaches past its front.
            float charge = CombatTellsH8S.LaneLength(TellShapeH8S.Charge, 10f, 0.9f, 3f, 1.2f, 1.6f);
            Assert.That(CombatTellsH8S.ReachHits(charge - hand - 9f, 1.2f, p, 1.6f), Is.True, "charge: just inside the lane's end is hit");
            Assert.That(CombatTellsH8S.ReachHits(charge + hand - 9f, 1.2f, p, 1.6f), Is.False, "charge: just past it is safe");
        }

        [Test]
        public void APounceNeverLandsPastItsLockedLane()
        {
            const float speed = 13f, active = 0.42f, body = 0.5f, reach = 1.1f, lockedAt = 4f, hand = 0.02f;
            float p = CombatTellsH8S.PlayerRadius;
            float lane = Pounce(lockedAt);
            foreach (float live in new[] { 4f, 5f, 7f, 12f })
            {
                float d = CombatTellsH8S.LeapDistance(live, true, lockedAt);
                float travel = CombatTellsH8S.LeapSpeed(speed, CombatTellsH8S.LeapGap(d, body), active) * active;
                Assert.That(CombatTellsH8S.ReachHits(lane + hand - travel, body, p, reach), Is.False, "player backed off to " + live + " m: nothing past the drawn lane is hit");
            }
            float chased = CombatTellsH8S.LeapSpeed(speed, CombatTellsH8S.LeapGap(12f, body), active) * active;
            Assert.That(CombatTellsH8S.ReachHits(lane + hand - chased, body, p, reach), Is.True, "following the live distance would have hit past the lane");
            float closer = CombatTellsH8S.LeapSpeed(speed, CombatTellsH8S.LeapGap(CombatTellsH8S.LeapDistance(2.5f, true, lockedAt), body), active);
            Assert.That(closer, Is.LessThan(CombatTellsH8S.LeapSpeed(speed, CombatTellsH8S.LeapGap(lockedAt, body), active)), "a player who stepped closer meets a shorter leap");
            Assert.That(CombatTellsH8S.LeapDistance(7f, false, lockedAt), Is.EqualTo(7f), "before the lock it follows the live distance");
        }

        /// <summary>The lane length of a made-up pounce (speed 13, active 0.42 s, body 0.5 m, reach 1.1 m) at a target distance.</summary>
        private static float Pounce(float targetDistance)
        {
            return CombatTellsH8S.LaneLength(TellShapeH8S.Lunge, 13f, 0.42f, targetDistance, 0.5f, 1.1f);
        }

        [Test]
        public void TheAimLocksBeforeLaunchAndOnlySnapsWithinFiftyDegrees()
        {
            // Creature.AttackMotion (the body stops turning) and the lane (its aim freezes) both ask AimLocked.
            foreach (float w in new[] { 0.3f, 0.8f, 2.5f })
            {
                Assert.That(CombatTellsH8S.AimLocked(w * 0.5f, w), Is.False, w + " s: it still tracks the player through the first half");
                Assert.That(CombatTellsH8S.AimLocked(w * 0.95f, w), Is.True, w + " s: the aim is fixed before the blow launches");
                bool was = false;
                for (int i = 0; i <= 40; i++)
                {
                    bool now = CombatTellsH8S.AimLocked(w * i / 40f, w);
                    Assert.That(!was || now, Is.True, w + " s: once locked it stays locked");
                    was = now;
                }
            }
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
