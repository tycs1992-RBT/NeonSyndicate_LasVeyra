using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Breakable creature parts (CreaturePartsDb, CreaturePartRules, CreaturePartBook): every species has at least one
    /// part and bosses two or three, ids are unique, materials exist, the part fight's numbers, a Resonance Burst doubling
    /// part damage, breaking, combined effects and the frontal shell. Pure data and maths, so they run offline.
    /// </summary>
    public sealed class CreaturePartsTests
    {
        [Test]
        public void EverySpeciesHasPartsAndBossesHaveTwoOrThree()
        {
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                PartDef[] parts = CreaturePartsDb.For(sp.Id);
                Assert.That(parts.Length, Is.GreaterThanOrEqualTo(1), sp.Id + " has a breakable part");
                if (sp.Boss) Assert.That(parts.Length, Is.InRange(2, 3), sp.Id + " (boss) has two or three");
                HashSet<string> ids = new HashSet<string>();
                foreach (PartDef p in parts)
                {
                    Assert.That(ids.Add(p.Id), Is.True, sp.Id + ":" + p.Id + " is unique");
                    Assert.That(p.Name, Is.Not.Empty);
                    Assert.That(p.Text, Is.Not.Empty);
                    Assert.That(p.HpShare, Is.InRange(0.05f, 0.5f), sp.Id + ":" + p.Id + " part health");
                    Assert.That(p.Effects, Is.Not.EqualTo(PartEffect.None), sp.Id + ":" + p.Id + " changes the fight");
                    Assert.That(MaterialDb.Get(p.Material), Is.Not.Null, sp.Id + ":" + p.Id + " drops a real material (" + p.Material + ")");
                    Assert.That(p.MaterialCount, Is.GreaterThanOrEqualTo(1));
                    Assert.That(p.FrontGuard, Is.InRange(0.2f, 1f));
                    Assert.That(CreaturePartsDb.Find(sp.Id, p.Id), Is.SameAs(p));
                }
            }
            Assert.That(CreaturePartsDb.For("nobody").Length, Is.EqualTo(0));
            Assert.That(CreaturePartsDb.Key("glass_mite", "carapace"), Is.EqualTo("glass_mite:carapace"));
        }

        [Test]
        public void TheContractExamplesAreThere()
        {
            Assert.That(CreaturePartsDb.Find("frost_stalker", "claws").Effects & PartEffect.MeleeHalf, Is.EqualTo(PartEffect.MeleeHalf));
            PartDef carapace = CreaturePartsDb.Find("glass_mite", "carapace");
            Assert.That(carapace.FrontGuard, Is.LessThan(1f), "the mite's shell cuts frontal damage until it breaks");
            Assert.That(CreaturePartsDb.Find("violet_wyvern", "wing_membrane").Effects & PartEffect.Grounded, Is.EqualTo(PartEffect.Grounded));
            Assert.That(CreaturePartsDb.Find("shard_golem", "core_plate").Effects & PartEffect.CoreExposed, Is.EqualTo(PartEffect.CoreExposed));
        }

        [Test]
        public void ThePartFightNumbers()
        {
            Assert.That(CreaturePartRules.MeleeScale(PartEffect.MeleeHalf), Is.EqualTo(0.5f), "claws broken: half reach and damage");
            Assert.That(CreaturePartRules.MeleeScale(PartEffect.Grounded), Is.EqualTo(1f));
            Assert.That(CreaturePartRules.SpeedScale(PartEffect.Slowed | PartEffect.MeleeHalf), Is.EqualTo(CreaturePartRules.SlowedScale));
            Assert.That(CreaturePartRules.DamageTakenScale(PartEffect.Weakened), Is.GreaterThan(1f));
            Assert.That(CreaturePartRules.PartDamage(100f, DamageType.Bullet, false), Is.EqualTo(100f));
            Assert.That(CreaturePartRules.PartDamage(100f, DamageType.Bullet, true), Is.EqualTo(100f * ResonanceRules.BurstPartMultiplier), "a burst doubles part damage");
            Assert.That(CreaturePartRules.PartDamage(100f, DamageType.Explosion, false), Is.LessThan(100f), "blasts spread");
            Assert.That(CreaturePartRules.PartDamage(100f, DamageType.Melee, false), Is.GreaterThan(100f), "blows smash");
            Assert.That(CreaturePartRules.PartDamage(100f, DamageType.Fall, false), Is.EqualTo(0f));
            Assert.That(CreaturePartRules.BonusLootChance(0), Is.EqualTo(0f));
            Assert.That(CreaturePartRules.BonusLootChance(2), Is.GreaterThan(CreaturePartRules.BonusLootChance(1)));
            Assert.That(CreaturePartRules.BonusLootChance(10), Is.EqualTo(CreaturePartRules.BonusLootCap));
            Assert.That(CreaturePartRules.BossBreakStagger, Is.LessThan(CreaturePartRules.BreakStagger));
        }

        [Test]
        public void PartsBreakOnceAndCombineTheirEffects()
        {
            CreaturePartBook b = new CreaturePartBook(CreaturePartsDb.For("quartz_drake"), 2400f);
            Assert.That(b.Count, Is.EqualTo(3));
            int wings = b.IndexOf(PartAnchor.Wings);
            Assert.That(wings, Is.GreaterThanOrEqualTo(0));
            float max = b.Max(wings);
            Assert.That(max, Is.EqualTo(2400f * b.Defs[wings].HpShare).Within(1e-3f));
            Assert.That(b.Damage(wings, max * 0.5f), Is.False);
            Assert.That(b.Has(PartEffect.Grounded), Is.False);
            Assert.That(b.Damage(wings, max), Is.True, "this hit breaks it");
            Assert.That(b.Damage(wings, max), Is.False, "a broken part does not break again");
            Assert.That(b.Broken(wings), Is.True);
            Assert.That(b.Has(PartEffect.Grounded), Is.True);
            Assert.That(b.BrokenCount, Is.EqualTo(1));
            int head = b.IndexOf(PartAnchor.Head);
            b.Damage(head, 99999f);
            Assert.That(b.Has(PartEffect.NoRanged) && b.Has(PartEffect.Grounded), Is.True, "effects add up");
            Assert.That(b.Damage(-1, 5f), Is.False);
            Assert.That(b.Damage(head, -5f), Is.False);
        }

        [Test]
        public void TheMiteShellCutsFrontalDamageUntilItCracks()
        {
            CreaturePartBook b = new CreaturePartBook(CreaturePartsDb.For("glass_mite"), 8f);
            Assert.That(b.FrontGuard, Is.EqualTo(0.5f));
            b.Damage(0, 100f);
            Assert.That(b.FrontGuard, Is.EqualTo(1f), "full damage from the front once broken");
            Assert.That(b.Has(PartEffect.ArmorLost), Is.True);
            Assert.That(b.Max(0), Is.GreaterThanOrEqualTo(1f), "parts always hold at least one point");
        }
    
        [Test]
        public void EveryBeastPartSitsOnBonesAHitCanReach()
        {
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                if (string.IsNullOrEmpty(sp.LookId)) continue;
                CreatureBody body = CreatureBuilder.Build(CreatureLooks.For(sp.LookId, 3), 0);
                HashSet<int> hitBones = new HashSet<int>();
                foreach (CreatureHitVolume h in body.HitVolumes) hitBones.Add(h.Bone);
                foreach (PartDef p in CreaturePartsDb.For(sp.Id))
                {
                    List<int> bones = CreatureParts.BeastBones(body.Skeleton, p.Anchor);
                    Assert.That(bones.Count, Is.GreaterThan(0), sp.Id + ":" + p.Id + " has bones");
                    bool reachable = false;
                    foreach (int b in bones) if (hitBones.Contains(b)) reachable = true;
                    Assert.That(reachable, Is.True, sp.Id + ":" + p.Id + " covers at least one hit volume");
                }
                if (sp.Flies && CreaturePartsDb.Find(sp.Id, "wing_membrane") != null) Assert.That(body.Skeleton.Wings.Length, Is.GreaterThan(0), sp.Id + " has wings to tear");
            }
        }
    }
}

