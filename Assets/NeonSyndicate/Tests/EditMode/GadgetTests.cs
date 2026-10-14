using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Gadgets (Phase 6): the twelve variants, unlock levels, cooldown maths and its cap, level and mod scaling, mod
    /// rolls and drop chances, blast falloff and cones, and the synthesized gadget sounds.
    /// </summary>
    public sealed class GadgetTests
    {
        private static bool Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (c > 126 || c == (char)0x2013 || c == (char)0x2014) return false;
            return true;
        }

        private static int MinOf(HashSet<int> s)
        {
            int m = int.MaxValue;
            foreach (int v in s) m = Mathf.Min(m, v);
            return m;
        }

        private static int MaxOf(HashSet<int> s)
        {
            int m = int.MinValue;
            foreach (int v in s) m = Mathf.Max(m, v);
            return m;
        }

        [Test]
        public void EveryVariantIsDefined()
        {
            Assert.That(GadgetRules.GadgetCount, Is.EqualTo(12));
            HashSet<string> variants = new HashSet<string>();
            for (int id = 0; id < GadgetRules.GadgetCount; id++)
            {
                GadgetSpec s = GadgetRules.Get(id);
                Assert.That(s, Is.Not.Null, "spec " + id);
                Assert.That(s.Id, Is.EqualTo(id));
                Assert.That(GadgetRules.IdOf(s.Kind, s.Variant), Is.EqualTo(id));
                Assert.That(GadgetRules.KindOf(id), Is.EqualTo(s.Kind));
                Assert.That(GadgetRules.VariantOf(id), Is.EqualTo(s.Variant));
                Assert.That(Clean(s.Name) && Clean(s.VariantName) && Clean(s.Blurb), Is.True, "plain ASCII text without dashes: " + s.FullName);
                Assert.That(variants.Add(s.VariantName), Is.True, "variant names are unique: " + s.VariantName);
                Assert.That(s.Cooldown, Is.GreaterThan(5f), s.FullName);
                Assert.That(s.Duration, Is.GreaterThan(0f), s.FullName);
                Assert.That(GadgetRules.Get(s.Kind, 1 - s.Variant).Name, Is.EqualTo(s.Name), "both variants share the gadget name");
            }
            Assert.That(GadgetRules.Get(-1), Is.Null);
            Assert.That(GadgetRules.Get(GadgetRules.GadgetCount), Is.Null);
        }

        [Test]
        public void SelfCastAndThrownGadgetsAreTheRightOnes()
        {
            Assert.That(GadgetRules.Get(GadgetKind.Drone, 0).SelfCast, Is.True);
            Assert.That(GadgetRules.Get(GadgetKind.Drone, 1).SelfCast, Is.True);
            Assert.That(GadgetRules.Get(GadgetKind.Barrier, 1).SelfCast, Is.True, "the Buckler is handheld");
            Assert.That(GadgetRules.Get(GadgetKind.Barrier, 0).SelfCast, Is.False, "the Palisade is placed");
            Assert.That(GadgetRules.Get(GadgetKind.Scanner, 0).SelfCast, Is.True);
            Assert.That(GadgetRules.Get(GadgetKind.Turret, 0).SelfCast, Is.False);
            Assert.That(GadgetRules.Get(GadgetKind.Mine, 1).SelfCast, Is.False);
            Assert.That(GadgetRules.Get(GadgetKind.Vapor, 0).SelfCast, Is.False);
            // Repeating effects need an interval; shields need hit points.
            foreach (GadgetKind k in new[] { GadgetKind.Turret, GadgetKind.Drone, GadgetKind.Vapor })
                for (int v = 0; v < 2; v++) Assert.That(GadgetRules.Get(k, v).Interval, Is.GreaterThan(0f), k + " " + v);
            Assert.That(GadgetRules.Get(GadgetKind.Barrier, 0).Shield, Is.GreaterThan(GadgetRules.Get(GadgetKind.Barrier, 1).Shield), "the wall is sturdier than the handheld shield");
        }

        [Test]
        public void UnlocksSpreadAcrossTheLevels()
        {
            HashSet<int> first = new HashSet<int>(), second = new HashSet<int>();
            for (int id = 0; id < GadgetRules.GadgetCount; id++)
            {
                int lvl = GadgetRules.UnlockLevel(id);
                if (GadgetRules.VariantOf(id) == 0) Assert.That(first.Add(lvl), Is.True, "one first variant per level");
                else Assert.That(second.Add(lvl), Is.True, "one second variant per level");
                Assert.That(GadgetRules.IsUnlocked(id, lvl, false), Is.True);
                Assert.That(GadgetRules.IsUnlocked(id, lvl - 1, false), Is.False);
                Assert.That(GadgetRules.IsUnlocked(id, 1, true), Is.True, "the F2 unlock opens everything");
            }
            Assert.That(MinOf(first), Is.EqualTo(1));
            Assert.That(MaxOf(first), Is.EqualTo(6));
            Assert.That(MinOf(second), Is.EqualTo(8));
            Assert.That(MaxOf(second), Is.EqualTo(13));
            int atStart = 0;
            for (int id = 0; id < GadgetRules.GadgetCount; id++) if (GadgetRules.IsUnlocked(id, 1, false)) atStart++;
            Assert.That(atStart, Is.EqualTo(1), "a new game starts with one gadget");
            Assert.That(GadgetRules.IsUnlocked(GadgetRules.IdOf(GadgetKind.Scanner, 0), 1, false), Is.True, "the Ripple scanner is the first gadget");
            Assert.That(GadgetRules.IsUnlocked(-1, 99, true), Is.False);
        }

        [Test]
        public void CooldownCutsAreCapped()
        {
            GadgetSpec s = GadgetRules.Get(GadgetKind.Turret, 0);
            Assert.That(GadgetRules.Cooldown(s, 0f), Is.EqualTo(s.Cooldown).Within(1e-4f));
            Assert.That(GadgetRules.Cooldown(s, 0.2f), Is.EqualTo(s.Cooldown * 0.8f).Within(1e-4f));
            Assert.That(GadgetRules.Cooldown(s, 0.95f), Is.EqualTo(s.Cooldown * (1f - GadgetRules.MaxCooldownCut)).Within(1e-4f), "never more than half off");
            Assert.That(GadgetRules.Cooldown(s, -1f), Is.EqualTo(s.Cooldown).Within(1e-4f));
            Assert.That(GadgetRules.Cooldown(null, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void CooldownTimerTicksAndCuts()
        {
            GadgetCooldown c = new GadgetCooldown();
            Assert.That(c.Ready, Is.True);
            Assert.That(c.Fraction, Is.EqualTo(0f));
            c.Start(10f);
            Assert.That(c.Ready, Is.False);
            Assert.That(c.Fraction, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(c.Tick(4f), Is.False);
            Assert.That(c.Remaining, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(c.Fraction, Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(c.Cut(3f), Is.False);
            Assert.That(c.Remaining, Is.EqualTo(3f).Within(1e-4f));
            Assert.That(c.Tick(-5f), Is.False, "negative time is ignored");
            Assert.That(c.Tick(3.5f), Is.True, "finishes exactly once");
            Assert.That(c.Ready, Is.True);
            Assert.That(c.Tick(1f), Is.False);
            Assert.That(c.Cut(5f), Is.False, "cutting a ready cooldown does nothing");
            c.Start(2f);
            Assert.That(c.Cut(5f), Is.True, "a big cut finishes it");
            c.Start(4f);
            c.Clear();
            Assert.That(c.Ready && c.Total == 0f, Is.True);
        }

        [Test]
        public void PowerGrowsWithLevelAndBonus()
        {
            Assert.That(GadgetRules.LevelScale(1), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(GadgetRules.LevelScale(50), Is.EqualTo(1.98f).Within(1e-4f));
            Assert.That(GadgetRules.LevelScale(500), Is.EqualTo(GadgetRules.LevelScale(50)), "capped at level 50");
            Assert.That(GadgetRules.LevelScale(0), Is.EqualTo(1f));
            GadgetSpec mine = GadgetRules.Get(GadgetKind.Mine, 0);
            Assert.That(GadgetRules.Power(mine, 1, 0f), Is.EqualTo(mine.Power).Within(1e-3f));
            Assert.That(GadgetRules.Power(mine, 1, 0.25f), Is.EqualTo(mine.Power * 1.25f).Within(1e-3f));
            Assert.That(GadgetRules.Power(mine, 1, -2f), Is.EqualTo(mine.Power).Within(1e-3f), "a negative bonus never weakens it");
            Assert.That(GadgetRules.Duration(mine, 0.5f), Is.EqualTo(mine.Duration * 1.5f).Within(1e-3f));
            Assert.That(GadgetRules.Radius(mine, 0.2f), Is.EqualTo(mine.Radius * 1.2f).Within(1e-3f));
            GadgetSpec wall = GadgetRules.Get(GadgetKind.Barrier, 0);
            Assert.That(GadgetRules.ShieldHealth(wall, 50, 0f), Is.EqualTo(wall.Shield * 1.98f).Within(0.01f));
        }

        [Test]
        public void ModValuesGrowWithRarity()
        {
            for (int st = 0; st < 4; st++)
            {
                GadgetModStat s = (GadgetModStat)st;
                float prevHigh = 0f;
                for (int r = 0; r <= (int)Rarity.Mythic; r++)
                {
                    float low = GadgetRules.RollModValue(s, (Rarity)r, 0f);
                    float high = GadgetRules.RollModValue(s, (Rarity)r, 0.999f);
                    Assert.That(low, Is.GreaterThan(0f));
                    Assert.That(high, Is.GreaterThanOrEqualTo(low));
                    Assert.That(low, Is.GreaterThanOrEqualTo(prevHigh), s + " rarity " + r + " never rolls below the rarity under it");
                    Assert.That(high, Is.LessThan(0.4f), "mods stay modest");
                    prevHigh = high;
                }
                Assert.That(Clean(GadgetRules.StatName(s)) && Clean(GadgetRules.StatText(s, 0.12f)), Is.True);
            }
            Assert.That(GadgetRules.RollModValue(GadgetModStat.Cooldown, Rarity.Mythic, 1f), Is.LessThanOrEqualTo(0.21f));
            // Two Mythic cooldown mods worth of cut plus a set still hit the cap.
            GadgetSpec s0 = GadgetRules.Get(0);
            Assert.That(GadgetRules.Cooldown(s0, 0.21f + 0.15f + 0.21f), Is.EqualTo(s0.Cooldown * 0.5f).Within(1e-4f));
        }

        [Test]
        public void ModBonusesMapToOneStat()
        {
            float c, p, d, r;
            GadgetRules.ModBonuses(null, out c, out p, out d, out r);
            Assert.That(c + p + d + r, Is.EqualTo(0f));
            GadgetRules.ModBonuses(new GadgetModSave { Stat = (int)GadgetModStat.Cooldown, Value = 0.1f }, out c, out p, out d, out r);
            Assert.That(c, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(p + d + r, Is.EqualTo(0f));
            GadgetRules.ModBonuses(new GadgetModSave { Stat = (int)GadgetModStat.Power, Value = 0.2f }, out c, out p, out d, out r);
            Assert.That(p, Is.EqualTo(0.2f).Within(1e-6f));
            GadgetRules.ModBonuses(new GadgetModSave { Stat = (int)GadgetModStat.Duration, Value = 0.3f }, out c, out p, out d, out r);
            Assert.That(d, Is.EqualTo(0.3f).Within(1e-6f));
            GadgetRules.ModBonuses(new GadgetModSave { Stat = 99, Value = 0.4f }, out c, out p, out d, out r);
            Assert.That(r, Is.EqualTo(0.4f).Within(1e-6f), "an unknown stat reads as the last one");
        }

        [Test]
        public void ModDropsFollowTheSource()
        {
            Assert.That(GadgetRules.ModDropChance(LootSource.Boss, false), Is.EqualTo(1f));
            Assert.That(GadgetRules.ModDropChance(LootSource.Champion, false), Is.GreaterThan(GadgetRules.ModDropChance(LootSource.Elite, false)));
            Assert.That(GadgetRules.ModDropChance(LootSource.Elite, false), Is.GreaterThan(GadgetRules.ModDropChance(LootSource.Normal, false)));
            Assert.That(GadgetRules.ModDropChance(LootSource.EliteCreature, false), Is.GreaterThan(GadgetRules.ModDropChance(LootSource.Creature, false)));
            for (int s = 0; s <= (int)LootSource.GreatBeast; s++)
            {
                float n = GadgetRules.ModDropChance((LootSource)s, false), v = GadgetRules.ModDropChance((LootSource)s, true);
                Assert.That(n, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
                Assert.That(v, Is.GreaterThanOrEqualTo(n).And.LessThanOrEqualTo(1f), "veteran never drops fewer");
            }
            Assert.That(GadgetRules.ModSalvageValue(Rarity.Mythic), Is.GreaterThan(GadgetRules.ModSalvageValue(Rarity.Common)));
        }

        [Test]
        public void BlastFalloffAndCones()
        {
            Assert.That(GadgetRules.BlastFalloff(0f, 4f), Is.EqualTo(1f));
            Assert.That(GadgetRules.BlastFalloff(1.6f, 4f), Is.EqualTo(1f).Within(1e-4f), "full damage in the inner 40 percent");
            Assert.That(GadgetRules.BlastFalloff(4f, 4f), Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(GadgetRules.BlastFalloff(4.01f, 4f), Is.EqualTo(0f));
            Assert.That(GadgetRules.BlastFalloff(1f, 0f), Is.EqualTo(0f));
            float prev = 1f;
            for (float d = 0f; d <= 4f; d += 0.25f)
            {
                float f = GadgetRules.BlastFalloff(d, 4f);
                Assert.That(f, Is.LessThanOrEqualTo(prev + 1e-5f), "falloff never rises with distance");
                prev = f;
            }
            Vector3 apex = Vector3.zero, fwd = Vector3.forward;
            Assert.That(GadgetRules.InCone(apex, fwd, 22f, 60f, new Vector3(0f, 0f, 30f)), Is.True);
            Assert.That(GadgetRules.InCone(apex, fwd, 22f, 60f, new Vector3(0f, 0f, 61f)), Is.False, "beyond reach");
            Assert.That(GadgetRules.InCone(apex, fwd, 22f, 60f, new Vector3(20f, 0f, 20f)), Is.False, "45 degrees off is outside 22");
            Assert.That(GadgetRules.InCone(apex, fwd, 40f, 9f, new Vector3(4f, 0f, 6f)), Is.True, "34 degrees off is inside 40");
            Assert.That(GadgetRules.InCone(apex, fwd, 40f, 9f, new Vector3(0f, 0f, -3f)), Is.False, "behind");
        }

        [Test]
        public void HintsNameTheKey()
        {
            Assert.That(GadgetRules.HintText(GadgetRules.Get(GadgetKind.Turret, 0), "1", false), Does.Contain("Hold 1").And.Contain("Spindle Turret"));
            Assert.That(GadgetRules.HintText(GadgetRules.Get(GadgetKind.Scanner, 0), "2", false), Does.Contain("Press 2"));
            Assert.That(GadgetRules.HintText(GadgetRules.Get(GadgetKind.Scanner, 0), "[D-pad Right]", true), Does.StartWith("Hold [D-pad Right]"), "the pad always holds");
            Assert.That(GadgetRules.HintText(null, "1", false), Is.EqualTo(""));
        }

        [Test]
        public void GadgetSoundsAreFiniteAndNormalized()
        {
            for (int i = 0; i < (int)GadgetSounds.Clip.Count; i++)
            {
                float[] b = GadgetSounds.Make((GadgetSounds.Clip)i);
                Assert.That(b.Length, Is.GreaterThan(AudioSynth.Rate / 20), "clip " + (GadgetSounds.Clip)i);
                float peak = 0f;
                foreach (float v in b)
                {
                    Assert.That(float.IsNaN(v) || float.IsInfinity(v), Is.False);
                    peak = Mathf.Max(peak, Mathf.Abs(v));
                }
                Assert.That(peak, Is.GreaterThan(0.2f).And.LessThanOrEqualTo(0.86f), "normalized: " + (GadgetSounds.Clip)i);
            }
        }
    }
}
