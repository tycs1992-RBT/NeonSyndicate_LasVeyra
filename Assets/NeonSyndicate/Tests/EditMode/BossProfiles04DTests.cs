using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The pure rules of the three upgraded 04D boss fights (BossProfiles04D and friends): which reward each fight
    /// teaches, phase thresholds that open once even under burst damage, the finite support budget, telegraph and
    /// recovery windows per difficulty, lanes that always leave a reachable safe spot, limited turning for shards and the
    /// Queen's aim lock, a move that reaches a flying player, the bounded pull, and one tested cleanup per way out.
    /// </summary>
    public sealed class BossProfiles04DTests
    {
        private const float Eps = 1e-3f;
        private static readonly string[] Upgraded = { "golem_foreman", "mirage_djinn", "wraith_queen" };
        private static readonly HumanoidCreature[] Kinds = { HumanoidCreature.GolemForeman, HumanoidCreature.MirageDjinn, HumanoidCreature.WraithQueen };
        private static readonly DifficultyLevel[] Levels = { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran };

        private static readonly BossMove04D[] AllMoves =
        {
            BossMove04D.ForemanOrb, BossMove04D.ForemanRain, BossMove04D.ForemanSlam, BossMove04D.ForemanThrow,
            BossMove04D.DjinnChoir, BossMove04D.DjinnSandstorm, BossMove04D.DjinnRelocate, BossMove04D.QueenBeams, BossMove04D.QueenShatter
        };

        // ================================================================== profiles

        [Test]
        public void UpgradedBossesHaveProfilesAndTheTitanKeepsItsScript()
        {
            foreach (string id in Upgraded)
            {
                Assert.That(BossProfiles04D.IsUpgraded(id), Is.True, id);
                Assert.That(BossProfiles04D.For(id).BossId, Is.EqualTo(id));
            }
            Assert.That(BossProfiles04D.For("glacier_titan"), Is.Null, "the Titan runs its original script");
            Assert.That(BossProfiles04D.For(HumanoidCreature.GlacierTitan), Is.Null);
            Assert.That(BossProfiles04D.For(""), Is.Null);
            Assert.That(BossProfiles04D.For((string)null), Is.Null);
            Assert.That(BossProfiles04D.For(HumanoidCreature.None), Is.Null);
            for (int i = 0; i < Kinds.Length; i++) Assert.That(BossProfiles04D.For(Kinds[i]).BossId, Is.EqualTo(Upgraded[i]));
        }

        [Test]
        public void EachFightTeachesTheMotifOfItsReward()
        {
            Assert.That(CrystalFocusDb.BehaviorOfUnique(BossProfiles04D.For("golem_foreman").SignatureUnique), Is.EqualTo(CrystalBehavior.Sunderheart));
            Assert.That(CrystalFocusDb.BehaviorOfUnique(BossProfiles04D.For("mirage_djinn").SignatureUnique), Is.EqualTo(CrystalBehavior.MirageChoir));
            Assert.That(CrystalFocusDb.BehaviorOfUnique(BossProfiles04D.For("wraith_queen").SignatureUnique), Is.EqualTo(CrystalBehavior.Mirrorwake));
            Assert.That(BossProfiles04D.For("golem_foreman").Signature, Is.EqualTo(BossMove04D.ForemanOrb), "gravity orb teaches Sunderheart");
            Assert.That(BossProfiles04D.For("mirage_djinn").Signature, Is.EqualTo(BossMove04D.DjinnChoir), "guided shards teach the Mirage Choir");
            Assert.That(BossProfiles04D.For("wraith_queen").Signature, Is.EqualTo(BossMove04D.QueenBeams), "reflected beams teach Mirrorwake");
            foreach (string id in Upgraded)
            {
                BossProfile04D p = BossProfiles04D.For(id);
                Assert.That(p.SignatureUnique, Is.EqualTo(CrystalFocusDb.SignatureFor(id)), id);
                Assert.That(BossProfiles04D.IsSignature(p.Signature), Is.True, id);
                Assert.That(BossProfiles04D.ReachesAir(p.Signature), Is.True, id + " signature contests the air");
                Assert.That(p.CoreBonus, Is.GreaterThan(1f), id + " window is worth punishing");
                Assert.That(p.CoreBonus, Is.LessThanOrEqualTo(1.6f), id + " window does not trivialise the fight");
                Assert.That(string.IsNullOrEmpty(p.WindowLabel), Is.False);
                Assert.That(string.IsNullOrEmpty(BossProfiles04D.MoveLabel(p.Signature)), Is.False);
            }
        }

        [Test]
        public void PhaseThresholdsMatchTheBossBarMarksAndTheBudgetIsFinite()
        {
            foreach (string id in Upgraded)
            {
                BossProfile04D p = BossProfiles04D.For(id);
                Assert.That(p.PhaseThresholds, Is.EqualTo(new[] { 0.66f, 0.33f }), id + " phases sit on the boss bar's marks");
                Assert.That(p.MaxSupport, Is.EqualTo(2), id + " at most two supports at once");
                Assert.That(p.ReinforcementBatches, Is.EqualTo(1), id + " one finite reinforcement batch");
            }
        }

        // ================================================================== phase latch

        private static BossPhaseLatch04D Latch()
        {
            return new BossPhaseLatch04D(new[] { BossProfiles04D.PhaseTwoAt, BossProfiles04D.PhaseThreeAt });
        }

        [Test]
        public void PhaseLatchOpensEachThresholdOnce()
        {
            BossPhaseLatch04D l = Latch();
            int skipped;
            Assert.That(l.Evaluate(1f, out skipped), Is.EqualTo(0));
            Assert.That(l.Evaluate(0.7f, out skipped), Is.EqualTo(0));
            Assert.That(l.Evaluate(0.6f, out skipped), Is.EqualTo(1), "phase two opens");
            Assert.That(skipped, Is.EqualTo(0));
            Assert.That(l.Evaluate(0.55f, out skipped), Is.EqualTo(0), "and only once");
            Assert.That(l.Evaluate(0.3f, out skipped), Is.EqualTo(2), "phase three opens");
            Assert.That(l.Evaluate(0.1f, out skipped), Is.EqualTo(0));
            Assert.That(l.Entered, Is.EqualTo(2));
            Assert.That(l.Skipped, Is.EqualTo(0));
            Assert.That(l.Phase, Is.EqualTo(2));
        }

        [Test]
        public void BurstAcrossBothThresholdsEntersTheDeepestAndSkipsTheOther()
        {
            BossPhaseLatch04D l = Latch();
            int skipped;
            Assert.That(l.Evaluate(0.2f, out skipped), Is.EqualTo(2), "one step from full to a fifth enters phase three");
            Assert.That(skipped, Is.EqualTo(1), "phase two is skipped explicitly");
            Assert.That(l.Fired(0) && l.Fired(1), Is.True);
            for (int i = 0; i < 20; i++) Assert.That(l.Evaluate(0.2f - i * 0.005f, out skipped), Is.EqualTo(0), "simultaneous collider hits cannot repeat a batch");
            Assert.That(l.Entered, Is.EqualTo(1), "exactly one transition for the burst");
            Assert.That(l.Skipped, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedEvaluationsHealingAndNaNCannotReopenAPhase()
        {
            BossPhaseLatch04D l = Latch();
            int skipped;
            Assert.That(l.Evaluate(0.5f, out skipped), Is.EqualTo(1));
            Assert.That(l.Evaluate(0.95f, out skipped), Is.EqualTo(0), "healing back up does not reset the latch");
            Assert.That(l.Evaluate(0.5f, out skipped), Is.EqualTo(0), "crossing again after a heal opens nothing");
            Assert.That(l.Evaluate(float.NaN, out skipped), Is.EqualTo(0));
            Assert.That(l.Evaluate(0.2f, out skipped), Is.EqualTo(2));
            Assert.That(l.Evaluate(0.9f, out skipped), Is.EqualTo(0));
            Assert.That(l.Evaluate(0f, out skipped), Is.EqualTo(0));
            Assert.That(l.Entered, Is.EqualTo(2));
        }

        [Test]
        public void LatchUsesTheOriginalStrictThresholdsAndSortsItsInput()
        {
            BossPhaseLatch04D l = Latch();
            int skipped;
            Assert.That(l.Evaluate(0.66f, out skipped), Is.EqualTo(0), "the original script opens below 0.66, not at it");
            Assert.That(l.Evaluate(0.6599f, out skipped), Is.EqualTo(1));
            Assert.That(l.Evaluate(0.33f, out skipped), Is.EqualTo(0));
            Assert.That(l.Evaluate(0.3299f, out skipped), Is.EqualTo(2));
            BossPhaseLatch04D r = new BossPhaseLatch04D(new[] { 0.33f, 0.66f });
            Assert.That(r.Evaluate(0.5f, out skipped), Is.EqualTo(1), "thresholds given lowest first still open highest first");
            BossPhaseLatch04D none = new BossPhaseLatch04D(null);
            Assert.That(none.Evaluate(0f, out skipped), Is.EqualTo(0));
            Assert.That(none.Count, Is.EqualTo(0));
        }

        // ================================================================== support budget

        [Test]
        public void OneFiniteBatchIsReservedBeforeScheduling()
        {
            BossAddBudget04D b = new BossAddBudget04D(2, 1);
            Assert.That(b.TryReserveBatch(), Is.True, "the one batch");
            Assert.That(b.BatchesLeft, Is.EqualTo(0), "consumed at reservation, before anything spawns");
            Assert.That(b.TryReserveBatch(), Is.False, "never a second batch");
            // The scheduled spawn failed (no clear ground): nothing is given back.
            b.NoteAdmitted(0);
            Assert.That(b.TryReserveBatch(), Is.False, "a failed spawn does not refund the batch");
        }

        [Test]
        public void AtMostTwoSupportsAreAlive()
        {
            BossAddBudget04D b = new BossAddBudget04D(2, 1);
            Assert.That(b.Allowance(5, 0), Is.EqualTo(2));
            Assert.That(b.Allowance(2, 1), Is.EqualTo(1));
            Assert.That(b.Allowance(2, 2), Is.EqualTo(0));
            Assert.That(b.Allowance(2, 7), Is.EqualTo(0), "never negative");
            Assert.That(b.Allowance(0, 0), Is.EqualTo(0));
            Assert.That(b.Allowance(1, -3), Is.EqualTo(1), "a bad count cannot open extra room");
        }

        [Test]
        public void SupportDeathNeverReopensABatch()
        {
            BossAddBudget04D b = new BossAddBudget04D(2, 1);
            Assert.That(b.TryReserveBatch(), Is.True);
            b.NoteAdmitted(b.Allowance(2, 0));
            Assert.That(b.Admitted, Is.EqualTo(2));
            // Both supports die.
            Assert.That(b.TryReserveBatch(), Is.False, "their deaths open no new batch");
            Assert.That(b.Admitted, Is.EqualTo(2), "the fight admitted exactly two over its whole length");
        }

        [Test]
        public void ClosedBudgetAdmitsNothing()
        {
            BossAddBudget04D b = new BossAddBudget04D(2, 1);
            b.Close();
            Assert.That(b.Closed, Is.True);
            Assert.That(b.TryReserveBatch(), Is.False);
            Assert.That(b.Allowance(2, 0), Is.EqualTo(0));
        }

        // ================================================================== timing per difficulty

        [Test]
        public void TelegraphsStayInTheReadableWindow()
        {
            foreach (BossMove04D m in AllMoves)
            {
                float story = BossTiming04D.Telegraph(m, DifficultyLevel.Story);
                float bal = BossTiming04D.Telegraph(m, DifficultyLevel.Balanced);
                float vet = BossTiming04D.Telegraph(m, DifficultyLevel.Veteran);
                Assert.That(bal, Is.InRange(0.7f, 1.2f), m + " Balanced");
                Assert.That(vet, Is.InRange(0.7f, 1.2f), m + " Veteran");
                Assert.That(story, Is.InRange(0.7f, 1.6f), m + " Story");
                Assert.That(story, Is.GreaterThan(bal), m + " Story is more forgiving");
                Assert.That(vet, Is.LessThanOrEqualTo(bal), m + " Veteran is tighter or equal");
            }
        }

        [Test]
        public void ForemanOrbWindupIsReadableAndLongerOnStory()
        {
            float bal = BossTiming04D.Telegraph(BossMove04D.ForemanOrb, DifficultyLevel.Balanced);
            float vet = BossTiming04D.Telegraph(BossMove04D.ForemanOrb, DifficultyLevel.Veteran);
            float story = BossTiming04D.Telegraph(BossMove04D.ForemanOrb, DifficultyLevel.Story);
            Assert.That(bal, Is.InRange(0.7f, 1.2f));
            Assert.That(vet, Is.InRange(0.7f, 1.2f));
            Assert.That(story, Is.GreaterThan(bal));
            Assert.That(story, Is.LessThanOrEqualTo(BossTiming04D.StoryTelegraphMax));
        }

        [Test]
        public void RecoveryWindowsStayInOneToTwoSeconds()
        {
            foreach (BossMove04D m in AllMoves)
            {
                float story = BossTiming04D.Recovery(m, DifficultyLevel.Story);
                float bal = BossTiming04D.Recovery(m, DifficultyLevel.Balanced);
                float vet = BossTiming04D.Recovery(m, DifficultyLevel.Veteran);
                Assert.That(bal, Is.InRange(1f, 2f), m + " Balanced");
                Assert.That(vet, Is.InRange(1f, 2f), m + " Veteran");
                Assert.That(story, Is.InRange(1f, 2.5f), m + " Story");
                Assert.That(story, Is.GreaterThanOrEqualTo(bal), m + " Story at least as long");
                Assert.That(vet, Is.LessThanOrEqualTo(bal), m.ToString());
            }
            foreach (string id in Upgraded)
            {
                BossMove04D sig = BossProfiles04D.For(id).Signature;
                Assert.That(BossTiming04D.Recovery(sig, DifficultyLevel.Balanced), Is.GreaterThan(1.2f), id + " signature leaves a real window");
            }
        }

        [Test]
        public void AimTrackingIllusionAndGapsAreBounded()
        {
            foreach (DifficultyLevel d in Levels)
            {
                Assert.That(BossTiming04D.AcquireSeconds(d), Is.InRange(0.3f, 0.7f), d.ToString());
                Assert.That(BossTiming04D.AimTurnRate(d), Is.InRange(30f, 90f), d.ToString());
                Assert.That(BossTiming04D.IllusionSeconds(d), Is.InRange(8f, 20f), d + " the illusion is bounded");
                for (int ph = -1; ph <= 4; ph++) Assert.That(BossTiming04D.MoveGap(ph, d), Is.GreaterThanOrEqualTo(1.2f));
            }
            Assert.That(BossTiming04D.IllusionSeconds(DifficultyLevel.Story), Is.GreaterThan(BossTiming04D.IllusionSeconds(DifficultyLevel.Veteran)));
            Assert.That(BossTiming04D.AimTurnRate(DifficultyLevel.Story), Is.LessThan(BossTiming04D.AimTurnRate(DifficultyLevel.Veteran)));
        }

        // ================================================================== lanes and safety

        [Test]
        public void LaneGeometry()
        {
            Lane04D l = Lane04D.Make(Vector3.zero, new Vector3(0f, 5f, 2f), 10f, 1f);
            Assert.That(l.Dir.y, Is.EqualTo(0f).Within(Eps), "lanes are flat");
            Assert.That(l.Dir.magnitude, Is.EqualTo(1f).Within(Eps));
            Assert.That(BossLanes04D.InLane(l, new Vector3(0f, 0f, 5f)), Is.True, "on the strip");
            Assert.That(BossLanes04D.InLane(l, new Vector3(0.9f, 3f, 5f)), Is.True, "height is ignored, side within half width");
            Assert.That(BossLanes04D.InLane(l, new Vector3(1.2f, 0f, 5f)), Is.False, "beside it");
            Assert.That(BossLanes04D.InLane(l, new Vector3(0f, 0f, 10.5f)), Is.False, "past its end");
            Assert.That(BossLanes04D.InLane(l, new Vector3(0f, 0f, -0.5f)), Is.True, "the cap behind the origin");
            Assert.That(BossLanes04D.InLane(l, new Vector3(0f, 0f, -2f)), Is.False, "well behind it");
            Lane04D zero = Lane04D.Make(Vector3.zero, Vector3.zero, 5f, 1f);
            Assert.That(zero.Dir, Is.EqualTo(Vector3.forward), "a zero direction falls back to +Z");
            Lane04D[] fan = new Lane04D[3];
            Assert.That(BossLanes04D.FanLanes(Vector3.zero, new Vector3(10f, 0f, 0f), 3, 30f, 12f, 1f, fan), Is.EqualTo(3));
            Assert.That(Vector3.Angle(fan[1].Dir, Vector3.right), Is.LessThan(0.5f), "the middle lane points at the target");
            Assert.That(Vector3.Angle(fan[0].Dir, fan[2].Dir), Is.EqualTo(60f).Within(0.5f));
            Lane04D[] radial = new Lane04D[4];
            Assert.That(BossLanes04D.RadialLanes(Vector3.zero, 4, 0f, 12f, 1f, radial), Is.EqualTo(4));
            Assert.That(Vector3.Angle(radial[0].Dir, radial[1].Dir), Is.EqualTo(90f).Within(0.5f));
            Assert.That(BossLanes04D.RadialLanes(Vector3.zero, 9, 0f, 12f, 1f, radial), Is.EqualTo(4), "never past the array");
        }

        [Test]
        public void ForemanRadialLanesAlwaysLeaveAReachableSafeSpot()
        {
            Vector3 arena = Vector3.zero;
            const float arenaRadius = 32f;
            Lane04D[] lanes = new Lane04D[4];
            int checks = 0;
            for (int count = 3; count <= 4; count++)
            {
                for (float baseDeg = 0f; baseDeg < 360f; baseDeg += 11f)
                {
                    for (float tx = -20f; tx <= 20f; tx += 10f)
                    {
                        Vector3 target = new Vector3(tx, 0f, tx * 0.5f);
                        int n = BossLanes04D.RadialLanes(target, count, baseDeg, BossProfiles04D.RadialLaneLength, BossProfiles04D.LaneHalfWidth, lanes);
                        for (float px = -8f; px <= 8f; px += 1.6f)
                        {
                            for (float pz = -8f; pz <= 8f; pz += 1.6f)
                            {
                                Vector3 player = target + new Vector3(px, 0f, pz);
                                if (player.magnitude > arenaRadius) continue;
                                Vector3 spot;
                                bool ok = BossLanes04D.FindSafeSpot(lanes, n, target, BossProfiles04D.RuptureRadius, player, BossLanes04D.SafeReach, arena, arenaRadius, out spot);
                                Assert.That(ok, Is.True, "radial " + count + " at " + baseDeg + " deg, player " + player);
                                Assert.That(MathUtil.FlatDistance(spot, player), Is.LessThanOrEqualTo(BossLanes04D.SafeReach + Eps));
                                Assert.That(BossLanes04D.IsSafe(lanes, n, target, BossProfiles04D.RuptureRadius, spot, arena, arenaRadius), Is.True);
                                checks++;
                            }
                        }
                    }
                }
            }
            Assert.That(checks, Is.GreaterThan(5000));
        }

        [Test]
        public void ForemanFanLanesAlwaysLeaveAReachableSafeSpot()
        {
            Vector3 boss = Vector3.zero;
            const float arenaRadius = 40f;
            Lane04D[] lanes = new Lane04D[3];
            for (float d = 4f; d <= 26f; d += 2f)
            {
                for (float yaw = 0f; yaw < 360f; yaw += 15f)
                {
                    Vector3 target = MathUtil.DirFromYaw(yaw) * d;
                    int n = BossLanes04D.FanLanes(boss, target, 3, BossProfiles04D.FanSpread, Mathf.Min(26f, d + 8f), BossProfiles04D.LaneHalfWidth, lanes);
                    for (float off = -4f; off <= 4f; off += 1f)
                    {
                        Vector3 player = target + MathUtil.RightOf(MathUtil.DirFromYaw(yaw)) * off;
                        Vector3 spot;
                        Assert.That(BossLanes04D.FindSafeSpot(lanes, n, target, BossProfiles04D.RuptureRadius, player, BossLanes04D.SafeReach, boss, arenaRadius, out spot), Is.True,
                            "fan at " + d + " m, " + yaw + " deg, offset " + off);
                    }
                }
            }
        }

        [Test]
        public void OneQueenBeamAlwaysLeavesASafeSpot()
        {
            System.Random r = new System.Random(4104);
            Lane04D[] lanes = new Lane04D[2];
            for (int k = 0; k < 2000; k++)
            {
                Vector3 queen = Vector3.zero;
                float fa = (float)r.NextDouble() * 360f;
                Vector3 focus = MathUtil.DirFromYaw(fa) * 12f;
                Vector3 player = new Vector3((float)r.NextDouble() * 40f - 20f, 0f, (float)r.NextDouble() * 40f - 20f);
                lanes[0] = Lane04D.Make(queen, focus - queen, 12f, BossProfiles04D.BeamRadius);
                lanes[1] = Lane04D.Make(focus, player - focus, Mathf.Max(2f, MathUtil.FlatDistance(focus, player) + 10f), BossProfiles04D.BeamRadius);
                Vector3 spot;
                Assert.That(BossLanes04D.FindSafeSpot(lanes, 2, player, 0f, player, BossLanes04D.SafeReach, queen, 40f, out spot), Is.True, "beam " + k);
            }
        }

        [Test]
        public void SafeSpotSearchReportsAFullyCoveredLayout()
        {
            Lane04D[] none = new Lane04D[0];
            Vector3 spot;
            Assert.That(BossLanes04D.FindSafeSpot(none, 0, Vector3.zero, 20f, Vector3.zero, BossLanes04D.SafeReach, Vector3.zero, 50f, out spot), Is.False,
                "a ring wider than the player's reach leaves no safe spot (the runtime never builds one)");
            Assert.That(BossLanes04D.FindSafeSpot(none, 0, Vector3.zero, 0f, new Vector3(100f, 0f, 0f), 1f, Vector3.zero, 10f, out spot), Is.False, "outside the arena is not a safe spot");
            Assert.That(BossLanes04D.FindSafeSpot(none, 0, Vector3.zero, 0f, Vector3.zero, 1f, Vector3.zero, 10f, out spot), Is.True);
        }

        [Test]
        public void BeamHitsTheBodyNearItsLineOnly()
        {
            Vector3 feet = new Vector3(0f, 0f, 10f);
            Assert.That(BossLanes04D.BeamHitsBody(new Vector3(-10f, 1.2f, 10f), new Vector3(10f, 1.2f, 10f), 0.85f, feet, 1.8f), Is.True, "chest height across the body");
            Assert.That(BossLanes04D.BeamHitsBody(new Vector3(-10f, 0.4f, 10f), new Vector3(10f, 0.4f, 10f), 0.85f, feet, 1.8f), Is.True, "at the shins");
            Assert.That(BossLanes04D.BeamHitsBody(new Vector3(-10f, 1.2f, 12f), new Vector3(10f, 1.2f, 12f), 0.85f, feet, 1.8f), Is.False, "two metres to the side");
            Assert.That(BossLanes04D.BeamHitsBody(new Vector3(-10f, 4.5f, 10f), new Vector3(10f, 4.5f, 10f), 0.85f, feet, 1.8f), Is.False, "well over the head");
            Assert.That(BossLanes04D.BeamHitsBody(new Vector3(0f, 1.2f, 0f), new Vector3(0f, 1.2f, 8f), 0.85f, feet, 1.8f), Is.False, "the committed line stops short (a wall clamped it)");
        }

        // ================================================================== turning and aim

        [Test]
        public void TurnTowardIsLimitedPerStep()
        {
            Vector3 v = BossLanes04D.TurnToward(Vector3.forward, Vector3.right, 30f);
            Assert.That(Vector3.Angle(Vector3.forward, v), Is.EqualTo(30f).Within(0.05f));
            Assert.That(Vector3.Angle(v, Vector3.right), Is.EqualTo(60f).Within(0.05f), "it turned toward the target");
            Assert.That(v.magnitude, Is.EqualTo(1f).Within(Eps));
            Vector3 done = BossLanes04D.TurnToward(Vector3.forward, new Vector3(1f, 0f, 3f), 45f);
            Assert.That(Vector3.Angle(done, new Vector3(1f, 0f, 3f)), Is.LessThan(0.05f), "inside the step it arrives");
            Vector3 up = BossLanes04D.TurnToward(Vector3.forward, Vector3.up, 20f);
            Assert.That(up.y, Is.GreaterThan(0.3f), "three-dimensional for flyers");
            Assert.That(Vector3.Angle(Vector3.forward, up), Is.EqualTo(20f).Within(0.05f));
        }

        [Test]
        public void TurnTowardHandlesOppositeAndDegenerateDirections()
        {
            Vector3 back = BossLanes04D.TurnToward(Vector3.forward, Vector3.back, 45f);
            Assert.That(Vector3.Angle(Vector3.forward, back), Is.EqualTo(45f).Within(0.05f), "straight behind still turns by the step");
            Assert.That(BossLanes04D.TurnToward(Vector3.zero, Vector3.right, 10f), Is.EqualTo(Vector3.right));
            Assert.That(BossLanes04D.TurnToward(Vector3.left * 3f, Vector3.zero, 10f), Is.EqualTo(Vector3.left));
            Vector3 still = BossLanes04D.TurnToward(Vector3.forward, Vector3.right, 0f);
            Assert.That(Vector3.Angle(still, Vector3.forward), Is.LessThan(0.05f), "a zero step does not turn");
        }

        [Test]
        public void ShardStopsSteeringForGoodOnceItLosesSight()
        {
            bool lost = false;
            Vector3 dir = Vector3.forward;
            Vector3 to = Vector3.right;
            Vector3 a = BossLanes04D.SteerShard(dir, to, 90f, 0.1f, true, true, ref lost);
            Assert.That(Vector3.Angle(dir, a), Is.EqualTo(9f).Within(0.05f), "90 degrees per second for a tenth of a second");
            Assert.That(lost, Is.False);
            Vector3 b = BossLanes04D.SteerShard(a, to, 90f, 0.1f, false, true, ref lost);
            Assert.That(lost, Is.True, "cover broke the line");
            Assert.That(Vector3.Angle(a, b), Is.LessThan(0.01f), "no steering without sight");
            Vector3 c = BossLanes04D.SteerShard(b, to, 90f, 0.1f, true, true, ref lost);
            Assert.That(Vector3.Angle(b, c), Is.LessThan(0.01f), "regaining sight never restarts the steering");
        }

        [Test]
        public void ShardFliesStraightAfterItsSteeringWindow()
        {
            bool lost = false;
            Vector3 d = BossLanes04D.SteerShard(Vector3.forward, Vector3.right, 90f, 0.1f, true, false, ref lost);
            Assert.That(Vector3.Angle(Vector3.forward, d), Is.LessThan(0.01f));
            Assert.That(lost, Is.False);
            foreach (DifficultyLevel l in Levels)
            {
                float maxTurn = BossProfiles04D.ChoirTurnRate(l) * BossProfiles04D.ChoirSteerSeconds(l);
                Assert.That(maxTurn, Is.LessThan(120f), l + ": a shard can never turn round to chase a player who crossed its lane");
            }
        }

        [Test]
        public void QueenAimFollowsAtALimitedRateThenNeverMovesAfterTheLock()
        {
            BossBeamAim04D aim = new BossBeamAim04D(Vector3.forward);
            aim.Track(Vector3.right, 60f, 0.25f);
            Assert.That(Vector3.Angle(Vector3.forward, aim.Aim), Is.EqualTo(15f).Within(0.05f), "60 degrees per second");
            aim.Track(Vector3.right, 60f, 0.25f);
            Assert.That(Vector3.Angle(Vector3.forward, aim.Aim), Is.EqualTo(30f).Within(0.05f));
            aim.Lock();
            Vector3 locked = aim.Aim;
            aim.Track(Vector3.back, 60f, 5f);
            aim.Track(Vector3.up, 999f, 1f);
            Assert.That(aim.Locked, Is.True);
            Assert.That(Vector3.Angle(locked, aim.Aim), Is.LessThan(0.001f), "after the telegraph commits the beam never follows the player");
            BossBeamAim04D zero = new BossBeamAim04D(Vector3.zero);
            Assert.That(zero.Aim, Is.EqualTo(Vector3.forward));
        }

        // ================================================================== move choice

        [Test]
        public void AFlyingPlayerAlwaysFacesAMoveThatReachesTheAir()
        {
            float[] rolls = { 0f, 0.2f, 0.45f, 0.5f, 0.55f, 0.8f, 0.999f };
            float[] dists = { 2f, 6f, 9f, 15f, 30f };
            foreach (HumanoidCreature k in Kinds)
            {
                for (int phase = 0; phase < 3; phase++)
                    foreach (float d in dists)
                        for (int since = 0; since < 3; since++)
                            for (int reloc = 0; reloc < 5; reloc++)
                                for (int plates = 0; plates < 2; plates++)
                                    foreach (float roll in rolls)
                                    {
                                        BossFightView04D v = new BossFightView04D { Phase = phase, Distance = d, Airborne = true, PlatesUp = plates == 1, SinceSignature = since, SinceRelocate = reloc };
                                        BossMove04D m = BossProfiles04D.Choose(k, v, roll);
                                        Assert.That(BossProfiles04D.ReachesAir(m) || m == BossMove04D.DjinnRelocate, Is.True, k + " chose " + m + " against a flyer");
                                    }
            }
        }

        [Test]
        public void TheSignatureComesAtLeastEveryOtherAttack()
        {
            System.Random r = new System.Random(7);
            foreach (HumanoidCreature k in Kinds)
            {
                int since = 1, reloc = 0, signature = 0, relocations = 0, attacks = 0, run = 0, worstRun = 0;
                for (int i = 0; i < 400; i++)
                {
                    BossFightView04D v = new BossFightView04D { Phase = i / 140, Distance = (float)r.NextDouble() * 30f, Airborne = r.NextDouble() < 0.2, PlatesUp = i < 100, SinceSignature = since, SinceRelocate = reloc };
                    BossMove04D m = BossProfiles04D.Choose(k, v, (float)r.NextDouble());
                    Assert.That(m, Is.Not.EqualTo(BossMove04D.None));
                    if (m != BossMove04D.DjinnRelocate) attacks++;
                    if (BossProfiles04D.IsSignature(m))
                    {
                        signature++;
                        since = 0;
                        run = 0;
                    }
                    else
                    {
                        since++;
                        if (m != BossMove04D.DjinnRelocate) run++;
                        worstRun = Mathf.Max(worstRun, run);
                    }
                    if (m == BossMove04D.DjinnRelocate)
                    {
                        relocations++;
                        reloc = 0;
                    }
                    else reloc++;
                }
                Assert.That(worstRun, Is.LessThanOrEqualTo(1), k + ": never two filler attacks in a row");
                Assert.That(signature * 2 + 1, Is.GreaterThanOrEqualTo(attacks), k + ": at least every other attack is the signature, all fight long");
                if (k == HumanoidCreature.MirageDjinn) Assert.That(relocations, Is.GreaterThanOrEqualTo(400 / 5), "the Djinn keeps shifting lanes");
                else Assert.That(relocations, Is.EqualTo(0));
            }
        }

        [Test]
        public void TheTitanAndUnknownKindsChooseNothing()
        {
            BossFightView04D v = new BossFightView04D { Distance = 5f, SinceSignature = 3 };
            Assert.That(BossProfiles04D.Choose(HumanoidCreature.GlacierTitan, v, 0.3f), Is.EqualTo(BossMove04D.None));
            Assert.That(BossProfiles04D.Choose(HumanoidCreature.None, v, 0.3f), Is.EqualTo(BossMove04D.None));
            Assert.That(BossProfiles04D.MoveLabel(BossMove04D.None), Is.EqualTo(""));
            foreach (BossMove04D m in AllMoves)
            {
                Assert.That(string.IsNullOrEmpty(BossProfiles04D.MoveLabel(m)), Is.False, m + " has a HUD line");
                Assert.That(string.IsNullOrEmpty(BossProfiles04D.MoveShortName(m)), Is.False, m.ToString());
            }
        }

        // ================================================================== fight rules

        [Test]
        public void PullIsBoundedAndNeverThroughWallsOrOnDrivers()
        {
            Assert.That(BossProfiles04D.PullVelocity(3f, false, false), Is.EqualTo(BossProfiles04D.PullSpeed));
            Assert.That(BossProfiles04D.PullVelocity(BossProfiles04D.PullRadius + 0.1f, false, false), Is.EqualTo(0f), "outside the pull radius");
            Assert.That(BossProfiles04D.PullVelocity(3f, true, false), Is.EqualTo(0f), "never through a wall");
            Assert.That(BossProfiles04D.PullVelocity(3f, false, true), Is.EqualTo(0f), "never drags a vehicle");
            Assert.That(BossProfiles04D.PullVelocity(0.1f, false, false), Is.EqualTo(0f), "nothing to pull at the centre");
            float carry = BossProfiles04D.PullDisplacement(BossProfiles04D.PullVelocity(3f, false, false));
            Assert.That(carry, Is.InRange(0.2f, 0.75f), "a short readable tug, not a vacuum");
            Assert.That(BossProfiles04D.PullDisplacement(-5f), Is.EqualTo(0f));
            Assert.That(BossProfiles04D.RuptureRadius + carry, Is.LessThan(BossProfiles04D.PullRadius), "a player at the pull's edge is not dragged into the rupture");
        }

        [Test]
        public void ExposedWindowAndPlatesScaleDamageTaken()
        {
            Assert.That(BossProfiles04D.ForemanBaseScale(true), Is.EqualTo(0.5f), "plates halve hits (original rule)");
            Assert.That(BossProfiles04D.ForemanBaseScale(false), Is.EqualTo(1.15f), "broken plates (original rule)");
            Assert.That(BossProfiles04D.DamageTakenScale(1f, false, 1.6f), Is.EqualTo(1f));
            Assert.That(BossProfiles04D.DamageTakenScale(1f, true, 1.6f), Is.EqualTo(1.6f).Within(Eps));
            Assert.That(BossProfiles04D.DamageTakenScale(0.5f, true, 1.6f), Is.EqualTo(0.8f).Within(Eps), "an open core still pays off behind the plates");
            Assert.That(BossProfiles04D.DamageTakenScale(1f, true, 0.4f), Is.EqualTo(1f), "a window never makes the boss tougher");
            Assert.That(BossProfiles04D.DamageTakenScale(-1f, true, 1.5f), Is.EqualTo(0f));
        }

        [Test]
        public void ChoirNumbersScaleWithDifficulty()
        {
            foreach (DifficultyLevel d in Levels)
            {
                int n = BossProfiles04D.ChoirCount(d);
                Assert.That(n, Is.InRange(3, 7));
                Assert.That(BossProfiles04D.ChoirMaxHits(d), Is.InRange(1, n - 1), d + ": most of a volley can never land");
                Assert.That(BossProfiles04D.ChoirTurnRate(d), Is.InRange(30f, 100f));
                Assert.That(BossProfiles04D.ChoirSteerSeconds(d), Is.InRange(0.5f, 1.2f));
            }
            Assert.That(BossProfiles04D.ChoirCount(DifficultyLevel.Story), Is.LessThan(BossProfiles04D.ChoirCount(DifficultyLevel.Veteran)));
            Assert.That(BossProfiles04D.ChoirTurnRate(DifficultyLevel.Story), Is.LessThan(BossProfiles04D.ChoirTurnRate(DifficultyLevel.Balanced)));
        }

        [Test]
        public void HighFlyersStayInReachButNotAcrossTheMap()
        {
            Assert.That(BossProfiles04D.ChoirLifeFor(5f), Is.EqualTo(BossProfiles04D.ChoirLife));
            Assert.That(BossProfiles04D.ChoirLifeFor(60f) * BossProfiles04D.ChoirSpeed, Is.GreaterThan(60f), "a shard can reach a flyer sixty metres away");
            Assert.That(BossProfiles04D.ChoirLifeFor(5000f), Is.EqualTo(BossProfiles04D.ChoirMaxLife));
            Assert.That(BossProfiles04D.BeamLengthFor(10f), Is.EqualTo(BossProfiles04D.BeamLength));
            Assert.That(BossProfiles04D.BeamLengthFor(60f), Is.GreaterThan(60f));
            Assert.That(BossProfiles04D.BeamLengthFor(5000f), Is.EqualTo(BossProfiles04D.BeamMaxLength));
            Assert.That(BossProfiles04D.Airborne(BossProfiles04D.AirHeight + 0.1f), Is.True);
            Assert.That(BossProfiles04D.Airborne(2.9f), Is.False, "ground rings still reach a player at 2.9 m");
        }

        [Test]
        public void RelocationNeedsALegalNewLane()
        {
            Vector3 center = Vector3.zero, player = new Vector3(0f, 0f, -10f), old = new Vector3(0f, 0f, 8f);
            Assert.That(BossProfiles04D.RelocateValid(new Vector3(14f, 0f, -2f), player, old, center, 50f), Is.True);
            Assert.That(BossProfiles04D.RelocateValid(new Vector3(2f, 0f, -12f), player, old, center, 50f), Is.False, "on top of the player");
            Assert.That(BossProfiles04D.RelocateValid(new Vector3(0f, 0f, 30f), player, old, center, 50f), Is.False, "too far from the player");
            Assert.That(BossProfiles04D.RelocateValid(new Vector3(3f, 0f, 9f), player, old, center, 50f), Is.False, "barely moved");
            Assert.That(BossProfiles04D.RelocateValid(new Vector3(46f, 0f, -10f), new Vector3(40f, 0f, 5f), old, center, 50f), Is.False, "outside the inner arena");
        }

        [Test]
        public void FocusPointsAndBeamCountsAreSane()
        {
            foreach (DifficultyLevel d in Levels) Assert.That(BossProfiles04D.FociCount(d), Is.InRange(3, 4));
            Assert.That(BossProfiles04D.BeamsPerPattern(0), Is.EqualTo(1), "phase one teaches one reflection");
            Assert.That(BossProfiles04D.BeamsPerPattern(1), Is.EqualTo(2));
            Assert.That(BossProfiles04D.BeamsPerPattern(2), Is.EqualTo(2), "never a beam wall");
            Assert.That(BossProfiles04D.FocusHealth(3400f), Is.EqualTo(136f).Within(Eps));
            Assert.That(BossProfiles04D.FocusHealth(100f), Is.EqualTo(90f), "a floor so a focus is a real target");
            Assert.That(BossProfiles04D.FocusHealth(3400f), Is.LessThan(3400f * 0.1f), "breaking a focus is quick next to the boss");
        }

        // ================================================================== cleanup

        [Test]
        public void EachWayOutClassifiesToItsOwnReason()
        {
            Assert.That(BossProfiles04D.ClassifyCleanup(true, true, true, true, true, true), Is.EqualTo(BossCleanup04D.BossDied), "a dead boss wins over every other signal");
            Assert.That(BossProfiles04D.ClassifyCleanup(false, true, true, false, false, false), Is.EqualTo(BossCleanup04D.Title), "session reset with the player gone");
            Assert.That(BossProfiles04D.ClassifyCleanup(false, true, false, true, true, true), Is.EqualTo(BossCleanup04D.Load), "session reset with the player placed again");
            Assert.That(BossProfiles04D.ClassifyCleanup(false, false, false, true, true, true), Is.EqualTo(BossCleanup04D.PlayerDied));
            Assert.That(BossProfiles04D.ClassifyCleanup(false, false, false, false, true, true), Is.EqualTo(BossCleanup04D.VeilExit));
            Assert.That(BossProfiles04D.ClassifyCleanup(false, false, false, false, false, true), Is.EqualTo(BossCleanup04D.PlayerLeft));
            Assert.That(BossProfiles04D.ClassifyCleanup(false, false, false, false, false, false), Is.EqualTo(BossCleanup04D.Removed));
        }

        [Test]
        public void OnlyABossDeathMayBeAVictory()
        {
            foreach (BossCleanup04D r in (BossCleanup04D[])System.Enum.GetValues(typeof(BossCleanup04D)))
                Assert.That(BossProfiles04D.CleanupMayBeVictory(r), Is.EqualTo(r == BossCleanup04D.BossDied), r.ToString());
        }

        private static BossOwnedLedger04D BusyLedger()
        {
            BossOwnedLedger04D l = new BossOwnedLedger04D();
            l.Open(BossOwned04D.Lane);
            l.Open(BossOwned04D.Lane);
            l.Open(BossOwned04D.Beam);
            l.Open(BossOwned04D.Bolt);
            l.Open(BossOwned04D.Orb);
            l.Open(BossOwned04D.Focus);
            l.Open(BossOwned04D.Decoy);
            l.Open(BossOwned04D.Support);
            l.Open(BossOwned04D.Illusion);
            l.Open(BossOwned04D.Arrival);
            return l;
        }

        [Test]
        public void EveryCleanupReleasesEverythingOnceAndRefusesLateArrivals()
        {
            BossCleanup04D[] reasons = { BossCleanup04D.BossDied, BossCleanup04D.PlayerLeft, BossCleanup04D.VeilExit, BossCleanup04D.PlayerDied, BossCleanup04D.Title, BossCleanup04D.Load, BossCleanup04D.Removed };
            foreach (BossCleanup04D r in reasons)
            {
                BossOwnedLedger04D l = BusyLedger();
                BossAddBudget04D b = new BossAddBudget04D(2, 1);
                Assert.That(b.TryReserveBatch(), Is.True, r + ": the arrival was scheduled");
                Assert.That(l.TotalLive, Is.EqualTo(10));
                Assert.That(l.Close(r), Is.True, r.ToString());
                b.Close();
                Assert.That(l.Reason, Is.EqualTo(r));
                Assert.That(l.ReleasedOnClose, Is.EqualTo(10), r + ": everything owned was released");
                Assert.That(l.TotalLive, Is.EqualTo(0));
                // The arrival ring bursts after the cleanup: the golem is refused.
                Assert.That(l.Open(BossOwned04D.Support), Is.False, r + ": nothing new after the owner is gone");
                Assert.That(b.Allowance(1, 0), Is.EqualTo(0), r.ToString());
                Assert.That(l.Close(BossCleanup04D.Removed), Is.False, r + ": the second cleanup does nothing");
                Assert.That(l.Reason, Is.EqualTo(r), "the first reason stands");
            }
        }

        [Test]
        public void SupportDeathReleasesItsEntryWithoutClosingTheFight()
        {
            BossOwnedLedger04D l = new BossOwnedLedger04D();
            BossAddBudget04D b = new BossAddBudget04D(2, 1);
            Assert.That(b.TryReserveBatch(), Is.True);
            l.Open(BossOwned04D.Support);
            l.Open(BossOwned04D.Support);
            b.NoteAdmitted(2);
            l.Release(BossOwned04D.Support);
            Assert.That(l.Live(BossOwned04D.Support), Is.EqualTo(1));
            Assert.That(l.Closed, Is.False, "a support's death is not the end of the fight");
            Assert.That(b.TryReserveBatch(), Is.False, "and it opens no new batch");
            l.Release(BossOwned04D.Support);
            l.Release(BossOwned04D.Support);
            Assert.That(l.Live(BossOwned04D.Support), Is.EqualTo(0), "never negative");
            Assert.That(l.Open(BossOwned04D.Lane), Is.True, "the fight goes on");
        }
    }
}
