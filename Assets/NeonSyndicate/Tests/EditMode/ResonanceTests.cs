using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The Resonance loop's rules (ResonanceRules, ChargeBucket, ResonanceMarkBook, CombatSounds): every gun charges the
    /// crystal bar at about the same rate per second of good aim, criticals and kills give more, the per-second cap
    /// holds, each offensive power maps to one of the four marks, marks expire and are consumed once, the burst and
    /// splash numbers, the magazine refill, and the synthesized sounds. Pure maths, so they run offline.
    /// </summary>
    public sealed class ResonanceTests
    {
        /// <summary>Energy per second of continuous body hits with a gun (every shot hits, every pellet lands).</summary>
        private static float PerSecond(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            float shot = ResonanceRules.ShotSeconds(d.Interval, false, false, 0f);
            int pellets = Mathf.Max(1, d.Pellets);
            float perHit = ResonanceRules.GunCharge(shot, pellets, false, false);
            return perHit * pellets / d.Interval;
        }

        [Test]
        public void SmgsAndSnipersChargeAboutEquallyPerSecond()
        {
            WeaponId[] guns = { WeaponId.MicroSpray, WeaponId.CoilBurst, WeaponId.VectorSidearm, WeaponId.BrassHammer, WeaponId.ArcCarbine, WeaponId.CinderLMG, WeaponId.LongWatch, WeaponId.GlassShot, WeaponId.RiotDrum, WeaponId.Buzzsaw };
            foreach (WeaponId g in guns)
            {
                float ps = PerSecond(g);
                Assert.That(ps, Is.InRange(ResonanceRules.ChargePerSecond * 0.9f, ResonanceRules.ChargePerSecond * 1.1f), g + " charges " + ps + " per second");
            }
            float smg = PerSecond(WeaponId.MicroSpray), sniper = PerSecond(WeaponId.LongWatch);
            Assert.That(Mathf.Abs(smg - sniper) / sniper, Is.LessThan(0.1f), "an SMG and a sniper rifle charge within 10% of each other");
        }

        [Test]
        public void CriticalsAndKillsChargeMoreAndMeleeChargesALittle()
        {
            float body = ResonanceRules.GunCharge(0.5f, 1, false, false);
            float crit = ResonanceRules.GunCharge(0.5f, 1, true, false);
            float kill = ResonanceRules.GunCharge(0.5f, 1, false, true);
            Assert.That(crit, Is.EqualTo(body * ResonanceRules.CritChargeFactor).Within(1e-4f));
            Assert.That(kill, Is.EqualTo(body + ResonanceRules.GunKillBonus).Within(1e-4f));
            Assert.That(ResonanceRules.MeleeCharge, Is.InRange(1f, 4f), "melee charges a little");
            Assert.That(ResonanceRules.GunCharge(0.5f, 8, false, false), Is.EqualTo(body / 8f).Within(1e-4f), "a shotgun's pellets split one shot's charge");
        }

        [Test]
        public void ShotSecondsCountSpinUpAndChargeShots()
        {
            Assert.That(ResonanceRules.ShotSeconds(0.1f, true, false, 0f), Is.LessThan(0.1f), "a spun-up gun fires faster");
            Assert.That(ResonanceRules.ShotSeconds(0.4f, false, true, 1f), Is.GreaterThan(1f), "a charge shot counts its hold");
            Assert.That(ResonanceRules.ShotSeconds(10f, false, false, 0f), Is.EqualTo(ResonanceRules.MaxShotSeconds));
            Assert.That(ResonanceRules.ShotSeconds(0.001f, false, false, 0f), Is.EqualTo(ResonanceRules.MinShotSeconds));
        }

        [Test]
        public void TheBucketCapsGainsPerSecond()
        {
            ChargeBucket b = new ChargeBucket(ResonanceRules.MaxChargePerSecond);
            float got = 0f;
            // A flood of sniper headshots in one second never gives more than the cap (plus what refills meanwhile).
            for (int i = 0; i < 20; i++) got += b.Take(10f, i * 0.01f);
            Assert.That(got, Is.LessThanOrEqualTo(ResonanceRules.MaxChargePerSecond * 1.2f + 0.01f));
            Assert.That(b.Take(1f, 5f), Is.EqualTo(1f).Within(1e-4f), "it refills over time");
            float second = 0f;
            for (int i = 0; i < 100; i++) second += b.Take(1f, 5f + i * 0.01f);
            Assert.That(second, Is.LessThanOrEqualTo(ResonanceRules.MaxChargePerSecond * 2f + 0.01f), "a sustained stream averages near the cap");
            Assert.That(b.Take(-3f, 9f), Is.EqualTo(0f), "never negative");
        }

        [Test]
        public void EveryOffensivePowerLeavesAMark()
        {
            Assert.That(ResonanceRules.StateForSpell("EMBER_CHAIN"), Is.EqualTo(ResonanceState.Kindled));
            Assert.That(ResonanceRules.StateForSpell("SUNBOLT"), Is.EqualTo(ResonanceState.Kindled));
            Assert.That(ResonanceRules.StateForSpell("STATIC_LANCE"), Is.EqualTo(ResonanceState.Stormlit));
            Assert.That(ResonanceRules.StateForSpell("ROOF_ROC_CALL"), Is.EqualTo(ResonanceState.Stormlit));
            Assert.That(ResonanceRules.StateForSpell("BRASS_GRAVITY"), Is.EqualTo(ResonanceState.Unmoored));
            Assert.That(ResonanceRules.StateForSpell("GLASS_SIGHT"), Is.EqualTo(ResonanceState.Glassbound));
            Assert.That(ResonanceRules.StateForSpell("CANAL_MEND"), Is.EqualTo(ResonanceState.None), "a heal marks nothing");
            Assert.That(ResonanceRules.StateForSpell("QUARTZ_WARD"), Is.EqualTo(ResonanceState.None));
            // Every spell id in the game is known (unknown ids would silently prime nothing).
            HashSet<string> ids = new HashSet<string>();
            foreach (Spell s in Abilities.Spells) ids.Add(s.Id);
            foreach (string id in new[] { "EMBER_CHAIN", "SUNBOLT", "STATIC_LANCE", "ROOF_ROC_CALL", "BRASS_GRAVITY", "GLASS_SIGHT" })
                Assert.That(ids.Contains(id), Is.True, id + " is a real spell id");
            for (int i = 1; i <= 4; i++)
            {
                ResonanceState s = (ResonanceState)i;
                Assert.That(ResonanceRules.StateName(s), Is.Not.Empty);
                Assert.That(ResonanceRules.SplashRadius(s), Is.GreaterThan(0f));
                Assert.That(ResonanceRules.SplashShare(s), Is.InRange(0.1f, 0.6f));
            }
        }

        [Test]
        public void MarksExpireAndAreConsumedOnce()
        {
            ResonanceMarkBook b = new ResonanceMarkBook();
            b.Prime(7, ResonanceState.Stormlit, 10f, ResonanceRules.MarkSeconds);
            Assert.That(b.StateOf(7, 12f), Is.EqualTo(ResonanceState.Stormlit));
            Assert.That(b.Remaining(7, 13f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(b.StateOf(7, 10f + ResonanceRules.MarkSeconds + 0.01f), Is.EqualTo(ResonanceState.None), "expired");
            b.Prime(7, ResonanceState.Kindled, 20f, 6f);
            b.Prime(7, ResonanceState.Glassbound, 21f, 6f);
            Assert.That(b.StateOf(7, 22f), Is.EqualTo(ResonanceState.Glassbound), "the newest mark replaces the old one");
            ResonanceState s;
            Assert.That(b.Consume(7, 22f, out s), Is.True);
            Assert.That(s, Is.EqualTo(ResonanceState.Glassbound));
            Assert.That(b.Consume(7, 22f, out s), Is.False, "a mark bursts once");
            b.Prime(1, ResonanceState.Unmoored, 0f, 1f);
            b.Prime(2, ResonanceState.Unmoored, 0f, 5f);
            List<int> gone = new List<int>();
            b.Prune(2f, gone);
            Assert.That(gone, Is.EquivalentTo(new[] { 1 }));
            Assert.That(b.Count, Is.EqualTo(1));
            b.Prime(3, ResonanceState.None, 0f, 5f);
            Assert.That(b.StateOf(3, 1f), Is.EqualTo(ResonanceState.None), "None never marks");
            Assert.That(ResonanceRules.MarkDuration(true), Is.LessThan(ResonanceRules.MarkDuration(false)), "bosses shake marks off sooner");
        }

        [Test]
        public void BurstsHitHarderButLessOnBosses()
        {
            Assert.That(ResonanceRules.BurstDamageMultiplier(false), Is.EqualTo(2.5f));
            Assert.That(ResonanceRules.BurstDamageMultiplier(true), Is.EqualTo(1.75f));
            Assert.That(ResonanceRules.SplashDamage(ResonanceState.Kindled, 100f), Is.EqualTo(35f).Within(1e-4f));
            Assert.That(ResonanceRules.SplashDamage(ResonanceState.Glassbound, 100000f), Is.EqualTo(ResonanceRules.SplashCap * 0.45f).Within(1e-3f), "the splash is capped");
            Assert.That(ResonanceRules.SplashDamage(ResonanceState.None, 100f), Is.EqualTo(0f));
            Assert.That(ResonanceRules.StormJumps, Is.InRange(2, 3), "lightning chains to two or three foes");
        }

        [Test]
        public void TheRefillFillsTheMagazineFromTheReserve()
        {
            Assert.That(ResonanceRules.RefillRounds(30, 4, 200), Is.EqualTo(26));
            Assert.That(ResonanceRules.RefillRounds(30, 4, 10), Is.EqualTo(10), "never more than the reserve");
            Assert.That(ResonanceRules.RefillRounds(30, 30, 200), Is.EqualTo(0), "a full magazine stays full");
            Assert.That(ResonanceRules.RefillRounds(0, 0, 200), Is.EqualTo(0), "guns without magazines");
            Assert.That(ResonanceRules.RefillRounds(8, -2, 200), Is.EqualTo(8));
        }

        [Test]
        public void CombatSoundsSynthesize()
        {
            for (int i = 0; i < (int)CombatSounds.Clip.Count; i++)
            {
                float[] b = CombatSounds.Make((CombatSounds.Clip)i);
                Assert.That(b.Length, Is.GreaterThan(1000), (CombatSounds.Clip)i + " has samples");
                float peak = 0f;
                bool finite = true;
                foreach (float v in b)
                {
                    if (float.IsNaN(v) || float.IsInfinity(v)) finite = false;
                    peak = Mathf.Max(peak, Mathf.Abs(v));
                }
                Assert.That(finite, Is.True, (CombatSounds.Clip)i + " is finite");
                Assert.That(peak, Is.InRange(0.5f, 1f), (CombatSounds.Clip)i + " is normalised");
            }
        }
    }
}
