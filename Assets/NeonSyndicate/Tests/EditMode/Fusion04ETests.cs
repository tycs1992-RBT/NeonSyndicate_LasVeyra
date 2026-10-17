using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Underheart and Bonds 04E, FUSION stream: the pure fusion rules (validation order and refusals, the single legal
    /// state machine, rollback and recovery, the no-refill invariants under toggling, explicit damage multipliers and
    /// handling), the garage rules (normalizing old or broken saves, registration, delivery health, trims never
    /// stacking), the form geometry (every form on every bike class fits its envelope and never enters the rider's
    /// space; Low keeps the silhouette), Jax's bay and courses on the real default city, and the save field freezes.
    /// </summary>
    public sealed class Fusion04ETests
    {
        // ================================================================== fakes for the machine's two ports

        private sealed class FakeCompanion : IFusionCompanionPort
        {
            public CompanionSnapshot State = new CompanionSnapshot { Uid = 7, Health01 = 0.4f, AssistCharge = 0.25f, AssistCooldown = 3f };
            public bool Hidden;
            public int HideCalls, ShowCalls, RestoreCalls;
            public bool ThrowOnHide;

            public CompanionSnapshot Snapshot(int uid) { return State; }

            public void SetFused(int uid, bool fused)
            {
                if (fused)
                {
                    HideCalls++;
                    if (ThrowOnHide) throw new InvalidOperationException("hide failed");
                    Hidden = true;
                    // While hidden, the game may try to change the companion: the restore must undo it.
                    State.AssistCharge = 1f;
                    State.AssistCooldown = 0f;
                    State.Health01 = 1f;
                }
                else
                {
                    ShowCalls++;
                    Hidden = false;
                }
            }

            public void Restore(CompanionSnapshot s)
            {
                RestoreCalls++;
                State = s;
            }
        }

        private sealed class FakeForm : IFusionFormPort
        {
            public bool Fail, Throw;
            public bool Built;
            public int Builds, Destroys;
            public float LastUnfold = -1f;

            public bool Build(string form, out string error)
            {
                Builds++;
                if (Throw) throw new InvalidOperationException("mesh failed");
                if (Fail) { error = "Not enough room."; return false; }
                Built = true;
                error = "";
                return true;
            }

            public void Show(float unfold) { LastUnfold = unfold; }

            public void Destroy()
            {
                Destroys++;
                Built = false;
            }
        }

        private static FusionFacts OkFacts()
        {
            return new FusionFacts
            {
                HasGarage = true, HasBonds = true, PlayerAlive = true, PlayerControllable = true, InVehicle = true, IsDriver = true,
                IsBike = true, IsPolice = false, IsPersonal = true, PersonalReason = "", VehicleDestroyed = false, OnFire = false, Upright = true,
                VehicleHealth01 = 0.9f, GroundedRays = 4, State = FusionState.Separate, CooldownLeft = 0f,
                StoryMission = false, MissionForbidsVehicles = false, VehicleMissionCritical = false, OtherJob = false, InVeil = false,
                HasCompanion = true, CompanionArchetype = BondIds.QuartzGuide, CompanionName = "Glint", CompanionRank = BondIds.RankAttuned,
                CompanionDowned = false, BondsCanFuse = true, BondsReason = "", FusionUnlocked = true, FormUnlocked = true,
                FirstFusionQuest = false, InBay = false, Passengers = 0, Speed = 0.2f, Underground = false, VehicleSpace = true,
                Headroom = 0f, RequiredHeadroom = 2.2f
            };
        }

        private static FusionVerdict OkVerdict(string form)
        {
            return new FusionVerdict { Ok = true, Code = FusionDenial.None, Message = "", Form = form };
        }

        // ================================================================== validation

        [Test]
        public void ValidationAllowsTheReadyCase()
        {
            FusionVerdict v = FusionRules.Validate(OkFacts());
            Assert.That(v.Ok, Is.True, v.Message);
            Assert.That(v.Form, Is.EqualTo(FusionIds.Ventwing));
            Assert.That(v.FirstFusion, Is.False);
            Assert.That(v.CalibratesForm, Is.False);
        }

        [Test]
        public void ValidationRefusesEachMissingConditionWithAReason()
        {
            List<KeyValuePair<FusionDenial, Func<FusionFacts, FusionFacts>>> cases = new List<KeyValuePair<FusionDenial, Func<FusionFacts, FusionFacts>>>
            {
                Case(FusionDenial.NoGarage, f => { f.HasGarage = false; return f; }),
                Case(FusionDenial.NoBonds, f => { f.HasBonds = false; return f; }),
                Case(FusionDenial.PlayerUnavailable, f => { f.PlayerAlive = false; return f; }),
                Case(FusionDenial.NotInVehicle, f => { f.InVehicle = false; return f; }),
                Case(FusionDenial.NotDriver, f => { f.IsDriver = false; return f; }),
                Case(FusionDenial.NotABike, f => { f.IsBike = false; return f; }),
                Case(FusionDenial.PoliceBike, f => { f.IsPolice = true; return f; }),
                Case(FusionDenial.MissionVehicle, f => { f.VehicleMissionCritical = true; return f; }),
                Case(FusionDenial.NotPersonal, f => { f.IsPersonal = false; f.PersonalReason = "This bike isn't yours."; return f; }),
                Case(FusionDenial.VehicleWrecked, f => { f.VehicleDestroyed = true; return f; }),
                Case(FusionDenial.VehicleDamaged, f => { f.VehicleHealth01 = 0.1f; return f; }),
                Case(FusionDenial.VehicleDamaged, f => { f.OnFire = true; return f; }),
                Case(FusionDenial.NotUpright, f => { f.Upright = false; return f; }),
                Case(FusionDenial.NotGrounded, f => { f.GroundedRays = 1; return f; }),
                Case(FusionDenial.AlreadyFused, f => { f.State = FusionState.Fused; return f; }),
                Case(FusionDenial.Busy, f => { f.State = FusionState.TransitionOut; return f; }),
                Case(FusionDenial.Cooldown, f => { f.CooldownLeft = 4f; return f; }),
                Case(FusionDenial.StoryMission, f => { f.StoryMission = true; return f; }),
                Case(FusionDenial.MissionForbids, f => { f.MissionForbidsVehicles = true; return f; }),
                Case(FusionDenial.OtherJob, f => { f.OtherJob = true; return f; }),
                Case(FusionDenial.InVeil, f => { f.InVeil = true; return f; }),
                Case(FusionDenial.NoCompanion, f => { f.HasCompanion = false; return f; }),
                Case(FusionDenial.UnknownForm, f => { f.CompanionArchetype = "unknown"; return f; }),
                Case(FusionDenial.CompanionRank, f => { f.CompanionRank = BondIds.RankBonded; return f; }),
                Case(FusionDenial.CompanionDowned, f => { f.CompanionDowned = true; return f; }),
                Case(FusionDenial.CompanionUnavailable, f => { f.BondsCanFuse = false; f.BondsReason = "Glint is too far away."; return f; }),
                Case(FusionDenial.NoEntitlement, f => { f.FusionUnlocked = false; return f; }),
                Case(FusionDenial.FormLocked, f => { f.FormUnlocked = false; return f; }),
                Case(FusionDenial.Passengers, f => { f.Passengers = 1; return f; }),
                Case(FusionDenial.TooFast, f => { f.Speed = 5f; return f; }),
                Case(FusionDenial.RegionTight, f => { f.Underground = true; f.VehicleSpace = false; return f; }),
                Case(FusionDenial.LowHeadroom, f => { f.Headroom = 1.6f; return f; })
            };
            foreach (KeyValuePair<FusionDenial, Func<FusionFacts, FusionFacts>> c in cases)
            {
                FusionVerdict v = FusionRules.Validate(c.Value(OkFacts()));
                Assert.That(v.Ok, Is.False, "expected a refusal for " + c.Key);
                Assert.That(v.Code, Is.EqualTo(c.Key));
                Assert.That(v.Message, Is.Not.Empty, "every refusal explains itself (" + c.Key + ")");
            }
        }

        private static KeyValuePair<FusionDenial, Func<FusionFacts, FusionFacts>> Case(FusionDenial d, Func<FusionFacts, FusionFacts> change)
        {
            return new KeyValuePair<FusionDenial, Func<FusionFacts, FusionFacts>>(d, change);
        }

        [Test]
        public void ARidingOrStolenBikeIsRefusedWithTheGaragesReason()
        {
            FusionFacts f = OkFacts();
            f.IsPersonal = false;
            f.PersonalReason = "This bike isn't yours. Phone > Bring my bike delivers your Vanta Stilettra.";
            FusionVerdict v = FusionRules.Validate(f);
            Assert.That(v.Code, Is.EqualTo(FusionDenial.NotPersonal));
            Assert.That(v.Message, Does.Contain("Bring my bike"));
        }

        [Test]
        public void TheFirstFusionNeedsTheQuestTheBayAndTheDrakeling()
        {
            FusionFacts f = OkFacts();
            f.FusionUnlocked = false;
            f.FormUnlocked = false;
            f.FirstFusionQuest = true;
            f.InBay = true;
            FusionVerdict v = FusionRules.Validate(f);
            Assert.That(v.Ok, Is.True, v.Message);
            Assert.That(v.FirstFusion, Is.True);
            f.InBay = false;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.NoEntitlement));
            f.InBay = true;
            f.FirstFusionQuest = false;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.NoEntitlement));
            f.FirstFusionQuest = true;
            f.CompanionArchetype = BondIds.AshWarden;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.NoEntitlement), "the quest exception is the drakeling's Ventwing only");
            f.CompanionArchetype = BondIds.QuartzGuide;
            f.Speed = 1.5f;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.TooFast), "the first fusion is parked in the bay");
        }

        [Test]
        public void AnUncalibratedFormCalibratesOnlyParkedInTheBay()
        {
            FusionFacts f = OkFacts();
            f.CompanionArchetype = BondIds.StormSkimmer;
            f.FormUnlocked = false;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.FormLocked));
            f.InBay = true;
            FusionVerdict v = FusionRules.Validate(f);
            Assert.That(v.Ok, Is.True, v.Message);
            Assert.That(v.CalibratesForm, Is.True);
            Assert.That(v.Form, Is.EqualTo(FusionIds.Coilrunner));
            f.Speed = 2f;
            Assert.That(FusionRules.Validate(f).Code, Is.EqualTo(FusionDenial.TooFast));
        }

        [Test]
        public void UnknownHeadroomPassesAndEnoughHeadroomPasses()
        {
            FusionFacts f = OkFacts();
            f.Headroom = 0f;
            Assert.That(FusionRules.Validate(f).Ok, Is.True);
            f.Underground = true;
            f.VehicleSpace = true;
            f.Headroom = 5f;
            Assert.That(FusionRules.Validate(f).Ok, Is.True);
            Assert.That(FusionRules.RequiredHeadroom(1.3f), Is.EqualTo(2.2f).Within(1e-4f));
            Assert.That(FusionRules.RequiredHeadroom(2f), Is.EqualTo(2f + FusionRules.HeadroomMargin).Within(1e-4f));
        }

        [Test]
        public void SeparationNeedsTheFusedStateLowSpeedGroundAndRoom()
        {
            Assert.That(FusionRules.ValidateSeparate(FusionState.Fused, 2f, 4, true).Ok, Is.True);
            Assert.That(FusionRules.ValidateSeparate(FusionState.Separate, 0f, 4, true).Code, Is.EqualTo(FusionDenial.NotFused));
            Assert.That(FusionRules.ValidateSeparate(FusionState.TransitionIn, 0f, 4, true).Code, Is.EqualTo(FusionDenial.Busy));
            Assert.That(FusionRules.ValidateSeparate(FusionState.Fused, 9f, 4, true).Code, Is.EqualTo(FusionDenial.TooFast));
            Assert.That(FusionRules.ValidateSeparate(FusionState.Fused, 1f, 0, true).Code, Is.EqualTo(FusionDenial.NotGrounded));
            Assert.That(FusionRules.ValidateSeparate(FusionState.Fused, 1f, 4, false).Code, Is.EqualTo(FusionDenial.NoRoomForCompanion));
        }

        // ================================================================== the single state machine

        [Test]
        public void TransitionTableAllowsOnlyTheDocumentedPath()
        {
            Assert.That(FusionRules.IsLegal(FusionState.Separate, FusionState.FusionRequested), Is.True);
            Assert.That(FusionRules.IsLegal(FusionState.Separate, FusionState.Fused), Is.False, "no shortcut into Fused");
            Assert.That(FusionRules.IsLegal(FusionState.Separate, FusionState.TransitionIn), Is.False);
            Assert.That(FusionRules.IsLegal(FusionState.Validating, FusionState.TransitionIn), Is.True);
            Assert.That(FusionRules.IsLegal(FusionState.TransitionIn, FusionState.Fused), Is.True);
            Assert.That(FusionRules.IsLegal(FusionState.TransitionIn, FusionState.Separate), Is.False, "a failed transition goes through Recovery");
            Assert.That(FusionRules.IsLegal(FusionState.Fused, FusionState.TransitionIn), Is.False);
            Assert.That(FusionRules.IsLegal(FusionState.Fused, FusionState.Separate), Is.False, "separation always transitions or recovers");
            Assert.That(FusionRules.IsLegal(FusionState.TransitionOut, FusionState.Separate), Is.True);
            Assert.That(FusionRules.IsLegal(FusionState.Recovery, FusionState.Separate), Is.True);
            Assert.That(FusionRules.IsLegal(FusionState.Recovery, FusionState.Fused), Is.False);
            foreach (FusionState s in Enum.GetValues(typeof(FusionState))) Assert.That(FusionRules.IsLegal(s, s), Is.False);
        }

        [Test]
        public void FuseThenSeparateRestoresTheSameCompanionExactly()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            CompanionSnapshot before = c.State;
            string reason;
            Assert.That(m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason), Is.True, reason);
            Assert.That(m.State, Is.EqualTo(FusionState.TransitionIn));
            Assert.That(c.Hidden, Is.True, "the free body is hidden only after the form was built");
            Assert.That(form.Builds, Is.EqualTo(1));
            Assert.That(m.Tick(FusionRules.TransitionInSeconds + 0.01f, false, false), Is.EqualTo(FusionEvent.BecameFused));
            Assert.That(m.IsFused, Is.True);
            Assert.That(m.CompanionUid, Is.EqualTo(7));
            Assert.That(m.RequestSeparate(OkVerdict(""), out reason), Is.True, reason);
            Assert.That(m.State, Is.EqualTo(FusionState.TransitionOut));
            Assert.That(m.Tick(FusionRules.TransitionOutSeconds + 0.01f, false, false), Is.EqualTo(FusionEvent.BecameSeparate));
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(c.Hidden, Is.False);
            Assert.That(c.State.Uid, Is.EqualTo(before.Uid));
            Assert.That(c.State.Health01, Is.EqualTo(before.Health01), "health comes back exactly, never refilled");
            Assert.That(c.State.AssistCharge, Is.EqualTo(before.AssistCharge));
            Assert.That(c.State.AssistCooldown, Is.EqualTo(before.AssistCooldown));
            Assert.That(form.Destroys, Is.EqualTo(1));
            Assert.That(m.Cooldown, Is.EqualTo(FusionRules.CooldownAfterSeparate).Within(1e-4f));
        }

        [Test]
        public void AFailedBuildKeepsTheOldStateAndCostsNothing()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm { Fail = true };
            string reason;
            Assert.That(m.RequestFuse(OkVerdict(FusionIds.Kilnback), 7, form, c, out reason), Is.False);
            Assert.That(reason, Is.EqualTo("Not enough room."));
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(c.HideCalls, Is.EqualTo(0), "the companion was never hidden");
            Assert.That(form.Destroys, Is.EqualTo(1), "whatever was built is removed");
            Assert.That(m.Cooldown, Is.EqualTo(0f));
            form.Fail = false;
            form.Throw = true;
            Assert.That(m.RequestFuse(OkVerdict(FusionIds.Kilnback), 7, form, c, out reason), Is.False);
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(c.HideCalls, Is.EqualTo(0));
            Assert.That(m.Cooldown, Is.EqualTo(0f));
        }

        [Test]
        public void AFailedCompanionHideRollsBackBothSides()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion { ThrowOnHide = true };
            FakeForm form = new FakeForm();
            CompanionSnapshot before = c.State;
            string reason;
            Assert.That(m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason), Is.False);
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(form.Built, Is.False, "the new form was removed");
            Assert.That(c.ShowCalls, Is.EqualTo(1), "the companion body is restored");
            Assert.That(c.RestoreCalls, Is.EqualTo(1));
            Assert.That(c.State.Health01, Is.EqualTo(before.Health01));
            Assert.That(m.Cooldown, Is.EqualTo(0f));
        }

        [Test]
        public void ARefusedVerdictNeverBuildsOrHidesAnything()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            string reason;
            Assert.That(m.RequestFuse(FusionVerdict.Deny(FusionDenial.Passengers, "Your passenger has to get off first."), 7, form, c, out reason), Is.False);
            Assert.That(reason, Does.Contain("passenger"));
            Assert.That(form.Builds, Is.EqualTo(0));
            Assert.That(c.HideCalls, Is.EqualTo(0));
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
        }

        [Test]
        public void AnAbortWhileUnfoldingCostsNothingAndRestoresTheCompanion()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            CompanionSnapshot before = c.State;
            string reason;
            m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason);
            m.Tick(0.2f, false, false);
            Assert.That(m.Recover("You left the chassis."), Is.True);
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(m.Cooldown, Is.EqualTo(0f), "a fusion that never finished costs nothing");
            Assert.That(c.Hidden, Is.False);
            Assert.That(c.State.AssistCharge, Is.EqualTo(before.AssistCharge));
            Assert.That(form.Built, Is.False);
            Assert.That(m.LastReason, Is.EqualTo("You left the chassis."));
        }

        [Test]
        public void ACrashDeathOrLossAlwaysEndsSeparateWithTheCompanionBack()
        {
            foreach (string why in new[] { "The chassis was wrecked.", "You went down.", "You left the chassis.", "The chassis was lost." })
            {
                FusionMachine m = new FusionMachine();
                FakeCompanion c = new FakeCompanion();
                FakeForm form = new FakeForm();
                CompanionSnapshot before = c.State;
                string reason;
                m.RequestFuse(OkVerdict(FusionIds.Coilrunner), 7, form, c, out reason);
                m.Tick(1f, false, false);
                Assert.That(m.IsFused, Is.True);
                Assert.That(m.Recover(why), Is.True);
                Assert.That(m.State, Is.EqualTo(FusionState.Separate), why);
                Assert.That(c.Hidden, Is.False, why);
                Assert.That(c.State.Health01, Is.EqualTo(before.Health01), why);
                Assert.That(form.Built, Is.False, why);
                Assert.That(m.Cooldown, Is.EqualTo(FusionRules.CooldownAfterRecovery).Within(1e-4f));
                Assert.That(m.Recover(why), Is.False, "recovering twice does nothing");
            }
        }

        [Test]
        public void ARecoveryDuringSeparationStillEndsSeparate()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            string reason;
            m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason);
            m.Tick(1f, false, false);
            m.RequestSeparate(OkVerdict(""), out reason);
            m.Tick(0.1f, false, false);
            Assert.That(m.Recover("You went down."), Is.True);
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
            Assert.That(c.Hidden, Is.False);
        }

        [Test]
        public void ARefusedSeparationStaysFused()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            string reason;
            m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason);
            m.Tick(1f, false, false);
            Assert.That(m.RequestSeparate(FusionRules.ValidateSeparate(FusionState.Fused, 1f, 4, false), out reason), Is.False);
            Assert.That(m.State, Is.EqualTo(FusionState.Fused));
            Assert.That(c.Hidden, Is.True);
            Assert.That(m.LastReason, Does.Contain("companion"));
        }

        [Test]
        public void EveryStateTheMachineEntersFollowsTheTable()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            string reason;
            for (int loop = 0; loop < 6; loop++)
            {
                m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason);
                m.Tick(0.3f, false, false);
                if (loop % 3 == 0) m.Recover("test");
                else
                {
                    m.Tick(1f, false, false);
                    m.RequestSeparate(loop % 2 == 0 ? OkVerdict("") : FusionVerdict.Deny(FusionDenial.TooFast, "slow"), out reason);
                    m.Tick(1f, false, false);
                    if (m.State == FusionState.Fused) m.Recover("test");
                }
                m.Tick(20f, false, false);
            }
            FusionState prev = FusionState.Separate;
            foreach (FusionState s in m.Trail)
            {
                Assert.That(FusionRules.IsLegal(prev, s), Is.True, prev + " -> " + s);
                prev = s;
            }
            Assert.That(m.State, Is.EqualTo(FusionState.Separate));
        }

        // ================================================================== no-refill invariants

        [Test]
        public void TogglingNeverRefillsHeatCooldownsChargesOrTheCompanion()
        {
            FusionMachine m = new FusionMachine();
            FakeCompanion c = new FakeCompanion();
            FakeForm form = new FakeForm();
            CompanionSnapshot companionAtStart = c.State;
            float chassisHealth = 0.55f;
            string reason;

            // Ventwing: one burst heats the vents and starts the cooldown.
            Assert.That(m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason), Is.True);
            m.Tick(1f, false, false);
            Assert.That(m.TryUseAbility(out reason), Is.True, reason);
            FormMeters hot = m.Meters(FusionIds.Ventwing);
            Assert.That(hot.Heat, Is.EqualTo(FusionRules.Ability(FusionIds.Ventwing).HeatPerUse).Within(1e-4f));

            float elapsed = 0f;
            for (int i = 0; i < 25; i++)
            {
                // Separate and fuse again as fast as the rules allow.
                m.Recover("toggle");
                m.Tick(0.05f, false, false);
                elapsed += 0.05f;
                Assert.That(m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason), Is.False, "the coupler cooldown blocks an instant re-fuse");
                FormMeters now = m.Meters(FusionIds.Ventwing);
                float coolest = Mathf.Max(0f, hot.Heat - FusionRules.Ability(FusionIds.Ventwing).CoolGround * elapsed);
                Assert.That(now.Heat, Is.GreaterThanOrEqualTo(coolest - 1e-4f), "heat only falls with time");
                Assert.That(now.Cooldown, Is.GreaterThanOrEqualTo(Mathf.Max(0f, hot.Cooldown - elapsed) - 1e-4f), "cooldown only falls with time");
                Assert.That(c.State.Health01, Is.EqualTo(companionAtStart.Health01));
                Assert.That(c.State.AssistCharge, Is.EqualTo(companionAtStart.AssistCharge));
                Assert.That(c.State.AssistCooldown, Is.EqualTo(companionAtStart.AssistCooldown));
                Assert.That(chassisHealth, Is.EqualTo(0.55f), "nothing in a fusion writes chassis health");
                // Wait out the cooldown in game time (it ticks) and fuse again.
                m.Tick(FusionRules.CooldownAfterRecovery, false, false);
                elapsed += FusionRules.CooldownAfterRecovery;
                Assert.That(m.RequestFuse(OkVerdict(FusionIds.Ventwing), 7, form, c, out reason), Is.True, reason);
                m.Tick(1f, false, false);
                elapsed += 1f;
            }

            // Coilrunner: spend both charges; toggling cannot bring them back, only the recharge time can.
            FusionMachine k = new FusionMachine();
            FakeCompanion c2 = new FakeCompanion();
            k.RequestFuse(OkVerdict(FusionIds.Coilrunner), 9, new FakeForm(), c2, out reason);
            k.Tick(1f, false, false);
            Assert.That(k.TryUseAbility(out reason), Is.True, reason);
            k.Tick(FusionRules.Ability(FusionIds.Coilrunner).Cooldown, false, false);
            Assert.That(k.TryUseAbility(out reason), Is.True, reason);
            Assert.That(k.Meters(FusionIds.Coilrunner).Charges, Is.EqualTo(0));
            for (int i = 0; i < 10; i++)
            {
                k.Recover("toggle");
                k.Tick(0.01f, false, false);
                Assert.That(k.Meters(FusionIds.Coilrunner).Charges, Is.EqualTo(0), "no charge comes back from a toggle");
            }
            k.Tick(FusionRules.Ability(FusionIds.Coilrunner).RechargeSeconds + 0.05f, false, false);
            Assert.That(k.Meters(FusionIds.Coilrunner).Charges, Is.EqualTo(1), "one charge returns per recharge period");
        }

        [Test]
        public void MetersOnlyImproveWithTimeAndUseSpendsThem()
        {
            foreach (string form in FusionIds.Forms)
            {
                FormAbilitySpec spec = FusionRules.Ability(form);
                FormMeters m = FormMeters.Fresh(spec);
                Assert.That(FusionRules.TryUse(ref m, spec), Is.True, form);
                FormMeters after = m;
                Assert.That(after.Cooldown, Is.EqualTo(spec.Cooldown).Within(1e-4f));
                Assert.That(FusionRules.TryUse(ref m, spec), Is.False, form + " cannot fire again during its cooldown");
                Assert.That(m.Cooldown, Is.EqualTo(after.Cooldown), "a refused use changes nothing");
                FormMeters zeroStep = FusionRules.Step(m, spec, 0f, false, false);
                Assert.That(zeroStep.Heat, Is.EqualTo(m.Heat));
                Assert.That(zeroStep.Charges, Is.EqualTo(m.Charges));
                FormMeters later = FusionRules.Step(m, spec, 1f, false, false);
                Assert.That(later.Heat, Is.LessThanOrEqualTo(m.Heat));
                Assert.That(later.Cooldown, Is.LessThanOrEqualTo(m.Cooldown));
                Assert.That(later.Charges, Is.GreaterThanOrEqualTo(m.Charges));
            }
            // Gliding builds heat until the vents overheat, then nothing fires until they cool to the release level.
            FormAbilitySpec vent = FusionRules.Ability(FusionIds.Ventwing);
            FormMeters g = FormMeters.Fresh(vent);
            for (int i = 0; i < 200 && !g.Overheated; i++) g = FusionRules.Step(g, vent, 0.1f, true, true);
            Assert.That(g.Overheated, Is.True);
            Assert.That(FusionRules.WhyNotUse(g, vent), Is.Not.Empty);
            for (int i = 0; i < 400 && g.Overheated; i++) g = FusionRules.Step(g, vent, 0.1f, false, false);
            Assert.That(g.Overheated, Is.False);
            Assert.That(g.Heat, Is.LessThanOrEqualTo(vent.OverheatRelease + 1e-4f));
        }

        [Test]
        public void AbilityImpulsesNeverPushPastTheirCap()
        {
            Vector3 fwd = new Vector3(0f, 0f, 1f);
            foreach (string form in FusionIds.Forms)
            {
                FormAbilitySpec spec = FusionRules.Ability(form);
                const float top = 50f;
                float cap = top * spec.SpeedCapFactor;
                Vector3 slow = FusionRules.CapImpulse(new Vector3(0f, 0f, 5f), fwd, spec.ForwardDeltaV, top, spec.SpeedCapFactor);
                Assert.That(slow.z, Is.EqualTo(5f + spec.ForwardDeltaV).Within(1e-3f), form);
                Vector3 fast = FusionRules.CapImpulse(new Vector3(0f, 0f, cap - 1f), fwd, spec.ForwardDeltaV, top, spec.SpeedCapFactor);
                Assert.That(MathUtil.Flat(fast).magnitude, Is.LessThanOrEqualTo(cap + 1e-3f), form);
                Vector3 already = FusionRules.CapImpulse(new Vector3(0f, -2f, cap + 10f), fwd, spec.ForwardDeltaV, top, spec.SpeedCapFactor);
                Assert.That(MathUtil.Flat(already).magnitude, Is.EqualTo(cap + 10f).Within(1e-3f), "never adds speed above the cap, never brakes either");
                Assert.That(already.y, Is.EqualTo(-2f), "vertical speed is untouched");
                Assert.That(spec.SpeedCapFactor, Is.LessThanOrEqualTo(1.12f + 1e-4f), "no form inflates speed far past top speed");
            }
            Assert.That(FusionRules.Ability(FusionIds.Ventwing).UpDeltaV, Is.InRange(3f, 6f), "a hop, not flight");
        }

        // ================================================================== handling and damage

        [Test]
        public void HandlingProfilesAreExplicitAndNeverInflateTopSpeed()
        {
            foreach (string form in FusionIds.Forms)
            foreach (string profile in new[] { FusionRules.ProfileUpdraft, FusionRules.ProfileVentBrake, "" })
            foreach (string tune in new[] { "", FusionRules.TuneGrip })
            {
                FusedHandling h = FusionRules.Handling(form, profile, tune);
                Assert.That(h.Active, Is.True);
                Assert.That(h.TopSpeed, Is.LessThanOrEqualTo(1f), form + " " + profile + " " + tune);
                Assert.That(h.Drive, Is.InRange(0.5f, 1f));
                Assert.That(h.Steer, Is.InRange(0.6f, 1f), "no form makes the steering twitchier");
                Assert.That(h.DamageScale, Is.GreaterThan(0f));
                Assert.That(h.GlideGravity, Is.GreaterThan(0f));
            }
            FusedHandling coil = FusionRules.Handling(FusionIds.Coilrunner, "", "");
            Assert.That(coil.Brake, Is.GreaterThanOrEqualTo(1.3f), "the Coilrunner brakes hard");
            Assert.That(coil.TopSpeed, Is.EqualTo(1f));
            FusedHandling kiln = FusionRules.Handling(FusionIds.Kilnback, "", "");
            Assert.That(kiln.TopSpeed, Is.LessThan(1f));
            Assert.That(kiln.Drive, Is.LessThan(1f));
            Assert.That(kiln.Steer, Is.LessThan(1f));
            Assert.That(kiln.DamageScale, Is.LessThan(1f), "the Kilnback is armoured");
            FusedHandling up = FusionRules.Handling(FusionIds.Ventwing, FusionRules.ProfileUpdraft, "");
            FusedHandling brake = FusionRules.Handling(FusionIds.Ventwing, FusionRules.ProfileVentBrake, "");
            Assert.That(up.GlideGravity, Is.LessThan(brake.GlideGravity), "Updraft glides longer");
            Assert.That(brake.Brake, Is.GreaterThan(up.Brake), "Vent Brake stops harder");
            Assert.That(brake.AirBrake, Is.GreaterThan(0f));
            Assert.That(up.GlideGravity, Is.InRange(0.2f, 0.6f));
            FusedHandling none = FusedHandling.None;
            Assert.That(none.Active, Is.False);
            Assert.That(none.GlideGravity, Is.EqualTo(1f));
            Assert.That(FusionRules.WithGlide(up, FusionIds.Ventwing, FusionRules.ProfileUpdraft, false).GlideGravity, Is.EqualTo(1f), "the glide is off unless engaged");
            Assert.That(FusionRules.WithGlide(FusionRules.Handling(FusionIds.Kilnback, "", ""), FusionIds.Kilnback, "", true).GlideGravity, Is.EqualTo(1f), "only the Ventwing glides");
            FusedHandling tuned = FusionRules.TuneHandling(FusionRules.TuneGrip);
            Assert.That(tuned.Grip, Is.GreaterThan(1f));
            Assert.That(tuned.TopSpeed, Is.LessThan(1f), "the tune is a sidegrade");
            Assert.That(tuned.Active, Is.False, "a tune never applies the fused damage scale");
        }

        [Test]
        public void FusedDamageMultipliersAreExplicitBoundedAndNeverZero()
        {
            Assert.That(FusionRules.ChassisDamageScale(FusionIds.Ventwing, false), Is.EqualTo(1f));
            Assert.That(FusionRules.ChassisDamageScale(FusionIds.Kilnback, false), Is.EqualTo(0.7f));
            Assert.That(FusionRules.ChassisDamageScale(FusionIds.Kilnback, true), Is.EqualTo(0.4f));
            Assert.That(FusionRules.ChassisDamageScale(FusionIds.Coilrunner, false), Is.EqualTo(1.1f));
            Assert.That(FusionRules.RiderDamageScale(FusionIds.Kilnback, true, true), Is.EqualTo(0.6f));
            Assert.That(FusionRules.RiderDamageScale(FusionIds.Kilnback, true, false), Is.EqualTo(0.85f));
            Assert.That(FusionRules.RiderDamageScale(FusionIds.Ventwing, true, true), Is.EqualTo(1f));
            foreach (string form in FusionIds.Forms)
            foreach (bool brace in new[] { false, true })
            foreach (bool front in new[] { false, true })
            {
                Assert.That(FusionRules.ChassisDamageScale(form, brace), Is.InRange(0.3f, 1.2f));
                Assert.That(FusionRules.RiderDamageScale(form, brace, front), Is.InRange(0.5f, 1f), "never invulnerable");
            }
            // A Kilnback that toggles through fusions never recovers a larger pool: damage is only ever scaled.
            float health = 800f;
            health -= 100f * FusionRules.ChassisDamageScale(FusionIds.Kilnback, false);
            Assert.That(health, Is.EqualTo(730f).Within(1e-3f));
        }

        // ================================================================== compatibility

        [Test]
        public void TheMatrixIsThreeFormsTimesThreeBikesAndNothingElse()
        {
            int ok = 0;
            foreach (string arch in BondIds.Archetypes)
            foreach (string cls in FusionIds.ChassisClasses)
            {
                string name = FusionRules.VehicleNameOf(cls);
                Assert.That(FusionRules.ChassisClassOf(name), Is.EqualTo(cls));
                string reason;
                Assert.That(FusionRules.Compatible(arch, name, true, false, out reason), Is.True, arch + " x " + cls + ": " + reason);
                ok++;
            }
            Assert.That(ok, Is.EqualTo(9));
            string why;
            Assert.That(FusionRules.Compatible(BondIds.QuartzGuide, "Vanta Arrow", false, false, out why), Is.False);
            Assert.That(why, Does.Contain("Cars"));
            Assert.That(FusionRules.Compatible(BondIds.QuartzGuide, VehicleDb.PoliceBikeName, true, true, out why), Is.False);
            Assert.That(FusionRules.ChassisClassOf(VehicleDb.PoliceBikeName), Is.Empty);
            Assert.That(FusionRules.Compatible("unknown", VehicleDb.SportBikeName, true, false, out why), Is.False);
            foreach (string arch in BondIds.Archetypes) Assert.That(FusionIds.Has(FusionIds.FormFor(arch)), Is.True);
            foreach (string id in FusionIds.Courses)
            {
                Assert.That(FusionCourses.AbilityUsesFor(id), Is.GreaterThan(0), id);
                Assert.That(FusionCourses.TextFor(id), Is.Not.Empty, id);
            }
        }

        // ================================================================== garage rules

        [Test]
        public void APersonalChassisExistsAfterThePrologueOrInFreeRoam()
        {
            Assert.That(GarageRules.ShouldCreatePersonal(true, 0), Is.True);
            Assert.That(GarageRules.ShouldCreatePersonal(false, 1), Is.True);
            Assert.That(GarageRules.ShouldCreatePersonal(false, 0), Is.False, "the prologue gives the story bike itself");
            ChassisRecord r = GarageRules.NewRecord(3, FusionIds.Sport, GarageRules.DefaultPaint(FusionIds.Sport), "story", new List<string> { ChassisTrims.EmberFins });
            Assert.That(r.Uid, Is.EqualTo(3));
            Assert.That(r.Paint.r, Is.EqualTo(0.45f).Within(1e-4f), "the sport chassis starts in Malik's bronze story paint");
            Assert.That(r.Trims, Does.Contain(ChassisTrims.EmberFins), "unlocked looks carry over to a new chassis");
            Assert.That(GarageRules.NewRecord(4, "hovercar", Color.red, "", null).Class, Is.EqualTo(FusionIds.Sport));
        }

        [Test]
        public void NormalizeRepairsOldEmptyAndBrokenBlocksWithoutGrantingAnything()
        {
            GarageSave empty = GarageRules.Normalize(null);
            Assert.That(empty.Chassis, Is.Empty);
            Assert.That(empty.FusionUnlocked, Is.False);
            Assert.That(empty.Forms, Is.Empty);
            Assert.That(empty.PersonalUid, Is.EqualTo(0));

            GarageSave s = new GarageSave { NextUid = 1, PersonalUid = 99, Profile = "turbo", FusionCooldown = float.NaN };
            s.Chassis.Add(new ChassisRecord { Uid = 2, Class = "sport", Health01 = float.NaN, PaintR = 4f, Trims = null, Trim = "trim_missing" });
            s.Chassis.Add(null);
            s.Chassis.Add(new ChassisRecord { Uid = 2, Class = "jetpack", Health01 = 1.6f, EngineUpgrade = 40 });
            s.Chassis.Add(new ChassisRecord { Uid = 0, Class = "skiff", Health01 = 0.3f, Wrecked = true, Tune = FusionRules.TuneGrip });
            s.Forms.AddRange(new[] { FusionIds.Ventwing, "hoverform", FusionIds.Ventwing });
            s.Meters = null;
            GarageSave n = GarageRules.Normalize(s);
            Assert.That(n.Chassis.Count, Is.EqualTo(3), "the null record is dropped");
            HashSet<int> uids = new HashSet<int>();
            foreach (ChassisRecord r in n.Chassis)
            {
                Assert.That(r.Uid, Is.GreaterThan(0));
                Assert.That(uids.Add(r.Uid), Is.True, "uids are unique after normalizing");
                Assert.That(GarageRules.IsClass(r.Class), Is.True);
                Assert.That(r.Health01, Is.InRange(0f, 1f));
                Assert.That(r.PaintR, Is.InRange(0f, 1f));
                Assert.That(r.EngineUpgrade, Is.LessThanOrEqualTo(5));
                Assert.That(r.Trims, Is.Not.Null);
            }
            Assert.That(n.Chassis[0].Health01, Is.EqualTo(1f), "a broken health value becomes full, never zero");
            Assert.That(n.Chassis[0].Trim, Is.Empty, "a look that is not unlocked is cleared");
            Assert.That(n.Chassis[2].Health01, Is.EqualTo(0f), "a wreck stays a wreck");
            Assert.That(n.Chassis[2].Tune, Is.Empty, "a tune that is not unlocked is cleared");
            Assert.That(n.NextUid, Is.GreaterThan(4));
            Assert.That(n.PersonalUid, Is.EqualTo(n.Chassis[0].Uid), "the personal uid points at an owned chassis");
            Assert.That(n.Forms, Is.EqualTo(new List<string> { FusionIds.Ventwing }));
            Assert.That(n.Profile, Is.Empty);
            Assert.That(n.Meters, Is.Not.Null);
            Assert.That(n.FusionCooldown, Is.EqualTo(0f));
            Assert.That(n.FusionUnlocked, Is.False);
        }

        [Test]
        public void NormalizeClampsSavedMetersSoAReloadIsNeverARefill()
        {
            GarageSave s = new GarageSave();
            s.Meters.Add(new FormMeterSave { Form = FusionIds.Coilrunner, Charges = 9, Cooldown = 99f, Heat = 2f, Recharge = -3f });
            s.Meters.Add(new FormMeterSave { Form = FusionIds.Coilrunner, Charges = 2 });
            s.Meters.Add(new FormMeterSave { Form = "jetpack" });
            s.FusionCooldown = 400f;
            GarageSave n = GarageRules.Normalize(s);
            Assert.That(n.Meters.Count, Is.EqualTo(1));
            FormAbilitySpec spec = FusionRules.Ability(FusionIds.Coilrunner);
            Assert.That(n.Meters[0].Charges, Is.EqualTo(spec.MaxCharges));
            Assert.That(n.Meters[0].Cooldown, Is.EqualTo(spec.Cooldown));
            Assert.That(n.Meters[0].Heat, Is.EqualTo(1f));
            Assert.That(n.Meters[0].Recharge, Is.EqualTo(0f));
            Assert.That(n.FusionCooldown, Is.EqualTo(FusionRules.CooldownAfterRecovery));
        }

        [Test]
        public void UidsAreNeverReused()
        {
            GarageSave s = new GarageSave();
            int a = GarageRules.AllocateUid(s);
            s.Chassis.Add(new ChassisRecord { Uid = a });
            int b = GarageRules.AllocateUid(s);
            s.Chassis.Add(new ChassisRecord { Uid = 40 });
            int c = GarageRules.AllocateUid(s);
            Assert.That(b, Is.Not.EqualTo(a));
            Assert.That(c, Is.GreaterThan(40));
        }

        [Test]
        public void RegistrationIsTheOnlyWayARiddenBikeBecomesYours()
        {
            string sport = VehicleDb.SportBikeName;
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, true, true, false, 1, 5000), Is.Empty);
            Assert.That(GarageRules.WhyNotRegister(false, false, "Vanta Arrow", false, false, true, true, true, false, 1, 5000), Is.Not.Empty);
            Assert.That(GarageRules.WhyNotRegister(true, true, VehicleDb.PoliceBikeName, false, false, true, true, true, false, 1, 5000), Is.Not.Empty);
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, true, false, true, true, true, false, 1, 5000), Is.Not.Empty);
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, true, true, true, true, false, 1, 5000), Does.Contain("job"));
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, false, true, true, false, 1, 5000), Is.Not.Empty);
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, false, true, false, 1, 5000), Does.Contain("bay"));
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, true, false, false, 1, 5000), Does.Contain("Stop"));
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, true, true, true, 1, 5000), Does.Contain("already"));
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, true, true, false, GarageRules.MaxOwned, 5000), Does.Contain("full"));
            Assert.That(GarageRules.WhyNotRegister(true, false, sport, false, false, true, true, true, false, 1, 10), Does.Contain("short"));
        }

        [Test]
        public void DeliveryKeepsTheRecordedHealthAndPatchesUpOnlyAWreck()
        {
            Assert.That(GarageRules.DeliveryHealth01(0.37f, false), Is.EqualTo(0.37f).Within(1e-5f), "a damaged chassis is delivered damaged");
            Assert.That(GarageRules.DeliveryHealth01(1f, false), Is.EqualTo(1f));
            Assert.That(GarageRules.DeliveryHealth01(0f, true), Is.EqualTo(GarageRules.RecoveryHealth01));
            Assert.That(GarageRules.DeliveryHealth01(0.9f, true), Is.EqualTo(GarageRules.RecoveryHealth01), "a wreck never comes back better than patched up");
        }

        [Test]
        public void TrimsAreCosmeticIdempotentAndNeverStack()
        {
            ChassisRecord r = GarageRules.NewRecord(1, FusionIds.Sport, Color.white, "", null);
            Assert.That(GarageRules.ApplyTrim(r, ChassisTrims.QuartzCrown), Is.False, "a locked look cannot be applied");
            r.Trims.Add(ChassisTrims.QuartzCrown);
            r.Trims.Add(FusionRules.TuneGrip);
            Assert.That(GarageRules.ApplyTrim(r, ChassisTrims.QuartzCrown), Is.True);
            Assert.That(GarageRules.ApplyTrim(r, ChassisTrims.QuartzCrown), Is.True);
            Assert.That(r.Trim, Is.EqualTo(ChassisTrims.QuartzCrown));
            Assert.That(GarageRules.ApplyTrim(r, FusionRules.TuneGrip), Is.False, "a tune is not a look");
            Assert.That(GarageRules.NextTrim(r), Is.Empty, "the cycle wraps to no look");
            Assert.That(GarageRules.ApplyTrim(r, ""), Is.True);
            Assert.That(GarageRules.NextTrim(r), Is.EqualTo(ChassisTrims.QuartzCrown));
            // Looks never touch handling: the fused handling ignores the record's look entirely.
            FusedHandling a = FusionRules.Handling(FusionIds.Ventwing, FusionRules.ProfileUpdraft, "");
            r.Trim = ChassisTrims.QuartzCrown;
            FusedHandling b = FusionRules.Handling(FusionIds.Ventwing, FusionRules.ProfileUpdraft, "");
            Assert.That(b.Drive, Is.EqualTo(a.Drive));
            Assert.That(b.Grip, Is.EqualTo(a.Grip));
            Assert.That(ChassisTrims.IsTune("uh_handling_sidegrade"), Is.True);
            foreach (string t in ChassisTrims.All) Assert.That(ChassisTrims.Title(t), Is.Not.EqualTo(t));
        }

        // ================================================================== form geometry

        private static readonly FormPoseInput[] Poses =
        {
            new FormPoseInput { Spread = 0f, Unfold = 1f },
            new FormPoseInput { Spread = 0.15f, Unfold = 1f, Pulse = 1f },
            new FormPoseInput { Spread = FormPose.IdleSpread, Unfold = 1f, Pulse = 1f },
            new FormPoseInput { Spread = 0.6f, Unfold = 1f, Steer = 1f, Vane = 1f, Brace = 1f, Flare = 1f, Pulse = 1f },
            new FormPoseInput { Spread = 1f, Glide = 1f, Unfold = 1f, Steer = -1f, Vane = -1f, Pulse = 1f },
            new FormPoseInput { Spread = 0.3f, Unfold = 0f }
        };

        [Test]
        public void EveryFormFitsItsEnvelopeAndNeverEntersTheRidersSpace()
        {
            List<Vector3> pts = new List<Vector3>();
            foreach (string form in FusionIds.Forms)
            foreach (string cls in FusionIds.ChassisClasses)
            foreach (bool low in new[] { false, true })
            {
                FormBlueprint bp = FusionFormGeometry.Build(form, cls, low);
                string tag = form + " on " + cls + (low ? " (Low)" : " (High)");
                Assert.That(bp.Parts.Count, Is.GreaterThan(1), tag);
                Assert.That(bp.VertexCount, Is.InRange(400, 6000), tag + " stays within the vertex budget");
                Vector3 tmin, tmax, fmin, fmax;
                bp.Envelope(true, out tmin, out tmax);
                bp.Envelope(false, out fmin, out fmax);
                Vector3 rmin, rmax, lmin, lmax;
                bp.Frame.RiderKeepout(out rmin, out rmax);
                bp.Frame.LegKeepout(out lmin, out lmax);
                foreach (FormPoseInput pose in Poses)
                {
                    pts.Clear();
                    bp.Vertices(pose, pts);
                    bool transitional = pose.Spread <= FormPose.IdleSpread && pose.Glide <= 0f && pose.Brace <= 0f && pose.Flare <= 0f && pose.Steer == 0f && pose.Vane == 0f;
                    foreach (Vector3 v in pts)
                    {
                        Assert.That(MathUtil.IsFinite(v), Is.True, tag);
                        Assert.That(Inside(v, fmin - Vector3.one * 0.06f, fmax + Vector3.one * 0.06f), Is.True, tag + " stays in its full envelope: " + v.ToString("F3"));
                        if (transitional) Assert.That(Inside(v, tmin - Vector3.one * 0.06f, tmax + Vector3.one * 0.06f), Is.True, tag + " unfolds inside the checked box: " + v.ToString("F3"));
                        Assert.That(InsideStrict(v, rmin, rmax), Is.False, tag + " enters the rider's space at " + v.ToString("F3"));
                        Assert.That(InsideStrict(v, lmin, lmax), Is.False, tag + " enters the rider's leg space at " + v.ToString("F3"));
                        Assert.That(v.y, Is.GreaterThanOrEqualTo(0.15f), tag + " keeps clear of the road and the thrusters");
                    }
                }
                // The fusion check box is modest enough for a street or Jax's bay.
                Assert.That(tmax.x - tmin.x, Is.LessThan(1.8f), tag);
                Assert.That(tmax.y, Is.LessThan(1.8f), tag);
                Assert.That(FusionRules.RequiredHeadroom(tmax.y), Is.LessThan(4.2f), tag + " fits under the garage bay's overhead beam");
            }
        }

        [Test]
        public void LowKeepsEverySilhouetteWithFewerVertices()
        {
            foreach (string form in FusionIds.Forms)
            foreach (string cls in FusionIds.ChassisClasses)
            {
                FormBlueprint high = FusionFormGeometry.Build(form, cls, false);
                FormBlueprint low = FusionFormGeometry.Build(form, cls, true);
                Assert.That(low.VertexCount, Is.LessThan(high.VertexCount * 0.75f), form + " " + cls);
                Vector3 hmin, hmax, lmin, lmax;
                high.Envelope(false, out hmin, out hmax);
                low.Envelope(false, out lmin, out lmax);
                for (int axis = 0; axis < 3; axis++)
                    Assert.That(lmax[axis] - lmin[axis], Is.GreaterThanOrEqualTo((hmax[axis] - hmin[axis]) * 0.9f), form + " " + cls + " axis " + axis + " keeps its outline on Low");
            }
        }

        [Test]
        public void TheThreeFormsHaveDistinctStructure()
        {
            foreach (string cls in FusionIds.ChassisClasses)
            {
                FormBlueprint vent = FusionFormGeometry.Build(FusionIds.Ventwing, cls, false);
                FormBlueprint kiln = FusionFormGeometry.Build(FusionIds.Kilnback, cls, false);
                FormBlueprint coil = FusionFormGeometry.Build(FusionIds.Coilrunner, cls, false);
                Assert.That(Count(vent, FormPartRole.WingRoot) + Count(vent, FormPartRole.WingMid) + Count(vent, FormPartRole.WingTip), Is.EqualTo(6), "two three-panel wings");
                Assert.That(Count(vent, FormPartRole.TailFin), Is.EqualTo(1));
                Assert.That(Count(vent, FormPartRole.TailVane), Is.EqualTo(2));
                Assert.That(Count(vent, FormPartRole.Emitter), Is.EqualTo(1));
                Assert.That(Count(kiln, FormPartRole.Prow), Is.EqualTo(1), "the forward brace");
                Assert.That(Count(kiln, FormPartRole.Plate), Is.GreaterThanOrEqualTo(4), "layered shell plates");
                Assert.That(Count(coil, FormPartRole.Spine), Is.GreaterThanOrEqualTo(4));
                Assert.That(Count(coil, FormPartRole.Fin), Is.EqualTo(2));
                Assert.That(coil.ArcPath.Count, Is.GreaterThanOrEqualTo(5), "a distinct discharge path");
                Assert.That(Count(kiln, FormPartRole.WingRoot) + Count(coil, FormPartRole.WingRoot), Is.EqualTo(0));
                Vector3 vmin, vmax, kmin, kmax, cmin, cmax;
                vent.Envelope(false, out vmin, out vmax);
                kiln.Envelope(false, out kmin, out kmax);
                coil.Envelope(false, out cmin, out cmax);
                Assert.That(vmax.x - vmin.x, Is.GreaterThan((kmax.x - kmin.x) * 2.5f), "the Ventwing's spread wings dwarf the Kilnback's width");
                Assert.That(cmax.x - cmin.x, Is.GreaterThan((kmax.x - kmin.x) * 1.4f), "the Coilrunner's fins reach wider than the Kilnback shell");
                Assert.That(kmax.z, Is.GreaterThan(cmax.z), "the Kilnback prow leads the silhouette");
            }
        }

        [Test]
        public void TheFormFrameMatchesTheRealBikeModels()
        {
            foreach (string cls in FusionIds.ChassisClasses)
            {
                VehicleDef d = VehicleDb.Find(FusionRules.VehicleNameOf(cls));
                Assert.That(d, Is.Not.Null, cls);
                FusionFrame f = FusionFrame.For(cls);
                Vector3 a = f.SeatLocal, b = BikeModels.RiderSeatLocal(d);
                Assert.That(Vector3.Distance(a, b), Is.LessThan(1e-4f), cls + " seat point");
                Assert.That(f.Length, Is.EqualTo(d.Length).Within(1e-4f), cls + " length");
                Assert.That(f.Width, Is.EqualTo(d.Width).Within(1e-4f), cls + " width");
                Assert.That(f.TopBox, Is.EqualTo(cls == FusionIds.Skiff));
            }
        }

        [Test]
        public void TrimsStayClearOfTheRiderAndHaveGeometry()
        {
            List<Vector3> pts = new List<Vector3>();
            foreach (string t in ChassisTrims.All)
            foreach (string cls in FusionIds.ChassisClasses)
            {
                FormBlueprint bp = FusionFormGeometry.BuildTrim(t, cls, false);
                Assert.That(bp.VertexCount, Is.GreaterThan(0), t + " " + cls);
                Assert.That(FusionFormGeometry.BuildTrim(t, cls, true).VertexCount, Is.LessThanOrEqualTo(bp.VertexCount));
                Vector3 rmin, rmax, lmin, lmax;
                bp.Frame.RiderKeepout(out rmin, out rmax);
                bp.Frame.LegKeepout(out lmin, out lmax);
                pts.Clear();
                bp.Vertices(FormPoseInput.Parked, pts);
                foreach (Vector3 v in pts)
                {
                    Assert.That(InsideStrict(v, rmin, rmax), Is.False, t + " " + cls + " " + v.ToString("F3"));
                    Assert.That(InsideStrict(v, lmin, lmax), Is.False, t + " " + cls + " " + v.ToString("F3"));
                }
            }
            Assert.That(FusionFormGeometry.BuildTrim("unknown", FusionIds.Sport, false).VertexCount, Is.EqualTo(0));
        }

        // ================================================================== Jax's bay and the courses on the real city

        [Test]
        public void TheComputedBaySitsInsideTheGaragesMarkedVehicleBay()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/InhabitedCity04C/Layouts/garage.json");
            string text = File.ReadAllText(path);
            Match bay = Regex.Match(text, "\"id\":\\s*\"vehicle_bay\",\\s*\"min\":\\s*\\[\\s*([-0-9.]+),\\s*([-0-9.]+),\\s*([-0-9.]+)\\s*\\],\\s*\"max\":\\s*\\[\\s*([-0-9.]+),\\s*([-0-9.]+),\\s*([-0-9.]+)\\s*\\]");
            Assert.That(bay.Success, Is.True, "garage.json keeps its vehicle_bay keepout");
            float x0 = Parse(bay.Groups[1].Value), y1 = Parse(bay.Groups[5].Value), z0 = Parse(bay.Groups[3].Value);
            float x1 = Parse(bay.Groups[4].Value), z1 = Parse(bay.Groups[6].Value);
            Vector3 c = GarageRules.BayLocalFallback;
            float r = GarageRules.BayRadius;
            Assert.That(c.x - r, Is.GreaterThanOrEqualTo(x0));
            Assert.That(c.x + r, Is.LessThanOrEqualTo(x1));
            Assert.That(c.z - r, Is.GreaterThanOrEqualTo(z0));
            Assert.That(c.z + r, Is.LessThanOrEqualTo(z1));
            Assert.That(y1, Is.GreaterThan(3f), "the bay keeps vehicle headroom");
            // The repair service point stays a separate stop.
            Match service = Regex.Match(text, "\"id\":\\s*\"service\",\\s*\"position\":\\s*\\[\\s*([-0-9.]+),\\s*([-0-9.]+),\\s*([-0-9.]+)\\s*\\]");
            Assert.That(service.Success, Is.True);
            Vector3 s = new Vector3(Parse(service.Groups[1].Value), 0f, Parse(service.Groups[3].Value));
            Assert.That(MathUtil.FlatDistance(s, c), Is.GreaterThan(4f));
        }

        [Test]
        public void TheBayAndJaxCoursesResolveOnTheDefaultCity()
        {
            CityLayout layout = TestData.City;
            VenueCatalog04C catalog = ReadVenueCatalog();
            VenueBinding04C garage = null;
            foreach (VenueBinding04C b in InhabitedCityBindings04C.Select(layout, catalog)) if (b.Definition.Id == FusionSystem.GarageVenue) garage = b;
            Assert.That(garage, Is.Not.Null);
            Assert.That(garage.Building, Is.Not.Null, "the garage parcel binds on seed 61724");
            Vector3 bay = garage.Origin + QMath.AY(garage.Yaw) * GarageRules.BayLocalFallback;
            Assert.That(garage.Building.Footprint.Contains(bay), Is.True, "the computed bay is inside the garage building");
            List<Vector3[]> loops = new List<Vector3[]>();
            for (int variant = 0; variant < 2; variant++)
            {
                string id = variant == 0 ? FusionCourses.JaxHandling : FusionCourses.JaxProfileTwo;
                FusionCourse course;
                Assert.That(FusionCourses.BuildRoadCourse(layout, id, garage.Outside, garage.Outward, variant, out course), Is.True, id);
                Assert.That(course.Points.Length, Is.InRange(3, 9), id);
                float length = 0f;
                Vector3 prev = garage.Outside;
                foreach (Vector3 p in course.Points)
                {
                    Assert.That(MathUtil.IsFinite(p), Is.True);
                    length += MathUtil.FlatDistance(prev, p);
                    prev = p;
                }
                Assert.That(length, Is.InRange(150f, 900f), id + " is a short loop");
                Assert.That(MathUtil.FlatDistance(course.Points[course.Points.Length - 1], garage.Outside), Is.LessThan(12f), id + " returns to the garage door");
                Assert.That(course.BrakeAt, Is.InRange(1, course.Points.Length - 2), id + " brakes at the turn, not at the start or the end");
                Assert.That(course.AbilityAt, Is.EqualTo(1));
                Assert.That(course.AbilityUses, Is.EqualTo(FusionCourses.AbilityUsesFor(id)));
                loops.Add(course.Points);
            }
            Assert.That(Vector3.Distance(loops[0][loops[0].Length / 2], loops[1][loops[1].Length / 2]), Is.GreaterThan(20f), "the two Jax loops take different roads");
        }

        [Test]
        public void CourseSamplingKeepsSpacingAndTheEnd()
        {
            List<Vector3> path = new List<Vector3> { Vector3.zero, new Vector3(100f, 0f, 0f), new Vector3(100f, 0f, 60f) };
            List<Vector3> pts = FusionCourses.Sample(path, 40f, 0);
            Assert.That(pts.Count, Is.EqualTo(4));
            Assert.That(pts[0].x, Is.EqualTo(40f).Within(1e-3f));
            Assert.That(pts[pts.Count - 1], Is.EqualTo(new Vector3(100f, 0f, 60f)));
            Assert.That(FusionCourses.Sample(path, 5f, 6).Count, Is.EqualTo(6));
            Assert.That(FusionCourses.Sample(new List<Vector3> { Vector3.zero }, 10f, 0), Is.Empty);
        }

        // ================================================================== save block freezes

        private static readonly string[] GarageSaveFields =
        {
            "Version:Int32", "NextUid:Int32", "PersonalUid:Int32", "Chassis:List<ChassisRecord>", "FusionUnlocked:Boolean", "Forms:List<String>",
            "Profile:String", "UnlockedTrims:List<String>", "Meters:List<FormMeterSave>", "FusionCooldown:Single"
        };

        private static readonly string[] ChassisRecordFields =
        {
            "Uid:Int32", "Class:String", "PaintR:Single", "PaintG:Single", "PaintB:Single", "Health01:Single", "EngineUpgrade:Int32", "Trim:String",
            "Trims:List<String>", "Source:String", "Wrecked:Boolean", "ArmorUpgrade:Int32", "Tune:String"
        };

        private static readonly string[] FormMeterSaveFields =
        {
            "Form:String", "Heat:Single", "Cooldown:Single", "Charges:Int32", "Recharge:Single", "Overheated:Boolean"
        };

        [Test]
        public void GarageSaveFieldsAreOnlyEverAppended()
        {
            Freeze(typeof(GarageSave), GarageSaveFields);
            Freeze(typeof(ChassisRecord), ChassisRecordFields);
            Freeze(typeof(FormMeterSave), FormMeterSaveFields);
            foreach (Type t in new[] { typeof(GarageSave), typeof(ChassisRecord), typeof(FormMeterSave) })
                Assert.That(Attribute.IsDefined(t, typeof(SerializableAttribute)), Is.True, t.Name + " stays [Serializable]");
            FieldInfo garage = typeof(SaveData).GetField("Garage");
            Assert.That(garage, Is.Not.Null);
            Assert.That(garage.FieldType, Is.EqualTo(typeof(GarageSave)));
            GarageSave fresh = new GarageSave();
            Assert.That(fresh.Chassis, Is.Not.Null);
            Assert.That(fresh.Forms, Is.Not.Null);
            Assert.That(fresh.UnlockedTrims, Is.Not.Null);
            Assert.That(fresh.Meters, Is.Not.Null);
            Assert.That(fresh.FusionUnlocked, Is.False, "an old save never gains fusion from a default");
            ChassisRecord rec = new ChassisRecord();
            Assert.That(rec.Trims, Is.Not.Null);
            Assert.That(rec.Tune, Is.Empty);
            Assert.That(rec.Wrecked, Is.False);
        }

        private static void Freeze(Type type, string[] frozen)
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Type ft = f.FieldType;
                string name = ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>) ? "List<" + ft.GetGenericArguments()[0].Name + ">" : ft.Name;
                now[f.Name] = name;
            }
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string have;
                Assert.That(now.TryGetValue(parts[0], out have), Is.True, type.Name + "." + parts[0] + " was renamed or removed; only append fields.");
                Assert.That(have, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type; add a new field instead.");
            }
        }

        // ================================================================== helpers

        /// <summary>The shipped 04C venue catalog, read with the strict quest JSON reader (no engine needed).</summary>
        public sealed class VenueCatalogInput { public string Version; public List<VenueDefinition04C> Venues = new List<VenueDefinition04C>(); }

        private static VenueCatalog04C ReadVenueCatalog()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/InhabitedCity04C/venues.json");
            List<string> errors = new List<string>();
            VenueCatalogInput input = QuestJson.Read<VenueCatalogInput>(File.ReadAllText(path), path, errors, null);
            Assert.That(errors, Is.Empty);
            return new VenueCatalog04C { Version = input.Version, Venues = input.Venues.ToArray() };
        }

        private static int Count(FormBlueprint bp, FormPartRole role)
        {
            int n = 0;
            foreach (FormPart p in bp.Parts) if (p.Role == role) n++;
            return n;
        }

        private static bool Inside(Vector3 v, Vector3 a, Vector3 b)
        {
            return v.x >= a.x && v.x <= b.x && v.y >= a.y && v.y <= b.y && v.z >= a.z && v.z <= b.z;
        }

        private static bool InsideStrict(Vector3 v, Vector3 a, Vector3 b)
        {
            return v.x > a.x && v.x < b.x && v.y > a.y && v.y < b.y && v.z > a.z && v.z < b.z;
        }

        private static float Parse(string s)
        {
            return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
