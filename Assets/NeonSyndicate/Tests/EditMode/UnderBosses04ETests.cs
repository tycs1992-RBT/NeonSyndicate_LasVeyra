using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E Underheart bosses, engine-free: profiles and phase thresholds (three phases for the principal bosses, two
    /// for the miniboss, each opened once), finite reinforcement budgets, telegraph and punish window timing, damage per
    /// part (a visible weakness, nothing ever invulnerable), the move choosers (a move that reaches a flyer, no input in
    /// what they see), first-win reward tokens exactly as the interface states with no duplicate on repeated callbacks,
    /// the arena boundary rules, the Matron's cooled plates, the reflector beam maths, the valve defense and escort
    /// state machines, and the procedural machine specs.
    /// </summary>
    public class UnderBosses04ETests
    {
        private static readonly DifficultyLevel[] Diffs = { DifficultyLevel.Story, DifficultyLevel.Balanced, DifficultyLevel.Veteran };

        [SetUp]
        public void SetUp()
        {
            UnderBossProfiles.ResetStatics();
        }

        // ================================================================== profiles and phases

        [Test]
        public void EveryBossIdHasAProfileAndNoExtraOnes()
        {
            Assert.That(UnderBossProfiles.All.Length, Is.EqualTo(UnderBossIds.All.Length));
            foreach (string id in UnderBossIds.All)
            {
                UnderBossProfile p = UnderBossProfiles.For(id);
                Assert.That(p, Is.Not.Null, id);
                Assert.That(p.Title, Is.EqualTo(UnderBossIds.Title(id)));
                Assert.That(p.BaseHealth, Is.GreaterThan(0f));
                Assert.That(p.BaseDamage, Is.GreaterThan(0f));
                Assert.That(UnderheartIds.HasAnchor(p.ArenaAnchor), Is.True, id + " arena anchor");
                if (!string.IsNullOrEmpty(p.GateAnchor)) Assert.That(UnderheartIds.HasAnchor(p.GateAnchor), Is.True, id + " gate anchor");
                Assert.That(p.PhaseNames.Length, Is.EqualTo(p.PhaseCount), id);
                Assert.That(string.IsNullOrEmpty(p.Lesson) || string.IsNullOrEmpty(p.Counter), Is.False, id);
            }
            Assert.That(UnderBossProfiles.For("glacier_titan"), Is.Null);
            Assert.That(UnderBossProfiles.For(null), Is.Null);
        }

        [Test]
        public void PrincipalBossesHaveThreePhasesAndTheMinibossTwo()
        {
            Assert.That(UnderBossProfiles.For(UnderBossIds.ValveGuardian).PhaseCount, Is.EqualTo(2));
            Assert.That(UnderBossProfiles.For(UnderBossIds.ValveGuardian).Principal, Is.False);
            foreach (string id in new[] { UnderBossIds.CinderMatron, UnderBossIds.Vaultbreaker, UnderBossIds.KilnRegent })
            {
                UnderBossProfile p = UnderBossProfiles.For(id);
                Assert.That(p.Principal, Is.True, id);
                Assert.That(p.PhaseCount, Is.EqualTo(3), id);
                Assert.That(p.PhaseThresholds[0], Is.GreaterThan(p.PhaseThresholds[1]), id + " highest first");
            }
        }

        [Test]
        public void PhasesOpenOnceEachAsHealthFalls()
        {
            UnderBossProfile p = UnderBossProfiles.For(UnderBossIds.CinderMatron);
            BossPhaseLatch04D latch = new BossPhaseLatch04D(p.PhaseThresholds);
            int skipped;
            Assert.That(latch.Evaluate(0.9f, out skipped), Is.EqualTo(0));
            Assert.That(latch.Evaluate(0.6f, out skipped), Is.EqualTo(1));
            Assert.That(latch.Evaluate(0.55f, out skipped), Is.EqualTo(0), "phase two does not reopen");
            Assert.That(latch.Evaluate(0.8f, out skipped), Is.EqualTo(0), "healing never reopens a phase");
            Assert.That(latch.Evaluate(0.3f, out skipped), Is.EqualTo(2));
            Assert.That(latch.Evaluate(0.01f, out skipped), Is.EqualTo(0));
            Assert.That(latch.Entered, Is.EqualTo(2));
            Assert.That(latch.Skipped, Is.EqualTo(0));
        }

        [Test]
        public void ABurstPastBothThresholdsEntersTheLastPhaseOnceAndCountsTheSkip()
        {
            UnderBossProfile p = UnderBossProfiles.For(UnderBossIds.KilnRegent);
            BossPhaseLatch04D latch = new BossPhaseLatch04D(p.PhaseThresholds);
            int skipped;
            Assert.That(latch.Evaluate(0.2f, out skipped), Is.EqualTo(2));
            Assert.That(skipped, Is.EqualTo(1));
            Assert.That(latch.Evaluate(0.5f, out skipped), Is.EqualTo(0), "the skipped phase never opens later");
        }

        [Test]
        public void TheMinibossLatchOpensItsSecondPhaseAtHalfHealth()
        {
            UnderBossProfile p = UnderBossProfiles.For(UnderBossIds.ValveGuardian);
            BossPhaseLatch04D latch = new BossPhaseLatch04D(p.PhaseThresholds);
            int skipped;
            Assert.That(latch.Evaluate(0.51f, out skipped), Is.EqualTo(0));
            Assert.That(latch.Evaluate(0.49f, out skipped), Is.EqualTo(1));
            Assert.That(UnderBossProfiles.PhaseOf(p, 0.49f), Is.EqualTo(1));
            Assert.That(UnderBossProfiles.PhaseOf(p, 0.51f), Is.EqualTo(0));
        }

        [Test]
        public void PhaseOfMatchesTheThresholds()
        {
            UnderBossProfile p = UnderBossProfiles.For(UnderBossIds.Vaultbreaker);
            Assert.That(UnderBossProfiles.PhaseOf(p, 1f), Is.EqualTo(0));
            Assert.That(UnderBossProfiles.PhaseOf(p, 0.65f), Is.EqualTo(1));
            Assert.That(UnderBossProfiles.PhaseOf(p, 0.32f), Is.EqualTo(2));
            Assert.That(UnderBossProfiles.PhaseOf(null, 0.1f), Is.EqualTo(0));
        }

        [Test]
        public void HealthGrowsWithDifficultyAndStaysPositive()
        {
            foreach (UnderBossProfile p in UnderBossProfiles.All)
            {
                float s = UnderBossProfiles.HealthFor(p, DifficultyLevel.Story);
                float b = UnderBossProfiles.HealthFor(p, DifficultyLevel.Balanced);
                float v = UnderBossProfiles.HealthFor(p, DifficultyLevel.Veteran);
                Assert.That(s, Is.LessThan(b), p.Id);
                Assert.That(b, Is.LessThan(v), p.Id);
                Assert.That(s, Is.GreaterThanOrEqualTo(p.BaseHealth / 3f), p.Id);
            }
            Assert.That(UnderBossProfiles.For(UnderBossIds.ValveGuardian).BaseHealth, Is.LessThan(UnderBossProfiles.For(UnderBossIds.CinderMatron).BaseHealth), "the miniboss is not the late boss at low health");
        }

        [Test]
        public void EveryPrincipalBossHasAtLeastTwoPunishWindows()
        {
            foreach (string id in UnderBossIds.All)
            {
                int n = UnderBossProfiles.PunishWindowCount(id);
                Assert.That(n, Is.GreaterThanOrEqualTo(2), id);
            }
        }

        // ================================================================== damage per part

        [Test]
        public void EveryBossHasAVisibleWeaknessThatOutdamagesItsHull()
        {
            UBossDamageState plain = new UBossDamageState();
            UBossDamageState open = new UBossDamageState { CoreOpen = true };
            float vgBody = UnderBossProfiles.PartMultiplier(UnderBossIds.ValveGuardian, UBossPart.Body, plain);
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.ValveGuardian, UBossPart.Weak, plain), Is.GreaterThan(vgBody));
            float maBody = UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Shell, plain);
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Weak, plain), Is.GreaterThan(maBody));
            float vbBody = UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Body, plain);
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Seam, plain), Is.GreaterThan(vbBody));
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Core, open), Is.GreaterThan(vbBody));
            float rgBody = UnderBossProfiles.PartMultiplier(UnderBossIds.KilnRegent, UBossPart.Body, plain);
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.KilnRegent, UBossPart.Regulator, open), Is.GreaterThan(rgBody));
        }

        [Test]
        public void NothingIsEverAnInvulnerableSponge()
        {
            UBossPart[] parts = (UBossPart[])System.Enum.GetValues(typeof(UBossPart));
            foreach (string id in UnderBossIds.All)
            {
                foreach (UBossPart part in parts)
                {
                    for (int mask = 0; mask < 32; mask++)
                    {
                        UBossDamageState s = new UBossDamageState
                        {
                            Lured = (mask & 1) != 0, ShellCracked = (mask & 2) != 0, CoreOpen = (mask & 4) != 0,
                            FurnaceShield = (mask & 8) != 0, PartBroken = (mask & 16) != 0
                        };
                        Assert.That(UnderBossProfiles.PartMultiplier(id, part, s), Is.GreaterThanOrEqualTo(UnderBossProfiles.MinimumMultiplier), id + " " + part + " " + mask);
                    }
                }
            }
        }

        [Test]
        public void TheFightsMechanicsChangeTheDamageTheWayTheHudSays()
        {
            UBossDamageState plain = new UBossDamageState();
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Weak, new UBossDamageState { Lured = true }),
                Is.LessThan(UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Weak, plain)), "lures protect her");
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Shell, new UBossDamageState { ShellCracked = true }),
                Is.GreaterThan(UnderBossProfiles.PartMultiplier(UnderBossIds.CinderMatron, UBossPart.Shell, plain)), "a quench cracks the shell");
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.FrontPlate, plain),
                Is.LessThan(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Body, plain)), "flanking beats the front plates");
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Core, plain),
                Is.LessThan(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Core, new UBossDamageState { CoreOpen = true })), "the hatch protects the core");
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Seam, new UBossDamageState { PartBroken = true }),
                Is.GreaterThan(UnderBossProfiles.PartMultiplier(UnderBossIds.Vaultbreaker, UBossPart.Seam, plain)), "a broken seam stays weak");
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.KilnRegent, UBossPart.Body, new UBossDamageState { FurnaceShield = true }),
                Is.LessThan(UnderBossProfiles.PartMultiplier(UnderBossIds.KilnRegent, UBossPart.Body, plain)), "burning furnaces feed it");
        }

        [Test]
        public void AnOpenWindowRaisesItsFocusPartToItsMultiplierAndOthersALittle()
        {
            UBossDamageState w = new UBossDamageState { WindowOpen = true, WindowFocus = UBossPart.Weak, WindowMul = 2.4f };
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.ValveGuardian, UBossPart.Weak, w), Is.EqualTo(2.4f).Within(1e-4f));
            float body = UnderBossProfiles.PartMultiplier(UnderBossIds.ValveGuardian, UBossPart.Body, new UBossDamageState());
            Assert.That(UnderBossProfiles.PartMultiplier(UnderBossIds.ValveGuardian, UBossPart.Body, w), Is.EqualTo(body * 1.25f).Within(1e-4f));
        }

        [Test]
        public void ElementsFollowEachProfile()
        {
            foreach (UnderBossProfile p in UnderBossProfiles.All)
            {
                Assert.That(UnderBossProfiles.ElementMultiplier(p, Element.None), Is.EqualTo(1f));
                if (p.Weakness != Element.None) Assert.That(UnderBossProfiles.ElementMultiplier(p, p.Weakness), Is.GreaterThan(1f), p.Id);
                if (p.Resists != Element.None) Assert.That(UnderBossProfiles.ElementMultiplier(p, p.Resists), Is.LessThan(1f).And.GreaterThan(0.5f), p.Id + " a resistance never makes a starter focus useless");
            }
        }

        // ================================================================== reinforcement budgets

        [Test]
        public void EachBossDeclaresAFiniteSmallReinforcementBudget()
        {
            Assert.That(UnderBossProfiles.For(UnderBossIds.ValveGuardian).DeclaredAdds, Is.EqualTo(2));
            Assert.That(UnderBossProfiles.For(UnderBossIds.CinderMatron).DeclaredAdds, Is.EqualTo(3));
            Assert.That(UnderBossProfiles.For(UnderBossIds.Vaultbreaker).DeclaredAdds, Is.EqualTo(4));
            Assert.That(UnderBossProfiles.For(UnderBossIds.KilnRegent).DeclaredAdds, Is.EqualTo(4));
            foreach (UnderBossProfile p in UnderBossProfiles.All)
            {
                Assert.That(p.MaxAddsAlive, Is.InRange(1, 3), p.Id);
                foreach (UBossAddBatch b in p.Batches)
                {
                    Assert.That(b.Phase, Is.InRange(1, p.PhaseCount - 1), p.Id + " batches arrive with a later phase, announced");
                    Assert.That(System.Array.IndexOf(EnemyIds04E.Hostile, b.Species), Is.GreaterThanOrEqualTo(0), p.Id + " " + b.Species);
                    Assert.That(string.IsNullOrEmpty(b.Announce), Is.False, p.Id);
                }
            }
        }

        [Test]
        public void TheAddLedgerNeverExceedsItsDeclaredTotalOrRefundsADeath()
        {
            AddLedger04E l = new AddLedger04E(4, 3, 2);
            Assert.That(l.Allow(10), Is.EqualTo(3), "at most three alive");
            l.NoteSpawned(3);
            Assert.That(l.Allow(1), Is.EqualTo(0));
            l.NoteDead(2);
            Assert.That(l.Allow(5), Is.EqualTo(1), "only the declared remainder");
            l.NoteSpawned(1);
            l.NoteDead(5);
            Assert.That(l.Dead, Is.EqualTo(4), "deaths never exceed spawns");
            Assert.That(l.Allow(1), Is.EqualTo(0), "a death never refunds the declared total");
            Assert.That(l.Spawned, Is.EqualTo(4));
        }

        [Test]
        public void BatchesFireOnceAndAClosedLedgerAdmitsNothing()
        {
            AddLedger04E l = new AddLedger04E(6, 3, 2);
            Assert.That(l.TryFireBatch(0), Is.True);
            Assert.That(l.TryFireBatch(0), Is.False);
            Assert.That(l.TryFireBatch(5), Is.False);
            l.Close();
            Assert.That(l.TryFireBatch(1), Is.False);
            Assert.That(l.Allow(2), Is.EqualTo(0));
        }

        [Test]
        public void ChoirWavesAreDeclaredAndMatchTheirMarks()
        {
            Assert.That(ChoirRules04E.WaveCount(ChoirRules04E.Valve), Is.EqualTo(3));
            Assert.That(ChoirRules04E.WaveCount(ChoirRules04E.Escort), Is.EqualTo(2));
            Assert.That(ChoirRules04E.WaveCount(ChoirRules04E.Reflectors), Is.EqualTo(0));
            Assert.That(ChoirRules04E.DeclaredAttackers(ChoirRules04E.Valve), Is.EqualTo(8));
            Assert.That(ChoirRules04E.DeclaredAttackers(ChoirRules04E.Escort), Is.EqualTo(5));
            foreach (string id in UnderBossIds.Mechanisms)
            {
                Assert.That(ChoirRules04E.Has(id), Is.True, id);
                for (int w = 0; w < ChoirRules04E.WaveCount(id); w++)
                    foreach (string sp in ChoirRules04E.WaveRoster(id, w)) Assert.That(System.Array.IndexOf(EnemyIds04E.Hostile, sp), Is.GreaterThanOrEqualTo(0), id + " " + sp);
            }
        }

        // ================================================================== timing

        [Test]
        public void EveryTelegraphIsLongEnoughToReadAndStoryIsLongest()
        {
            foreach (string id in UnderBossIds.All)
            {
                foreach (UBossMove m in UnderBossProfiles.MovesOf(id))
                {
                    float s = UBossTiming.Telegraph(m, DifficultyLevel.Story);
                    float b = UBossTiming.Telegraph(m, DifficultyLevel.Balanced);
                    float v = UBossTiming.Telegraph(m, DifficultyLevel.Veteran);
                    Assert.That(v, Is.GreaterThanOrEqualTo(UBossTiming.MinTell), id + " " + m);
                    Assert.That(s, Is.GreaterThanOrEqualTo(b), id + " " + m);
                    Assert.That(b, Is.GreaterThanOrEqualTo(v), id + " " + m);
                    Assert.That(string.IsNullOrEmpty(UnderBossProfiles.MoveLabel(m)), Is.False, m.ToString());
                }
            }
        }

        [Test]
        public void PunishWindowsStayInBoundsAndStoryIsLongerThanVeteran()
        {
            foreach (string id in UnderBossIds.All)
            {
                foreach (UBossMove m in UnderBossProfiles.MovesOf(id))
                {
                    float b = UBossTiming.BaseRecovery(m);
                    if (b <= 0f)
                    {
                        foreach (DifficultyLevel d in Diffs) Assert.That(UBossTiming.Recovery(m, d), Is.EqualTo(0f), m + " leaves no window");
                        continue;
                    }
                    Assert.That(UBossTiming.Recovery(m, DifficultyLevel.Story), Is.GreaterThan(UBossTiming.Recovery(m, DifficultyLevel.Veteran)), m.ToString());
                    foreach (DifficultyLevel d in Diffs)
                        Assert.That(UBossTiming.Recovery(m, d), Is.InRange(UBossTiming.MinWindow, UBossTiming.StoryMaxWindow), m + " " + d);
                }
                foreach (float s in UnderBossProfiles.SpecialWindows(id))
                    foreach (DifficultyLevel d in Diffs) Assert.That(UBossTiming.Window(s, d), Is.InRange(UBossTiming.MinWindow, UBossTiming.StoryMaxWindow), id);
            }
        }

        [Test]
        public void APunishWindowOpensClosesAndOnlyExtends()
        {
            PunishWindow04E w = new PunishWindow04E();
            Assert.That(w.IsOpen(0f), Is.False);
            w.Open(10f, 2f, 2.4f, UBossPart.Weak, "VALVE");
            Assert.That(w.IsOpen(10.5f), Is.True);
            Assert.That(w.IsOpen(12.01f), Is.False);
            Assert.That(w.Remaining01(10f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(w.Remaining01(11f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(w.Count, Is.EqualTo(1));
            w.Open(11f, 0.5f, 1.3f, UBossPart.Body, "SHORT");
            Assert.That(w.Until, Is.EqualTo(12f).Within(1e-4f), "a second trigger never shortens it");
            Assert.That(w.Multiplier, Is.EqualTo(2.4f).Within(1e-4f), "nor lowers its multiplier");
            Assert.That(w.Count, Is.EqualTo(1), "an extension is not a new window");
            w.Open(11.5f, 2f, 2.4f, UBossPart.Weak, "LONGER");
            Assert.That(w.Until, Is.EqualTo(13.5f).Within(1e-4f));
            w.Close();
            Assert.That(w.IsOpen(11.6f), Is.False);
            w.Open(20f, 0f, 2f, UBossPart.Weak, "NONE");
            Assert.That(w.IsOpen(20f), Is.False, "a zero-length window never opens");
        }

        [Test]
        public void MoveGapsShortenWithPhaseButNeverBelowTheFloor()
        {
            foreach (DifficultyLevel d in Diffs)
            {
                Assert.That(UBossTiming.MoveGap(0, d, false), Is.GreaterThanOrEqualTo(UBossTiming.MoveGap(2, d, false)));
                Assert.That(UBossTiming.MoveGap(2, d, true), Is.GreaterThanOrEqualTo(1.1f));
            }
        }

        // ================================================================== move choosers

        [Test]
        public void AFlyingPlayerAlwaysMeetsAMoveThatReachesTheAir()
        {
            foreach (string id in UnderBossIds.All)
            {
                for (int phase = 0; phase < 3; phase++)
                {
                    for (float roll = 0f; roll <= 1f; roll += 0.1f)
                    {
                        UBossView v = new UBossView { Phase = phase, Distance = 12f, Airborne = true, SinceSignature = 0, Heat = 10f };
                        UBossMove m = UnderBossProfiles.Choose(id, v, roll);
                        bool ok = UnderBossProfiles.ReachesAir(m) || (id == UnderBossIds.CinderMatron && m == UBossMove.MaCinderNova) || (id == UnderBossIds.KilnRegent && (m == UBossMove.RgFurnaces || m == UBossMove.RgBeamLanes || m == UBossMove.RgRegulatorLanes));
                        Assert.That(ok, Is.True, id + " phase " + phase + " roll " + roll + " chose " + m);
                    }
                }
            }
        }

        [Test]
        public void ChoosersOnlyPickTheirOwnMovesAndNeverNothing()
        {
            foreach (string id in UnderBossIds.All)
            {
                List<UBossMove> own = new List<UBossMove>(UnderBossProfiles.MovesOf(id));
                for (int phase = 0; phase < 3; phase++)
                {
                    for (float d = 2f; d < 30f; d += 4f)
                    {
                        for (float roll = 0f; roll <= 1f; roll += 0.125f)
                        {
                            UBossView v = new UBossView { Phase = phase, Distance = d, SinceSignature = (int)(roll * 4f), Heat = roll * 120f, PlateReady = roll > 0.5f, CycleNext = (int)(roll * 2f) % 2, Fused = roll > 0.9f };
                            UBossMove m = UnderBossProfiles.Choose(id, v, roll);
                            Assert.That(m, Is.Not.EqualTo(UBossMove.None), id);
                            Assert.That(own.Contains(m), Is.True, id + " chose " + m);
                        }
                    }
                }
            }
            Assert.That(UnderBossProfiles.Choose("nobody", new UBossView(), 0.5f), Is.EqualTo(UBossMove.None));
        }

        [Test]
        public void TheSignatureMechanicsAreForcedWhenTheirMomentComes()
        {
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.Vaultbreaker, new UBossView { Heat = VaultHeat.Max, Distance = 5f }, 0.1f), Is.EqualTo(UBossMove.VbOverheat));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.KilnRegent, new UBossView { Fused = true, Distance = 20f }, 0.9f), Is.EqualTo(UBossMove.RgDisruption));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.CinderMatron, new UBossView { Phase = 2, SinceSignature = 3, Distance = 9f }, 0.5f), Is.EqualTo(UBossMove.MaCinderNova));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.ValveGuardian, new UBossView { SinceSignature = 2, Distance = 9f }, 0.5f), Is.EqualTo(UBossMove.VgSteamJet));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.KilnRegent, new UBossView { Phase = 1, SinceSignature = 2, CycleNext = 0, Distance = 12f }, 0.5f), Is.EqualTo(UBossMove.RgFurnaces));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.KilnRegent, new UBossView { Phase = 1, SinceSignature = 2, CycleNext = 1, Distance = 12f }, 0.5f), Is.EqualTo(UBossMove.RgBeamLanes));
            Assert.That(UnderBossProfiles.Choose(UnderBossIds.KilnRegent, new UBossView { Phase = 1, SinceSignature = 2, CycleBusy = true, Distance = 12f }, 0.9f), Is.Not.EqualTo(UBossMove.RgFurnaces), "one cycle at a time");
        }

        [Test]
        public void WhatTheBossesSeeContainsNoPlayerInput()
        {
            string[] allowed = { "Phase", "Distance", "Airborne", "SinceSignature", "PlayerFlanking", "Heat", "PlateReady", "CycleNext", "CycleBusy", "Fused" };
            FieldInfo[] fields = typeof(UBossView).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo f in fields) Assert.That(System.Array.IndexOf(allowed, f.Name), Is.GreaterThanOrEqualTo(0), "UBossView." + f.Name + " is not an observable fact");
            Assert.That(fields.Length, Is.EqualTo(allowed.Length));
        }

        [Test]
        public void TheVaultbreakerOverheatsAfterAFewMovesAndSoonerInPhaseThree()
        {
            int Count(int phase)
            {
                float h = 0f;
                int n = 0;
                while (h < VaultHeat.Max && n < 50)
                {
                    h += VaultHeat.Add(UBossMove.VbHammer, phase);
                    n++;
                }
                return n;
            }
            Assert.That(Count(0), Is.InRange(3, 6));
            Assert.That(Count(2), Is.LessThan(Count(0)));
            Assert.That(VaultHeat.Add(UBossMove.VbOverheat, 0), Is.EqualTo(0f));
            Assert.That(VaultHeat.SeamThreshold(DifficultyLevel.Story), Is.LessThan(VaultHeat.SeamThreshold(DifficultyLevel.Veteran)));
        }

        [Test]
        public void AChargeQuenchesOnlyWhenItCrossesTheCooledPlate()
        {
            Vector3 plate = new Vector3(0f, 0f, 10f);
            Assert.That(UnderBossProfiles.ChargeCrossesPlate(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 20f), plate, 2.2f), Is.True);
            Assert.That(UnderBossProfiles.ChargeCrossesPlate(new Vector3(4f, 0f, 0f), new Vector3(4f, 0f, 20f), plate, 2.2f), Is.False, "beside the plate");
            Assert.That(UnderBossProfiles.ChargeCrossesPlate(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 6f), plate, 2.2f), Is.False, "stopped short");
            Assert.That(UnderBossProfiles.OnPlate(new Vector3(1f, 0.1f, 10.5f), plate, 2.6f, 2.5f), Is.True);
            Assert.That(UnderBossProfiles.OnPlate(new Vector3(3f, 0f, 10f), plate, 2.6f, 2.5f), Is.False);
            Assert.That(UnderBossProfiles.OnPlate(new Vector3(0f, 6f, 10f), plate, 2.6f, 2.5f), Is.False, "flying over it is not standing on it");
        }

        // ================================================================== rewards

        private sealed class FakeSink : IBossRewardSink04E
        {
            public readonly Dictionary<string, string> Tokens = new Dictionary<string, string>();
            public int Calls;
            public bool Refuse;
            public bool Has(string token) { return Tokens.ContainsKey(token); }

            public bool Grant(string rewardId, string token, string source)
            {
                Calls++;
                if (Refuse || Tokens.ContainsKey(token)) return false;
                Tokens[token] = rewardId;
                return true;
            }
        }

        [Test]
        public void FirstWinGrantsMatchTheInterfaceExactly()
        {
            UBossGrant[] g = new UBossGrant[UnderBossRewards.MaxGrants];
            Assert.That(UnderBossRewards.GrantsFor(UnderBossIds.ValveGuardian, g), Is.EqualTo(1));
            Assert.That(g[0].Reward, Is.EqualTo("uh_descent_piece"));
            Assert.That(g[0].Token, Is.EqualTo("boss04e:valve_guardian"));
            Assert.That(UnderBossRewards.GrantsFor(UnderBossIds.CinderMatron, g), Is.EqualTo(2));
            Assert.That(g[0].Reward, Is.EqualTo("uh_matron_aspect"));
            Assert.That(g[0].Token, Is.EqualTo("boss04e:cinder_matron"));
            Assert.That(g[1].Reward, Is.EqualTo("uh_matron_gear_choice"));
            Assert.That(g[1].Token, Is.EqualTo("boss04e:cinder_matron:gear"));
            Assert.That(UnderBossRewards.GrantsFor(UnderBossIds.Vaultbreaker, g), Is.EqualTo(1));
            Assert.That(g[0].Reward, Is.EqualTo("uh_vault_module"));
            Assert.That(g[0].Token, Is.EqualTo("boss04e:vaultbreaker"));
            Assert.That(UnderBossRewards.GrantsFor(UnderBossIds.KilnRegent, g), Is.EqualTo(2));
            Assert.That(g[0].Reward, Is.EqualTo("uh_regent_pinnacle"));
            Assert.That(g[0].Token, Is.EqualTo("boss04e:kiln_regent"));
            Assert.That(g[1].Reward, Is.EqualTo("uh_bond_awaken"));
            Assert.That(g[1].Token, Is.EqualTo("boss04e:kiln_regent:bond"));
            Assert.That(UnderBossRewards.GrantsFor("nobody", g), Is.EqualTo(0));
            Assert.That(UnderBossRewards.GrantsFor(UnderBossIds.KilnRegent, new UBossGrant[1]), Is.EqualTo(0), "too small an array is refused");
        }

        [Test]
        public void EveryGrantedRewardIsADefinedRewardId()
        {
            UBossGrant[] g = new UBossGrant[UnderBossRewards.MaxGrants];
            foreach (string id in UnderBossIds.All)
            {
                int n = UnderBossRewards.GrantsFor(id, g);
                for (int i = 0; i < n; i++)
                {
                    Assert.That(RewardIds04E.Has(g[i].Reward), Is.True, g[i].Reward);
                    Assert.That(g[i].Token.StartsWith(UnderBossIds.Token(id)), Is.True, g[i].Token);
                }
            }
        }

        [Test]
        public void RepeatedVictoryCallbacksNeverGrantTwice()
        {
            FakeSink sink = new FakeSink();
            int failed;
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.CinderMatron, sink, "test", out failed), Is.EqualTo(2));
            Assert.That(failed, Is.EqualTo(0));
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.CinderMatron, sink, "test", out failed), Is.EqualTo(0), "a repeated callback");
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.CinderMatron, sink, "rematch", out failed), Is.EqualTo(0), "a rematch");
            Assert.That(sink.Calls, Is.EqualTo(2), "held tokens are never offered again");
            Assert.That(sink.Tokens.Count, Is.EqualTo(2));
            Assert.That(UnderBossRewards.EverDefeated(UnderBossIds.CinderMatron, sink), Is.True);
            Assert.That(UnderBossRewards.EverDefeated(UnderBossIds.KilnRegent, sink), Is.False);
        }

        [Test]
        public void AQuestClaimWithTheSameTokenFirstLeavesNothingToGrant()
        {
            FakeSink sink = new FakeSink();
            sink.Tokens["boss04e:kiln_regent"] = "uh_regent_pinnacle";
            int failed;
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.KilnRegent, sink, "test", out failed), Is.EqualTo(1), "only the bond token was missing");
            Assert.That(sink.Tokens.ContainsKey("boss04e:kiln_regent:bond"), Is.True);
            Assert.That(sink.Tokens.Count, Is.EqualTo(2));
        }

        [Test]
        public void ARefusedGrantStaysRetryableAndThenLandsOnce()
        {
            FakeSink sink = new FakeSink { Refuse = true };
            int failed;
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.Vaultbreaker, sink, "test", out failed), Is.EqualTo(0));
            Assert.That(failed, Is.EqualTo(1));
            Assert.That(UnderBossRewards.EverDefeated(UnderBossIds.Vaultbreaker, sink), Is.False, "never claimed as earned while refused");
            sink.Refuse = false;
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.Vaultbreaker, sink, "retry", out failed), Is.EqualTo(1));
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.Vaultbreaker, sink, "retry", out failed), Is.EqualTo(0));
            Assert.That(UnderBossRewards.GrantFirstWin(UnderBossIds.Vaultbreaker, null, "none", out failed), Is.EqualTo(0));
        }

        [Test]
        public void TheVictoryLatchCommitsExactlyOnce()
        {
            VictoryLatch04E l = new VictoryLatch04E();
            Assert.That(l.Committed, Is.False);
            Assert.That(l.TryCommit(), Is.True);
            Assert.That(l.TryCommit(), Is.False);
            Assert.That(l.TryCommit(), Is.False);
            Assert.That(l.Committed, Is.True);
        }

        // ================================================================== arena boundary

        [Test]
        public void LeavingTheArenaWarnsThenResets()
        {
            ArenaWatch04E w = new ArenaWatch04E();
            Assert.That(w.Tick(0.1f, 10f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Inside));
            Assert.That(w.Tick(0.1f, 24f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Warning));
            for (int i = 0; i < 50; i++) Assert.That(w.Tick(0.1f, 24f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Warning), "still counting at " + w.OutsideFor);
            Assert.That(w.WarningLeft, Is.LessThan(1f));
            Assert.That(w.Tick(1f, 24f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Left));
            ArenaWatch04E back = new ArenaWatch04E();
            back.Tick(3f, 24f, 0f, 20f, false);
            Assert.That(back.Tick(0.1f, 15f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Inside));
            Assert.That(back.OutsideFor, Is.EqualTo(0f), "stepping back in clears the count");
        }

        [Test]
        public void FarOutOrFarAboveOrBelowLeavesAtOnce()
        {
            Assert.That(new ArenaWatch04E().Tick(0.016f, 20f + ArenaWatch04E.HardLeaveMargin + 1f, 0f, 20f, false), Is.EqualTo(ArenaVerdict.Left));
            Assert.That(new ArenaWatch04E().Tick(0.016f, 5f, ArenaWatch04E.VerticalLimit + 1f, 20f, false), Is.EqualTo(ArenaVerdict.Left));
            Assert.That(new ArenaWatch04E().Tick(0.016f, 5f, -ArenaWatch04E.VerticalLimit - 1f, 20f, false), Is.EqualTo(ArenaVerdict.Left));
        }

        [Test]
        public void HoldingAgainstTheSealWithdrawsAndLettingGoResets()
        {
            ArenaWatch04E w = new ArenaWatch04E();
            Assert.That(w.Tick(1f, 18f, 0f, 20f, true), Is.EqualTo(ArenaVerdict.Inside));
            w.Tick(0.1f, 18f, 0f, 20f, false);
            Assert.That(w.WithdrawFor, Is.EqualTo(0f));
            Assert.That(w.Tick(1.5f, 18f, 0f, 20f, true), Is.EqualTo(ArenaVerdict.Inside));
            Assert.That(w.Tick(1.1f, 18f, 0f, 20f, true), Is.EqualTo(ArenaVerdict.Withdraw));
        }

        [Test]
        public void TheFightStartsOnlyInsideTheBoundaryAtFloorHeight()
        {
            Assert.That(ArenaWatch04E.InsideToStart(19f, 0.5f, 20f), Is.True);
            Assert.That(ArenaWatch04E.InsideToStart(21f, 0f, 20f), Is.False);
            Assert.That(ArenaWatch04E.InsideToStart(5f, 12f, 20f), Is.False, "a street above the arena is not inside it");
        }

        // ================================================================== reflector beam maths

        [Test]
        public void MirrorsTurnPassOrReverseAnAxisBeam()
        {
            Vector2 up = new Vector2(0f, 1f);
            Assert.That(Near(ReflectorPuzzle04E.Reflect(up, 1), new Vector2(1f, 0f)), Is.True, "45 degrees turns +z to +x");
            Assert.That(Near(ReflectorPuzzle04E.Reflect(up, 3), new Vector2(-1f, 0f)), Is.True, "135 degrees turns +z to -x");
            Assert.That(Near(ReflectorPuzzle04E.Reflect(up, 0), new Vector2(0f, -1f)), Is.True, "a mirror across the beam sends it back");
            Assert.That(Near(ReflectorPuzzle04E.Reflect(up, 2), up), Is.True, "a mirror along the beam lets it pass");
            Assert.That(Near(ReflectorPuzzle04E.Reflect(new Vector2(1f, 0f), 3), new Vector2(0f, -1f)), Is.True);
            Assert.That(Near(ReflectorPuzzle04E.Reflect(new Vector2(0f, -1f), 3), new Vector2(1f, 0f)), Is.True);
            Assert.That(Near(ReflectorPuzzle04E.MirrorNormal(4), ReflectorPuzzle04E.MirrorNormal(0)), Is.True, "orientations wrap");
            Assert.That(Near(ReflectorPuzzle04E.MirrorNormal(-1), ReflectorPuzzle04E.MirrorNormal(3)), Is.True);
        }

        private static bool Near(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude < 1e-6f;
        }

        [Test]
        public void TheCrownPuzzleStartsUnsolvedAndHasExactlyOneSolution()
        {
            ReflectorPuzzle04E p = ReflectorPuzzle04E.CrownLayout();
            Assert.That(p.Solved, Is.False);
            Assert.That(p.CountSolutions(), Is.EqualTo(1));
            Assert.That(p.Solved, Is.False, "counting restores the player's orientations");
            p.Orientation = (int[])p.Solution.Clone();
            List<Vector2> path = new List<Vector2>();
            int last;
            Assert.That(p.Trace(path, out last), Is.EqualTo(BeamEnd.Receptor));
            Assert.That(path.Count, Is.EqualTo(6), "emitter, A, B, C, D, receptor");
            Assert.That(Near(path[path.Count - 1], p.ReceptorPos), Is.True);
            Assert.That(last, Is.EqualTo(3), "the last bend is at D");
        }

        [Test]
        public void TurningAReflectorFourTimesBringsItBack()
        {
            ReflectorPuzzle04E p = ReflectorPuzzle04E.CrownLayout();
            int before = p.Orientation[2];
            for (int i = 0; i < 4; i++) Assert.That(p.Rotate(2), Is.True);
            Assert.That(p.Orientation[2], Is.EqualTo(before));
            Assert.That(p.Rotate(9), Is.False);
            Assert.That(p.Rotate(-1), Is.False);
        }

        [Test]
        public void TheBeamEndsOnPillarsAndNeverLoopsForever()
        {
            ReflectorPuzzle04E p = ReflectorPuzzle04E.CrownLayout();
            int last;
            Assert.That(p.Trace(null, out last), Is.EqualTo(BeamEnd.Blocked), "the scrambled start sends the beam back into the emitter housing");
            p.Orientation = new[] { 1, 3, 2, 0 };
            Assert.That(p.Trace(null, out last), Is.EqualTo(BeamEnd.Blocked), "C along the beam lets it hit the pillar");
            p.Orientation = new[] { 1, 0, 0, 0 };
            Assert.That(p.Trace(null, out last), Is.EqualTo(BeamEnd.Escaped), "B along the beam lets it leave the room");
            // Every one of the 256 states ends in finite steps with a known ending.
            int states = 0;
            for (int s = 0; s < 256; s++)
            {
                int v = s;
                for (int i = 0; i < 4; i++)
                {
                    p.Orientation[i] = v % 4;
                    v /= 4;
                }
                List<Vector2> path = new List<Vector2>();
                BeamEnd e = p.Trace(path, out last);
                Assert.That(path.Count, Is.LessThanOrEqualTo(ReflectorPuzzle04E.MaxBounces + 3));
                if (e == BeamEnd.Receptor) states++;
            }
            Assert.That(states, Is.EqualTo(1));
        }

        [Test]
        public void AClosedRingOfMirrorsStillEndsInFiniteSteps()
        {
            ReflectorPuzzle04E p = new ReflectorPuzzle04E();
            p.EmitterPos = new Vector2(0f, -5f);
            p.EmitterDir = new Vector2(0f, 1f);
            p.ReceptorPos = new Vector2(50f, 50f);
            p.Mirrors.Add(new Vector2(0f, 0f));
            p.Mirrors.Add(new Vector2(6f, 0f));
            p.Mirrors.Add(new Vector2(6f, 6f));
            p.Mirrors.Add(new Vector2(0f, 6f));
            p.Orientation = new int[4];
            for (int s = 0; s < 256; s++)
            {
                int v = s;
                for (int i = 0; i < 4; i++)
                {
                    p.Orientation[i] = v % 4;
                    v /= 4;
                }
                List<Vector2> path = new List<Vector2>();
                int last;
                BeamEnd e = p.Trace(path, out last);
                Assert.That(e, Is.Not.EqualTo(BeamEnd.Receptor), "no receptor in reach");
                Assert.That(path.Count, Is.LessThanOrEqualTo(ReflectorPuzzle04E.MaxBounces + 3), "state " + s);
            }
        }

        // ================================================================== valve defense

        [Test]
        public void TheValveWavesArriveAtTheirMarksOnceEach()
        {
            ValveOperation04E op = new ValveOperation04E(DifficultyLevel.Balanced);
            Assert.That(op.Tick(1f, true, 0), Is.EqualTo(-1), "nothing before the player starts it");
            op.Start();
            List<int> waves = new List<int>();
            for (int i = 0; i < 600; i++)
            {
                int w = op.Tick(0.1f, true, 0);
                if (w >= 0) waves.Add(w);
            }
            Assert.That(waves, Is.EqualTo(new List<int> { 0, 1, 2 }));
            Assert.That(op.FullyOpen, Is.True);
            Assert.That(op.Done(1), Is.False, "attackers still stand");
            Assert.That(op.Done(0), Is.True);
        }

        [Test]
        public void TheValveOnlyOpensWhileThePlayerIsNear()
        {
            ValveOperation04E op = new ValveOperation04E(DifficultyLevel.Story);
            op.Start();
            op.Tick(5f, true, 0);
            float p = op.Progress;
            op.Tick(5f, false, 0);
            Assert.That(op.Progress, Is.EqualTo(p), "paused while away");
            Assert.That(op.Seconds, Is.LessThan(new ValveOperation04E(DifficultyLevel.Veteran).Seconds));
        }

        [Test]
        public void ASeizedValveResetsTheMechanismNotTheQuest()
        {
            ValveOperation04E op = new ValveOperation04E(DifficultyLevel.Balanced);
            op.Start();
            op.Tick(10f, true, 0);
            int r = -1;
            for (int i = 0; i < 100 && r != -2; i++) r = op.Tick(0.5f, true, 3);
            Assert.That(r, Is.EqualTo(-2));
            Assert.That(op.Started, Is.False);
            Assert.That(op.Progress, Is.EqualTo(0f));
            Assert.That(op.Integrity, Is.EqualTo(ValveOperation04E.MaxIntegrity));
            Assert.That(op.WavesDue, Is.EqualTo(0), "the waves re-arm for the next attempt");
            Assert.That(op.Resets, Is.EqualTo(1));
            op.Start();
            Assert.That(op.Tick(0.1f, true, 0), Is.EqualTo(0), "the first wave comes again on the restart");
        }

        // ================================================================== escort

        [Test]
        public void TheConstructWaitsForThePlayerAndArrives()
        {
            EscortRun04E run = new EscortRun04E(22f);
            run.Tick(2f, 30f, 0);
            Assert.That(run.Distance, Is.EqualTo(0f));
            Assert.That(run.Waiting, Is.True);
            List<int> waves = new List<int>();
            for (int i = 0; i < 400 && !run.Arrived; i++)
            {
                int w = run.Tick(0.1f, 4f, 0);
                if (w >= 0) waves.Add(w);
            }
            Assert.That(run.Arrived, Is.True);
            Assert.That(waves, Is.EqualTo(new List<int> { 0, 1 }));
            Assert.That(run.Tick(0.1f, 4f, 0), Is.EqualTo(-1), "nothing after arriving");
        }

        [Test]
        public void ABrokenConstructRebootsAtTheStart()
        {
            EscortRun04E run = new EscortRun04E(30f);
            for (int i = 0; i < 50; i++) run.Tick(0.1f, 3f, 0);
            Assert.That(run.Distance, Is.GreaterThan(5f));
            int r = -1;
            for (int i = 0; i < 200 && r != -2; i++) r = run.Tick(0.1f, 3f, 2);
            Assert.That(r, Is.EqualTo(-2));
            Assert.That(run.Distance, Is.EqualTo(0f));
            Assert.That(run.Health, Is.EqualTo(EscortRun04E.MaxHealth));
            Assert.That(run.WavesDue, Is.EqualTo(0));
            Assert.That(run.Resets, Is.EqualTo(1));
        }

        // ================================================================== bodies

        private static int CountHit(MachineSpec s, UBossPart part)
        {
            int n = 0;
            foreach (MachinePartSpec p in s.Parts) if (p.Collider != MachineCollider.None && p.Hit == part) n++;
            return n;
        }

        private static void CheckSpec(MachineSpec s, string name)
        {
            Assert.That(s.Parts.Count, Is.GreaterThan(0), name);
            for (int i = 0; i < s.Parts.Count; i++)
            {
                MachinePartSpec p = s.Parts[i];
                Assert.That(p.Parent, Is.LessThan(i), name + " " + p.Name + " parents come first");
                if (p.Collider == MachineCollider.Box) Assert.That(p.ColliderSize.x * p.ColliderSize.y * p.ColliderSize.z, Is.GreaterThan(0f), name + " " + p.Name);
                if (p.Collider == MachineCollider.Sphere || p.Collider == MachineCollider.Capsule) Assert.That(p.ColliderSize.x, Is.GreaterThan(0f), name + " " + p.Name);
            }
            Assert.That(s.VertexCount, Is.GreaterThan(100), name + " has real geometry");
        }

        [Test]
        public void TheValveGuardianHasItsValveWeakPoint()
        {
            MachineSpec s = UnderBossShapes04E.ValveGuardian();
            CheckSpec(s, "valve guardian");
            Assert.That(CountHit(s, UBossPart.Weak), Is.EqualTo(1));
            Assert.That(s.Part("Valve").Glow, Is.True);
            Assert.That(s.Height, Is.InRange(3f, 4.5f));
            Assert.That(s.Part("Valve").Pivot.z, Is.LessThan(0f), "the valve wheel is on its back");
        }

        [Test]
        public void TheVaultbreakerHasPlatesSeamsAndACoreBehindHatches()
        {
            MachineSpec s = UnderBossShapes04E.Vaultbreaker();
            CheckSpec(s, "vaultbreaker");
            Assert.That(CountHit(s, UBossPart.Seam), Is.EqualTo(3));
            Assert.That(CountHit(s, UBossPart.Core), Is.EqualTo(1));
            Assert.That(CountHit(s, UBossPart.FrontPlate), Is.EqualTo(3), "the sloped plates and two hatch doors");
            Assert.That(CountHit(s, UBossPart.Limb), Is.GreaterThanOrEqualTo(6), "four legs and two arms");
            Assert.That(s.Part("HatchL").Pivot.z, Is.GreaterThan(s.Part("Core").Pivot.z), "the doors sit in front of the core");
            Assert.That(s.Part("SeamL").Pivot.x, Is.LessThan(0f));
            Assert.That(s.Part("SeamR").Pivot.x, Is.GreaterThan(0f));
            Assert.That(s.Part("SeamRear").Pivot.z, Is.LessThan(0f));
        }

        [Test]
        public void TheKilnRegentHasACrownACagedRegulatorAndAPedestal()
        {
            MachineSpec s = UnderBossShapes04E.KilnRegent();
            CheckSpec(s, "kiln regent");
            Assert.That(CountHit(s, UBossPart.Crown), Is.EqualTo(1));
            Assert.That(CountHit(s, UBossPart.Regulator), Is.EqualTo(1));
            Assert.That(s.Part("Cage").Pivot.z, Is.GreaterThan(s.Part("Regulator").Pivot.z), "the cage stands in front of the regulator");
            Assert.That(s.Height, Is.GreaterThan(6f), "it towers over the player");
            Assert.That(s.Part("Pedestal").Parent, Is.EqualTo(-1));
        }

        [Test]
        public void ArenaPropsCarryTheirShootableParts()
        {
            CheckSpec(UnderBossShapes04E.Furnace(), "furnace");
            Assert.That(UnderBossShapes04E.Furnace().Part("Vent"), Is.Not.Null);
            CheckSpec(UnderBossShapes04E.Lure(), "lure");
            Assert.That(UnderBossShapes04E.Lure().Part("Crystal").Glow, Is.True);
            CheckSpec(UnderBossShapes04E.CooledPlate(2.6f), "plate");
            CheckSpec(UnderBossShapes04E.EscortConstruct(), "construct");
            CheckSpec(UnderBossShapes04E.ValveMechanism(), "valve");
            CheckSpec(UnderBossShapes04E.ReflectorStand(), "reflector");
            CheckSpec(UnderBossShapes04E.BeamPost(false), "emitter");
            CheckSpec(UnderBossShapes04E.BeamPost(true), "receptor");
        }

        [Test]
        public void TheMatronBodyIsAHeavyBeastWithAWeakPoint()
        {
            CreatureBody body = CreatureBuilder.Build(CinderMatronFight.Look(), 0);
            Assert.That(body.Height, Is.InRange(1.8f, 4.5f));
            bool weak = false;
            foreach (CreatureHitVolume h in body.HitVolumes) if (h.WeakPoint) weak = true;
            Assert.That(weak, Is.True);
            Assert.That(body.HitVolumes.Count, Is.GreaterThan(8));
            Assert.That(body.Skeleton.Legs.Length, Is.EqualTo(4));
        }
    }
}
