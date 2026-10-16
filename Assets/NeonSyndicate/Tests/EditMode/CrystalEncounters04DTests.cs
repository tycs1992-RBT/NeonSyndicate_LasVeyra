using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04D finite encounters, engine-free: the state machine, the spawn budget and its single-use reservations, the
    /// victory predicate (a removal or a timeout is never a victory), abort and admission decisions, perception and
    /// leash intents, the owner-keyed local quiet with paused time excluded, the scoped monster-combat noise rule and
    /// the Resonance Breach site rules on the default city.
    /// </summary>
    public class CrystalEncounters04DTests
    {
        [SetUp]
        public void SetUp()
        {
            LocalQuiet.ResetStatics();
        }

        [TearDown]
        public void TearDown()
        {
            LocalQuiet.ResetStatics();
        }

        // ================================================================== state machine

        [Test]
        public void TheVictoryPathIsLegal()
        {
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Available, EncounterState.Preparing), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Preparing, EncounterState.Active), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Active, EncounterState.Victory), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Victory, EncounterState.Cleared), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Cleared, EncounterState.Preparing), Is.True, "an explicit replay");
        }

        [Test]
        public void TheFailurePathIsLegalAndEndsAvailableAfterRetry()
        {
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Active, EncounterState.Retreating), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Preparing, EncounterState.Retreating), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Retreating, EncounterState.Aborted), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Aborted, EncounterState.AvailableAfterRetry), Is.True);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.AvailableAfterRetry, EncounterState.Preparing), Is.True);
        }

        [Test]
        public void NoFailureOrTimeoutStateCanReachAReward()
        {
            EncounterState[] losing = { EncounterState.Retreating, EncounterState.Aborted, EncounterState.AvailableAfterRetry, EncounterState.Unavailable, EncounterState.Preparing };
            foreach (EncounterState s in losing)
            {
                Assert.That(EncounterRules04D.CanTransition(s, EncounterState.Victory), Is.False, s + " -> Victory");
                Assert.That(EncounterRules04D.CanTransition(s, EncounterState.Cleared) && s != EncounterState.Unavailable, Is.False, s + " -> Cleared");
            }
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Active, EncounterState.Cleared), Is.False, "victory must be declared first");
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Unavailable, EncounterState.Preparing), Is.False);
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Retreating, EncounterState.Active), Is.False);
        }

        [Test]
        public void OnlyRestingStatesMayStartAndOnlyRunningStatesHoldResources()
        {
            Assert.That(EncounterRules04D.CanStart(EncounterState.Available), Is.True);
            Assert.That(EncounterRules04D.CanStart(EncounterState.AvailableAfterRetry), Is.True);
            Assert.That(EncounterRules04D.CanStart(EncounterState.Cleared), Is.True);
            Assert.That(EncounterRules04D.CanStart(EncounterState.Aborted), Is.False, "the retry wait");
            Assert.That(EncounterRules04D.CanStart(EncounterState.Unavailable), Is.False);
            Assert.That(EncounterRules04D.CanStart(EncounterState.Active), Is.False);
            Assert.That(EncounterRules04D.IsRunning(EncounterState.Preparing), Is.True);
            Assert.That(EncounterRules04D.IsRunning(EncounterState.Active), Is.True);
            Assert.That(EncounterRules04D.IsRunning(EncounterState.Retreating), Is.True);
            Assert.That(EncounterRules04D.IsRunning(EncounterState.Cleared), Is.False);
            Assert.That(EncounterRules04D.IsRunning(EncounterState.Aborted), Is.False);
        }

        // ================================================================== budget and reservations

        [Test]
        public void ReservationsRespectTheTotalAndTheAliveCap()
        {
            EncounterBudget b = new EncounterBudget(4, EncounterRules04D.AliveCap(DifficultyLevel.Story));
            int t1 = b.Reserve(), t2 = b.Reserve(), t3 = b.Reserve();
            Assert.That(t1 > 0 && t2 > 0 && t3 > 0, Is.True);
            Assert.That(b.Reserve(), Is.EqualTo(0), "Story keeps three alive or reserved");
            Assert.That(b.Admit(t1) && b.Admit(t2) && b.Admit(t3), Is.True);
            Assert.That(b.Reserve(), Is.EqualTo(0), "three alive on Story");
            Assert.That(b.MarkDefeated(), Is.True);
            int t4 = b.Reserve();
            Assert.That(t4 > 0, Is.True, "one fell: the reinforcement fits");
            Assert.That(b.Admit(t4), Is.True);
            Assert.That(b.Remaining, Is.EqualTo(0));
            Assert.That(b.Reserve(), Is.EqualTo(0), "the authored total is spent");
        }

        [Test]
        public void BalancedAdmitsTheReinforcementWhileTheOpeningWaveLives()
        {
            EncounterBudget b = new EncounterBudget(EncounterRules04D.TotalOpponents, EncounterRules04D.AliveCap(DifficultyLevel.Balanced));
            for (int i = 0; i < EncounterRules04D.InitialWave; i++) Assert.That(b.Admit(b.Reserve()), Is.True);
            int r = b.Reserve();
            Assert.That(r > 0, Is.True);
            Assert.That(b.Admit(r), Is.True);
            Assert.That(b.Alive, Is.EqualTo(4));
            Assert.That(b.Alive <= b.AliveCap, Is.True);
        }

        [Test]
        public void TwoCallbacksCannotAdmitOneReservationTwice()
        {
            EncounterBudget b = new EncounterBudget(4, 4);
            int t = b.Reserve();
            Assert.That(b.Admit(t), Is.True);
            Assert.That(b.Admit(t), Is.False);
            Assert.That(b.Cancel(t), Is.False, "an admitted slot cannot be cancelled");
            Assert.That(b.Spawned, Is.EqualTo(1));
            Assert.That(b.Admit(0), Is.False);
            Assert.That(b.Admit(99), Is.False);
        }

        [Test]
        public void CancellingReleasesCapacityExactlyOnce()
        {
            EncounterBudget b = new EncounterBudget(2, 2);
            int a = b.Reserve(), c = b.Reserve();
            Assert.That(b.Reserve(), Is.EqualTo(0));
            Assert.That(b.Cancel(a), Is.True);
            Assert.That(b.Cancel(a), Is.False);
            Assert.That(b.Admit(a), Is.False, "a cancelled reservation never spawns");
            Assert.That(b.Reserved, Is.EqualTo(1));
            Assert.That(b.CancelAll(), Is.EqualTo(1));
            Assert.That(b.Reserved, Is.EqualTo(0));
            Assert.That(b.Admit(c), Is.False);
        }

        [Test]
        public void OnlyDeathsOfEveryAuthoredOpponentAreAVictory()
        {
            EncounterBudget b = new EncounterBudget(4, 4);
            for (int i = 0; i < 4; i++) b.Admit(b.Reserve());
            for (int i = 0; i < 3; i++) b.MarkDefeated();
            Assert.That(b.VictoryReady, Is.False, "one is still alive");
            b.MarkDefeated();
            Assert.That(b.VictoryReady, Is.True);
        }

        [Test]
        public void AMissingActorIsNeverAKill()
        {
            EncounterBudget b = new EncounterBudget(4, 4);
            for (int i = 0; i < 4; i++) b.Admit(b.Reserve());
            for (int i = 0; i < 3; i++) b.MarkDefeated();
            Assert.That(b.MarkRemoved(), Is.True);
            Assert.That(b.Defeated, Is.EqualTo(3));
            Assert.That(b.Alive, Is.EqualTo(0));
            Assert.That(b.VictoryReady, Is.False, "a removal ends nothing but the encounter");
        }

        [Test]
        public void APendingReservationOrAFailedSpawnBlocksVictory()
        {
            EncounterBudget b = new EncounterBudget(4, 4);
            for (int i = 0; i < 3; i++) b.Admit(b.Reserve());
            int pending = b.Reserve();
            for (int i = 0; i < 3; i++) b.MarkDefeated();
            Assert.That(b.VictoryReady, Is.False, "the reinforcement is still reserved");
            Assert.That(b.Admit(pending), Is.True);
            Assert.That(b.AdmitFailed(pending), Is.True);
            Assert.That(b.AdmitFailed(pending), Is.False);
            Assert.That(b.FailedSpawns, Is.EqualTo(1));
            Assert.That(b.VictoryReady, Is.False, "a failed body is not a defeat");
        }

        [Test]
        public void CompoundDeathCallbacksCannotOvercount()
        {
            EncounterBudget b = new EncounterBudget(1, 1);
            b.Admit(b.Reserve());
            Assert.That(b.MarkDefeated(), Is.True);
            Assert.That(b.MarkDefeated(), Is.False);
            Assert.That(b.MarkRemoved(), Is.False);
            Assert.That(b.Defeated, Is.EqualTo(1));
            Assert.That(b.VictoryReady, Is.True);
        }

        [Test]
        public void TheReinforcementIsCalledOnceAfterArrival()
        {
            Assert.That(EncounterRules04D.ReinforcementDue(false, false, 2, 99f), Is.False, "not before the player arrives");
            Assert.That(EncounterRules04D.ReinforcementDue(false, true, 0, 5f), Is.False);
            Assert.That(EncounterRules04D.ReinforcementDue(false, true, 1, 0f), Is.True, "the first fall calls it");
            Assert.That(EncounterRules04D.ReinforcementDue(false, true, 0, EncounterRules04D.ReinforceAfterSeconds), Is.True);
            Assert.That(EncounterRules04D.ReinforcementDue(true, true, 3, 99f), Is.False, "never a second batch");
        }

        // ================================================================== abort and admission

        [Test]
        public void AbortReasonsComeInPriorityOrderAndTimeoutIsNeverVictory()
        {
            EncounterRules04D.WatchFacts f = new EncounterRules04D.WatchFacts();
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.None));
            f.ApproachSeconds = EncounterRules04D.ApproachLimit + 1f;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.ApproachTimeout));
            f.Engaged = true;
            f.PlayerDistanceFromOrigin = EncounterRules04D.AbortRadius - 1f;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.None), "the approach clock stops after arrival");
            f.PlayerDistanceFromOrigin = EncounterRules04D.AbortRadius + 1f;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.LeftArea));
            f.ActorLost = true;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.ActorLost));
            f.OtherOwner = true;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.OtherCombatOwner));
            f.MissionActive = true;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.MissionStarted));
            f.VeilChanged = true;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.VeilCrossed));
            f.PlayerDown = true;
            Assert.That(EncounterRules04D.Check(f), Is.EqualTo(EncounterAbortReason.PlayerDowned));
            Assert.That(EncounterRules04D.CanTransition(EncounterState.Retreating, EncounterState.Victory), Is.False);
        }

        [Test]
        public void EveryAbortReasonHasAPlainLine()
        {
            foreach (EncounterAbortReason r in System.Enum.GetValues(typeof(EncounterAbortReason)))
                Assert.That(string.IsNullOrEmpty(EncounterRules04D.AbortLine(r)), Is.False, r.ToString());
        }

        private static EncounterRules04D.AdmissionFacts Clear()
        {
            return new EncounterRules04D.AdmissionFacts { Playing = true, SiteValid = true };
        }

        [Test]
        public void AdmissionIsAllowedOnlyWhenTheWorldIsQuiet()
        {
            Assert.That(EncounterRules04D.Refusal(Clear()), Is.Null);
            EncounterRules04D.AdmissionFacts f;
            f = Clear(); f.MissionActive = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("mission"));
            f = Clear(); f.WantedStars = 2; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("wanted"));
            f = Clear(); f.BossFight = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("boss"));
            f = Clear(); f.RaidActive = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("raid"));
            f = Clear(); f.CaptureActive = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("capture"));
            f = Clear(); f.DepotActive = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("depot"));
            f = Clear(); f.EventNear = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("world event"));
            f = Clear(); f.InVeil = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("Veil"));
            f = Clear(); f.RunActive = true; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("already"));
            f = Clear(); f.RetryWait = 12.2f; Assert.That(EncounterRules04D.Refusal(f), Does.Contain("13 s"));
            f = Clear(); f.PlayerDown = true; Assert.That(EncounterRules04D.Refusal(f), Is.Not.Null);
            f = Clear(); f.Playing = false; Assert.That(EncounterRules04D.Refusal(f), Is.Not.Null);
            f = Clear(); f.QaBusy = true; Assert.That(EncounterRules04D.Refusal(f), Is.Not.Null);
            f = Clear(); f.SiteValid = false; f.SiteReason = "blocked by a parked car"; Assert.That(EncounterRules04D.Refusal(f), Is.EqualTo("blocked by a parked car"));
        }

        [Test]
        public void EveryRefusalIsOneLine()
        {
            EncounterRules04D.AdmissionFacts f = Clear();
            f.MissionActive = true;
            f.WantedStars = 1;
            f.EventNear = true;
            string r = EncounterRules04D.Refusal(f);
            Assert.That(r.Contains("\n"), Is.False);
            Assert.That(r, Does.Contain("mission"), "the most important reason wins");
        }

        // ================================================================== perception and leash

        private static EncounterRules04D.ActorFacts Facts(ActorIntent04D intent)
        {
            return new EncounterRules04D.ActorFacts { Intent = intent, ReactionDelay = 0.6f, HasLastKnown = true, SinceSeen = 0f };
        }

        [Test]
        public void ASightingNeedsAReactionDelayBeforeTheFight()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Guard);
            f.Sees = true;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Acquire));
            f = Facts(ActorIntent04D.Acquire);
            f.Sees = true;
            f.IntentSeconds = 0.3f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Acquire));
            f.IntentSeconds = 0.61f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Engage));
            f.Sees = false;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Search), "lost sight while reacting: go look");
        }

        [Test]
        public void HearingOnlyStartsASearch()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Guard);
            f.Heard = true;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Search), "audio investigation, not aimed fire");
        }

        [Test]
        public void PerfectPursuitStopsAfterEightSecondsWithoutSight()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Engage);
            f.SinceSeen = EncounterRules04D.LostSightSeconds - 0.1f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Engage));
            f.SinceSeen = EncounterRules04D.LostSightSeconds;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Search));
            f.HasLastKnown = false;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return));
        }

        [Test]
        public void TheHardLeashAndThePursuitLimitAlwaysSendItHome()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Engage);
            f.Sees = true;
            f.DistanceFromOrigin = EncounterRules04D.HardLeash + 0.5f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return), "120 m from the origin");
            f.DistanceFromOrigin = 20f;
            f.Pursuit = EncounterRules04D.PursuitLimit;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return), "25 s of unrecovered pursuit");
            f = Facts(ActorIntent04D.Search);
            f.Pursuit = EncounterRules04D.PursuitLimit + 1f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return));
        }

        [Test]
        public void ASearchLooksBrieflyThenReturns()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Search);
            f.AtGoal = true;
            f.GoalSeconds = 1f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Search));
            f.GoalSeconds = EncounterRules04D.SearchLinger;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return));
            f = Facts(ActorIntent04D.Search);
            f.IntentSeconds = EncounterRules04D.SearchMax;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return), "a search that cannot arrive still ends");
            f = Facts(ActorIntent04D.Search);
            f.Sees = true;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Acquire));
        }

        [Test]
        public void AReturnDoesNotPingPongAtTheLeashEdge()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Return);
            f.Sees = true;
            f.TargetReachable = true;
            f.DistanceFromOrigin = 110f;
            f.TargetDistanceFromOrigin = 115f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return));
            f.DistanceFromOrigin = 60f;
            f.TargetDistanceFromOrigin = 70f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Acquire));
            f = Facts(ActorIntent04D.Return);
            f.AtGoal = true;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Guard));
            f = Facts(ActorIntent04D.Return);
            f.IntentSeconds = EncounterRules04D.ReturnMax;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Guard));
        }

        [Test]
        public void AReturnDoesNotReengageAnUnreachableTarget()
        {
            EncounterRules04D.ActorFacts f = Facts(ActorIntent04D.Return);
            f.Sees = true;
            f.TargetReachable = false;
            f.DistanceFromOrigin = 20f;
            f.TargetDistanceFromOrigin = 25f;
            Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Return), "a player on a roof watches it walk home");
        }

        [Test]
        public void WithdrawalOverridesEverything()
        {
            foreach (ActorIntent04D i in System.Enum.GetValues(typeof(ActorIntent04D)))
            {
                EncounterRules04D.ActorFacts f = Facts(i);
                f.Sees = true;
                f.Withdrawing = true;
                Assert.That(EncounterRules04D.NextIntent(f), Is.EqualTo(ActorIntent04D.Withdraw), i.ToString());
            }
        }

        [Test]
        public void UnrecoveredPursuitGrowsUntilAReachableSighting()
        {
            float p = 0f;
            for (int i = 0; i < 10; i++) p = EncounterRules04D.AccumulatePursuit(p, ActorIntent04D.Engage, false, true, 1f);
            Assert.That(p, Is.EqualTo(10f).Within(1e-4f));
            p = EncounterRules04D.AccumulatePursuit(p, ActorIntent04D.Engage, true, false, 1f);
            Assert.That(p, Is.EqualTo(11f).Within(1e-4f), "a flyer in sight but out of reach is not recovered");
            p = EncounterRules04D.AccumulatePursuit(p, ActorIntent04D.Search, true, true, 1f);
            Assert.That(p, Is.EqualTo(0f));
            Assert.That(EncounterRules04D.AccumulatePursuit(7f, ActorIntent04D.Return, false, false, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void GroundBodiesCannotReachAPlayerHigherThanAStep()
        {
            Assert.That(EncounterRules04D.Reachable(2f, 0f), Is.True);
            Assert.That(EncounterRules04D.Reachable(EncounterRules04D.MaxStepUp + 0.01f, 0f), Is.False, "no invisible stairs");
            Assert.That(EncounterRules04D.Reachable(-3f, 0f), Is.True);
        }

        [Test]
        public void ABeamsManySamplesCannotChainStun()
        {
            float last = -99f;
            int flinches = 0;
            // Ten beam samples a second for three seconds.
            for (int i = 0; i < 30; i++)
            {
                float now = i * 0.1f;
                if (EncounterRules04D.FlinchAllowed(now, last))
                {
                    flinches++;
                    last = now;
                }
            }
            Assert.That(flinches, Is.EqualTo(3), "one flinch per resistance window, not thirty");
        }

        [Test]
        public void PressureHalvesEachHalvingTime()
        {
            Assert.That(EncounterRules04D.DecayPressure(0.4f, 2.5f, 2.5f), Is.EqualTo(0.2f).Within(1e-4f));
            Assert.That(EncounterRules04D.DecayPressure(0.4f, 0f, 2.5f), Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(EncounterRules04D.DecayPressure(0.4f, 1f, 0f), Is.EqualTo(0f));
        }

        // ================================================================== local quiet

        [Test]
        public void AftermathTimeCountsOnlyActivePlayAndNeedsDeparture()
        {
            QuietLedger q = new QuietLedger();
            Vector3 c = new Vector3(100f, 0f, 100f);
            Assert.That(q.Acquire("win", QuietKind.Aftermath, c, 90f, 120f, 120f), Is.True);
            List<string> released = new List<string>();
            Vector3 inside = c + new Vector3(20f, 0f, 0f);
            q.Tick(60f, inside, true, released);
            for (int i = 0; i < 1000; i++) q.Tick(0f, inside, true, released);
            Assert.That(q.Find("win").Elapsed, Is.EqualTo(60f).Within(1e-3f), "paused frames add nothing");
            Assert.That(q.Blocks(c), Is.True);
            q.Tick(70f, inside, true, released);
            Assert.That(q.Holds("win"), Is.True, "time served, but the player never left");
            q.Tick(1f, c + new Vector3(110f, 0f, 0f), true, released);
            Assert.That(q.Holds("win"), Is.True, "beyond the radius but inside the departure ring");
            Assert.That(q.Tick(1f, c + new Vector3(121f, 0f, 0f), true, released), Is.EqualTo(1));
            Assert.That(released, Is.EquivalentTo(new[] { "win" }));
            Assert.That(q.Blocks(c), Is.False);
            Assert.That(q.Release("win"), Is.False, "released exactly once");
        }

        [Test]
        public void LeavingEarlyDoesNotShortenTheQuiet()
        {
            QuietLedger q = new QuietLedger();
            q.Acquire("win", QuietKind.Aftermath, Vector3.zero, 90f, 120f, 120f);
            Vector3 far = new Vector3(500f, 0f, 0f);
            q.Tick(119f, far, true, null);
            Assert.That(q.Holds("win"), Is.True);
            q.Tick(1f, far, true, null);
            Assert.That(q.Holds("win"), Is.False);
        }

        [Test]
        public void QuietIsOwnerKeyedAndOverlappingOwnersNeverClearEachOther()
        {
            QuietLedger q = new QuietLedger();
            Vector3 p = new Vector3(10f, 0f, 0f);
            Assert.That(q.Acquire("boss:golem_foreman", QuietKind.Aftermath, Vector3.zero, 90f, 120f, 120f), Is.True);
            Assert.That(q.Acquire("encounter:resonance_breach:aftermath", QuietKind.Aftermath, p, 90f, 120f, 120f), Is.True);
            Assert.That(q.Count, Is.EqualTo(2));
            Assert.That(q.Release("encounter:resonance_breach:aftermath"), Is.True);
            Assert.That(q.Blocks(p), Is.True, "the boss owner still holds the area");
            Assert.That(q.Release("encounter:resonance_breach:aftermath"), Is.False);
            Assert.That(q.Release("nobody"), Is.False);
            Assert.That(q.Count, Is.EqualTo(1));
        }

        [Test]
        public void AcquiringTheSameOwnerRefreshesInsteadOfStacking()
        {
            QuietLedger q = new QuietLedger();
            q.Acquire("win", QuietKind.Aftermath, Vector3.zero, 90f, 120f, 120f);
            q.Tick(100f, Vector3.zero, true, null);
            Assert.That(q.Acquire("win", QuietKind.Aftermath, Vector3.zero, 90f, 120f, 120f), Is.False);
            Assert.That(q.Count, Is.EqualTo(1));
            Assert.That(q.Find("win").Elapsed, Is.EqualTo(0f));
        }

        [Test]
        public void AdmissionLeasesNeverExpireByTimeAndOrphansAreSwept()
        {
            QuietLedger q = new QuietLedger();
            q.Acquire("encounter:resonance_breach:1", QuietKind.Admission, Vector3.zero, 140f, 0f, 140f);
            q.Acquire("encounter:resonance_breach:2", QuietKind.Admission, Vector3.zero, 140f, 0f, 140f);
            q.Acquire("other:keep", QuietKind.Admission, Vector3.zero, 50f, 0f, 50f);
            q.Tick(100000f, new Vector3(9999f, 0f, 0f), true, null);
            Assert.That(q.Count, Is.EqualTo(3));
            List<string> released = new List<string>();
            Assert.That(q.ReleaseKind(QuietKind.Admission, "encounter:", "encounter:resonance_breach:2", released), Is.EqualTo(1));
            Assert.That(released, Is.EquivalentTo(new[] { "encounter:resonance_breach:1" }));
            Assert.That(q.Holds("encounter:resonance_breach:2"), Is.True);
            Assert.That(q.Holds("other:keep"), Is.True, "another system's lease is never swept");
        }

        [Test]
        public void QuietCoversAFlatDiscAndIgnoresBadTime()
        {
            QuietLedger q = new QuietLedger();
            q.Acquire("a", QuietKind.Aftermath, Vector3.zero, 90f, 120f, 120f);
            Assert.That(q.Blocks(new Vector3(0f, 80f, 89f)), Is.True, "height does not matter");
            Assert.That(q.Blocks(new Vector3(0f, 0f, 91f)), Is.False);
            q.Tick(float.NaN, new Vector3(500f, 0f, 0f), true, null);
            q.Tick(float.PositiveInfinity, new Vector3(500f, 0f, 0f), true, null);
            q.Tick(-50f, new Vector3(500f, 0f, 0f), true, null);
            Assert.That(q.Holds("a"), Is.True);
            Assert.That(q.Find("a").Elapsed, Is.EqualTo(0f));
        }

        [Test]
        public void LocalQuietStaticWrapsTheLedgerAndResets()
        {
            Vector3 c = new Vector3(-210f, 0f, -68f);
            Assert.That(LocalQuiet.Blocks(c), Is.False);
            Assert.That(LocalQuiet.AcquireAftermath("encounter:resonance_breach:aftermath", c, 10f), Is.True);
            QuietLease l = LocalQuiet.Find("encounter:resonance_breach:aftermath");
            Assert.That(l.Radius, Is.EqualTo(LocalQuiet.QuietRadius), "never smaller than the standard quiet");
            Assert.That(l.DepartRadius, Is.EqualTo(LocalQuiet.QuietRadius + LocalQuiet.DepartMargin));
            Assert.That(l.Duration, Is.EqualTo(LocalQuiet.QuietSeconds));
            Assert.That(LocalQuiet.AcquireAdmission("encounter:resonance_breach:7", c, 140f), Is.True);
            Assert.That(LocalQuiet.Count, Is.EqualTo(2));
            Assert.That(LocalQuiet.Blocks(c + new Vector3(130f, 0f, 0f)), Is.True, "the admission lease covers the whole encounter area");
            LocalQuiet.ResetStatics();
            Assert.That(LocalQuiet.Count, Is.EqualTo(0));
            Assert.That(LocalQuiet.Blocks(c), Is.False);
        }

        [Test]
        public void TwentyEncounterCyclesLeaveNoLeasesOrReservationsBehind()
        {
            for (int cycle = 1; cycle <= 20; cycle++)
            {
                string owner = "encounter:resonance_breach:" + cycle;
                EncounterBudget b = new EncounterBudget(EncounterRules04D.TotalOpponents, 4);
                LocalQuiet.AcquireAdmission(owner, Vector3.zero, 140f);
                for (int i = 0; i < 3; i++) b.Admit(b.Reserve());
                int r = b.Reserve();
                if (cycle % 2 == 0)
                {
                    // Victory: the reinforcement arrives, everyone falls, the lease becomes the aftermath.
                    b.Admit(r);
                    for (int i = 0; i < 4; i++) b.MarkDefeated();
                    Assert.That(b.VictoryReady, Is.True);
                    Assert.That(LocalQuiet.Release(owner), Is.True);
                    LocalQuiet.AcquireAftermath("encounter:resonance_breach:aftermath", Vector3.zero, 90f);
                    LocalQuiet.Tick(121f, new Vector3(500f, 0f, 0f), true, null);
                }
                else
                {
                    // Abort: reservations go back, the bodies are removed (not defeated), the lease is released.
                    Assert.That(b.CancelAll(), Is.EqualTo(1));
                    for (int i = 0; i < 3; i++) b.MarkRemoved();
                    Assert.That(b.VictoryReady, Is.False);
                    Assert.That(LocalQuiet.Release(owner), Is.True);
                }
                Assert.That(b.Reserved, Is.EqualTo(0), "cycle " + cycle);
                Assert.That(b.Alive, Is.EqualTo(0), "cycle " + cycle);
                Assert.That(LocalQuiet.Count, Is.EqualTo(0), "cycle " + cycle);
            }
        }

        // ================================================================== scoped noise

        [Test]
        public void MonsterCombatNoiseNeedsALiveEncounterTheAreaAndProvenance()
        {
            float far = 999f;
            Assert.That(EncounterRules04D.NoiseExempt(false, 10f, 1f, 1f, 0f), Is.False, "no live encounter");
            Assert.That(EncounterRules04D.NoiseExempt(true, EncounterRules04D.NoiseAreaRadius + 1f, 1f, 1f, 0f), Is.False, "outside its area");
            Assert.That(EncounterRules04D.NoiseExempt(true, 30f, far, far, far), Is.False, "no provenance: still a crime");
            Assert.That(EncounterRules04D.NoiseExempt(true, 30f, 11f, far, far), Is.True, "landed near an owned opponent");
            Assert.That(EncounterRules04D.NoiseExempt(true, 30f, far, 2.5f, far), Is.True, "passed by an owned opponent");
            Assert.That(EncounterRules04D.NoiseExempt(true, 30f, far, far, 1.5f), Is.True, "the player just hurt one");
            Assert.That(EncounterRules04D.NoiseExempt(true, 30f, far, far, 2.5f), Is.False);
        }

        [Test]
        public void HarmingCiviliansStillCounts()
        {
            Assert.That(EncounterRules04D.CrimeExempt(true, true, false, true), Is.False, "a civilian victim is a crime even mid-fight");
            Assert.That(EncounterRules04D.CrimeExempt(false, true, false, true), Is.False);
            Assert.That(EncounterRules04D.CrimeExempt(true, true, true, false), Is.True, "hitting the contract's own creature");
            Assert.That(EncounterRules04D.CrimeExempt(true, false, false, true), Is.True, "victimless noise with provenance");
            Assert.That(EncounterRules04D.CrimeExempt(true, false, false, false), Is.False);
            Assert.That(EncounterRules04D.CrimeExempt(false, false, false, true), Is.False, "a carjack is never noise");
        }

        [Test]
        public void SegmentDistanceMeasuresToTheShotPath()
        {
            Vector3 a = Vector3.zero, b = new Vector3(10f, 0f, 0f);
            Assert.That(EncounterRules04D.SegmentDistance(a, b, new Vector3(5f, 0f, 2f)), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(EncounterRules04D.SegmentDistance(a, b, new Vector3(-3f, 0f, 4f)), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(EncounterRules04D.SegmentDistance(a, a, new Vector3(0f, 3f, 4f)), Is.EqualTo(5f).Within(1e-4f));
        }

        // ================================================================== difficulty and roster

        [Test]
        public void DifficultyKeepsTheSameRulesWithModestNumbers()
        {
            Assert.That(EncounterRules04D.AliveCap(DifficultyLevel.Story), Is.EqualTo(3));
            Assert.That(EncounterRules04D.AliveCap(DifficultyLevel.Balanced), Is.EqualTo(4));
            Assert.That(EncounterRules04D.AliveCap(DifficultyLevel.Veteran), Is.EqualTo(4));
            Assert.That(EncounterRules04D.ReactionDelay(DifficultyLevel.Story) > EncounterRules04D.ReactionDelay(DifficultyLevel.Veteran), Is.True);
            Assert.That(EncounterRules04D.WardenRecovery(DifficultyLevel.Story) > EncounterRules04D.WardenRecovery(DifficultyLevel.Veteran), Is.True);
            foreach (DifficultyLevel d in System.Enum.GetValues(typeof(DifficultyLevel)))
            {
                Assert.That(EncounterRules04D.RefractorCharge(d), Is.GreaterThanOrEqualTo(0.7f), "a readable telegraph on " + d);
                Assert.That(EncounterRules04D.LinkDamageTaken(d), Is.InRange(0.3f, 0.9f));
                Assert.That(EncounterRules04D.SkirmisherPause(d), Is.GreaterThanOrEqualTo(0.5f));
                Assert.That(EncounterRules04D.DamageScale(d), Is.InRange(0.5f, 1.2f));
                foreach (EncounterRoleKind04D r in System.Enum.GetValues(typeof(EncounterRoleKind04D)))
                    Assert.That(EncounterRules04D.HealthScale(r, d), Is.InRange(0.3f, 1.2f));
            }
            Assert.That(EncounterRules04D.AimLockFraction, Is.InRange(0.5f, 0.9f), "the aim locks before the release");
        }

        [Test]
        public void TheRosterIsFourAuthoredRolesOnExistingHumanoidSpecies()
        {
            Assert.That(EncounterRules04D.TotalOpponents, Is.EqualTo(4));
            Assert.That(EncounterRules04D.InitialWave, Is.EqualTo(3));
            Assert.That(EncounterRules04D.RoleAt(0), Is.EqualTo(EncounterRoleKind04D.Warden));
            Assert.That(EncounterRules04D.RoleAt(1), Is.EqualTo(EncounterRoleKind04D.Refractor));
            Assert.That(EncounterRules04D.RoleAt(2), Is.EqualTo(EncounterRoleKind04D.Skirmisher));
            Assert.That(EncounterRules04D.RoleAt(3), Is.EqualTo(EncounterRoleKind04D.Resonator), "the reinforcement");
            foreach (EncounterRoleKind04D r in System.Enum.GetValues(typeof(EncounterRoleKind04D)))
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(EncounterRules04D.SpeciesFor(r));
                Assert.That(sp, Is.Not.Null, r.ToString());
                Assert.That(sp.IsHumanoid, Is.True, "the overlay drives humanoid creatures: " + r);
                Assert.That(sp.Boss, Is.False);
                Assert.That(string.IsNullOrEmpty(EncounterRules04D.DisplayName(r)), Is.False);
            }
        }

        // ================================================================== the Resonance Breach site

        [Test]
        public void TheDefaultCityHasLegalBreachGroundNearTheLofts()
        {
            CityLayout city = TestData.City;
            Vector3 arrival;
            Assert.That(EncounterSiteRules04D.Arrival(city, out arrival), Is.True, "Veyra Lofts exists");
            List<Vector3> c = new List<Vector3>();
            EncounterSiteRules04D.Candidates(city, arrival, c);
            Assert.That(c.Count, Is.GreaterThan(0), "at least one open spot 160 to 320 m from the arrival");
            for (int i = 0; i < c.Count; i++)
            {
                float d = MathUtil.FlatDistance(c[i], arrival);
                Assert.That(EncounterSiteRules04D.InBand(d), Is.True, "candidate " + i + " at " + d);
                Assert.That(d, Is.GreaterThan(StarterSafetyRules.LoftsRadius), "outside the starter safety radius");
                Assert.That(EncounterSiteRules04D.InStarterZone(city, c[i]), Is.False);
                Assert.That(EncounterSiteRules04D.LayoutProblem(city, arrival, c[i], EncounterSiteRules04D.Clearance), Is.Null, "candidate " + i);
                Assert.That(city.IsInsideBuilding(c[i], EncounterSiteRules04D.Clearance), Is.False);
                Assert.That(city.NearAnyRoad(c[i], EncounterSiteRules04D.Clearance), Is.False);
                for (int k = 0; k < i; k++) Assert.That(MathUtil.FlatDistance(c[i], c[k]), Is.GreaterThanOrEqualTo(EncounterSiteRules04D.CandidateSpacing));
            }
        }

        [Test]
        public void SiteCandidatesAreDeterministic()
        {
            CityLayout city = TestData.City;
            Vector3 arrival;
            EncounterSiteRules04D.Arrival(city, out arrival);
            List<Vector3> a = new List<Vector3>(), b = new List<Vector3>();
            EncounterSiteRules04D.Candidates(city, arrival, a);
            EncounterSiteRules04D.Candidates(city, arrival, b);
            Assert.That(b, Is.EqualTo(a));
        }

        [Test]
        public void TheStarterZonesAreNeverBreachGround()
        {
            CityLayout city = TestData.City;
            Poi lofts = city.FindPoi("Veyra Lofts");
            Vector3 arrival;
            EncounterSiteRules04D.Arrival(city, out arrival);
            Assert.That(EncounterSiteRules04D.InStarterZone(city, lofts.Position), Is.True);
            Assert.That(EncounterSiteRules04D.InStarterZone(city, new Vector3(120f, 0f, 0f)), Is.True, "the promenade");
            Assert.That(EncounterSiteRules04D.LayoutProblem(city, arrival, lofts.Position, EncounterSiteRules04D.Clearance), Is.Not.Null);
            Assert.That(EncounterSiteRules04D.LayoutProblem(city, arrival, arrival + new Vector3(400f, 0f, 0f), 1f), Is.Not.Null, "out of the band");
        }

        [Test]
        public void TheAuthoredAnchorsFaceTheApproach()
        {
            Vector3 center = new Vector3(-210f, 0f, -68f);
            Vector3 approach = new Vector3(1f, 0f, 0f);
            List<Vector3> a = new List<Vector3>();
            EncounterSiteRules04D.Anchors(center, approach, a);
            Assert.That(a.Count, Is.EqualTo(5));
            Assert.That(Vector3.Dot(a[0] - center, approach), Is.GreaterThan(0f), "the warden stands in front");
            Assert.That(Vector3.Dot(a[1] - center, approach), Is.LessThan(0f), "the refractor stands at the back");
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(MathUtil.FlatDistance(a[i], center), Is.LessThan(EncounterSiteRules04D.Clearance + 1f), "anchor " + i + " inside the open radius");
                for (int k = 0; k < i; k++) Assert.That(MathUtil.FlatDistance(a[i], a[k]), Is.GreaterThan(3f), "anchors " + i + " and " + k + " apart");
            }
            Assert.That(EncounterSiteRules04D.Nudge(center, a[2], 0), Is.EqualTo(a[2]));
            for (int n = 1; n <= EncounterSiteRules04D.NudgeCount; n++)
                Assert.That(MathUtil.FlatDistance(EncounterSiteRules04D.Nudge(center, a[2], n), center), Is.LessThanOrEqualTo(MathUtil.FlatDistance(a[2], center) + 1e-3f));
        }
    }
}
