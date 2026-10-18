using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The creature runtime's pure rules: the bestiary data, the ambient spawn table (place, time, weather, world), the
    /// spawn caps, loot rolls, attack timing and choice, perception and temperament, the fairness caps, swarm and flight
    /// maths, the synthesized creature voices, and the CreatureSave freeze list.
    /// </summary>
    public sealed class CreatureLogicTests
    {
        // ------------------------------------------------------------------ bestiary data

        [Test]
        public void SpeciesHaveUniqueIdsAndSaneNumbers()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                Assert.That(ids.Add(s.Id), Is.True, "species ids are unique (" + s.Id + ")");
                Assert.That(CreatureSpeciesDb.Get(s.Id), Is.SameAs(s), s.Id + " is found by id");
                Assert.That(string.IsNullOrEmpty(s.Name) || string.IsNullOrEmpty(s.Lore) || string.IsNullOrEmpty(s.Behaviour) || string.IsNullOrEmpty(s.Weak), Is.False, s.Id + " has a name, lore, behaviour and weak point text");
                Assert.That(s.Health, Is.GreaterThan(0f), s.Id + " health");
                Assert.That(s.Poise, Is.GreaterThan(0f), s.Id + " poise");
                Assert.That(s.Damage, Is.GreaterThan(0f), s.Id + " damage");
                Assert.That(s.Lumen, Is.GreaterThan(0), s.Id + " Lumen");
                Assert.That(s.PackMax, Is.GreaterThanOrEqualTo(s.PackMin), s.Id + " pack size range");
                Assert.That(s.IsHumanoid == string.IsNullOrEmpty(s.LookId), Is.True, s.Id + " is either a humanoid creature or has a creature look, not both");
                if (!s.IsHumanoid)
                {
                    Assert.That(Array.IndexOf(CreatureLooks.Species, s.LookId), Is.GreaterThanOrEqualTo(0), s.Id + " look id is known to CreatureLooks");
                    Assert.That(s.Attacks.Length, Is.GreaterThan(0), s.Id + " has attacks");
                }
                if (s.Boss) Assert.That(Array.IndexOf(s.Habitats, Habitat.Arena), Is.GreaterThanOrEqualTo(0), s.Id + " (boss) lives in an arena");
            }
            Assert.That(CreatureSpeciesDb.Get("nobody"), Is.Null, "unknown ids give null");
        }

        [Test]
        public void EveryLootMaterialExists()
        {
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                foreach (LootRoll l in s.Loot)
                {
                    Assert.That(MaterialDb.Get(l.Material), Is.Not.Null, s.Id + " drops a known material (" + l.Material + ")");
                    Assert.That(l.Chance, Is.InRange(0.01f, 1f), s.Id + " " + l.Material + " chance");
                    Assert.That(l.Max, Is.GreaterThanOrEqualTo(l.Min), s.Id + " " + l.Material + " count range");
                }
                if (s.Boss) Assert.That(s.Loot.Length, Is.GreaterThan(0), s.Id + " (boss) drops something");
            }
        }

        [Test]
        public void AttacksAreReadable()
        {
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                foreach (CreatureAttack a in s.Attacks)
                {
                    string w = s.Id + " " + a.Name;
                    Assert.That(a.MaxRange, Is.GreaterThanOrEqualTo(a.MinRange), w + " range");
                    Assert.That(a.Cooldown, Is.GreaterThan(0f), w + " cooldown");
                    for (int d = 0; d < 3; d++)
                    {
                        float scale = d == 0 ? 1.3f : (d == 1 ? 1f : 0.85f);
                        AttackTiming t = CreatureAttackDb.Timing(a, scale, 1f);
                        bool heavy = (a.Flags & (DamageFlags.Knockdown | DamageFlags.Unblockable)) != 0;
                        float floor = (heavy ? CreatureAttackDb.MinHeavyWindup : CreatureAttackDb.MinWindup) - 1e-4f;
                        Assert.That(t.Windup, Is.GreaterThanOrEqualTo(floor), w + " wind-up never drops below the readable floor");
                        Assert.That(t.Recovery, Is.GreaterThanOrEqualTo(0.2f), w + " leaves a recovery to punish");
                        Assert.That(t.WindupFrac, Is.InRange(0f, 1f), w + " wind-up fraction");
                        Assert.That(t.StrikeFrac, Is.InRange(t.WindupFrac, 1f), w + " strike fraction");
                    }
                }
            }
        }

        [Test]
        public void BigBodiesWindUpSlower()
        {
            CreatureAttack a = CreatureSpeciesDb.Get("quartz_drake").Attacks[0];
            AttackTiming small = CreatureAttackDb.Timing(a, 1f, 1f);
            AttackTiming big = CreatureAttackDb.Timing(a, 1f, 6f);
            Assert.That(big.Windup, Is.GreaterThan(small.Windup), "a heavy body telegraphs longer");
            AttackTiming story = CreatureAttackDb.Timing(a, 1.3f, 1f);
            Assert.That(story.Windup, Is.GreaterThan(small.Windup), "Story difficulty gives more time to react");
        }

        [Test]
        public void AttackPickRespectsRangeCooldownAndState()
        {
            CreatureAttack near = new CreatureAttack { Name = "near", MinRange = 0f, MaxRange = 2f, Use = AttackUse.Ground, Weight = 1f };
            CreatureAttack far = new CreatureAttack { Name = "far", MinRange = 5f, MaxRange = 20f, Use = AttackUse.Ground, Shape = AttackShape.Projectile, Weight = 1f };
            CreatureAttack air = new CreatureAttack { Name = "air", MinRange = 0f, MaxRange = 30f, Use = AttackUse.Air, Weight = 1f };
            CreatureAttack[] set = { near, far, air };
            float[] ready = new float[3];
            Assert.That(CreatureAttackDb.Pick(set, ready, 10f, 1f, 0f, AttackUse.Ground, 0.5f), Is.EqualTo(0), "close: the bite");
            Assert.That(CreatureAttackDb.Pick(set, ready, 10f, 8f, 0f, AttackUse.Ground, 0.5f), Is.EqualTo(1), "far: the projectile");
            Assert.That(CreatureAttackDb.Pick(set, ready, 10f, 8f, 90f, AttackUse.Ground, 0.5f), Is.EqualTo(-1), "a projectile needs the target in front");
            Assert.That(CreatureAttackDb.Pick(set, ready, 10f, 8f, 0f, AttackUse.Air, 0.5f), Is.EqualTo(2), "flying: only the air attack");
            ready[0] = 20f;
            Assert.That(CreatureAttackDb.Pick(set, ready, 10f, 1f, 0f, AttackUse.Ground, 0.5f), Is.EqualTo(-1), "the bite is cooling down");
            Assert.That(CreatureAttackDb.Pick(new CreatureAttack[0], ready, 10f, 1f, 0f, AttackUse.Ground, 0.5f), Is.EqualTo(-1), "no attacks: nothing");
        }

        [Test]
        public void AttackTimingPhases()
        {
            AttackTiming t;
            t.Windup = 0.5f;
            t.Active = 0.2f;
            t.Recovery = 0.3f;
            Assert.That(t.PhaseAt(0.1f), Is.EqualTo(0), "wind-up");
            Assert.That(t.PhaseAt(0.6f), Is.EqualTo(1), "active");
            Assert.That(t.PhaseAt(0.8f), Is.EqualTo(2), "recovery");
            Assert.That(t.PhaseAt(1.2f), Is.EqualTo(3), "done");
        }

        // ------------------------------------------------------------------ ecology

        private static List<SpawnCandidate> Table(HabitatContext c)
        {
            List<SpawnCandidate> l = new List<SpawnCandidate>();
            CreatureEcology.Candidates(c, l);
            return l;
        }

        private static bool Has(List<SpawnCandidate> l, string id)
        {
            for (int i = 0; i < l.Count; i++) if (l[i].Species == id && l[i].Weight > 0f) return true;
            return false;
        }

        [Test]
        public void CityCrystalHordesExistByDayAndGrowMoreDangerousAtNight()
        {
            HabitatContext c = new HabitatContext { City = true, Park = true, Construction = true, Grave = true, Night = 0f };
            List<SpawnCandidate> day = Table(c);
            Assert.That(Has(day, "hollow") && Has(day, "glass_mite"), Is.True, "citywide crystal threats by daylight");
            Assert.That(Has(day, "neon_jackal") && Has(day, "shard_golem"), Is.True, "parks and worksites remain creature habitats");
            c.Night = 1f;
            List<SpawnCandidate> night = Table(c);
            Assert.That(Has(night, "hollow_shade"), Is.True, "grave shades remain nocturnal-heavy");
            Assert.That(CreatureEcology.SpawnInterval(c), Is.LessThan(CreatureEcology.SpawnInterval(new HabitatContext { City = true, Night = 0f })));
            c.Park = c.Construction = c.Grave = false;
            foreach (float light in new[] { 0f, 1f })
            {
                c.Night = light;
                List<SpawnCandidate> plain = Table(c);
                Assert.That(Has(plain, "hollow") && Has(plain, "glass_mite"), Is.True, "ordinary city streets no longer empty");
                foreach (SpawnCandidate candidate in plain)
                    Assert.That(candidate.Spot, Is.EqualTo(SpawnSpot.CityRift), "generic threats use safe existing city walk nodes");
            }
            Assert.That((int)SpawnSpot.CityRift, Is.GreaterThan((int)SpawnSpot.Inner), "existing spot enum order preserved");
        }

        [Test]
        public void CrystalHordeElitesAndFocusDropsStayBoundedWithoutTinySwarmFarming()
        {
            CreatureSpecies jackal = CreatureSpeciesDb.Get("neon_jackal");
            CreatureSpecies mite = CreatureSpeciesDb.Get("glass_mite");
            CreatureSpecies golem = CreatureSpeciesDb.Get("shard_golem");
            HabitatContext city = new HabitatContext { City = true, Night = 0f };
            float day = CrystalHordeRules04I.EliteChance(city, jackal);
            city.Night = 1f;
            float night = CrystalHordeRules04I.EliteChance(city, jackal);
            city.Grave = true;
            float grave = CrystalHordeRules04I.EliteChance(city, jackal);
            Assert.That(day, Is.GreaterThan(0f));
            Assert.That(night, Is.GreaterThan(day));
            Assert.That(grave, Is.GreaterThan(night));
            Assert.That(grave, Is.LessThanOrEqualTo(0.2f));
            Assert.That(CrystalHordeRules04I.EliteChance(city, mite), Is.Zero, "mite swarm cannot spawn an elite per mite");
            Assert.That(CrystalHordeRules04I.FocusChance(jackal, false, 0.01f), Is.EqualTo(0.12f));
            Assert.That(CrystalHordeRules04I.FocusChance(jackal, true, 0.01f), Is.EqualTo(0.6f));
            Assert.That(CrystalHordeRules04I.FocusChance(golem, false, 0.02f), Is.EqualTo(0.12f));
            Assert.That(CrystalHordeRules04I.FocusChance(mite, false, 0.01f), Is.EqualTo(0.01f));
            Assert.That(CrystalHordeRules04I.FocusChance(jackal, true, 0.96f), Is.EqualTo(0.96f), "never reduce native rarer odds");
            Assert.That(CrystalHordeRules04I.FocusChance(null, false, 1f), Is.Zero);
        }

        [Test]
        public void WildernessTables()
        {
            HabitatContext day = new HabitatContext { Frost = true, Night = 0f };
            List<SpawnCandidate> fd = Table(day);
            Assert.That(Has(fd, "frost_stalker"), Is.True, "Frostline by day: stalkers");
            Assert.That(Has(fd, "snow_hollow"), Is.False, "snow hollows only at night");
            day.FrostLake = true;
            float lake = 0f, open = 0f;
            foreach (SpawnCandidate s in Table(day)) if (s.Species == "rime_griffin") lake = s.Weight;
            day.FrostLake = false;
            foreach (SpawnCandidate s in Table(day)) if (s.Species == "rime_griffin") open = s.Weight;
            Assert.That(lake, Is.GreaterThan(open), "rime griffins gather near the frozen lake");
            HabitatContext fn = new HabitatContext { Frost = true, Night = 1f };
            Assert.That(Has(Table(fn), "snow_hollow"), Is.True, "Frostline at night: snow hollows");

            HabitatContext bd = new HabitatContext { Badlands = true, Night = 0f };
            List<SpawnCandidate> b = Table(bd);
            Assert.That(Has(b, "sun_griffin") && Has(b, "sand_golem") && Has(b, "dune_wyrm"), Is.True, "Badlands by day: sun griffins, sand golems, the wyrm");
            Assert.That(Has(b, "quartz_drake"), Is.False, "the drake roosts near the canyon only");
            bd.Canyon = true;
            Assert.That(Has(Table(bd), "quartz_drake"), Is.True, "near the canyon: the drake");
            HabitatContext bn = new HabitatContext { Badlands = true, Night = 1f };
            Assert.That(Has(Table(bn), "sun_griffin"), Is.False, "sun griffins hunt by day");
            Assert.That(CreatureEcology.SpawnInterval(bn), Is.LessThan(CreatureEcology.SpawnInterval(bd)), "more creatures at night");
        }

        [Test]
        public void InnerWorldIsDenser()
        {
            HabitatContext inner = new HabitatContext { Inner = true, City = true, Night = 0f };
            List<SpawnCandidate> l = Table(inner);
            foreach (string id in new[] { "hollow", "mirror_wraith", "crystal_basilisk", "violet_wyvern", "hollow_shade" })
                Assert.That(Has(l, id), Is.True, "Inner World: " + id);
            HabitatContext city = new HabitatContext { City = true, Park = true, Night = 1f };
            Assert.That(CreatureEcology.SpawnInterval(inner), Is.LessThan(CreatureEcology.SpawnInterval(city)), "the Inner World spawns more often");
        }

        [Test]
        public void EcologyCoversTheBestiary()
        {
            // Every place, both times of day and the Inner World: the table only names real, non-boss species, and
            // every non-boss species lives somewhere.
            HashSet<string> spawned = new HashSet<string>();
            for (int night = 0; night <= 1; night++)
            {
                HabitatContext[] places =
                {
                    new HabitatContext { City = true, Park = true, Construction = true, Grave = true },
                    new HabitatContext { Frost = true, FrostLake = true },
                    new HabitatContext { Badlands = true, Quarry = true, Canyon = true },
                    new HabitatContext { Inner = true }
                };
                foreach (HabitatContext c0 in places)
                {
                    HabitatContext c = c0;
                    c.Night = night;
                    foreach (SpawnCandidate s in Table(c))
                    {
                        CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
                        Assert.That(sp, Is.Not.Null, "the table names a real species (" + s.Species + ")");
                        Assert.That(sp.Boss, Is.False, s.Species + " is not a boss (bosses live in arenas)");
                        Assert.That(s.Max, Is.GreaterThanOrEqualTo(s.Min), s.Species + " group size range");
                        spawned.Add(s.Species);
                    }
                }
            }
            // 04E Underheart species have no surface habitat on purpose: the underground ecology and finite encounters
            // place them. Only those ids may skip the wild, so a surface species can never lose its habitat by accident.
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                bool underheart = Array.IndexOf(EnemyIds04E.Hostile, sp.Id) >= 0 || Array.IndexOf(EnemyIds04E.Residents, sp.Id) >= 0;
                if (underheart)
                {
                    Assert.That(sp.Habitats.Length, Is.EqualTo(0), sp.Id + " (Underheart) has no surface habitat");
                    Assert.That(spawned.Contains(sp.Id), Is.False, sp.Id + " (Underheart) never spawns in the surface wild");
                    continue;
                }
                if (!sp.Boss) Assert.That(spawned.Contains(sp.Id), Is.True, sp.Id + " appears somewhere in the wild");
            }
        }

        [Test]
        public void WeatherShiftsTheMix()
        {
            Assert.That(CreatureEcology.WeatherFactor("violet_wyvern", Weather.Storm), Is.LessThan(1f), "flyers stay down in storms");
            Assert.That(CreatureEcology.WeatherFactor("quartz_drake", Weather.Sandstorm), Is.LessThan(1f), "and in sandstorms");
            Assert.That(CreatureEcology.WeatherFactor("hollow_shade", Weather.Fog), Is.GreaterThan(1f), "shades love fog");
            Assert.That(CreatureEcology.WeatherFactor("sand_golem", Weather.Sandstorm), Is.GreaterThan(1f), "golems rise in sandstorms");
            Assert.That(CreatureEcology.WeatherFactor("neon_jackal", Weather.Clear), Is.EqualTo(1f), "clear weather changes nothing");
        }

        [Test]
        public void PickIsWeighted()
        {
            List<SpawnCandidate> l = new List<SpawnCandidate>
            {
                new SpawnCandidate("a", 1f, 1, 1, SpawnSpot.Open),
                new SpawnCandidate("b", 0f, 1, 1, SpawnSpot.Open),
                new SpawnCandidate("c", 3f, 1, 1, SpawnSpot.Open)
            };
            SpawnCandidate p;
            Assert.That(CreatureEcology.Pick(l, 0f, out p) && p.Species == "a", Is.True, "roll 0: the first");
            Assert.That(CreatureEcology.Pick(l, 0.3f, out p) && p.Species == "c", Is.True, "past a's share: c (b weighs nothing)");
            Assert.That(CreatureEcology.Pick(l, 0.999f, out p) && p.Species == "c", Is.True, "roll near 1: the last");
            Assert.That(CreatureEcology.Pick(new List<SpawnCandidate>(), 0.5f, out p), Is.False, "nothing to pick");
        }

        [Test]
        public void CapsAndCosts()
        {
            int g;
            float b;
            CreatureEcology.Caps(GraphicsPreset.High, out g, out b);
            Assert.That(g, Is.EqualTo(6), "High: 6 groups");
            Assert.That(b, Is.EqualTo(18f), "High: 18 bodies");
            int lg;
            float lb;
            CreatureEcology.Caps(GraphicsPreset.Low, out lg, out lb);
            Assert.That(lg < g && lb < b, Is.True, "Low keeps fewer");
            Assert.That(CreatureEcology.BodyCost(CreatureSpeciesDb.Get("glass_mite")), Is.EqualTo(0.25f), "a mite is a quarter body");
            Assert.That(CreatureEcology.BodyCost(CreatureSpeciesDb.Get("dune_wyrm")), Is.EqualTo(2f), "great beasts cost two");
            Assert.That(CreatureEcology.MaxAlive(CreatureSpeciesDb.Get("quartz_drake")), Is.EqualTo(1), "one drake at a time");
            SpawnCandidate swarm = new SpawnCandidate("glass_mite", 1f, 8, 16, SpawnSpot.Construction);
            Assert.That(CreatureEcology.GroupSize(swarm, 0.999f, GraphicsPreset.High), Is.EqualTo(16), "full swarm on High");
            Assert.That(CreatureEcology.GroupSize(swarm, 0.999f, GraphicsPreset.Low), Is.LessThan(16), "smaller swarms on Low");
            Assert.That(CreatureEcology.GroupSize(swarm, 0f, GraphicsPreset.High), Is.EqualTo(8), "roll 0: the minimum");
        }

        // ------------------------------------------------------------------ loot

        [Test]
        public void LootRolls()
        {
            LootRoll[] table = { new LootRoll("jackal_fang", 1f, 2, 3), new LootRoll("neon_hide", 0f, 1, 1) };
            List<KeyValuePair<string, int>> into = new List<KeyValuePair<string, int>>();
            CreatureLoot.Roll(table, 0f, false, new GRand(7), into);
            Assert.That(into.Count, Is.EqualTo(1), "a sure drop always drops; a zero chance never does");
            Assert.That(into[0].Value, Is.InRange(2, 3), "count within its range");
            into.Clear();
            CreatureLoot.Roll(table, 1f, false, new GRand(7), into);
            Assert.That(into.Count, Is.EqualTo(2), "material find raises every chance");
            into.Clear();
            CreatureLoot.Roll(new[] { new LootRoll("jackal_fang", 1f, 1, 1) }, 0f, true, new GRand(7), into);
            Assert.That(into.Count == 1 && into[0].Value == 2, Is.True, "elites roll twice (and stack)");
            int total = 0;
            GRand r = new GRand(99);
            for (int i = 0; i < 2000; i++)
            {
                into.Clear();
                CreatureLoot.Roll(new[] { new LootRoll("jackal_fang", 0.25f, 1, 1) }, 0f, false, r, into);
                total += into.Count;
            }
            Assert.That(total, Is.InRange(400, 600), "a 25 percent drop lands about a quarter of the time");
        }

        // ------------------------------------------------------------------ minds

        [Test]
        public void PerceptionRewardsStealth()
        {
            float walk = CreatureBrainLogic.NoticeRange(30f, 1f, false, 2f, false, 0f, true, false);
            float sneak = CreatureBrainLogic.NoticeRange(30f, 1f, true, 0f, false, 0f, true, false);
            float drive = CreatureBrainLogic.NoticeRange(30f, 1f, false, 15f, true, 0f, true, false);
            float sense = CreatureBrainLogic.NoticeRange(30f, 0.5f, false, 2f, false, 0f, true, false);
            Assert.That(sneak, Is.EqualTo(walk * 0.5f).Within(0.01f), "crouched and still halves the range");
            Assert.That(drive, Is.GreaterThan(walk), "engines carry");
            Assert.That(sense, Is.LessThan(walk), "SkillMods.CreatureSense shortens it");
            Assert.That(CreatureBrainLogic.NoticeRange(30f, 1f, false, 2f, false, 0f, true, true), Is.LessThan(walk), "fog hides you");
            Assert.That(CreatureBrainLogic.CanNotice(10f, 30f, 20f), Is.True, "in front and in range");
            Assert.That(CreatureBrainLogic.CanNotice(10f, 170f, 20f), Is.False, "behind and not close");
            Assert.That(CreatureBrainLogic.CanNotice(5f, 170f, 20f), Is.True, "behind but very close (it hears you)");
            Assert.That(CreatureBrainLogic.NoiseReach(ThreatKind.Gunshot, 30f), Is.GreaterThan(30f), "gunshots carry past sight");
        }

        [Test]
        public void TerritorialWarnsFirst()
        {
            Assert.That(CreatureBrainLogic.Territorial(30f, 18f, 7f, 0f, 5f, false), Is.EqualTo(TerritorialStep.Calm), "outside the warning radius: calm");
            Assert.That(CreatureBrainLogic.Territorial(12f, 18f, 7f, 1f, 5f, false), Is.EqualTo(TerritorialStep.Warn), "inside: a warning");
            Assert.That(CreatureBrainLogic.Territorial(12f, 18f, 7f, 6f, 5f, false), Is.EqualTo(TerritorialStep.Attack), "stay too long: it attacks");
            Assert.That(CreatureBrainLogic.Territorial(5f, 18f, 7f, 0f, 5f, false), Is.EqualTo(TerritorialStep.Attack), "too close: it attacks");
            Assert.That(CreatureBrainLogic.Territorial(30f, 18f, 7f, 0f, 5f, true), Is.EqualTo(TerritorialStep.Attack), "hurt it: it attacks");
        }

        [Test]
        public void FleeLeashAndStagger()
        {
            Assert.That(CreatureBrainLogic.FleeHealth("neon_jackal", true), Is.GreaterThan(CreatureBrainLogic.FleeHealth("neon_jackal", false)), "a pack without its leader breaks sooner");
            Assert.That(CreatureBrainLogic.FleeHealth("quartz_drake", false), Is.EqualTo(0f), "apex beasts never run");
            Assert.That(CreatureBrainLogic.ShouldLeash(80f, 70f, false), Is.True, "past the leash: go home");
            Assert.That(CreatureBrainLogic.ShouldLeash(80f, 70f, true), Is.False, "mission creatures follow you");
            Assert.That(CreatureBrainLogic.ShouldLeash(80f, 0f, false), Is.False, "no leash (bosses)");
            Assert.That(CreatureBrainLogic.StaggerSeconds(5f, false), Is.GreaterThan(CreatureBrainLogic.StaggerSeconds(0.6f, false)), "big bodies reel longer");
            Assert.That(CreatureBrainLogic.StaggerSeconds(5f, true), Is.LessThan(CreatureBrainLogic.StaggerSeconds(5f, false)), "bosses recover sooner");
            Assert.That(CreatureBrainLogic.TurnRate(6f), Is.LessThan(CreatureBrainLogic.TurnRate(0.6f)), "big bodies turn slowly (flank them)");
            Assert.That(CreatureBrainLogic.RestBetweenAttacks(0, 0.5f, 1f), Is.GreaterThan(CreatureBrainLogic.RestBetweenAttacks(2, 0.5f, 1f)), "Story rests longer than Veteran");
        }

        [Test]
        public void NoInstantDeaths()
        {
            for (int d = 0; d <= 2; d++)
            {
                float scale = d == 0 ? 0.6f : (d == 1 ? 1f : 1.35f);
                float raw = CreatureBrainLogic.CapHit(10000f, 200f, d, scale);
                float share = d == 0 ? 0.25f : (d == 1 ? 0.35f : 0.5f);
                Assert.That(raw * scale, Is.EqualTo(200f * share).Within(0.01f), "difficulty " + d + ": one hit takes at most its share");
                Assert.That(CreatureBrainLogic.CapHit(5f, 200f, d, scale), Is.EqualTo(5f), "small hits are untouched");
            }
            Assert.That(CreatureBrainLogic.ElementMultiplier(Element.Ember, Element.Ember, Element.Frost), Is.EqualTo(1.5f), "weakness");
            Assert.That(CreatureBrainLogic.ElementMultiplier(Element.Frost, Element.Ember, Element.Frost), Is.EqualTo(0.6f), "resistance");
            Assert.That(CreatureBrainLogic.ElementMultiplier(Element.None, Element.Ember, Element.Frost), Is.EqualTo(1f), "no element");
        }

        [Test]
        public void SwarmAndFlightMaths()
        {
            Vector3[] mates = { new Vector3(0.1f, 0f, 0f) };
            Vector3 v = SwarmMath.Steer(Vector3.zero, mates, 1, Vector3.zero, Vector3.zero, Vector3.zero, 5f, 1f, 0f, 0f);
            Assert.That(v.x, Is.LessThan(0f), "a mite steers away from a mate that is too close");
            Vector3 far = SwarmMath.Steer(Vector3.zero, mates, 0, Vector3.zero, Vector3.zero, new Vector3(100f, 0f, 0f), 5f, 1f, 0f, 0f);
            Assert.That(far.magnitude, Is.LessThanOrEqualTo(5.001f), "never faster than its speed");
            Assert.That(far.x, Is.GreaterThan(0f), "streams toward the target");
            Vector3 o = FlightMath.OrbitPoint(Vector3.zero, 10f, 90f, 5f);
            Assert.That(new Vector2(o.x, o.z).magnitude, Is.EqualTo(10f).Within(0.001f), "orbit radius");
            Assert.That(o.y, Is.EqualTo(5f), "orbit height");
            Assert.That(FlightMath.DiveAim(Vector3.zero, new Vector3(4f, 9f, 0f), 0.5f), Is.EqualTo(new Vector3(2f, 0f, 0f)), "dives lead flat movement");
            Assert.That(FlightMath.OrbitRate(20f, 10f), Is.GreaterThan(0f), "orbit rate");
        }

        // ------------------------------------------------------------------ voices, rewards and saves

        [Test]
        public void CreatureVoicesAreClean()
        {
            for (int c = 0; c < (int)CreatureSounds.Clip.Count; c++)
            {
                float[] b = CreatureSounds.Make((CreatureSounds.Clip)c);
                Assert.That(b, Is.Not.Null.And.Not.Empty, ((CreatureSounds.Clip)c) + " has samples");
                float peak = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    Assert.That(TestData.Finite(b[i]), Is.True, ((CreatureSounds.Clip)c) + " sample " + i + " is finite");
                    peak = Mathf.Max(peak, Mathf.Abs(b[i]));
                }
                Assert.That(peak, Is.InRange(0.01f, 1.0001f), ((CreatureSounds.Clip)c) + " is audible and never clips");
            }
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
                foreach (CreatureCry cry in Enum.GetValues(typeof(CreatureCry)))
                    Assert.That((int)CreatureSounds.For(s.Id, cry), Is.InRange(0, (int)CreatureSounds.Clip.Count - 1), s.Id + " " + cry + " maps to a clip");
            Assert.That(CreatureSounds.PitchFor(6f), Is.LessThan(CreatureSounds.PitchFor(0.4f)), "big bodies sound deeper");
        }

        [Test]
        public void ScanAndKillRewards()
        {
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                int scan = Compendium.ScanLumen(s);
                Assert.That(scan, Is.InRange(15, 300), s.Id + " scan Lumen");
                Assert.That(CreatureDirector.LumenFor(s, true), Is.GreaterThan(CreatureDirector.LumenFor(s, false)), s.Id + " elites pay more");
            }
        }

        /// <summary>CreatureSave lives inside SaveData: its fields may only be appended (old saves keep their bestiary).</summary>
        [Test]
        public void CreatureSaveIsOnlyAppended()
        {
            string[] frozen =
            {
                "Seen:List<String>", "Scanned:List<String>", "KillIds:List<String>", "KillCounts:List<Int32>", "BossesDefeated:List<String>",
                "ArenasFound:List<String>"
            };
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(CreatureSave).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Type t = f.FieldType;
                string name = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? "List<" + t.GetGenericArguments()[0].Name + ">" : t.Name;
                now[f.Name] = name;
            }
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string type;
                Assert.That(now.TryGetValue(parts[0], out type), Is.True, "CreatureSave." + parts[0] + " was renamed or removed");
                Assert.That(type, Is.EqualTo(parts[1]), "CreatureSave." + parts[0] + " changed type");
            }
            Assert.That(Attribute.IsDefined(typeof(CreatureSave), typeof(SerializableAttribute)), Is.True, "CreatureSave stays [Serializable]");
            CreatureSave fresh = new CreatureSave();
            Assert.That(fresh.Seen != null && fresh.Scanned != null && fresh.KillIds != null && fresh.KillCounts != null && fresh.BossesDefeated != null && fresh.ArenasFound != null, Is.True, "every list starts empty, never null");
        }
    }
}
