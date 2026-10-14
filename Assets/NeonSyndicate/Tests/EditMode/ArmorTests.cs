using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Enemy armour plates (ArmorRules) and floating damage numbers (DamageNumberBook): who wears what, how the pool is
    /// shared between the pieces, where a bullet lands, bullets stripping a plate before health, helmets blunting
    /// headshots, explosions spreading, Volt rounds eating armour, and numbers merging for fast guns. Pure, offline.
    /// </summary>
    public sealed class ArmorTests
    {
        [Test]
        public void RolesAndTiersWearMoreArmour()
        {
            ArmorLoadout civ = ArmorRules.LoadoutFor(NpcRole.Civilian, false, false, false, 1, 0f);
            Assert.That(civ.Any, Is.False, "civilians wear nothing");
            Assert.That(ArmorRules.LoadoutFor(NpcRole.Companion, false, false, false, 1, 0f).Any, Is.False);
            ArmorLoadout cop = ArmorRules.LoadoutFor(NpcRole.Police, false, false, false, 1, 0.9f);
            ArmorLoadout swat = ArmorRules.LoadoutFor(NpcRole.Swat, false, false, false, 1, 0.9f);
            Assert.That(cop.Chest && cop.Helmet && !cop.Shoulders, Is.True, "police: vest and helmet");
            Assert.That(swat.Chest && swat.Helmet && swat.Shoulders, Is.True, "SWAT: the full set");
            Assert.That(swat.Pool, Is.GreaterThan(cop.Pool * 2f), "police heavies carry far more");
            Assert.That(ArmorRules.LoadoutFor(NpcRole.Gang, false, false, false, 1, 0.9f).Any, Is.False, "most gang members wear no vest");
            Assert.That(ArmorRules.LoadoutFor(NpcRole.Gang, false, false, false, 1, 0.1f).Chest, Is.True, "some do");
            ArmorLoadout enemy = ArmorRules.LoadoutFor(NpcRole.Enemy, false, false, false, 1, 0.5f);
            ArmorLoadout elite = ArmorRules.LoadoutFor(NpcRole.Enemy, true, false, false, 1, 0.5f);
            ArmorLoadout champ = ArmorRules.LoadoutFor(NpcRole.Enemy, true, true, false, 1, 0.5f);
            ArmorLoadout armored = ArmorRules.LoadoutFor(NpcRole.Enemy, true, true, true, 1, 0.5f);
            Assert.That(elite.Pool, Is.GreaterThan(enemy.Pool));
            Assert.That(elite.Shoulders, Is.True, "elites add shoulder guards");
            Assert.That(champ.Pool, Is.GreaterThan(elite.Pool));
            Assert.That(armored.Pool, Is.GreaterThan(champ.Pool), "the Armored affix adds more");
            Assert.That(ArmorRules.LoadoutFor(NpcRole.Police, false, false, false, 0, 0f).Pool, Is.LessThan(cop.Pool), "Story is softer");
            Assert.That(ArmorRules.LoadoutFor(NpcRole.Police, false, false, false, 2, 0f).Pool, Is.GreaterThan(cop.Pool), "Veteran is tougher");
        }

        [Test]
        public void ThePoolIsSharedBetweenThePiecesWorn()
        {
            ArmorLoadout full = new ArmorLoadout { Pool = 160f, Chest = true, Helmet = true, Shoulders = true };
            float sum = 0f;
            for (int i = 0; i < ArmorRules.PieceCount; i++) sum += ArmorRules.PieceMax(full, (ArmorPiece)i);
            Assert.That(sum, Is.EqualTo(160f).Within(1e-3f));
            Assert.That(ArmorRules.PieceMax(full, ArmorPiece.Chest), Is.EqualTo(80f).Within(1e-3f), "half on the chest");
            Assert.That(ArmorRules.PieceMax(full, ArmorPiece.Helmet), Is.EqualTo(40f).Within(1e-3f));
            ArmorLoadout vest = new ArmorLoadout { Pool = 45f, Chest = true };
            Assert.That(ArmorRules.PieceMax(vest, ArmorPiece.Chest), Is.EqualTo(45f).Within(1e-3f), "a lone vest holds it all");
            Assert.That(ArmorRules.PieceMax(vest, ArmorPiece.Helmet), Is.EqualTo(0f));
        }

        [Test]
        public void BulletsLandOnThePieceTheyHit()
        {
            Assert.That(ArmorRules.Locate(true, 1.7f, 0f), Is.EqualTo(ArmorPiece.Helmet));
            Assert.That(ArmorRules.Locate(false, 1.2f, 0.02f), Is.EqualTo(ArmorPiece.Chest));
            Assert.That(ArmorRules.Locate(false, 1.42f, 0.22f), Is.EqualTo(ArmorPiece.ShoulderR));
            Assert.That(ArmorRules.Locate(false, 1.42f, -0.22f), Is.EqualTo(ArmorPiece.ShoulderL));
            Assert.That(ArmorRules.Locate(false, 0.6f, 0f), Is.EqualTo(ArmorPiece.None), "legs are never armoured");
        }

        [Test]
        public void BulletsStripThePlateFirst()
        {
            ArmorSplit a = ArmorRules.Direct(20f, 50f, 1f, 1f);
            Assert.That(a.Armor, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(a.Health, Is.EqualTo(0f).Within(1e-4f), "an intact plate stops the round");
            ArmorSplit b = ArmorRules.Direct(30f, 10f, 1f, 1f);
            Assert.That(b.Armor, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(b.Health, Is.EqualTo(20f).Within(1e-4f), "the rest of a plate-breaking hit reaches health");
            ArmorSplit c = ArmorRules.Direct(30f, 0f, 1f, 1f);
            Assert.That(c.Health, Is.EqualTo(30f), "a broken plate stops nothing");
            ArmorSplit volt = ArmorRules.Direct(20f, 100f, ArmorRules.ElementVsArmor(Element.Volt), 1f);
            Assert.That(volt.Armor, Is.EqualTo(30f).Within(1e-4f), "lightning rounds eat armour faster");
            ArmorSplit voltBreak = ArmorRules.Direct(20f, 15f, 1.5f, 1f);
            Assert.That(voltBreak.Armor + voltBreak.Health * 1.5f, Is.EqualTo(30f).Within(1e-3f), "the overflow keeps its share");
            ArmorSplit melee = ArmorRules.Direct(40f, 100f, 1f, ArmorRules.MeleeAbsorb);
            Assert.That(melee.Health, Is.EqualTo(20f).Within(1e-4f), "a blow is half stopped");
            Assert.That(ArmorRules.Stops(DamageType.Fire), Is.False, "fire passes");
            Assert.That(ArmorRules.Stops(DamageType.Power), Is.False, "crystal powers pass");
            Assert.That(ArmorRules.Stops(DamageType.Bullet), Is.True);
        }

        [Test]
        public void HelmetsBluntHeadshotsUntilTheyBreak()
        {
            Assert.That(ArmorRules.HeadshotMultiplier(3f, true), Is.EqualTo(ArmorRules.HelmetedHeadshot));
            Assert.That(ArmorRules.HeadshotMultiplier(3f, false), Is.EqualTo(3f), "the full crit once the helmet is off");
            Assert.That(ArmorRules.HeadshotMultiplier(1f, true), Is.EqualTo(1f), "never more than the body's own");
        }

        [Test]
        public void ExplosionsSpreadOverEveryPlate()
        {
            float[] hp = { 80f, 40f, 0f, 20f };
            float[] got = new float[4];
            float health = ArmorRules.Spread(100f, hp, got);
            Assert.That(got[2], Is.EqualTo(0f), "a broken plate takes nothing");
            Assert.That(got[0], Is.GreaterThan(got[1]), "bigger plates take more");
            float armour = got[0] + got[1] + got[3];
            Assert.That(armour + health, Is.EqualTo(100f).Within(1e-3f), "nothing is lost");
            Assert.That(armour, Is.EqualTo(100f * ArmorRules.ExplosionAbsorb).Within(1e-3f));
            float[] small = { 5f, 0f, 0f, 0f };
            float h2 = ArmorRules.Spread(100f, small, got);
            Assert.That(got[0], Is.EqualTo(5f).Within(1e-4f), "never more than a plate holds");
            Assert.That(h2, Is.EqualTo(95f).Within(1e-3f));
            float[] none = { 0f, 0f, 0f, 0f };
            Assert.That(ArmorRules.Spread(50f, none, got), Is.EqualTo(50f), "bare bodies take it all");
        }

        [Test]
        public void FastGunsMergeIntoOneClimbingNumber()
        {
            DamageNumberBook b = new DamageNumberBook();
            for (int i = 0; i < 10; i++)
            {
                b.Add(1, DamageNumberKind.Normal, Element.None, 7f, Vector3.zero);
                b.Tick(0.06f);
            }
            Assert.That(b.Count, Is.EqualTo(1), "ten SMG hits make one number");
            Assert.That(b[0].Amount, Is.EqualTo(70f).Within(1e-3f));
            Assert.That(b[0].Hits, Is.EqualTo(10));
            b.Add(1, DamageNumberKind.Armor, Element.None, 5f, Vector3.zero);
            b.Add(2, DamageNumberKind.Normal, Element.None, 5f, Vector3.zero);
            b.Add(1, DamageNumberKind.Element, Element.Volt, 5f, Vector3.zero);
            b.Add(1, DamageNumberKind.Element, Element.Ember, 5f, Vector3.zero);
            Assert.That(b.Count, Is.EqualTo(5), "armour, other targets and other elements stay separate");
            b.Tick(0.5f);
            b.Add(1, DamageNumberKind.Normal, Element.None, 3f, Vector3.zero);
            Assert.That(b.Count, Is.EqualTo(6), "an old number is not merged into");
            b.Tick(5f);
            Assert.That(b.Count, Is.EqualTo(0), "numbers fade out");
            b.Add(3, DamageNumberKind.Normal, Element.None, 0.2f, Vector3.zero);
            Assert.That(b.Count, Is.EqualTo(0), "slivers are not shown");
            for (int i = 0; i < 60; i++) b.Add(100 + i, DamageNumberKind.Crit, Element.None, 9f, Vector3.zero);
            Assert.That(b.Count, Is.EqualTo(DamageNumberBook.MaxEntries), "the oldest go when there are too many");
        }

        [Test]
        public void BurstNumbersAreTheBiggestAndLast()
        {
            Assert.That(DamageNumberBook.SizeOf(DamageNumberKind.Burst, 100f), Is.GreaterThan(DamageNumberBook.SizeOf(DamageNumberKind.Crit, 100f)));
            Assert.That(DamageNumberBook.SizeOf(DamageNumberKind.Crit, 100f), Is.GreaterThan(DamageNumberBook.SizeOf(DamageNumberKind.Normal, 100f)));
            Assert.That(DamageNumberBook.LifeOf(DamageNumberKind.Burst), Is.GreaterThan(DamageNumberBook.LifeOf(DamageNumberKind.Normal)));
            DamageNumberEntry e = new DamageNumberEntry { Kind = DamageNumberKind.Normal, Age = 0.4f, Life = 0.9f };
            Assert.That(DamageNumberBook.Alpha(e), Is.EqualTo(1f).Within(1e-4f), "fully visible mid-life");
            e.Age = 0.89f;
            Assert.That(DamageNumberBook.Alpha(e), Is.LessThan(0.1f), "faded at the end");
            Assert.That(DamageNumberBook.Rise(e), Is.GreaterThan(0f));
        }
    }
}
