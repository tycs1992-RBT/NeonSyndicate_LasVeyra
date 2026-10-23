using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S crystal focus heat readability (FocusHeatRulesH8S): bands, the next-shot check against the real focus
    /// profiles, shots and beam seconds left, one cue per crossing, and the cached HUD strings.
    /// </summary>
    public sealed class FocusHeatH8STests
    {
        [Test]
        public void BandsReadTheHeatWithoutChangingIt()
        {
            Assert.That(FocusHeatRulesH8S.Band(0f, false, 0.1f), Is.EqualTo(HeatBandH8S.Cool));
            Assert.That(FocusHeatRulesH8S.Band(float.NaN, false, 0.1f), Is.EqualTo(HeatBandH8S.Cool));
            Assert.That(FocusHeatRulesH8S.Band(0.5f, false, 0.1f), Is.EqualTo(HeatBandH8S.Warm));
            Assert.That(FocusHeatRulesH8S.Band(0.8f, false, 0.1f), Is.EqualTo(HeatBandH8S.High));
            Assert.That(FocusHeatRulesH8S.Band(0.92f, false, 0.1f), Is.EqualTo(HeatBandH8S.Critical), "the next shot overheats");
            Assert.That(FocusHeatRulesH8S.Band(0.5f, false, 0.6f), Is.EqualTo(HeatBandH8S.Critical), "a big charged shot can be critical from warm");
            Assert.That(FocusHeatRulesH8S.Band(0.3f, true, 0.1f), Is.EqualTo(HeatBandH8S.Venting));
            Assert.That(FocusHeatRulesH8S.HighAt, Is.GreaterThan(FocusHeatRulesH8S.WarmAt));
            Assert.That(FocusHeatRulesH8S.HighAt, Is.LessThan(1f));
        }

        [Test]
        public void TheNextShotUsesTheRealProfiles()
        {
            CrystalProfile shard = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            CrystalProfile prism = CrystalFocusDb.Get(CrystalBehavior.PrismThread);
            CrystalProfile sunder = CrystalFocusDb.Get(CrystalBehavior.Sunderheart);
            Assert.That(FocusHeatRulesH8S.NextHeat(false, false, shard.Heat, shard.ChargedHeat), Is.EqualTo(shard.Heat));
            Assert.That(FocusHeatRulesH8S.NextHeat(false, true, shard.Heat, shard.ChargedHeat), Is.EqualTo(shard.ChargedHeat), "a ready fan adds its charged heat");
            Assert.That(FocusHeatRulesH8S.NextHeat(true, false, prism.Heat, prism.ChargedHeat), Is.EqualTo(prism.Heat * FocusHeatRulesH8S.BeamCriticalSeconds).Within(1e-5f), "the beam's heat is per second");
            Assert.That(sunder.ChargedHeat, Is.GreaterThan(sunder.Heat));
            Assert.That(FocusHeatRulesH8S.Band(0.55f, false, FocusHeatRulesH8S.NextHeat(false, true, sunder.Heat, sunder.ChargedHeat)), Is.EqualTo(HeatBandH8S.Critical),
                "a concentrated orb at 55 percent heat would overheat: the HUD says so before the release");
        }

        [Test]
        public void ShotsAndBeamSecondsLeft()
        {
            Assert.That(FocusHeatRulesH8S.ShotsLeft(0.5f, 0.2f), Is.EqualTo(2), "0.7 and 0.9 fit, the third overheats");
            Assert.That(FocusHeatRulesH8S.ShotsLeft(0.8f, 0.2f), Is.EqualTo(0), "the next shot overheats");
            Assert.That(FocusHeatRulesH8S.ShotsLeft(0f, 0.25f), Is.EqualTo(3), "exactly 1.0 is the overheat, not a fourth shot");
            Assert.That(FocusHeatRulesH8S.ShotsLeft(1f, 0.1f), Is.EqualTo(0));
            Assert.That(FocusHeatRulesH8S.ShotsLeft(0.4f, 0f), Is.EqualTo(-1), "no heat, no count");
            Assert.That(FocusHeatRulesH8S.BeamSecondsLeft(0.72f, 0.28f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(FocusHeatRulesH8S.BeamSecondsLeft(0.3f, 0f), Is.EqualTo(-1f));
            for (int i = 0; i < 6; i++)
            {
                CrystalProfile p = CrystalFocusDb.Get((CrystalBehavior)(i + 1));
                if (p == null || p.Behavior == CrystalBehavior.PrismThread) continue;
                int fromCold = FocusHeatRulesH8S.ShotsLeft(0f, p.Heat);
                Assert.That(fromCold, Is.GreaterThanOrEqualTo(1), p.Name + " fires at least once from cold before it overheats");
                Assert.That(fromCold * p.Heat, Is.LessThan(1f), p.Name + " the counted shots really fit");
            }
        }

        [Test]
        public void OneCuePerCrossingAndNoneWhenCooling()
        {
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Warm, HeatBandH8S.High), Is.EqualTo(HeatCueH8S.Warn));
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Cool, HeatBandH8S.High), Is.EqualTo(HeatCueH8S.Warn));
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.High, HeatBandH8S.High), Is.EqualTo(HeatCueH8S.None), "staying high is silent");
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.High, HeatBandH8S.Critical), Is.EqualTo(HeatCueH8S.Critical));
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Cool, HeatBandH8S.Critical), Is.EqualTo(HeatCueH8S.Critical), "a big shot skips straight to critical");
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Critical, HeatBandH8S.Critical), Is.EqualTo(HeatCueH8S.None));
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Critical, HeatBandH8S.Venting), Is.EqualTo(HeatCueH8S.None), "the overheat has its own vent sound");
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Venting, HeatBandH8S.Venting), Is.EqualTo(HeatCueH8S.None));
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Venting, HeatBandH8S.Cool), Is.EqualTo(HeatCueH8S.Ready), "the vent ended: fire again");
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.High, HeatBandH8S.Warm), Is.EqualTo(HeatCueH8S.None), "cooling is silent");
            Assert.That(FocusHeatRulesH8S.Cue(HeatBandH8S.Critical, HeatBandH8S.High), Is.EqualTo(HeatCueH8S.None));
        }

        [Test]
        public void HudWordsAreCachedConstants()
        {
            Assert.That(FocusHeatRulesH8S.Label(HeatBandH8S.High), Is.EqualTo("HEAT HIGH"));
            Assert.That(FocusHeatRulesH8S.Label(HeatBandH8S.Cool), Is.EqualTo(""));
            Assert.That(FocusHeatRulesH8S.Prompt(HeatBandH8S.High, false), Does.StartWith("HEAT HIGH").And.Contains("[R]"));
            Assert.That(FocusHeatRulesH8S.Prompt(HeatBandH8S.High, true), Does.Contain("[D-pad Down]"), "the reload tap on every pad family");
            Assert.That(FocusHeatRulesH8S.Prompt(HeatBandH8S.Critical, false), Does.StartWith(FocusHeatRulesH8S.Label(HeatBandH8S.Critical)));
            Assert.That(FocusHeatRulesH8S.Prompt(HeatBandH8S.Warm, false), Is.EqualTo(""));
            Assert.That(ReferenceEquals(FocusHeatRulesH8S.Prompt(HeatBandH8S.High, false), FocusHeatRulesH8S.Prompt(HeatBandH8S.High, false)), Is.True, "no new string per call");
            Assert.That(FocusHeatRulesH8S.ShotsText(3), Is.EqualTo("3 LEFT"));
            Assert.That(FocusHeatRulesH8S.ShotsText(40), Is.EqualTo("12+ LEFT"));
            Assert.That(FocusHeatRulesH8S.ShotsText(-1), Is.EqualTo(""));
            Assert.That(ReferenceEquals(FocusHeatRulesH8S.ShotsText(2), FocusHeatRulesH8S.ShotsText(2)), Is.True);
            Assert.That(FocusHeatRulesH8S.SecondsText(1.35f), Is.EqualTo("1.4 s"), "a countdown rounds up");
            Assert.That(FocusHeatRulesH8S.SecondsText(0.01f), Is.EqualTo("0.1 s"), "never 0.0 while time is left");
            Assert.That(FocusHeatRulesH8S.SecondsText(0f), Is.EqualTo("0.0 s"));
            Assert.That(FocusHeatRulesH8S.SecondsText(float.NaN), Is.EqualTo("0.0 s"));
            Assert.That(FocusHeatRulesH8S.SecondsText(50f), Is.EqualTo("9.9 s"));
            Assert.That(FocusHeatRulesH8S.SecondsText(2f), Is.EqualTo("2.0 s"));
        }
    }
}
