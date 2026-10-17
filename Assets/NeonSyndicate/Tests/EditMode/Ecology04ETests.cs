using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E underground ecology's pure rules: the six roles are distinct and readable (their own bodies, attack sets,
    /// parts, cries and loot, tells and punish windows a player can learn), finite encounters stay finite (the roster is
    /// clamped, reinforcements only come from the declared budget and the alive cap), victory needs a confirmed death of
    /// every spawned foe (a removal, a failure, a cancel or an abort never counts), the residents are never hostile in
    /// the refuge, and spawn probes underground stay on the anchor's own floor.
    /// </summary>
    public sealed class Ecology04ETests
    {
        private static readonly string[] Materials04E = { MaterialIds04E.EmberCrystal, MaterialIds04E.AshResin, MaterialIds04E.CoolantSalt, MaterialIds04E.RegulatorCog };

        /// <summary>The templates the 04E species were aliased to before they had their own bodies and moves.</summary>
        private static string TemplateOf(string id)
        {
            switch (id)
            {
                case "ember_scavenger": return "neon_jackal";
                case "ash_burrower": return "crystal_basilisk";
                case "glasswing_ambusher": return "violet_wyvern";
                case "furnace_caster": return "hollow_shade";
                case "repair_construct": return "hollow_shade";
                case "cinder_moth": return "hollow_shade";
                case "kiln_crab": return "crystal_basilisk";
                case "ember_hatchling": return "neon_jackal";
                default: return null;
            }
        }

        private static readonly string[] OriginalSpecies =
        {
            "neon_jackal", "frost_stalker", "crystal_basilisk", "violet_wyvern", "rime_griffin",
            "sun_griffin", "quartz_drake", "dune_wyrm", "hollow_shade", "glass_mite"
        };

        private static List<string> AllNew()
        {
            List<string> ids = new List<string>(EnemyIds04E.Hostile);
            ids.AddRange(EnemyIds04E.Residents);
            return ids;
        }

        // ================================================================== roles distinct and readable

        [Test]
        public void SixRolesAreDistinctAndReadable()
        {
            UnderRoleDef04E[] all = UnderheartRoles04E.All;
            Assert.That(all.Length, Is.EqualTo(6), "six underground roles");
            HashSet<UnderRole04E> roles = new HashSet<UnderRole04E>();
            HashSet<string> titles = new HashSet<string>();
            HashSet<string> species = new HashSet<string>();
            foreach (UnderRoleDef04E d in all)
            {
                Assert.That(Array.IndexOf(EnemyIds04E.Hostile, d.Species), Is.GreaterThanOrEqualTo(0), d.Species + " is a 04E hostile id");
                Assert.That(species.Add(d.Species), Is.True, d.Species + " has one role");
                Assert.That(d.Role, Is.Not.EqualTo(UnderRole04E.None), d.Species + " has a role");
                Assert.That(roles.Add(d.Role), Is.True, d.Species + " role " + d.Role + " is unique");
                Assert.That(titles.Add(d.Title), Is.True, d.Species + " title is unique");
                Assert.That(string.IsNullOrEmpty(d.Signature) || string.IsNullOrEmpty(d.Tell) || string.IsNullOrEmpty(d.Retreat), Is.False, d.Species + " describes its signature, tell and retreat");
                Assert.That(d.TellSeconds, Is.GreaterThanOrEqualTo(UnderheartRoles04E.MinTell), d.Species + " tell is readable");
                Assert.That(d.PunishSeconds, Is.GreaterThanOrEqualTo(UnderheartRoles04E.MinPunish), d.Species + " leaves a punish window");
                Assert.That(d.ActiveSeconds, Is.GreaterThan(0f), d.Species + " active time");
                Assert.That(d.CooldownMin, Is.GreaterThan(0f), d.Species + " cooldown");
                Assert.That(d.CooldownMax, Is.GreaterThanOrEqualTo(d.CooldownMin), d.Species + " cooldown range");
                Assert.That(d.RangeMax, Is.GreaterThan(d.RangeMin), d.Species + " signature range");
                Assert.That(d.RetreatHealth, Is.InRange(0.05f, 1f), d.Species + " retreat threshold");
                Assert.That(d.RetreatSeconds, Is.GreaterThan(0f), d.Species + " retreat time");
                HashSet<string> counters = new HashSet<string>();
                foreach (string c in d.Counters) if (!string.IsNullOrEmpty(c)) counters.Add(c);
                Assert.That(counters.Count, Is.GreaterThanOrEqualTo(3), d.Species + " has at least three distinct counters");
                Assert.That(UnderheartRoles04E.For(d.Species), Is.SameAs(d), d.Species + " is found by id");
                Assert.That(UnderheartRoles04E.RoleOf(d.Species), Is.EqualTo(d.Role));
                for (int i = 0; i <= 10; i++)
                {
                    float cd = UnderheartRoles04E.Cooldown(d, i / 10f);
                    Assert.That(cd, Is.InRange(d.CooldownMin, d.CooldownMax), d.Species + " cooldown roll stays in range");
                }
            }
            foreach (string id in EnemyIds04E.Residents)
            {
                Assert.That(UnderheartRoles04E.IsResident(id), Is.True, id + " is a resident");
                Assert.That(UnderheartRoles04E.For(id), Is.Null, id + " has no combat role");
            }
            Assert.That(UnderheartRoles04E.IsUnderheart("neon_jackal"), Is.False, "surface species have no 04E behaviour");
        }

        [Test]
        public void EveryNewSpeciesHasItsOwnBody()
        {
            List<string> ids = AllNew();
            foreach (string id in ids)
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                Assert.That(sp, Is.Not.Null, id + " exists");
                if (sp.IsHumanoid)
                {
                    Assert.That(id, Is.EqualTo(EnemyIds04E.ExtractionSentinel), "only the sentinel is humanoid");
                    continue;
                }
                Assert.That(sp.LookId, Is.EqualTo(id), id + " uses its own look entry, not a template's");
                Assert.That(Array.IndexOf(CreatureLooks.Species, id), Is.GreaterThanOrEqualTo(0), id + " is a CreatureLooks species");
            }
            // Pairwise different silhouettes among the five beast roles and against the templates they replaced.
            List<string> beasts = new List<string>();
            foreach (string id in EnemyIds04E.Hostile) if (!CreatureSpeciesDb.Get(id).IsHumanoid) beasts.Add(id);
            Assert.That(beasts.Count, Is.EqualTo(5), "five beast roles");
            int[] seeds = { 3, 61724, 9001 };
            foreach (int seed in seeds)
            {
                for (int a = 0; a < beasts.Count; a++)
                {
                    CreatureLook la = CreatureLooks.For(beasts[a], seed);
                    CreatureBody ba = CreatureBuilder.Build(la, 0);
                    for (int b = a + 1; b < beasts.Count; b++)
                    {
                        CreatureLook lb = CreatureLooks.For(beasts[b], seed);
                        CreatureBody bb = CreatureBuilder.Build(lb, 0);
                        Assert.That(Distinct(la, ba, lb, bb), Is.True, beasts[a] + " and " + beasts[b] + " read as different bodies (seed " + seed + ")");
                    }
                }
                foreach (string id in ids)
                {
                    CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                    if (sp.IsHumanoid) continue;
                    CreatureLook mine = CreatureLooks.For(id, seed);
                    CreatureLook tpl = CreatureLooks.For(TemplateOf(id), seed);
                    CreatureBody mb = CreatureBuilder.Build(mine, 0);
                    CreatureBody tb = CreatureBuilder.Build(tpl, 0);
                    Assert.That(Distinct(mine, mb, tpl, tb), Is.True, id + " is not its old template's body (seed " + seed + ")");
                    Assert.That(ShapeDifferences(mine, tpl), Is.GreaterThanOrEqualTo(2), id + " changes the shape, not just the colours, of " + TemplateOf(id));
                    Assert.That(mb.Mesh, Is.Not.Null, id + " builds a mesh");
                    Assert.That(mb.HitVolumes.Count, Is.GreaterThan(0), id + " has hit volumes");
                }
            }
        }

        /// <summary>Different plans, or at least 12% apart in some dimension of the built bounds.</summary>
        private static bool Distinct(CreatureLook la, CreatureBody ba, CreatureLook lb, CreatureBody bb)
        {
            if (la.Plan != lb.Plan) return true;
            Vector3 sa = ba.Bounds.size, sb = bb.Bounds.size;
            for (int i = 0; i < 3; i++)
            {
                float x = sa[i], y = sb[i];
                float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                if (m > 1e-4f && Mathf.Abs(x - y) / m >= 0.12f) return true;
            }
            return false;
        }

        /// <summary>How many shape features (not colours) differ between two looks.</summary>
        private static int ShapeDifferences(CreatureLook a, CreatureLook b)
        {
            int n = 0;
            if (a.Plan != b.Plan) n++;
            if (a.HeadStyle != b.HeadStyle) n++;
            if (a.Plates != b.Plates) n++;
            if (a.Horns != b.Horns) n++;
            if (Mathf.Abs(a.Spines - b.Spines) >= 3) n++;
            if (a.Beak != b.Beak) n++;
            if (a.Coat != b.Coat) n++;
            if (a.Forelegs != b.Forelegs) n++;
            if (Mathf.Abs(a.WingSpan - b.WingSpan) > 0.8f) n++;
            if (Mathf.Abs(a.Bulk - b.Bulk) > 0.3f) n++;
            if (Mathf.Abs(a.LegLength - b.LegLength) > 0.2f) n++;
            return n;
        }

        [Test]
        public void AttackSetsArePerRoleWithTellsAndRecovery()
        {
            HashSet<string> oldNames = new HashSet<string>();
            foreach (string id in OriginalSpecies)
                foreach (CreatureAttack a in CreatureAttackDb.For(id)) oldNames.Add(a.Name);
            foreach (string id in AllNew())
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                if (sp.IsHumanoid) continue;
                CreatureAttack[] set = CreatureAttackDb.For(id);
                Assert.That(set, Is.Not.Null.And.Not.Empty, id + " has an attack set");
                Assert.That(set, Is.Not.SameAs(CreatureAttackDb.For(TemplateOf(id))), id + " is not an alias of its template's set");
                int damaging = 0;
                foreach (CreatureAttack a in set)
                {
                    Assert.That(oldNames.Contains(a.Name), Is.False, id + " attack '" + a.Name + "' is its own, not a template move");
                    Assert.That(a.Recovery, Is.GreaterThanOrEqualTo(0.2f), id + " " + a.Name + " recovers");
                    if (a.Shape == AttackShape.Call || a.Damage <= 0f) continue;
                    damaging++;
                    Assert.That(a.Windup, Is.GreaterThanOrEqualTo(0.35f), id + " " + a.Name + " has a readable wind-up");
                }
                bool harmless = id == EnemyIds04E.CinderMoth || id == EnemyIds04E.EmberHatchling;
                if (harmless) Assert.That(damaging, Is.EqualTo(0), id + " has no damaging move at all");
                else if (Array.IndexOf(EnemyIds04E.Hostile, id) >= 0) Assert.That(damaging, Is.GreaterThanOrEqualTo(2), id + " has at least two damaging moves");
                else Assert.That(damaging, Is.GreaterThanOrEqualTo(1), id + " can defend itself");
            }
            // The kiln crab warns first: its first move is a call (the clack), used by the warning.
            Assert.That(CreatureAttackDb.For(EnemyIds04E.KilnCrab)[0].Shape, Is.EqualTo(AttackShape.Call), "the crab's clack comes first");
        }

        [Test]
        public void PartsCriesAndLootAreTheirOwn()
        {
            HashSet<string> mats = new HashSet<string>(Materials04E);
            foreach (string id in AllNew())
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                PartDef[] parts = CreaturePartsDb.For(id);
                Assert.That(parts, Is.Not.Null.And.Not.Empty, id + " has breakable parts");
                string tpl = TemplateOf(id);
                if (tpl != null) Assert.That(parts, Is.Not.SameAs(CreaturePartsDb.For(tpl)), id + " parts are not its template's");
                foreach (PartDef p in parts)
                {
                    Assert.That(p.HpShare, Is.InRange(0.05f, 0.5f), id + "." + p.Id + " health share");
                    Assert.That(p.Effects, Is.Not.EqualTo(PartEffect.None), id + "." + p.Id + " changes the fight when broken");
                    Assert.That(mats.Contains(p.Material), Is.True, id + "." + p.Id + " drops a 04E material (" + p.Material + ")");
                }
                bool hostile = Array.IndexOf(EnemyIds04E.Hostile, id) >= 0;
                if (hostile)
                {
                    Assert.That(sp.Loot, Is.Not.Null.And.Not.Empty, id + " drops materials");
                    foreach (LootRoll r in sp.Loot) Assert.That(mats.Contains(r.Material), Is.True, id + " loot uses 04E materials only (" + r.Material + ")");
                }
                Assert.That(sp.Habitats.Length, Is.EqualTo(0), id + " never spawns from the surface ambient table");
                if (tpl != null)
                {
                    int differ = 0;
                    CreatureCry[] cries = { CreatureCry.Attack, CreatureCry.Roar, CreatureCry.Death };
                    foreach (CreatureCry c in cries) if (CreatureSounds.For(id, c) != CreatureSounds.For(tpl, c)) differ++;
                    Assert.That(differ, Is.GreaterThanOrEqualTo(1), id + " sounds different from " + tpl);
                }
            }
        }

        // ================================================================== rosters finite

        [Test]
        public void RostersAreClampedAndValidated()
        {
            List<string> species = new List<string>();
            List<bool> elite = new List<bool>();
            int reinf;
            string reason;
            List<SkirmishEntry> roster = new List<SkirmishEntry>
            {
                new SkirmishEntry { Species = EnemyIds04E.EmberScavenger, Count = 50 },
                new SkirmishEntry { Species = EnemyIds04E.ExtractionSentinel, Count = 4, Elite = true }
            };
            Assert.That(SkirmishRules04E.Expand(roster, 99, null, species, elite, out reinf, out reason), Is.True, reason);
            Assert.That(species.Count, Is.EqualTo(SkirmishRules04E.MaxRosterBodies), "the roster is capped");
            Assert.That(elite.Count, Is.EqualTo(species.Count));
            int scav = 0;
            foreach (string s in species) if (s == EnemyIds04E.EmberScavenger) scav++;
            Assert.That(scav, Is.EqualTo(SkirmishRules04E.MaxEntryCount), "one line is capped");
            Assert.That(elite[species.Count - 1], Is.True, "elite flags follow their line");
            Assert.That(reinf, Is.EqualTo(SkirmishRules04E.MaxReinforcements), "the reinforcement budget is capped");
            Assert.That(SkirmishRules04E.Expand(roster, -3, null, species, elite, out reinf, out reason), Is.True);
            Assert.That(reinf, Is.EqualTo(0), "a negative budget means none");

            Func<string, bool> known = id => id == EnemyIds04E.EmberScavenger;
            Assert.That(SkirmishRules04E.Expand(roster, 0, known, species, elite, out reinf, out reason), Is.False, "an unknown species is refused");
            Assert.That(reason, Does.Contain("unknown"));
            Assert.That(SkirmishRules04E.Expand(new List<SkirmishEntry>(), 0, null, species, elite, out reinf, out reason), Is.False, "an empty roster is refused");
            Assert.That(SkirmishRules04E.Expand(null, 0, null, species, elite, out reinf, out reason), Is.False, "no roster is refused");
            Assert.That(SkirmishRules04E.Expand(new List<SkirmishEntry> { new SkirmishEntry { Species = EnemyIds04E.EmberScavenger, Count = 0 } }, 2, null, species, elite, out reinf, out reason), Is.False, "zero bodies is refused");
            Assert.That(SkirmishRules04E.Expand(new List<SkirmishEntry> { new SkirmishEntry { Species = "" } }, 0, null, species, elite, out reinf, out reason), Is.False, "a line with no species is refused");

            Assert.That(SkirmishRules04E.ClampRadius(1f), Is.EqualTo(SkirmishRules04E.MinRadius));
            Assert.That(SkirmishRules04E.ClampRadius(500f), Is.EqualTo(SkirmishRules04E.MaxRadius));
            Assert.That(SkirmishRules04E.ClampScale(100f), Is.LessThanOrEqualTo(4f));
            Assert.That(SkirmishRules04E.ClampScale(-1f), Is.GreaterThan(0f));
            for (int d = 0; d < 4; d++) Assert.That(SkirmishRules04E.AliveCap(d), Is.InRange(4, 6), "alive cap by difficulty");

            List<Vector3> posts = new List<Vector3>();
            Vector3 center = new Vector3(10f, -26f, 40f);
            SkirmishRules04E.ArenaPoints(center, 16f, 7, 33f, posts);
            Assert.That(posts.Count, Is.EqualTo(7));
            foreach (Vector3 p in posts)
            {
                float d = new Vector2(p.x - center.x, p.z - center.z).magnitude;
                Assert.That(d, Is.InRange(16f * 0.35f - 0.01f, 16f * 0.75f + 0.01f), "posts stay inside the room");
                Assert.That(p.y, Is.EqualTo(center.y), "posts keep the room's floor height");
            }
        }

        // ================================================================== reinforcement budget respected

        [Test]
        public void ReinforcementsOnlyComeFromTheDeclaredBudget()
        {
            SkirmishLedger04E L = new SkirmishLedger04E(3, 2, 4);
            int[] roster = { L.ReserveRoster(), L.ReserveRoster(), L.ReserveRoster() };
            Assert.That(L.ReserveRoster(), Is.EqualTo(0), "the roster cannot grow");
            Assert.That(L.CanReserveReinforcement, Is.False, "no reinforcement before the roster is in the room");
            Assert.That(L.ReserveReinforcement(), Is.EqualTo(0));
            foreach (int t in roster) Assert.That(L.Admit(t), Is.True);
            Assert.That(L.Admit(roster[0]), Is.False, "a token is used once");
            Assert.That(L.RosterComplete, Is.True);
            Assert.That(SkirmishRules04E.ReinforcementDue(L.RosterTotal, L.RosterSpawned, L.Dead, L.Alive, L.ReinforcementsOutstanding), Is.False, "not due while most of the roster stands");
            Assert.That(L.MarkDead(), Is.True);
            Assert.That(L.MarkDead(), Is.True);
            Assert.That(SkirmishRules04E.ReinforcementDue(L.RosterTotal, L.RosterSpawned, L.Dead, L.Alive, L.ReinforcementsOutstanding), Is.True, "due once half the roster is down");
            int r1 = L.ReserveReinforcement();
            int r2 = L.ReserveReinforcement();
            Assert.That(r1, Is.GreaterThan(0));
            Assert.That(r2, Is.GreaterThan(0));
            Assert.That(L.ReserveReinforcement(), Is.EqualTo(0), "the budget is spent");
            Assert.That(L.IsReinforcement(r1), Is.True);
            Assert.That(L.Withdraw(r2), Is.True, "a reinforcement with no safe arrival point is withdrawn");
            Assert.That(L.Admit(r2), Is.False, "a withdrawn token never spawns");
            Assert.That(L.DeclaredTotal, Is.EqualTo(4), "the HUD total shrinks with the withdrawal");
            Assert.That(L.Admit(r1), Is.True);
            Assert.That(L.ReinforcementsSpawned, Is.EqualTo(1));
            Assert.That(L.ReinforcementsOutstanding, Is.EqualTo(0));

            // The alive cap holds reinforcements back while too many foes stand.
            SkirmishLedger04E C = new SkirmishLedger04E(4, 3, 4);
            for (int i = 0; i < 4; i++) Assert.That(C.Admit(C.ReserveRoster()), Is.True);
            Assert.That(C.CanReserveReinforcement, Is.False, "four alive at a cap of four: no more bodies");
            C.MarkDead();
            Assert.That(C.CanReserveReinforcement, Is.True);
            Assert.That(C.ReserveReinforcement(), Is.GreaterThan(0));
            Assert.That(C.CanReserveReinforcement, Is.False, "a reservation counts against the cap");
        }

        [Test]
        public void RandomEncountersNeverExceedTheirBudgetOrCap()
        {
            System.Random rng = new System.Random(4004);
            for (int run = 0; run < 400; run++)
            {
                int roster = rng.Next(1, SkirmishRules04E.MaxRosterBodies + 1);
                int budget = rng.Next(0, SkirmishRules04E.MaxReinforcements + 1);
                int cap = SkirmishRules04E.AliveCap(rng.Next(0, 4));
                SkirmishLedger04E L = new SkirmishLedger04E(roster, budget, cap);
                List<int> pending = new List<int>();
                for (int i = 0; i < roster; i++) pending.Add(L.ReserveRoster());
                for (int step = 0; step < 200; step++)
                {
                    int op = rng.Next(0, 6);
                    if (op == 0 && pending.Count > 0)
                    {
                        int k = rng.Next(pending.Count);
                        int t = pending[k];
                        pending.RemoveAt(k);
                        if (L.Admit(t) && rng.Next(10) == 0) L.AdmitFailed(t);
                    }
                    else if (op == 1) L.MarkDead();
                    else if (op == 2 && rng.Next(8) == 0) L.MarkLost();
                    else if (op == 3)
                    {
                        int t = L.ReserveReinforcement();
                        if (t > 0) pending.Add(t);
                    }
                    else if (op == 4 && pending.Count > 0 && rng.Next(4) == 0) L.Withdraw(pending[rng.Next(pending.Count)]);
                    else if (op == 5 && pending.Count > 0) L.Admit(pending[rng.Next(pending.Count)]);
                    Assert.That(L.Spawned, Is.LessThanOrEqualTo(roster + budget), "never more bodies than declared");
                    Assert.That(L.ReinforcementsSpawned + L.ReinforcementsReserved + L.ReinforcementsWithdrawn, Is.LessThanOrEqualTo(budget), "reinforcements come from the budget only");
                    Assert.That(L.ReinforcementsSpawned == 0 || L.RosterSpawned + L.FailedRoster == roster || L.FailedRoster > 0 || L.RosterSpawned == roster, Is.True, "reinforcements follow the roster");
                    Assert.That(L.Alive, Is.GreaterThanOrEqualTo(0));
                    Assert.That(L.Dead + L.Lost, Is.LessThanOrEqualTo(L.Spawned + L.ReinforcementsWithdrawn + L.FailedRoster));
                    if (L.VictoryReady)
                    {
                        Assert.That(L.Lost, Is.EqualTo(0), "victory never follows a lost body");
                        Assert.That(L.Dead, Is.EqualTo(L.Spawned), "victory means every spawned foe is dead");
                        Assert.That(L.FailedRoster, Is.EqualTo(0));
                    }
                }
            }
        }

        // ================================================================== victory requires confirmed deaths

        [Test]
        public void VictoryRequiresConfirmedDeaths()
        {
            SkirmishLedger04E L = new SkirmishLedger04E(2, 0, 4);
            int a = L.ReserveRoster(), b = L.ReserveRoster();
            Assert.That(L.VictoryReady, Is.False, "nothing spawned is not a victory");
            L.Admit(a);
            L.MarkDead();
            Assert.That(L.VictoryReady, Is.False, "the roster is not complete");
            L.Admit(b);
            Assert.That(L.VictoryReady, Is.False, "one still stands");
            L.MarkDead();
            Assert.That(L.VictoryReady, Is.True, "both confirmed dead");
            Assert.That(L.MarkDead(), Is.False, "a death is counted once");

            // A body removed without a death (despawn, cleanup, a fall) makes the encounter unwinnable.
            SkirmishLedger04E M = new SkirmishLedger04E(2, 0, 4);
            M.Admit(M.ReserveRoster());
            M.Admit(M.ReserveRoster());
            M.MarkLost();
            M.MarkDead();
            Assert.That(M.VictoryReady, Is.False, "a removal never counts as a defeat");
            Assert.That(M.Unwinnable, Is.True);
            SkirmishRules04E.WatchFacts f = new SkirmishRules04E.WatchFacts { Unwinnable = M.Unwinnable, Engaged = true };
            Assert.That(SkirmishRules04E.Check(f), Is.EqualTo(SkirmishEnd04E.ActorLost), "a lost body ends the encounter without a victory");

            // An outstanding reinforcement blocks victory until it is spawned and killed, or withdrawn.
            SkirmishLedger04E R = new SkirmishLedger04E(1, 1, 4);
            R.Admit(R.ReserveRoster());
            R.MarkDead();
            Assert.That(R.VictoryReady, Is.False, "an announced reinforcement is still to come");
            int t = R.ReserveReinforcement();
            Assert.That(R.VictoryReady, Is.False, "a reserved reinforcement blocks victory");
            Assert.That(R.CancelAll(), Is.EqualTo(1), "cancel releases the reservation");
            Assert.That(R.VictoryReady, Is.False, "cancelling never turns into a victory");
            Assert.That(R.Admit(t), Is.False, "a cancelled token never spawns");
            int t2 = R.ReserveReinforcement();
            Assert.That(R.Admit(t2), Is.True);
            Assert.That(R.VictoryReady, Is.False);
            R.MarkDead();
            Assert.That(R.VictoryReady, Is.True, "the reinforcement confirmed dead too");

            // A roster body that could not be created ends the encounter (no victory, ever).
            SkirmishLedger04E F = new SkirmishLedger04E(2, 0, 4);
            int f1 = F.ReserveRoster(), f2 = F.ReserveRoster();
            F.Admit(f1);
            F.Admit(f2);
            Assert.That(F.AdmitFailed(f2), Is.True);
            F.MarkDead();
            Assert.That(F.VictoryReady, Is.False);
            Assert.That(SkirmishRules04E.Check(new SkirmishRules04E.WatchFacts { AnyFailedRoster = F.FailedRoster > 0 }), Is.EqualTo(SkirmishEnd04E.SpawnFailed));

            // An empty ledger can never win, and an abort leaves the predicate false.
            Assert.That(new SkirmishLedger04E(0, 0, 4).VictoryReady, Is.False);
            SkirmishLedger04E X = new SkirmishLedger04E(3, 0, 4);
            X.ReserveRoster();
            X.ReserveRoster();
            X.ReserveRoster();
            X.CancelAll();
            Assert.That(X.VictoryReady, Is.False, "an aborted encounter is never a victory");
        }

        [Test]
        public void EncounterWatchRules()
        {
            SkirmishRules04E.WatchFacts f = new SkirmishRules04E.WatchFacts();
            Assert.That(SkirmishRules04E.Check(f), Is.EqualTo(SkirmishEnd04E.None));
            f.AwaySeconds = 999f;
            Assert.That(SkirmishRules04E.Check(f), Is.EqualTo(SkirmishEnd04E.None), "away before engaging keeps the encounter waiting");
            f.Engaged = true;
            f.AwaySeconds = SkirmishRules04E.LeaveSeconds - 0.1f;
            Assert.That(SkirmishRules04E.Check(f), Is.EqualTo(SkirmishEnd04E.None));
            f.AwaySeconds = SkirmishRules04E.LeaveSeconds;
            Assert.That(SkirmishRules04E.Check(f), Is.EqualTo(SkirmishEnd04E.LeftArea));
            Assert.That(SkirmishRules04E.Check(new SkirmishRules04E.WatchFacts { PlayerDown = true, Engaged = true }), Is.EqualTo(SkirmishEnd04E.PlayerDowned));
            Assert.That(SkirmishRules04E.Check(new SkirmishRules04E.WatchFacts { InVeil = true }), Is.EqualTo(SkirmishEnd04E.VeilCrossed));
            foreach (SkirmishEnd04E e in Enum.GetValues(typeof(SkirmishEnd04E)))
                if (e != SkirmishEnd04E.None) Assert.That(string.IsNullOrEmpty(SkirmishRules04E.EndLine(e)), Is.False, e + " has a plain line");

            // Arenas and engagement are layer aware: a street arena never overlaps the room below it.
            Vector3 street = new Vector3(100f, 0.5f, 100f);
            Vector3 below = new Vector3(100f, -26f, 100f);
            Assert.That(SkirmishRules04E.ArenasOverlap(street, 16f, below, 16f), Is.False, "street and Underheart arenas never conflict");
            Assert.That(SkirmishRules04E.ArenasOverlap(below, 16f, below + new Vector3(20f, 0f, 0f), 16f), Is.True, "two rooms on one floor do");
            Assert.That(SkirmishRules04E.PlayerEngaged(street, below, 16f), Is.False, "standing on the street above does not engage the room");
            Assert.That(SkirmishRules04E.PlayerAway(street, below, 16f), Is.True, "the street above counts as away");
            Assert.That(SkirmishRules04E.BeyondLeash(below + new Vector3(16f + SkirmishRules04E.LeashMargin + 1f, 0f, 0f), below, 16f), Is.True);
            Assert.That(SkirmishRules04E.BeyondLeash(below + new Vector3(10f, 0f, 0f), below, 16f), Is.False);

            // Reinforcement kinds come from the declared roster only.
            List<string> roster = new List<string> { "a", "a", "a", "b" };
            for (int i = 0; i < 8; i++) Assert.That(SkirmishRules04E.ReinforcementSpecies(roster, i), Is.EqualTo(i % 2 == 0 ? "a" : "b"));
        }

        // ================================================================== residents never hostile in the refuge

        [Test]
        public void ResidentsAreNeverHostileInTheRefuge()
        {
            int refugeSlots = 0;
            foreach (ResidentSlot04E s in ResidentRules04E.Slots)
            {
                Assert.That(UnderheartIds.HasAnchor(s.Anchor), Is.True, s.Anchor + " is a frozen Underheart anchor");
                Assert.That(UnderheartIds.RegionOfAnchor(s.Anchor), Is.EqualTo(s.Region), s.Anchor + " belongs to its region");
                Assert.That(UnderheartRoles04E.IsResident(s.Species), Is.True, s.Anchor + " places a resident");
                Assert.That(s.Count, Is.InRange(1, ResidentRules04E.MaxPerRegion));
                ResidentPlan04E plan = ResidentRules04E.Plan(s.Species, s.Region, s.Anchor, false);
                Assert.That(plan.NoRewards, Is.True, s.Anchor + " residents never give rewards");
                if (ResidentRules04E.IsRefuge(s.Region, s.Anchor))
                {
                    refugeSlots++;
                    Assert.That(plan.Calm, Is.True, s.Anchor + " residents are calm in the refuge");
                    Assert.That(plan.Invulnerable, Is.True, s.Anchor + " residents cannot be hurt in the refuge");
                    Assert.That(plan.Territorial, Is.False, s.Anchor + " residents never defend in the refuge");
                }
                if (s.Species != EnemyIds04E.KilnCrab) Assert.That(plan.Territorial, Is.False, s.Species + " is never territorial");
                if (ResidentRules04E.IsObservationSite(s.Anchor)) Assert.That(plan.Calm, Is.True, s.Anchor + " study subjects stay calm");
            }
            Assert.That(refugeSlots, Is.GreaterThanOrEqualTo(3), "the refuge has residents");
            Assert.That(ResidentRules04E.Plan(EnemyIds04E.KilnCrab, UnderheartIds.LanternRefuge, "refuge.sample_drop", false).Calm, Is.True, "the refuge crab is calm");
            Assert.That(ResidentRules04E.Plan(EnemyIds04E.KilnCrab, UnderheartIds.EmberCanals, "canals.branch_cache", false).Territorial, Is.True, "a canals crab guards its nest");
            ResidentPlan04E venue = ResidentRules04E.Plan(EnemyIds04E.EmberHatchling, "", ResidentRules04E.VenueNestAnchor, true);
            Assert.That(venue.Calm && venue.Invulnerable && venue.NoRewards, Is.True, "the Pressure House nest is safe and gives nothing");
            Assert.That(UnderheartIds.HasAnchor(ResidentRules04E.GuidedHomeAnchor), Is.True, "the guided clutch has a home anchor");

            // The crab warns, then defends; leaving the ring settles it.
            Assert.That(ResidentRules04E.CrabMood(false, false), Is.EqualTo(CrabMood04E.Settled));
            Assert.That(ResidentRules04E.CrabMood(false, true), Is.EqualTo(CrabMood04E.Warning));
            Assert.That(ResidentRules04E.CrabMood(true, true), Is.EqualTo(CrabMood04E.Defending));
            Assert.That(ResidentRules04E.CrabWarningLeft(0f), Is.EqualTo(ResidentRules04E.CrabPatience).Within(1e-4f));
            Assert.That(ResidentRules04E.CrabWarningLeft(ResidentRules04E.CrabPatience + 3f), Is.EqualTo(0f));
            Assert.That(ResidentRules04E.CrabWarningLeft(2f), Is.LessThan(ResidentRules04E.CrabWarningLeft(1f)), "the warning ring fills over time");

            // Hatchlings follow warmth when the player is near, and sit in the nest when sheltered.
            Vector3 home = new Vector3(0f, -26f, 0f);
            bool follow;
            Vector3 near = home + new Vector3(3f, 0f, 0f);
            ResidentRules04E.HatchlingGoal(home, home, near, true, false, false, 0, out follow);
            Assert.That(follow, Is.True, "a hatchling follows a near player");
            Vector3 g = ResidentRules04E.HatchlingGoal(home, home, near, true, true, false, 0, out follow);
            Assert.That(follow, Is.False, "a sheltered hatchling stays");
            Assert.That(Vector3.Distance(g, home), Is.LessThan(2f), "and sits at its nest");
            ResidentRules04E.HatchlingGoal(home, home, near, false, false, false, 0, out follow);
            Assert.That(follow, Is.False, "nobody in the same space: it stays home");
            ResidentRules04E.HatchlingGoal(home, home, home + new Vector3(30f, 0f, 0f), true, false, false, 0, out follow);
            Assert.That(follow, Is.False, "a far player is not followed");

            // Moths drift close to their crystal.
            for (int i = 0; i < 50; i++)
            {
                Vector3 d = ResidentRules04E.MothDrift(i * 0.37f, 77);
                Assert.That(d.magnitude, Is.LessThan(4f), "moths stay near their warm crystal");
            }
        }

        [Test]
        public void ResidentsLiveInTheCurrentAndAdjacentRegionsOnly()
        {
            List<string> into = new List<string>();
            string[] regions = UnderheartIds.Regions;
            for (int i = 0; i < regions.Length; i++)
            {
                ResidentRules04E.DesiredRegions(regions[i], false, into);
                Assert.That(into[0], Is.EqualTo(regions[i]), "the current region first");
                Assert.That(into.Count, Is.LessThanOrEqualTo(3));
                foreach (string r in into)
                {
                    int j = Array.IndexOf(regions, r);
                    Assert.That(Mathf.Abs(j - i), Is.LessThanOrEqualTo(1), regions[i] + " never prepares " + r);
                    Assert.That(ResidentRules04E.CurrentOrAdjacent(regions[i], r), Is.True);
                }
            }
            ResidentRules04E.DesiredRegions("", false, into);
            Assert.That(into.Count, Is.EqualTo(0), "nothing on the street");
            ResidentRules04E.DesiredRegions("", true, into);
            Assert.That(into, Is.EqualTo(new List<string> { UnderheartIds.ServiceLevel }), "only the service level near the stair entrance");
            Assert.That(ResidentRules04E.CurrentOrAdjacent(UnderheartIds.ServiceLevel, UnderheartIds.FoundryCrown), Is.False);

            for (int preset = 0; preset < 4; preset++)
            {
                int cap = ResidentRules04E.Cap(preset, false);
                Assert.That(cap, Is.InRange(6, 16));
                Assert.That(ResidentRules04E.Cap(preset, true), Is.LessThan(cap), "fewer residents while an encounter runs");
                Assert.That(ResidentRules04E.Cap(preset, true), Is.GreaterThanOrEqualTo(2));
            }
            Dictionary<string, int> perRegion = new Dictionary<string, int>();
            foreach (ResidentSlot04E s in ResidentRules04E.Slots)
            {
                int n;
                perRegion.TryGetValue(s.Region, out n);
                perRegion[s.Region] = n + s.Count;
            }
            foreach (KeyValuePair<string, int> kv in perRegion) Assert.That(kv.Value, Is.LessThanOrEqualTo(ResidentRules04E.MaxPerRegion), kv.Key + " resident total");
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = ResidentRules04E.PlacementOffset("canals.cool_pocket", i, 0);
                Vector3 b = ResidentRules04E.PlacementOffset("canals.cool_pocket", i, 1);
                Assert.That(a.y, Is.EqualTo(0f), "placement offsets stay on the anchor's floor");
                Assert.That((a - b).sqrMagnitude, Is.GreaterThan(0.01f), "a retry tries a different spot");
                Assert.That(MathUtil.Flat(a).magnitude, Is.LessThan(5f), "residents stay near their anchor");
            }
        }

        // ================================================================== placement probes stay on the underground floor

        [Test]
        public void UndergroundProbesStayOnTheAnchorFloor()
        {
            // A column under a city street: the street (0), the Underheart ceiling (-21.5), the room's floor (-26) and a
            // deeper level's floor (-40). A creature placed at -26 must land at -26: never on the street, the ceiling or below.
            List<float> column = new List<float> { 0f, -21.5f, -26f, -40f };
            Vector3 anchor = new Vector3(120f, -26f, 80f);
            Assert.That(UnderheartSpawnRules04E.UndergroundByHeight(anchor), Is.True);
            float[] asks = { 3f, 20f, 80f };
            foreach (float up in asks)
            {
                Vector3 start;
                float dist;
                UnderheartSpawnRules04E.Probe(anchor, up, true, out start, out dist);
                Assert.That(start.y, Is.LessThan(anchor.y + UnderheartSpawnRules04E.MinHeadroom), "the probe starts below any ceiling at least " + UnderheartSpawnRules04E.MinHeadroom + " m up (asked " + up + ")");
                Assert.That(start.y - dist, Is.GreaterThan(-40f), "the probe never reaches the floor below");
                float y;
                Assert.That(UnderheartSpawnRules04E.FirstHit(column, start, dist, out y), Is.True);
                Assert.That(y, Is.EqualTo(-26f), "lands on the anchor's own floor (asked " + up + ")");
            }
            // A point a little above its floor (a goal, a flying spawn) still finds that floor.
            Vector3 hover = anchor + Vector3.up * 1.0f;
            Vector3 s2;
            float d2;
            UnderheartSpawnRules04E.Probe(hover, 20f, true, out s2, out d2);
            float y2;
            Assert.That(UnderheartSpawnRules04E.FirstHit(column, s2, d2, out y2), Is.True);
            Assert.That(y2, Is.EqualTo(-26f));
            // Without the cap the old long probe would have found the street.
            float old;
            UnderheartSpawnRules04E.FirstHit(column, anchor + Vector3.up * 80f, 80f + 60f, out old);
            Assert.That(old, Is.EqualTo(0f), "the old 80 m probe hit the street (the bug this fixes)");
            Assert.That(UnderheartSpawnRules04E.AllowsLongFallback(true), Is.False, "no long fallback underground");
            Assert.That(UnderheartSpawnRules04E.AllowsLongFallback(false), Is.True, "the street keeps its fallback");
            // A pit with no floor within reach: nothing found (the spawn is refused instead of landing elsewhere).
            List<float> pit = new List<float> { 0f, -21.5f, -60f };
            Vector3 s3;
            float d3;
            UnderheartSpawnRules04E.Probe(anchor, 3f, true, out s3, out d3);
            float y3;
            Assert.That(UnderheartSpawnRules04E.FirstHit(pit, s3, d3, out y3), Is.False, "no floor nearby: no placement");

            // Surface probes are unchanged.
            Vector3 surf = new Vector3(5f, 0.2f, 5f);
            Assert.That(UnderheartSpawnRules04E.UndergroundByHeight(surf), Is.False);
            Vector3 s4;
            float d4;
            UnderheartSpawnRules04E.Probe(surf, 3f, false, out s4, out d4);
            Assert.That(s4.y, Is.EqualTo(surf.y + 3f).Within(1e-4f));
            Assert.That(d4, Is.EqualTo(3f + UnderheartSpawnRules04E.SurfaceReach).Within(1e-4f));
        }

        [Test]
        public void StarterSafetyIsLayerAware()
        {
            Assert.That(StarterSafetyRules.OnSurfaceLayer(new Vector3(0f, 0.5f, 0f), false), Is.True, "the street is the surface layer");
            Assert.That(StarterSafetyRules.OnSurfaceLayer(new Vector3(0f, -26f, 0f), false), Is.False, "deep underground is not");
            Assert.That(StarterSafetyRules.OnSurfaceLayer(new Vector3(0f, -2f, 0f), true), Is.False, "an Underheart cell near the surface is not");
            Assert.That(WorldBounds.SameLayer(new Vector3(0f, 0.5f, 0f), new Vector3(0f, -26f, 0f)), Is.False, "the street and the room below are different layers");
        }
    }
}
