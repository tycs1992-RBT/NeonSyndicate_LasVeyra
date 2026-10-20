using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The loot gun generator: same seed gives the same gun, stats stay inside their bounds, damage per second sits in
    /// sane bands against the shop guns and the enemies (no gun one-shots a Shard-Touched elite with a body shot), names
    /// are clean, traits are valid, and loot slots plug into the weapon inventory like any other weapon.
    /// </summary>
    public sealed class LootGeneratorTests
    {
        private static IEnumerable<LootGun> Sample(int count, int seedBase)
        {
            for (int i = 0; i < count; i++)
            {
                GRand r = new GRand(seedBase + i * 7919);
                Rarity rarity = (Rarity)r.Range(0, 6);
                int level = r.Range(1, LootGenerator.MaxLevel + 1);
                yield return LootGenerator.Generate(seedBase * 31 + i * 104729 + 1, level, rarity);
            }
        }

        [TearDown]
        public void ClearSlots()
        {
            Weapons.ResetStatics();
        }

        [Test]
        public void SameSeedGivesTheSameGun()
        {
            for (int i = 0; i < 300; i++)
            {
                Rarity rarity = (Rarity)(i % 6);
                LootGun a = LootGenerator.Generate(1000 + i * 13, 1 + i % 50, rarity);
                LootGun b = LootGenerator.Generate(1000 + i * 13, 1 + i % 50, rarity);
                Assert.That(b.Name, Is.EqualTo(a.Name), "name " + i);
                Assert.That(b.Archetype, Is.EqualTo(a.Archetype));
                Assert.That(b.Maker, Is.EqualTo(a.Maker));
                Assert.That(b.Element, Is.EqualTo(a.Element));
                Assert.That(b.Parts, Is.EqualTo(a.Parts));
                Assert.That(b.Traits, Is.EqualTo(a.Traits));
                Assert.That(b.Stats.Damage, Is.EqualTo(a.Stats.Damage));
                Assert.That(b.Stats.Interval, Is.EqualTo(a.Stats.Interval));
                Assert.That(b.Stats.Clip, Is.EqualTo(a.Stats.Clip));
            }
        }

        [Test]
        public void SeedsGiveVariety()
        {
            HashSet<string> names = new HashSet<string>();
            HashSet<LootArchetype> kinds = new HashSet<LootArchetype>();
            HashSet<LootMaker> makers = new HashSet<LootMaker>();
            HashSet<LootTrait> traits = new HashSet<LootTrait>();
            foreach (LootGun g in Sample(4000, 3))
            {
                names.Add(g.Name);
                kinds.Add(g.Archetype);
                makers.Add(g.Maker);
                foreach (LootTrait t in g.Traits) traits.Add(t);
            }
            Assert.That(names.Count, Is.GreaterThan(300), "distinct names");
            Assert.That(kinds.Count, Is.EqualTo(LootGenerator.ArchetypeCount), "every archetype rolls");
            Assert.That(makers.Count, Is.EqualTo(LootGenerator.MakerCount), "every maker rolls");
            Assert.That(traits.Count, Is.GreaterThanOrEqualTo(12), "at least twelve distinct traits roll");
        }

        [Test]
        public void StatsStayInBounds()
        {
            foreach (LootGun g in Sample(4000, 11))
            {
                LootStats s = g.Stats;
                string w = g.Name + " (" + g.Rarity + " " + g.Archetype + " lv " + g.Level + ")";
                Assert.That(s.Damage, Is.GreaterThan(0f), w + " damage");
                Assert.That(s.Interval, Is.InRange(0.04f, 2f), w + " interval");
                Assert.That(s.SpreadAim, Is.LessThanOrEqualTo(s.SpreadHip), w + " aims tighter than the hip");
                Assert.That(s.Recoil, Is.GreaterThan(0f), w + " recoil");
                Assert.That(s.Range, Is.GreaterThan(10f), w + " range");
                Assert.That(s.Reload, Is.InRange(0.3f, 6f), w + " reload");
                Assert.That(s.CritChance, Is.InRange(0f, 0.5f), w + " crit chance");
                Assert.That(s.CritMultiplier, Is.InRange(1f, 3f), w + " crit multiplier");
                Assert.That(s.Handling, Is.InRange(0.6f, 1.5f), w + " handling");
                if (g.Element == Element.None) Assert.That(s.ElementChance, Is.EqualTo(0f), w + " no element, no element chance");
                else Assert.That(s.ElementChance, Is.InRange(0.05f, 0.6f), w + " element chance");
                if (g.Archetype == LootArchetype.CrystalCaster)
                {
                    Assert.That(s.Clip, Is.EqualTo(0), w + " casters have no magazine");
                    Assert.That(s.HeatPerShot, Is.InRange(0.02f, 0.2f), w + " heat per shot");
                    Assert.That(g.Element, Is.Not.EqualTo(Element.None), w + " casters are always elemental");
                }
                else Assert.That(s.Clip, Is.InRange(1, 120), w + " magazine");
                if (g.Archetype == LootArchetype.Launcher)
                {
                    Assert.That(s.BlastRadius, Is.GreaterThan(3f), w + " blast");
                    Assert.That(s.ProjectileSpeed, Is.GreaterThan(10f), w + " projectile speed");
                }
                if (g.Maker == LootMaker.Veyra) Assert.That(g.Element, Is.Not.EqualTo(Element.None), w + " the elemental maker always rolls an element");
                WeaponDef d = LootGenerator.ToWeaponDef(g, WeaponId.Loot2);
                Assert.That(d.Id, Is.EqualTo(WeaponId.Loot2));
                Assert.That(d.IsGun && d.IsLoot, Is.True, w + " is a loot gun");
                Assert.That(d.UsesAmmo, Is.False, w + " live focus uses Resonance");
                Assert.That(WeaponController.BehaviorOf(d), Is.Not.EqualTo(CrystalBehavior.None), w + " live crystal function");
                if (d.UsesAmmo) Assert.That(d.MaxAmmo, Is.GreaterThanOrEqualTo(d.ClipSize), w + " max ammo holds a magazine");
                Assert.That(d.Overheats, Is.True, w + " every live focus has independent heat");
            }
        }

        [Test]
        public void NoGunOneShotsAnEliteWithABodyShot()
        {
            List<LootGun> guns = new List<LootGun>(Sample(5000, 21));
            for (int i = 0; i < 1500; i++) guns.Add(LootGenerator.Generate(500000 + i, LootGenerator.MaxLevel, (Rarity)(3 + i % 3)));
            for (int u = 0; u < LootUniques.All.Length; u++)
                for (int k = 0; k < 40; k++) guns.Add(LootGenerator.Generate(9000 + k * 31, LootGenerator.MaxLevel, Rarity.Legendary, -1, -1, u));
            foreach (LootGun g in guns)
                Assert.That(LootGenerator.PullDamage(g), Is.LessThan(LootDropRules.WeakestEliteHealth), g.Name + " (" + g.Rarity + " lv " + g.Level + ") one trigger pull");
        }

        private static float ReferenceDps(LootArchetype a, bool rocket)
        {
            // Frozen generation reference, from 04E. Live crystal definitions intentionally use different shot timing.
            // Keeping the saved generator against its original numeric baseline detects accidental catalog retuning.
            switch (a)
            {
                case LootArchetype.Pistol: return 26f / 0.17f;
                case LootArchetype.HandCannon: return 72f / 0.55f;
                case LootArchetype.Smg: return 17f / 0.075f;
                case LootArchetype.Shotgun: return 15f * 8f / 0.85f;
                case LootArchetype.Sniper: return 160f / 1.2f;
                case LootArchetype.Launcher: return rocket ? 280f / 1.6f : 170f / 0.9f;
                default: return 31f / 0.11f;
            }
        }

        [Test]
        public void DamagePerSecondSitsInSaneBands()
        {
            // Saved generation at level 1 stays near its frozen 04E numeric baseline.
            for (int a = 0; a < LootGenerator.ArchetypeCount; a++)
            {
                for (int i = 0; i < 200; i++)
                {
                    LootGun g = LootGenerator.Generate(7000 + i * 17 + a * 100003, 1, Rarity.Uncommon, a);
                    g.Traits.Clear();
                    LootGenerator.Refresh(g);
                    float ratio = LootGenerator.BurstDps(g, false) / ReferenceDps(g.Archetype, LootGenerator.IsRocket(g));
                    Assert.That(ratio, Is.InRange(0.45f, 1.6f), g.Name + " (" + g.Archetype + ") against the shop gun");
                }
            }
            // A Legendary is clearly stronger than an Uncommon of the same level, but never runs away.
            for (int a = 0; a < LootGenerator.ArchetypeCount; a++)
            {
                double unc = 0, leg = 0;
                const int n = 300;
                for (int i = 0; i < n; i++)
                {
                    unc += LootGenerator.BurstDps(LootGenerator.Generate(80000 + i * 13 + a * 7, 20, Rarity.Uncommon, a), false);
                    leg += LootGenerator.BurstDps(LootGenerator.Generate(90000 + i * 13 + a * 7, 20, Rarity.Legendary, a), false);
                }
                Assert.That(leg / unc, Is.GreaterThan(1.15), "a Legendary beats an Uncommon for archetype " + a);
                for (int i = 0; i < 300; i++)
                {
                    LootGun top = LootGenerator.Generate(95000 + i * 29 + a * 11, LootGenerator.MaxLevel, Rarity.Mythic, a);
                    if (top.Archetype != (LootArchetype)a) continue;
                    float ratio = LootGenerator.BurstDps(top, true) / ReferenceDps(top.Archetype, LootGenerator.IsRocket(top));
                    Assert.That(ratio, Is.LessThan(4.5f), top.Name + " at the top item level against the shop gun");
                }
            }
        }

        [Test]
        public void RarityAndLevelOnlyRaiseDamage()
        {
            foreach (LootGun g in Sample(400, 31))
            {
                LootGun c = g.Clone();
                c.Unique = -1;
                float last = 0f;
                for (int r = 0; r <= (int)Rarity.Mythic; r++)
                {
                    c.Rarity = (Rarity)r;
                    LootGenerator.Refresh(c);
                    Assert.That(c.Stats.Damage, Is.GreaterThan(last), c.Name + " rarity " + r);
                    last = c.Stats.Damage;
                }
                LootGun lv = g.Clone();
                lv.Level = 1;
                LootGenerator.Refresh(lv);
                float low = lv.Stats.Damage;
                lv.Level = LootGenerator.MaxLevel;
                LootGenerator.Refresh(lv);
                Assert.That(lv.Stats.Damage, Is.GreaterThan(low), lv.Name + " level");
            }
        }

        // FNV-1a hashes of words that must never appear in a gun's name (real gun makers, other games and studios). The
        // words themselves are kept out of the project on purpose.
        private static readonly HashSet<uint> Blocked = new HashSet<uint>
        {
            1785934759u, 407547228u, 110121816u, 2108164047u, 4014941211u, 11273417u, 3042857725u, 3661939679u, 253971482u, 4223155778u,
            3007971253u, 1059325489u, 3850603172u, 1272922393u, 3627556811u, 3845446577u, 3485384095u, 4065539383u, 3553623801u, 705593453u,
            2926994440u, 2765962797u, 3158329108u, 762519762u, 131123429u, 2758403418u, 4222697468u, 2057994710u, 2619510035u, 1965155344u,
            2491172157u, 1851719655u, 1725763617u, 3403900493u, 1470303765u, 1650541761u, 3365659147u, 2186889668u, 3992020290u, 4242052453u
        };

        private static uint Fnv(string s)
        {
            uint h = 2166136261u;
            foreach (char ch in s)
            {
                h ^= ch;
                h *= 16777619u;
            }
            return h;
        }

        private static void CheckName(string name)
        {
            Assert.That(string.IsNullOrEmpty(name), Is.False, "a name");
            foreach (char ch in name)
            {
                bool ok = char.IsLetter(ch) || ch == ' ' || ch == '\'';
                Assert.That(ok && ch < 128, Is.True, "'" + name + "' uses only plain letters, spaces and apostrophes (no dashes)");
            }
            foreach (string word in name.ToLowerInvariant().Replace("'s", "").Split(' '))
                Assert.That(Blocked.Contains(Fnv(word)), Is.False, "'" + name + "' has a blocked word");
        }

        [Test]
        public void NamesAreCleanAndUniquesAreDistinct()
        {
            foreach (LootGun g in Sample(3000, 41)) CheckName(g.Name);
            HashSet<string> seen = new HashSet<string>();
            foreach (LootUniqueDef u in LootUniques.All)
            {
                CheckName(u.Name);
                Assert.That(seen.Add(u.Name), Is.True, "unique names are unique: " + u.Name);
                Assert.That(string.IsNullOrEmpty(u.Flavor), Is.False, u.Name + " has a flavor line");
                Assert.That(u.Flavor.IndexOf((char)0x2014) < 0 && u.Flavor.IndexOf((char)0x2013) < 0, Is.True, u.Name + " flavor has no long dashes");
                if (u.GuaranteedOnly)
                {
                    // 04D crystal foci: granted only by offers and boss entitlements; their behavior is their signature.
                    Assert.That(u.Traits.Length, Is.EqualTo(0), u.Name + " carries no legacy traits");
                    int index=System.Array.IndexOf(LootUniques.All,u);
                    if(RegionalArsenal04OE.IsUnique(index))
                    {
                        Assert.That(CrystalFocusDb.BehaviorOfUnique(index),Is.EqualTo(CrystalBehavior.None),"Regional blades never grant an original focus");
                        Assert.That(u.Rarity,Is.EqualTo(Rarity.Legendary));
                    }
                    else Assert.That(CrystalFocusDb.BehaviorOfUnique(index), Is.Not.EqualTo(CrystalBehavior.None), u.Name + " has a crystal behavior");
                    continue;
                }
                Assert.That(u.Rarity >= Rarity.Legendary, Is.True, u.Name + " is Legendary or Mythic");
                Assert.That(u.Traits.Length, Is.EqualTo(u.Rarity == Rarity.Mythic ? 3 : 2), u.Name + " signature traits");
                if (!string.IsNullOrEmpty(u.BossId))
                {
                    CreatureSpecies sp = CreatureSpeciesDb.Get(u.BossId);
                    Assert.That(sp != null && sp.Boss, Is.True, u.Name + " belongs to a real boss");
                }
            }
            for (int i = 0; i < 8; i++) CheckName(LootGenerator.MakerShort[i % LootGenerator.MakerCount]);
        }

        [Test]
        public void TraitsAreValidForTheirGun()
        {
            foreach (LootGun g in Sample(5000, 51))
            {
                Assert.That(LootGenerator.TraitSetValid(g), Is.True, g.Name + " traits " + LootGenerator.TraitLine(g));
                int extra = g.Maker == LootMaker.Oddwire ? 1 : 0;
                int most = g.IsUnique ? (g.Rarity == Rarity.Mythic ? 3 : 2) : LootGenerator.TraitCountFor(g.Rarity, 0f) + extra;
                Assert.That(g.Traits.Count, Is.LessThanOrEqualTo(most), g.Name + " trait count");
                if (!g.IsUnique && g.Rarity >= Rarity.Rare) Assert.That(g.Traits.Count, Is.GreaterThanOrEqualTo(1), g.Name + " Rare and up always have a trait");
                if (g.Rarity >= Rarity.Legendary) Assert.That(g.IsUnique, Is.True, "Legendary and Mythic drops are named uniques");
            }
            foreach (LootUniqueDef u in LootUniques.All)
            {
                LootGun g = new LootGun { Archetype = u.Archetype };
                g.Traits.AddRange(u.Traits);
                Assert.That(LootGenerator.TraitSetValid(g), Is.True, u.Name + " signature traits fit a " + u.Archetype);
            }
            for (int t = 0; t <= (int)LootTrait.SpinUp; t++)
            {
                Assert.That(string.IsNullOrEmpty(LootGenerator.TraitName((LootTrait)t)), Is.False, "trait name " + t);
                bool anywhere = false;
                for (int a = 0; a < LootGenerator.ArchetypeCount; a++) anywhere |= LootGenerator.TraitValid((LootArchetype)a, (LootTrait)t);
                Assert.That(anywhere, Is.True, "trait " + (LootTrait)t + " fits at least one archetype");
            }
        }

        [Test]
        public void LootSlotsWorkInTheWeaponInventory()
        {
            Assert.That(Weapons.Get(WeaponId.Loot1).IsLoot, Is.False, "an empty slot is a stand-in");
            WeaponInventory empty = new WeaponInventory();
            empty.GiveAll();
            Assert.That(empty.Has(WeaponId.Loot1), Is.False, "GiveAll never owns an empty loot slot");
            LootGun g = LootGenerator.Generate(424242, 10, Rarity.Rare, (int)LootArchetype.AssaultRifle);
            WeaponDef d = LootGenerator.ToWeaponDef(g, WeaponId.Loot1);
            Weapons.SetLoot(0, d);
            Assert.That(Weapons.Get(WeaponId.Loot1), Is.SameAs(d), "a filled slot returns its gun");
            Assert.That(Weapons.LootFilled(WeaponId.Loot1) && !Weapons.LootFilled(WeaponId.Loot2), Is.True);
            WeaponInventory inv = new WeaponInventory();
            Assert.That(inv.Give(WeaponId.Loot1, 90), Is.True, "a new loot weapon");
            Assert.That(inv.Clip[(int)WeaponId.Loot1], Is.EqualTo(d.ClipSize), "magazine loaded");
            Assert.That(inv.TotalAmmo(WeaponId.Loot1), Is.EqualTo(int.MaxValue), "live availability uses shared Resonance");
            inv.Ammo[(int)AmmoType.Rifle] = 90;
            Assert.That(inv.Reserve(WeaponId.Loot1), Is.EqualTo(0), "legacy reserve is not a second focus resource");
            Assert.That(inv.ConsumeRound(WeaponId.Loot1), Is.True);
            Assert.That(inv.InSlot(WeaponSlot.Rifle).Contains(WeaponId.Loot1), Is.True, "an assault rifle sits in the rifle slot");
            Assert.That(inv.SelectSlot(WeaponSlot.Rifle), Is.True);
            Assert.That(inv.Current, Is.EqualTo(WeaponId.Loot1));
            WeaponInventory all = new WeaponInventory();
            all.GiveAll();
            Assert.That(all.Has(WeaponId.Loot1) && !all.Has(WeaponId.Loot2), Is.True, "GiveAll owns filled loot slots only");
            Weapons.ResetStatics();
            Assert.That(Weapons.Get(WeaponId.Loot1).IsLoot, Is.False, "ResetStatics empties the slots");
        }
    }
}
