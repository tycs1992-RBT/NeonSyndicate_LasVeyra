using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S hit feedback: when a hit earns an impact freeze (HitWeightRulesH8S), how kills are confirmed and stream, the
    /// killing blow's damage number and the number text cache (no string per frame), and the new synthesized cues.
    /// </summary>
    public sealed class CombatFeedbackH8STests
    {
        [Test]
        public void MeleeAndBurstsKeepTheirOwnFreeze()
        {
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Melee, false, 1f, true, KillTierH8S.Elite, false, 9f), Is.EqualTo(0f), "PlayerCombat freezes melee");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, true, 1f, true, KillTierH8S.Elite, false, 9f), Is.EqualTo(0f), "ResonanceSystem freezes bursts");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 1f, true, KillTierH8S.Boss, false, 9f), Is.EqualTo(0f), "a boss's death sequence slows the world itself");
        }

        [Test]
        public void OnlyKillsAndHeavyHitsFreezeAndNeverTooOften()
        {
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.05f, false, KillTierH8S.None, false, 9f), Is.EqualTo(0f), "an ordinary hit has no freeze");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.05f, false, KillTierH8S.Normal, false, 9f), Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Power, false, 0.05f, false, KillTierH8S.Elite, false, 9f), Is.EqualTo(0.06f).Within(1e-5f));
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.3f, false, KillTierH8S.None, false, 9f), Is.EqualTo(0.04f).Within(1e-5f), "a quarter of its health in one hit");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.15f, true, KillTierH8S.None, false, 9f), Is.EqualTo(0.025f).Within(1e-5f), "a big critical");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.05f, true, KillTierH8S.None, false, 9f), Is.EqualTo(0f), "a small critical");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Explosion, false, 1f, false, KillTierH8S.Normal, false, 0.1f), Is.EqualTo(0f), "within MinStopGap of the last freeze");
            Assert.That(HitWeightRulesH8S.HitStop(DamageType.Explosion, false, 1f, false, KillTierH8S.Normal, false, float.NaN), Is.EqualTo(0f), "a broken clock never freezes");
            float full = HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.05f, false, KillTierH8S.Elite, false, 9f);
            float reduced = HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 0.05f, false, KillTierH8S.Elite, true, 9f);
            Assert.That(reduced, Is.EqualTo(full * HitWeightRulesH8S.ReducedScale).Within(1e-5f), "reduced effects shrink it like the melee freeze");
            for (int k = 0; k < 4; k++)
                Assert.That(HitWeightRulesH8S.HitStop(DamageType.Bullet, false, 5f, true, (KillTierH8S)k, false, 9f), Is.LessThanOrEqualTo(HitWeightRulesH8S.MaxStop));
            Assert.That(HitWeightRulesH8S.MinStopGap, Is.GreaterThanOrEqualTo(0.2f), "a fast gun never stutters");
        }

        [Test]
        public void KillTiersAndStreaks()
        {
            Assert.That(HitWeightRulesH8S.KillTier(false, true, true), Is.EqualTo(KillTierH8S.None));
            Assert.That(HitWeightRulesH8S.KillTier(true, false, false), Is.EqualTo(KillTierH8S.Normal));
            Assert.That(HitWeightRulesH8S.KillTier(true, true, false), Is.EqualTo(KillTierH8S.Elite));
            Assert.That(HitWeightRulesH8S.KillTier(true, true, true), Is.EqualTo(KillTierH8S.Boss));
            int s = 0;
            s = HitWeightRulesH8S.NextStreak(s, 5f);
            Assert.That(s, Is.EqualTo(0), "a lone kill starts no streak");
            s = HitWeightRulesH8S.NextStreak(s, 0.3f);
            s = HitWeightRulesH8S.NextStreak(s, 0.3f);
            Assert.That(s, Is.EqualTo(2));
            for (int i = 0; i < 10; i++) s = HitWeightRulesH8S.NextStreak(s, 0.1f);
            Assert.That(s, Is.EqualTo(HitWeightRulesH8S.MaxStreakSteps), "a streak stops climbing");
            Assert.That(HitWeightRulesH8S.NextStreak(s, HitWeightRulesH8S.StreakWindow + 0.01f), Is.EqualTo(0), "a pause ends it");
            Assert.That(HitWeightRulesH8S.NextStreak(s, float.NaN), Is.EqualTo(0));
            Assert.That(HitWeightRulesH8S.StreakPitch(0), Is.EqualTo(1f).Within(1e-5f));
            float last = 1f;
            for (int i = 1; i <= HitWeightRulesH8S.MaxStreakSteps; i++)
            {
                float p = HitWeightRulesH8S.StreakPitch(i);
                Assert.That(p, Is.GreaterThan(last), "each kill in a streak rings a little higher");
                last = p;
            }
            Assert.That(HitWeightRulesH8S.StreakPitch(99), Is.EqualTo(last).Within(1e-5f));
            Assert.That(last, Is.LessThan(1.6f), "and never squeaks");
        }

        [Test]
        public void TheKillingBlowStandsOut()
        {
            DamageNumberBook b = new DamageNumberBook();
            b.Add(1, DamageNumberKind.Normal, Element.None, 20f, Vector3.zero);
            b.Tick(0.1f);
            b.Add(1, DamageNumberKind.Normal, Element.None, 15f, Vector3.zero, true);
            Assert.That(b.Count, Is.EqualTo(1), "the killing blow merges into the climbing number");
            DamageNumberEntry e = b[0];
            Assert.That(e.Kill, Is.True);
            Assert.That(e.Age, Is.EqualTo(0f), "it pops afresh");
            Assert.That(e.Life, Is.GreaterThanOrEqualTo(DamageNumberBook.KillLife));
            Assert.That(e.Amount, Is.EqualTo(35f).Within(1e-4f));
            DamageNumberEntry plain = e;
            plain.Kill = false;
            Assert.That(DamageNumberBook.DrawSize(e), Is.GreaterThan(DamageNumberBook.DrawSize(plain)), "drawn bigger");
            b.Add(2, DamageNumberKind.Crit, Element.None, 50f, Vector3.zero, true);
            Assert.That(b[1].Kill, Is.True, "a killing blow that starts its own number is marked too");
            b.Add(3, DamageNumberKind.Normal, Element.None, 9f, Vector3.zero);
            Assert.That(b[2].Kill, Is.False);
        }

        [Test]
        public void NumberTextIsCachedNotRebuiltEveryFrame()
        {
            DamageNumberBook b = new DamageNumberBook();
            b.Add(1, DamageNumberKind.Normal, Element.None, 7.4f, Vector3.zero);
            Assert.That(b[0].Text, Is.EqualTo("7"));
            string before = b[0].Text;
            b.Tick(0.05f);
            Assert.That(ReferenceEquals(b[0].Text, before), Is.True, "ageing never rebuilds the text");
            DamageNumberEntry e = b[0];
            DamageNumberBook.RefreshText(ref e);
            Assert.That(ReferenceEquals(e.Text, before), Is.True, "an unchanged amount keeps its string");
            b.Add(1, DamageNumberKind.Normal, Element.None, 5f, Vector3.zero);
            Assert.That(b[0].Text, Is.EqualTo("12"), "a merged hit updates it");
            b.Add(2, DamageNumberKind.Burst, Element.None, 88f, Vector3.zero);
            Assert.That(b[1].Text, Is.EqualTo("88!"), "a burst keeps its exclamation");
            DamageNumberEntry fresh = new DamageNumberEntry { Amount = 3f };
            DamageNumberBook.RefreshText(ref fresh);
            Assert.That(fresh.Text, Is.EqualTo("3"), "an entry with no text yet gets one");
        }

        [Test]
        public void NewCuesAreCleanSynthesizedClips()
        {
            CombatSounds.Clip[] clips = { CombatSounds.Clip.PhaseShift, CombatSounds.Clip.Kill, CombatSounds.Clip.KillHeavy, CombatSounds.Clip.HeatWarn, CombatSounds.Clip.HeatCritical, CombatSounds.Clip.VentReady };
            foreach (CombatSounds.Clip c in clips)
            {
                float[] d = CombatSounds.Make(c);
                Assert.That(d.Length, Is.GreaterThan(AudioSynth.Rate / 10), c + " has a body");
                Assert.That(d.Length, Is.LessThanOrEqualTo(AudioSynth.Rate * 2), c + " stays short");
                float peak = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    Assert.That(float.IsNaN(d[i]) || float.IsInfinity(d[i]), Is.False, c + " sample " + i);
                    peak = Mathf.Max(peak, Mathf.Abs(d[i]));
                }
                Assert.That(peak, Is.InRange(0.5f, 0.901f), c + " is normalized");
            }
            Assert.That((int)CombatSounds.Clip.Burst, Is.EqualTo(0), "the original clips keep their places");
            Assert.That((int)CombatSounds.Clip.PartBreak, Is.EqualTo(4));
        }
    }
}
