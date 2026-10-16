using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04D crystal weapons, engine-free rules: the per-uid heat book, the beam integrator (frame-rate equivalence, hitch
    /// discard, ramp reset, proc accumulator, tap spam), the disc's victim and bank rules, Mirage Choir lock eligibility
    /// and dwell, the Mirrorwake redirect and focus rules, query saturation failing closed, projectile admission, the
    /// volley budget and Sunderheart's pull limits.
    /// </summary>
    public class CrystalWeapons04DTests
    {
        private const float Tol = 1e-4f;

        // ================================================================== heat book

        /// <summary>Advances the heat clock in frame-sized steps (Tick caps a single step at one second).</summary>
        private static void Run(CrystalHeat h, float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / 0.05f - 1e-3f);
            for (int i = 0; i < steps; i++) h.Tick(seconds / steps);
        }

        [Test]
        public void HeatAccumulatesAndOverheatsExactlyAtOne()
        {
            CrystalHeat h = new CrystalHeat();
            for (int i = 0; i < 11; i++) Assert.That(h.Add(7, 0.09f, 1.6f), Is.False);
            Assert.That(h.HeatOf(7), Is.EqualTo(0.99f).Within(Tol));
            Assert.That(h.CanFire(7), Is.True);
            Assert.That(h.Add(7, 0.09f, 1.6f), Is.True, "the twelfth shard overheats");
            Assert.That(h.HeatOf(7), Is.EqualTo(1f).Within(Tol));
            Assert.That(h.IsVenting(7), Is.True);
            Assert.That(h.CanFire(7), Is.False);
            Assert.That(h.Add(7, 0.09f, 1.6f), Is.False, "a venting focus takes no more heat");
        }

        [Test]
        public void PassiveCoolingWaitsForTheQuietDelay()
        {
            CrystalHeat h = new CrystalHeat();
            h.Add(1, 0.5f, 1.6f);
            h.Tick(0.3f);
            Assert.That(h.HeatOf(1), Is.EqualTo(0.5f).Within(Tol), "no cooling inside the 0.35 s delay");
            h.Tick(0.1f);
            Assert.That(h.HeatOf(1), Is.EqualTo(0.5f - CrystalFocusDb.PassiveCooling * 0.1f).Within(Tol));
            for (int i = 0; i < 40; i++) h.Tick(0.1f);
            Assert.That(h.HeatOf(1), Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void ForcedVentBlocksFiringForItsDurationThenClears()
        {
            CrystalHeat h = new CrystalHeat();
            Assert.That(h.Add(3, 1.2f, 2f), Is.True);
            h.Tick(1f);
            Assert.That(h.CanFire(3), Is.False);
            Assert.That(h.HeatOf(3), Is.EqualTo(0.5f).Within(Tol), "heat drains linearly over the vent");
            Run(h, 1.01f);
            Assert.That(h.CanFire(3), Is.True);
            Assert.That(h.HeatOf(3), Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void HeatBelongsToTheUidSoSwappingNeverResetsAnOverheat()
        {
            CrystalHeat h = new CrystalHeat();
            h.Add(10, 1f, 2f);
            // The player swaps to another focus (uid 11) and fires it; uid 10 keeps venting on the same clock.
            h.Add(11, 0.3f, 2f);
            h.Tick(0.5f);
            Assert.That(h.IsVenting(10), Is.True, "swapping away does not end the vent");
            Assert.That(h.HeatOf(11), Is.EqualTo(0.3f - CrystalFocusDb.PassiveCooling * 0.5f).Within(Tol));
            // Back to uid 10 at once: still overheated.
            Assert.That(h.CanFire(10), Is.False);
            Run(h, 1.6f);
            Assert.That(h.CanFire(10), Is.True);
        }

        [Test]
        public void ManualVentNeedsHeatAndScalesWithIt()
        {
            CrystalHeat h = new CrystalHeat();
            Assert.That(h.StartManualVent(4, 2f), Is.False, "nothing to vent");
            h.Add(4, 0.03f, 2f);
            Assert.That(h.StartManualVent(4, 2f), Is.False, "below the minimum");
            h.Add(4, 0.47f, 2f);
            Assert.That(h.StartManualVent(4, 2f), Is.True);
            Assert.That(h.StartManualVent(4, 2f), Is.False, "already venting");
            Assert.That(h.CanFire(4), Is.False);
            // 0.5 heat: the vent takes 2 x 0.5 = 1 s and drains from 0.5 (no jump to full).
            Assert.That(h.HeatOf(4), Is.EqualTo(0.5f).Within(Tol));
            h.Tick(0.5f);
            Assert.That(h.HeatOf(4), Is.EqualTo(0.25f).Within(Tol));
            h.Tick(0.51f);
            Assert.That(h.CanFire(4), Is.True);
        }

        [Test]
        public void ResonanceVentAtMostOncePerCooldownPerUid()
        {
            CrystalHeat h = new CrystalHeat();
            Assert.That(h.ResonanceVent(5), Is.False, "a cool focus has nothing to vent");
            h.Add(5, 0.6f, 2f);
            Assert.That(h.ResonanceVent(5), Is.True);
            Assert.That(h.HeatOf(5), Is.EqualTo(0f).Within(Tol));
            h.Add(5, 1f, 2f);
            Assert.That(h.IsVenting(5), Is.True);
            Assert.That(h.ResonanceVent(5), Is.False, "the burst, vent, burst loop is refused inside the cooldown");
            Assert.That(h.IsVenting(5), Is.True);
            // Another focus is independent.
            h.Add(6, 0.5f, 2f);
            Assert.That(h.ResonanceVent(6), Is.True);
            Run(h, 2.05f);
            Assert.That(h.IsVenting(5), Is.False);
            Assert.That(h.ResonanceCooldownLeft(5), Is.GreaterThan(1.9f), "the cooldown outlives the vent");
            Run(h, 2f);
            Assert.That(h.ResonanceCooldownLeft(5), Is.EqualTo(0f).Within(Tol));
            h.Add(5, 0.4f, 2f);
            Assert.That(h.ResonanceVent(5), Is.True, "allowed again after the cooldown");
        }

        [Test]
        public void OneHugeFrameAdvancesTheHeatClockAtMostOneSecond()
        {
            CrystalHeat h = new CrystalHeat();
            h.Add(8, 1f, 2f);
            h.Tick(30f);
            Assert.That(h.Now, Is.EqualTo(1f).Within(Tol));
            Assert.That(h.IsVenting(8), Is.True, "a hitch never skips a vent in one step");
        }

        [Test]
        public void PausedOrBadFrameTimesNeverCoolOrAdvance()
        {
            CrystalHeat h = new CrystalHeat();
            h.Add(2, 0.5f, 2f);
            h.Tick(0f);
            h.Tick(-1f);
            h.Tick(float.NaN);
            Assert.That(h.Now, Is.EqualTo(0f));
            Assert.That(h.HeatOf(2), Is.EqualTo(0.5f).Within(Tol));
        }

        // ================================================================== beam integrator

        private static float SimulateBeam(int fps, float seconds, out int ticks, out float heat, out int procs)
        {
            CrystalBeamIntegrator b = new CrystalBeamIntegrator();
            float dt = 1f / fps;
            int frames = Mathf.RoundToInt(seconds * fps);
            float damage = 0f;
            heat = 0f;
            ticks = 0;
            procs = 0;
            for (int f = 0; f < frames; f++)
            {
                int n = b.Advance(dt);
                for (int i = 0; i < n; i++)
                {
                    float mul = b.TickRamp(42, true);
                    if (b.TickProc(true)) procs++;
                    damage += 10f * mul;
                    heat += 0.028f;
                    ticks++;
                }
            }
            return damage;
        }

        [Test]
        public void BeamTotalsMatchAt30And60And120Fps()
        {
            int t30, t60, t120, p30, p60, p120;
            float h30, h60, h120;
            float d30 = SimulateBeam(30, 3f, out t30, out h30, out p30);
            float d60 = SimulateBeam(60, 3f, out t60, out h60, out p60);
            float d120 = SimulateBeam(120, 3f, out t120, out h120, out p120);
            Assert.That(t30, Is.EqualTo(30));
            Assert.That(t60, Is.EqualTo(30));
            Assert.That(t120, Is.EqualTo(30));
            Assert.That(Mathf.Abs(d30 - d60) / d60, Is.LessThan(0.01f));
            Assert.That(Mathf.Abs(d120 - d60) / d60, Is.LessThan(0.01f));
            Assert.That(Mathf.Abs(h30 - h120) / h120, Is.LessThan(0.01f));
            Assert.That(p30, Is.EqualTo(p120));
            Assert.That(p60, Is.EqualTo(6), "one proc roll per 0.5 s of contact");
        }

        [Test]
        public void BeamHitchIsDiscardedNotPaidOutAsABurst()
        {
            CrystalBeamIntegrator b = new CrystalBeamIntegrator();
            int n = b.Advance(1.0f);
            Assert.That(n, Is.EqualTo(1), "a one second hitch integrates only BeamMaxStep");
            Assert.That(b.DiscardedSeconds, Is.EqualTo(0.9f).Within(1e-3f));
            Assert.That(b.SimulatedSeconds, Is.EqualTo(0.1f).Within(1e-3f));
            Assert.That(b.Advance(0f), Is.EqualTo(0));
            Assert.That(b.Advance(-0.5f), Is.EqualTo(0));
            Assert.That(b.Advance(float.NaN), Is.EqualTo(0));
            Assert.That(b.Advance(float.PositiveInfinity), Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void BeamRampGrowsWithAimedContactAndResetsOnLoss()
        {
            CrystalBeamIntegrator b = new CrystalBeamIntegrator();
            float first = b.TickRamp(9, true);
            Assert.That(first, Is.EqualTo(1f).Within(Tol));
            float last = first;
            for (int i = 0; i < 15; i++) last = b.TickRamp(9, true);
            Assert.That(last, Is.EqualTo(CrystalFocusDb.BeamRampMax).Within(Tol), "full ramp after 1.5 s of maintained aimed contact");
            Assert.That(b.TickRamp(9, true), Is.EqualTo(CrystalFocusDb.BeamRampMax).Within(Tol), "capped at the maximum");
            Assert.That(b.TickRamp(0, true), Is.EqualTo(1f), "no contact this tick");
            Assert.That(b.TickRamp(9, true), Is.EqualTo(1f).Within(Tol), "reconnecting starts from zero");
            for (int i = 0; i < 10; i++) b.TickRamp(9, true);
            Assert.That(b.TickRamp(8, true), Is.EqualTo(1f).Within(Tol), "a new target restarts the ramp");
            for (int i = 0; i < 10; i++) b.TickRamp(8, true);
            Assert.That(b.TickRamp(8, false), Is.EqualTo(1f), "hip fire never ramps");
            Assert.That(b.TickRamp(8, true), Is.EqualTo(1f).Within(Tol));
            for (int i = 0; i < 10; i++) b.TickRamp(8, true);
            b.BreakContact();
            Assert.That(b.TickRamp(8, true), Is.EqualTo(1f).Within(Tol), "an occluded frame breaks contact");
        }

        [Test]
        public void BeamProcsOnlyOnContactTime()
        {
            CrystalBeamIntegrator b = new CrystalBeamIntegrator();
            int procs = 0;
            for (int i = 0; i < 30; i++) if (b.TickProc(false)) procs++;
            Assert.That(procs, Is.EqualTo(0));
            for (int i = 0; i < 30; i++) if (b.TickProc(true)) procs++;
            Assert.That(procs, Is.EqualTo(6));
        }

        [Test]
        public void TappingTheBeamCannotMintExtraTicks()
        {
            CrystalBeamIntegrator b = new CrystalBeamIntegrator();
            int ticks = 0;
            // 120 frames a second for 2 s, the trigger down every other frame: only 1 s of beam is simulated.
            for (int f = 0; f < 240; f++) if ((f & 1) == 0) ticks += b.Advance(1f / 120f);
            Assert.That(ticks, Is.InRange(9, 10));
        }

        // ================================================================== Gyre Crescent

        [Test]
        public void DiscHitsEachActorOnceOutAndOnceBack()
        {
            CrystalShotRules.DiscBook d = new CrystalShotRules.DiscBook();
            Assert.That(d.TryHit(5), Is.EqualTo(DiscHitVerdict.Outbound));
            Assert.That(d.TryHit(5), Is.EqualTo(DiscHitVerdict.None), "a compound body is one actor, one outbound hit");
            Assert.That(d.Recall(), Is.True);
            Assert.That(d.Recall(), Is.False);
            Assert.That(d.TryHit(5), Is.EqualTo(DiscHitVerdict.Return));
            Assert.That(d.TryHit(5), Is.EqualTo(DiscHitVerdict.None));
            Assert.That(d.TryHit(0), Is.EqualTo(DiscHitVerdict.None));
        }

        [Test]
        public void DiscDamagesAtMostThreeUniqueActors()
        {
            CrystalShotRules.DiscBook d = new CrystalShotRules.DiscBook();
            Assert.That(d.TryHit(1), Is.EqualTo(DiscHitVerdict.Outbound));
            Assert.That(d.TryHit(2), Is.EqualTo(DiscHitVerdict.Outbound));
            Assert.That(d.TryHit(3), Is.EqualTo(DiscHitVerdict.Outbound));
            Assert.That(d.TryHit(4), Is.EqualTo(DiscHitVerdict.None), "a fourth actor is never struck");
            d.Recall();
            Assert.That(d.TryHit(2), Is.EqualTo(DiscHitVerdict.Return), "an existing victim may take its one return hit");
            Assert.That(d.TryHit(4), Is.EqualTo(DiscHitVerdict.None));
            Assert.That(d.UniqueVictims, Is.EqualTo(3));
        }

        [Test]
        public void DiscBanksTwiceOffStaticWorldThenReturnsAndABlockedReturnQuenches()
        {
            CrystalShotRules.DiscBook d = new CrystalShotRules.DiscBook();
            Assert.That(d.OnSurface(true), Is.EqualTo(DiscSurfaceVerdict.Bank));
            Assert.That(d.OnSurface(true), Is.EqualTo(DiscSurfaceVerdict.Bank));
            Assert.That(d.OnSurface(true), Is.EqualTo(DiscSurfaceVerdict.BeginReturn), "a third surface ends the outbound flight");
            Assert.That(d.Returning, Is.True);
            Assert.That(d.OnSurface(true), Is.EqualTo(DiscSurfaceVerdict.Quench), "a wall on the way home quenches the disc");
            CrystalShotRules.DiscBook e = new CrystalShotRules.DiscBook();
            Assert.That(e.OnSurface(false), Is.EqualTo(DiscSurfaceVerdict.BeginReturn), "cars, props and wrecks are not bank surfaces");
            Assert.That(e.Banks, Is.EqualTo(0));
        }

        [Test]
        public void DiscReturnsWhenItsTravelBudgetRunsOut()
        {
            CrystalShotRules.DiscBook d = new CrystalShotRules.DiscBook();
            Assert.That(d.Budget, Is.EqualTo(32f));
            Assert.That(d.Spend(31f), Is.False);
            Assert.That(d.Spend(1.5f), Is.True);
            Assert.That(d.Returning, Is.True);
            Assert.That(d.Spend(10f), Is.False, "the return is not charged to the outbound budget");
            d.Reset();
            Assert.That(d.Returning, Is.False);
            Assert.That(d.Travelled, Is.EqualTo(0f));
            Assert.That(d.TryHit(1), Is.EqualTo(DiscHitVerdict.Outbound), "a pooled disc forgets its old victims");
        }

        // ================================================================== Mirage Choir

        private static CrystalShotRules.LockCandidate Hostile()
        {
            return new CrystalShotRules.LockCandidate { Alive = true, Hostile = true, Visible = true, LineOfSight = true, Distance = 20f, AngleDeg = 4f };
        }

        [Test]
        public void LockEligibilityNeedsAVisibleHostileInConeRangeAndSight()
        {
            float range = 48f, half = 9f;
            Assert.That(CrystalShotRules.LockEligible(Hostile(), range, half), Is.True);
            CrystalShotRules.LockCandidate c = Hostile();
            c.Protected = true;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False, "civilians, staff and companions are never locked");
            c = Hostile();
            c.Peaceful = true;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False, "peaceful wildlife is never locked");
            c = Hostile();
            c.Hostile = false;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False);
            c = Hostile();
            c.LineOfSight = false;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False, "no lock through walls");
            c = Hostile();
            c.Visible = false;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False);
            c = Hostile();
            c.Alive = false;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False);
            c = Hostile();
            c.Distance = 48.5f;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False);
            c = Hostile();
            c.AngleDeg = 9.5f;
            Assert.That(CrystalShotRules.LockEligible(c, range, half), Is.False);
        }

        [Test]
        public void LocksNeedSteadyDwellAndCapAtThree()
        {
            CrystalShotRules.LockBook b = new CrystalShotRules.LockBook();
            Assert.That(b.Update(5, 0.1f), Is.EqualTo(0), "first sight credits no time");
            Assert.That(b.Update(5, 0.1f), Is.EqualTo(0));
            Assert.That(b.Update(5, 0.1f), Is.EqualTo(0));
            Assert.That(b.Update(5, 0.1f), Is.EqualTo(0), "0.3 s is not yet a lock");
            Assert.That(b.Update(5, 0.1f), Is.EqualTo(5), "locked after 0.35 s of steady aim on one candidate");
            Assert.That(b.Count, Is.EqualTo(1));
            Assert.That(b.Update(5, 0.5f), Is.EqualTo(0), "a locked actor is not locked twice");
            b.Update(6, 0.1f);
            b.Update(6, 0.3f);
            Assert.That(b.DwellProgress, Is.GreaterThan(0.8f));
            Assert.That(b.Update(7, 0.3f), Is.EqualTo(0), "switching candidates restarts the dwell");
            Assert.That(b.DwellProgress, Is.EqualTo(0f));
            Assert.That(b.Update(7, 0.2f), Is.EqualTo(0));
            Assert.That(b.Update(7, 0.2f), Is.EqualTo(7));
            b.Update(8, 0f);
            Assert.That(b.Update(8, 0.4f), Is.EqualTo(8));
            Assert.That(b.Count, Is.EqualTo(3));
            b.Update(9, 0f);
            Assert.That(b.Update(9, 1f), Is.EqualTo(0), "never more than three locks");
            b.Drop(7);
            Assert.That(b.Count, Is.EqualTo(2));
            b.Update(9, 0f);
            Assert.That(b.Update(9, 0.4f), Is.EqualTo(9));
            b.Clear();
            Assert.That(b.Count, Is.EqualTo(0));
        }

        [Test]
        public void VolleyBudgetLimitsDamageAndProcsOnOneActor()
        {
            Assert.That(CrystalShotRules.ChoirShardScale(0), Is.EqualTo(1f));
            Assert.That(CrystalShotRules.ChoirShardScale(2), Is.EqualTo(1f));
            Assert.That(CrystalShotRules.ChoirShardScale(3), Is.EqualTo(0.5f));
            Assert.That(CrystalShotRules.ChoirShardScale(4), Is.EqualTo(0.5f));
            Assert.That(CrystalShotRules.ChoirVolleyCapShards(5), Is.EqualTo(4f), "five shards on one actor deal four shards' worth");
            Assert.That(CrystalShotRules.ChoirProcAllowed(0), Is.True);
            Assert.That(CrystalShotRules.ChoirProcAllowed(1), Is.False);
            Assert.That(CrystalShotRules.ChoirShardTarget(0, 0), Is.EqualTo(-1), "no locks: straight fan");
            Assert.That(CrystalShotRules.ChoirShardTarget(4, 3), Is.EqualTo(1));
            Assert.That(CrystalShotRules.ChoirShardTarget(3, 2), Is.EqualTo(1));
        }

        // ================================================================== Mirrorwake

        [Test]
        public void RedirectNeverHitsTheDirectVictimAgain()
        {
            Assert.That(CrystalShotRules.RedirectAllowed(5, 5), Is.False, "direct and redirected damage never land on one victim");
            Assert.That(CrystalShotRules.RedirectAllowed(5, 6), Is.True);
            Assert.That(CrystalShotRules.RedirectAllowed(0, 6), Is.True);
            Assert.That(CrystalShotRules.RedirectAllowed(0, 0), Is.False);
        }

        [Test]
        public void FocusAimToleranceAndPlantingRules()
        {
            Vector3 o = Vector3.zero, f = new Vector3(0f, 0f, 1f);
            Assert.That(CrystalShotRules.AimedAtFocus(o, f, 60f, new Vector3(1f, 0f, 10f), 1.2f), Is.True);
            Assert.That(CrystalShotRules.AimedAtFocus(o, f, 60f, new Vector3(1.3f, 0f, 10f), 1.2f), Is.False);
            Assert.That(CrystalShotRules.AimedAtFocus(o, f, 60f, new Vector3(0f, 0f, -5f), 1.2f), Is.False, "a focus behind the muzzle");
            Assert.That(CrystalShotRules.AimedAtFocus(o, f, 60f, new Vector3(0f, 0f, 70f), 1.2f), Is.False, "beyond the pulse's range");
            Assert.That(CrystalShotRules.AimedAtFocus(o, Vector3.zero, 60f, new Vector3(0f, 0f, 5f), 1.2f), Is.False);
            Assert.That(CrystalShotRules.CanPlantFocus(true, 20f, false), Is.True);
            Assert.That(CrystalShotRules.CanPlantFocus(true, 25.5f, false), Is.False);
            Assert.That(CrystalShotRules.CanPlantFocus(true, 10f, true), Is.False, "a pulse that hit someone plants nothing");
            Assert.That(CrystalShotRules.CanPlantFocus(false, 10f, false), Is.False, "only static World geometry takes a focus");
        }

        // ================================================================== queries and admission

        [Test]
        public void SaturatedQueryFailsClosedWithoutAProvenBlocker()
        {
            Assert.That(CrystalShotRules.Saturation(15, 16, false), Is.EqualTo(SaturationVerdict.Proceed));
            Assert.That(CrystalShotRules.Saturation(0, 16, false), Is.EqualTo(SaturationVerdict.Proceed));
            Assert.That(CrystalShotRules.Saturation(16, 16, false), Is.EqualTo(SaturationVerdict.Quench), "a full buffer may hide the nearest wall");
            Assert.That(CrystalShotRules.Saturation(16, 16, true), Is.EqualTo(SaturationVerdict.Proceed));
        }

        [Test]
        public void AdmissionIsWholeEventsUnderTheGraphicsCap()
        {
            Assert.That(CrystalShotRules.ProjectileCap(false), Is.EqualTo(64));
            Assert.That(CrystalShotRules.ProjectileCap(true), Is.EqualTo(32));
            Assert.That(CrystalShotRules.Admit(59, 5, 64), Is.True);
            Assert.That(CrystalShotRules.Admit(60, 5, 64), Is.False, "a volley is refused whole, never trimmed");
            Assert.That(CrystalShotRules.Admit(31, 1, 32), Is.True);
            Assert.That(CrystalShotRules.Admit(32, 1, 32), Is.False);
            Assert.That(CrystalShotRules.Admit(10, 0, 32), Is.True);
        }

        [Test]
        public void ProjectilesSubstepAtMostFiftyMillisecondsAndCapTheFrame()
        {
            Assert.That(CrystalShotRules.Substeps(0.2f, 0.05f), Is.EqualTo(4));
            Assert.That(CrystalShotRules.Substeps(0.0166f, 0.05f), Is.EqualTo(1));
            Assert.That(CrystalShotRules.Substeps(0.051f, 0.05f), Is.EqualTo(2));
            Assert.That(CrystalShotRules.Substeps(0f, 0.05f), Is.EqualTo(0));
            Assert.That(CrystalShotRules.FrameStep(1f), Is.EqualTo(CrystalShotRules.MaxFrameStep));
            Assert.That(CrystalShotRules.FrameStep(-1f), Is.EqualTo(0f));
            Assert.That(CrystalShotRules.FrameStep(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void FanDirectionsSpreadSymmetricallyAboutUp()
        {
            Vector3 r = CrystalShotRules.RotateAboutUp(new Vector3(0f, 0f, 1f), 90f);
            Assert.That(r.x, Is.EqualTo(1f).Within(Tol));
            Assert.That(r.z, Is.EqualTo(0f).Within(Tol));
            Assert.That(CrystalShotRules.FanOffset(0, 3, 6f), Is.EqualTo(-6f).Within(Tol));
            Assert.That(CrystalShotRules.FanOffset(1, 3, 6f), Is.EqualTo(0f).Within(Tol));
            Assert.That(CrystalShotRules.FanOffset(2, 3, 6f), Is.EqualTo(6f).Within(Tol));
            Assert.That(CrystalShotRules.FanOffset(0, 1, 6f), Is.EqualTo(0f));
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            Assert.That(p.Secondary, Is.LessThan(p.Primary), "a fan shard is weaker than a primary shard");
        }

        // ================================================================== Sunderheart

        [Test]
        public void PullOnlyMovesMinorHostileCharactersItCanSee()
        {
            Assert.That(CrystalShotRules.PullAllowed(true, true, true, false, true, true), Is.True);
            Assert.That(CrystalShotRules.PullAllowed(true, true, true, false, true, false), Is.False, "never through walls");
            Assert.That(CrystalShotRules.PullAllowed(true, true, false, false, true, true), Is.False, "bosses and elites are not pulled");
            Assert.That(CrystalShotRules.PullAllowed(true, true, true, true, true, true), Is.False, "never civilians");
            Assert.That(CrystalShotRules.PullAllowed(true, true, true, false, false, true), Is.False, "never vehicles or scenery");
            Assert.That(CrystalShotRules.PullAllowed(true, false, true, false, true, true), Is.False);
            Assert.That(CrystalShotRules.PullAllowed(false, true, true, false, true, true), Is.False);
        }

        [Test]
        public void PullIsBoundedAndRuptureVictimsAreCapped()
        {
            Assert.That(CrystalShotRules.NudgeSpeed(0f, 3.5f), Is.EqualTo(0f));
            Assert.That(CrystalShotRules.NudgeSpeed(3.5f, 3.5f), Is.EqualTo(CrystalShotRules.MaxNudgeSpeed).Within(Tol));
            Assert.That(CrystalShotRules.NudgeSpeed(30f, 3.5f), Is.LessThanOrEqualTo(CrystalShotRules.MaxNudgeSpeed));
            Assert.That(CrystalShotRules.RuptureRadius(true, 3.5f), Is.EqualTo(2.5f));
            Assert.That(CrystalShotRules.RuptureRadius(false, 3.5f), Is.EqualTo(3.5f));
            Assert.That(CrystalShotRules.BossPoise(100f), Is.EqualTo(12f).Within(Tol));
            List<float> d = new List<float> { 3f, 0.5f, 2f, 1f, 2.5f, 0.1f, 3.4f, 1.5f };
            List<int> order = new List<int>();
            CrystalShotRules.NearestFirst(d, CrystalShotRules.RuptureMaxVictims, order);
            Assert.That(order.Count, Is.EqualTo(6));
            Assert.That(order[0], Is.EqualTo(5));
            Assert.That(order[1], Is.EqualTo(1));
            Assert.That(order.Contains(6), Is.False, "the farthest two are left out");
            Assert.That(order.Contains(0), Is.False);
        }
    }
}
